using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using BlipFace.View;
using BlipFace.Service.Communication;
using BlipFace.Service.Entities;
using BlipFace.Model;
using System.Timers;
using BlipFace.Helpers;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Threading;
using Timer=System.Timers.Timer;

namespace BlipFace.Presenter
{
    public enum UpdateMode
    {
        Dashboard,
        Secretary,
        Archive
    }

    /// <summary>
    /// Klasa prezentera do naszego głównego widoku, zgodnie z wzorcem MVP
    /// </summary>
    public class StatusesPresenter : IPresenter
    {
        private enum ConnectivityStatus
        {
            Online,
            Offline
        } ;


        private UpdateMode mode = UpdateMode.Dashboard;


        /// <summary>
        /// widok 
        /// </summary>
        private IStatusesView view;

        private readonly UserViewModel blipfaceUser;

        /// <summary>
        /// Klasa do komunikacji z blipem, 
        /// todo: trzeba pomyśleć o innym przechowywaiu hasła
        /// </summary>
        private readonly BlipCommunication blpCom; // = new BlipCommunication("blipface", @"12Faceewq");

        private readonly Timer updateStatusTimer;

        #region const section

        /// <summary>
        /// Limity pobierania statusów
        /// </summary>
        private int statusesLimit = 40;

        /// <summary>
        /// co ile czasu mamy aktualizować 
        /// </summary>
        private int refreshTimeSec = 15;

        /// <summary>
        /// po jakim czasie ma nastąpić timout
        /// </summary>
        private int webGetTimout = 20;

        #endregion

        /// <summary>
        /// Kolejka statusów w której są przechowywane statusy z updateów
        /// </summary>
        private readonly Queue<StatusViewModel> statusUpdateQueue;


        private readonly object lockUpdateQueue = new object();

        /// <summary>
        /// Wątek do konsumpcji statusów z kolejki
        /// </summary>
        private Thread consumeStatusesThread;


        /// <summary>
        /// przechowuje ostatnie pobrane Id statusu
        /// </summary>
        private uint newestStatusId;

        private readonly object lockLastStatus = new object();


        /// <summary>
        /// informuje czy na początku statusy zostały pobrane
        /// </summary>
        private bool statusesLoaded = false;


        private static readonly Regex LinkRegex = new Regex(@"(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}\S*");
        private int currentPage = 0;


        /// <summary>
        /// Konstruktor główny
        /// </summary>
        /// <param name="user">zalogowany użytkownik</param>
        public StatusesPresenter(UserViewModel user)
        {
            try
            {
                refreshTimeSec = (int) Properties.Settings.Default["RefreshTimeSec"];
                statusesLimit = (int) Properties.Settings.Default["StatusesLimit"];
                webGetTimout = (int) Properties.Settings.Default["WebGetTimoutSec"];
            }
            catch (Exception)
            {
                throw;
            }

            //sprawdzamy parametry
            CheckParameters();

            statusUpdateQueue = new Queue<StatusViewModel>(2*statusesLimit);
            

            this.blipfaceUser = user;
            blpCom = new BlipCommunication(blipfaceUser.UserName, blipfaceUser.Password, webGetTimout);


            blpCom.StatusesLoaded += new EventHandler<StatusesLoadingEventArgs>(BlpComStatusesLoaded);

            blpCom.MainStatusLoaded += new EventHandler<MainStatusLoadingEventArgs>(BlpComMainStatusLoaded);

            blpCom.StatusesAdded += new EventHandler<EventArgs>(BlpComStatusesAdded);

            blpCom.StatusesUpdated += new EventHandler<StatusesLoadingEventArgs>(BlpComStatusesUpdated);

            blpCom.ExceptionOccure += new EventHandler<ExceptionEventArgs>(BlpComExceptionOccure);

            blpCom.CommunicationError += new EventHandler<CommunicationErrorEventArgs>(BlpComCommunicationError);


            updateStatusTimer = new Timer(refreshTimeSec*1000); //time in milisconds
            updateStatusTimer.Enabled = false;
            updateStatusTimer.Elapsed += new ElapsedEventHandler(UpdateStatusTimerElapsed);
        }


        /// <summary>
        /// Sprawdza czy są poprawne wszystkie stałe
        /// </summary>
        private void CheckParameters()
        {
            if (!ValidationHelper.ChceckRange(statusesLimit, 1, 50))
            {
                statusesLimit = 40;
            }

            if (!ValidationHelper.ChceckRange(refreshTimeSec, 5, 3600))
            {
                refreshTimeSec = 20;
            }

            if (!ValidationHelper.ChceckRange(webGetTimout, 5, 3600))
            {
                webGetTimout = 20;
            }
        }

        #region IPresenter Members

        public void SetView(IView view)
        {
            if (view is IStatusesView)
            {
                this.view = (IStatusesView) view;
            }
            else
            {
                string message =
                    string.Format(@"Przekazano nieodpowiedni widok, oczekiwano widoku typu {0} a podano {1} ",
                                  typeof (ILoginView), view.GetType().ToString());
                throw new ArgumentException(message);
            }
        }

        public void Init()
        {
            //var empty =new ObservableCollection<StatusViewModel>();
            //empty.Add(new StatusViewModel());
            //view.Statuses = empty;

            LoadUserMainStatus(blipfaceUser.UserName);

            //todo: pobrać listę statusów
            LoadStatuses(blipfaceUser.UserName);

            //włączamy timer, lecz początkowo z wydłużonym czasem
            StartListeningForUpdates(240);
        }

        

        public void Close()
        {
            StopConsuerThread();

            if (updateStatusTimer != null)
            {
                updateStatusTimer.Dispose();
            }

        }

        #endregion

        #region Calbacks

        /// <summary>
        /// Callback do zdarzenie gdzie podczas pobierania, dodawania itp wystąpi wyjątek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlpComExceptionOccure(object sender, ExceptionEventArgs e)
        {
            view.Error = e.Error;

            //gdy licznik jest zatrzymany to go uruchamiamy
            updateStatusTimer.Start();
        }


        /// <summary>
        /// handler Gdy nie mozemy się skomunikować z blipem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlpComCommunicationError(object sender, CommunicationErrorEventArgs e)
        {
            view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Offline);


            if (e.AfterAction == BlipActions.AfterStatusAdd)
            {
                view.ShowInfo("Nie udało się dodać statusu, prawdopodobnie blip jest przeciążony");
            }

            //gdy licznik jest zatrzymany to go uruchamiamy
            updateStatusTimer.Start();
        }


        /// <summary>
        /// Calback do akutalizacji, metoda wywoływana co updateTime
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateStatusTimerElapsed(object sender, ElapsedEventArgs e)
        {
            //gdy upłynie czas to pobierz status główny 
            LoadUserMainStatus(blipfaceUser.UserName);

            LoadOrUpdateDashboard();

            //trzeba tu uruchomić na nowo czasomierz, nie ma sensu w zdarzeniach bo 
            //gdy nic nie ma do pobrania to nie są wywoływane
            //updateStatusTimer.Start();
        }


        /// <summary>
        /// Callback do zdarzenia gdy statusy zostają zaktualizowane
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlpComStatusesUpdated(object sender, StatusesLoadingEventArgs e)
        {
            if (e.Statuses.Count < 1)
                return;
            try
            {
                lock (lockLastStatus)
                {
                    //jeżeli pobrało jakieś statusu i pierwszy(najnowszy) ma id większe 
                    //od dotychczaoswego to przypisz
                    if (newestStatusId == 0 || (e.Statuses[0].Id > newestStatusId))
                    {
                        newestStatusId = e.Statuses[0].Id;
                    }
                    else
                    {
                        //jeżeli nie ma statusów lub najnowszy pobrany status ma id mniejsze
                        //to można przerwać przetwarzanie gdyż one już są
                        return;
                    }
                }

                IList<StatusViewModel> sts = ViewModelHelper.MapToViewStatus(e.Statuses, blipfaceUser.UserName);


                //blokujemy kolejką gdy dodajemy do niej nowe statusy,
                EnqueueStatuses(sts);

                //pobierz z kolejki dodane wyżej statusu i przetworz je
                //AddStatusesWithHyperlinks(true);


                view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Online);
                //view.Error = 
                // view.Statuses.Insert(0, statuses[0]);

                //updateStatusTimer.Start();
            }
            catch (Exception exp)
            {
                view.Error = exp;
            }
        }

        /// <summary>
        /// Wstawia statusy do kolejki, w kolejności odwrotnej gdyż chcemy aby najnowsze były wstawione na końcu
        /// </summary>
        /// <param name="sts"></param>
        private void EnqueueStatuses(IList<StatusViewModel> sts)
        {
            //dodajemy statusy od końca
            for (int i = sts.Count - 1; i >= 0; i--)
            {
                lock (lockUpdateQueue)
                {
                    statusUpdateQueue.Enqueue(sts[i]);
                    Monitor.Pulse(lockUpdateQueue);
                }
            }
        }

        /// <summary>
        /// calback do zdarzenia gdy statusy zostają załadowane od nowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlpComStatusesLoaded(object sender, StatusesLoadingEventArgs e)
        {
            try
            {
                lock (lockUpdateQueue)
                {
                    statusUpdateQueue.Clear();
                }


                if (e.Statuses.Count > 0)
                {
                    lock (lockLastStatus)
                    {
                        //ładujemy statusy od nowa więc, nie ma co sprawdzać
                        //od razu przypisujemy najnowszy status
                        newestStatusId = e.Statuses[0].Id;
                    }
                    //informacja że załadowano statusy
                    statusesLoaded = true;

                    if (consumeStatusesThread != null)
                    {
                        //consumeStatusesThread.IsAlive;

                        //przerywamy wątek
                        consumeStatusesThread.Abort();
                    }

                    IList<StatusViewModel> sts = ViewModelHelper.MapToViewStatus(e.Statuses, blipfaceUser.UserName);


                    //EnqueueStatuses(sts, QueueKind.LoadsQueue);

                    consumeStatusesThread = new Thread(new ParameterizedThreadStart(ConsumeLoads));
                    consumeStatusesThread.Name = "BlipFace consume Loaded Statuses";
                    consumeStatusesThread.IsBackground = true;
                    consumeStatusesThread.Start(sts);

                    //ThreadPool.QueueUserWorkItem(c => ConsumeLoads());

                    /*
                    //ostatni ststus
                    StatusViewModel initStatus = sts[0];
                    sts.RemoveAt(0);

                    //jak najszybciej ustawiamy status na liście, aby użytkownik nie czekał
                    //aż zostaną przetworzone wszystkie statusy
                    var oneStatusList = new ObservableCollection<StatusViewModel>();


                    RetriveStatusHyperlinks(initStatus);
                    oneStatusList.Add(initStatus);


                    //inicjujemy listę statusów tylko jednym statusem, pozostałe będą dodawanie
                    if (view.Statuses != null)
                        view.Statuses = null;
                    view.Statuses = oneStatusList;

                    for (int i = 0; i < sts.Count; i++)
                    {
                        if (view.Statuses != null)
                        {
                            RetriveStatusHyperlinks(sts[i]);

                            view.AddStatus(sts[i], false);
                        }
                        else
                        {
                            //wyjdź z metody

                            statusesLoaded = false;
                            return;
                        }
                    }

                    */

                    //view.Statuses = new List<StatusViewModel>(sts);
                    view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Online);
                }
            }
            catch (Exception exeption)
            {
                view.Error = exeption;
            }


            if (mode != UpdateMode.Archive)
            {
                ThreadPool.QueueUserWorkItem(c => ConsumeStatuses());

                //co by się nie działo to trzeba to uruchomić uruchamiamy wątek z przetważaniem statusów
                //consumeStatusesThread =new Thread(ConsumeStatuses);
                //consumeStatusesThread.Name = "BlipFace consume Statuses";
                //consumeStatusesThread.IsBackground = true;
                //consumeStatusesThread.Start();

                //uruchamiamy z normalnym czasem timer
                StartListeningForUpdates(refreshTimeSec);
            }
        }


        private void RetriveStatusHyperlinks(StatusViewModel status)
        {
            var linkMatches = LinkRegex.Matches(status.Content);

            if (linkMatches.Count > 0)
            {
                for (int i = 0; i < linkMatches.Count; i++)
                {
                    var url = linkMatches[i].Value;

                    int index = url.LastIndexOf("/");
                    int indexOfHash = url.IndexOf("#", index);


                    //code to co mamy na końcu linka albo w rdir.pl/code lub blip.pl/s/code
                    string code;
                    if (indexOfHash > index)
                    {
                        int len = indexOfHash - 1 - index;
                        code = url.Substring(index + 1, len);
                    }
                    else
                    {
                        code = url.Substring(index + 1);
                    }

                    if (url.Contains("blip.pl"))
                    {
                        //gdy mamy do czynienia z blipnięciem cytowaniem
                        //http://blip.pl/s/11552391
                        BlipStatus blpStat = blpCom.GetUpdate(code);
                        if (blpStat != null)
                        {
                            if (status.Cites == null)
                            {
                                status.Cites = new Dictionary<string, string>();
                            }

                            string blipContent = blpStat.User.Login + ": " + blpStat.Content;
                            status.Cites.Add(url, blipContent);
                        }
                    }
                    else if (url.Contains("rdir.pl"))
                    {
                        string originalLink = blpCom.GetShortLink(code);
                        if (!string.IsNullOrEmpty(originalLink))
                        {
                            //url skrócony
                            if (status.Links == null)
                            {
                                status.Links = new Dictionary<string, string>();
                            }
                            status.Links.Add(url, originalLink);
                        }
                    }
                    else
                    {
                        if (status.Links == null)
                        {
                            status.Links = new Dictionary<string, string>();
                        }
                        //url nie skrócony np. do youtube.com
                        status.Links.Add(url, url);
                    }
                }
            }
        }

        /// <summary>
        /// Metoda wykonywana w oddzielnym wątku, pobiera ona statusy(pochodzące z updateów) z kolejki 
        /// i przetwarza je i wstawia do widoku, null zatrzymuje kolejkę
        /// </summary>
        private void ConsumeStatuses()

        {
            while (true)
            {
                try
                {
                    StatusViewModel status = null;

                    lock (lockUpdateQueue)
                    {
                        while (statusUpdateQueue.Count == 0)
                        {
                            Monitor.Wait(lockUpdateQueue, TimeSpan.FromSeconds(refreshTimeSec));
                        }
                        status = statusUpdateQueue.Dequeue();
                    }

                    if (status == null)
                    {
                        break;
                    }

                    if (view.Statuses[0].StatusId < status.StatusId)
                    {
                        RetriveStatusHyperlinks(status);

                        view.AddStatus(status, true);
                    }
                }
                catch (Exception exp)
                {
                    view.Error = exp;
                }
            }
        }


        /// <summary>
        /// Metoda wykonywana w oddzielnym wątku, pobiera ona statusy(pochodzące z ponownego załadowania dashboardu)
        ///  z kolejki i przetwarza je i wstawia do widoku, null zatrzymuje kolejkę
        /// </summary>
        private void ConsumeLoads(object list)
        {
            IList<StatusViewModel> sts = (IList<StatusViewModel>) list;

            //ostatni ststus
            StatusViewModel initStatus = sts[0];
            sts.RemoveAt(0);

            //jak najszybciej ustawiamy status na liście, aby użytkownik nie czekał
            //aż zostaną przetworzone wszystkie statusy
            var oneStatusList = new ObservableCollection<StatusViewModel>();
            RetriveStatusHyperlinks(initStatus);
            oneStatusList.Add(initStatus);
            //inicjujemy listę statusów tylko jednym statusem, pozostałe będą dodawanie
            if (view.Statuses != null)
                view.Statuses = null;
            view.Statuses = oneStatusList;

            for (int i = 0; i < sts.Count; i++)
            {
                RetriveStatusHyperlinks(sts[i]);
                view.AddStatus(sts[i], false);
            }
        }


        /// <summary>
        /// callback do zdarzenia gdy status zostanie dodany
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlpComStatusesAdded(object sender, EventArgs e)
        {
            //tylko powiadomienie że dodał 
            view.TextMessage = string.Empty;
            view.PicturePath = string.Empty;


            LoadUserMainStatus(blipfaceUser.UserName);

            //aktualizujemy kokpit

            LoadOrUpdateDashboard();

            //gdy licznik jest zatrzymany to go uruchamiamy
            // updateStatusTimer.Start();
        }


        /// <summary>
        /// calback do zdarzenia gdy główny status zostanie załadowany od nowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlpComMainStatusLoaded(object sender, MainStatusLoadingEventArgs e)
        {
            StatusViewModel sts = ViewModelHelper.MapToViewStatus(e.MainStatus);

            if (view.MainStatus == null || !view.MainStatus.Content.Equals(sts.Content))
            {
                RetriveStatusHyperlinks(sts);
                view.MainStatus = sts;
            }
            view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Online);
        }

        #endregion

        #region Metody prywatne, pomocnicze

        /// <summary>
        /// W zależności od stanu ładuje cały kokpit lub aktualizuje go
        /// </summary>
        private void LoadOrUpdateDashboard()
        {
            uint ststusId = 0;

            //blokujemy dostęp do ostatniego statusu
            lock (lockLastStatus)
            {
                //zczytujemy ostatnie ID
                ststusId = newestStatusId;
            }

            //jeśli udało się zczytać ostatnie id i już załadowano statusy
            if (ststusId > 0 && statusesLoaded)
            {
                //ualtualinij dashboard
                UpdateStatusesList(blipfaceUser.UserName, ststusId);
            }
            else
            {
                LoadStatuses(blipfaceUser.UserName);
            }
        }

        /// <summary>
        /// tworzy obiekt z informacjami o stanie połaćzenia z blipem
        /// </summary>
        /// <param name="connectivityStatus"></param>
        /// <returns></returns>
        private TitleMessageViewModel SetConnectivityStatus(ConnectivityStatus connectivityStatus)
        {
            switch (connectivityStatus)
            {
                case
                    ConnectivityStatus.Online:
                    return new TitleMessageViewModel()
                               {Title = AppMessages.OnlineTitle, Message = AppMessages.OnlineMessage};
                case ConnectivityStatus.Offline:

                    return new TitleMessageViewModel()
                               {Title = AppMessages.OfflineTitle, Message = AppMessages.OfflineMessage};

                default:
                    return null;
            }
        }


        /// <summary>
        /// Ustawia timer który odpytuje blipa czy pojawiły się nowe statusy
        /// </summary>
        /// <param name="updateInterval"></param>
        private void StartListeningForUpdates(int updateInterval)
        {
            //start timera
            if (updateStatusTimer != null)
            {
                updateStatusTimer.Interval = updateInterval*1000; //time in milisconds
                updateStatusTimer.Enabled = true;
            }
        }


        /// <summary>
        /// Pobiera główny status asynchronicznie, po wysłaniu
        /// status będzie zwrócony jako zgłosznie zdarzenia MainStatusLoaded
        /// </summary>
        private void LoadUserMainStatus(string user)
        {
            //ładuje asynchronicznie główny status,
            //po załadowaniu zgłaszane jest zdarzenie MainStatusLoaded
            blpCom.GetUserMainStatus(user);
        }


        /// <summary>
        /// ładuje cały Dashboard użytkownika
        /// </summary>
        /// <param name="user">nazwa użytkownika którego dashboard ma załadować</param>
        private void LoadStatuses(string user)
        {
            //lock (lockQueue)
            //{
            //    statusQueue.Clear();
            //}

            if (view.Statuses != null)
                view.Statuses = null;

            switch (mode)
            {
                case UpdateMode.Dashboard:
                    blpCom.GetUserDashboard(user, statusesLimit, 0);
                    break;
                case UpdateMode.Secretary:
                    blpCom.GetDirectMessages(user, statusesLimit);
                    break;
                case UpdateMode.Archive:
                    blpCom.GetUserDashboard(user, statusesLimit, currentPage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        /// <summary>
        /// Aktualizacja, pobranie części updateów z dashborda użytkownika
        /// </summary>
        /// <param name="user">nazwa użytkownika</param>
        /// <param name="since">od jakiego id mamy pobrać</param>
        private void UpdateStatusesList(string user, uint since)
        {
            // updateStatusTimer.Stop();

            switch (mode)
            {
                case UpdateMode.Dashboard:
                    blpCom.GetUserDashboardSince(user, since, statusesLimit);
                    break;
                case UpdateMode.Secretary:
                    blpCom.GetDirectMessagesSince(user, since, statusesLimit);
                    break;
                case UpdateMode.Archive:


                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Metody Publiczne

        /// <summary>
        /// Pozwala dodać nowy satus
        /// </summary>
        public void AddStatus(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            //todo: zwrócić uwagę na to co może się dziać w trakcie dodawania statusu
            //szczególnie gdy jest błąd dodawania, a inne updaty (np pobieranie głównego statusu)
            //mogą odblokować panel do wpisywania wiadomości
            //zatrzymujemy licznik czasu


            // updateStatusTimer.Stop();

            blpCom.AddUpdateAsync(content);
        }

        /// <summary>
        /// Pozwala dodać nowy satus wraz z obrazkiem
        /// </summary>
        public void AddStatus(string content, string pictureFileName)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(pictureFileName))
            {
                throw new ArgumentNullException("content,pictureFile", "content lub picture są puste");
            }

            //todo: zwrócić uwagę na to co może się dziać w trakcie dodawania statusu
            //szczególnie gdy jest błąd dodawania, a inne updaty (np pobieranie głównego statusu)
            //mogą odblokować panel do wpisywania wiadomości
            //zatrzymujemy licznik czasu
            //updateStatusTimer.Enabled = false;

            //updateStatusTimer.Stop();
            blpCom.AddUpdateAsync(content, pictureFileName);
        }

        /// <summary>
        /// Tworzy treść wiadomości do cytowania
        /// </summary>
        /// <param name="status"></param>
        /// <param name="text"></param>
        /// <param name="position"></param>
        public int MakeCitation(StatusViewModel status, string text, int position)
        {
            StringBuilder blipLink = new StringBuilder("http://blip.pl", 26);

            if (status.Type == "DirectedMessage")
            {
                blipLink.Append("/dm/");
            }
            else
            {
                blipLink.Append("/s/");
            }
            blipLink.Append(status.StatusId);

            view.TextMessage = text.Insert(position, blipLink.ToString());

            return position + blipLink.ToString().Length;
        }


        /// <summary>
        /// Konstruje format wiadomości skierowanej
        /// </summary>
        /// <param name="status">cały satus na którego użytkownik chce odpowiedzieć</param>
        /// <param name="messageText">dotychczasowa treść wiadomości</param>
        public void MakeDirectMessage(StatusViewModel status, string messageText)
        {
            //format wiadomości dla zwykłej odpowiedzia
            string userFormat = string.Format(">{0}:", status.UserLogin);


            //regex wyszukujące czy wiadomość nie rozpoczyna się jak prywatana
            Regex regexPrivateMessage = new Regex(@"^>>.*?:");

            //regex wyszukujące czy wiadomośc nie rozpoczyna się jak skierowana
            Regex regexDirectMessage = new Regex(@"^>.*:?");

            string blipMessage;
            if (regexPrivateMessage.IsMatch(messageText))
            {
                //jesli rozpoczyna się jak prywatna to zamień na kierowaną
                blipMessage = regexPrivateMessage.Replace(messageText, userFormat);
            }
            else if (regexDirectMessage.IsMatch(messageText))
            {
                //jeśli ropoczyna się jak kierowana to zamień z powrotem na kierowaną
                //może się wydawać bez sensu, lecz przydaje się gdy bedziemy chcieli 
                //wysłać do innej osoby niż jest już ustawione
                blipMessage = regexDirectMessage.Replace(messageText, userFormat);
            }
            else
            {
                //jeżeli nie jest do nikogo to wstaw na początek
                blipMessage = messageText.Insert(0, userFormat);
            }

            //string blipMessage = Regex.Replace(messageText, @"^>.*:", userFormat, RegexOptions.IgnoreCase);

            view.TextMessage = blipMessage;
            //string userFormat = string.Format(">{0}: ", status.UserLogin);
            //string blipMessage = Regex.Replace(messageText, @"^>>.*:", userFormat, RegexOptions.IgnoreCase);

            //view.TextMessage = blipMessage;
        }

        /// <summary>
        /// Konstruje format wiadomość prywatnej
        /// </summary>
        /// <param name="status">status na któego użytkownik chce odpowiedzieć prywatnie</param>
        /// <param name="messageText">dotychczasowa treść wiadomości</param>
        public void MakePrivateMessage(StatusViewModel status, string messageText)
        {
            string userFormat = string.Format(">>{0}:", status.UserLogin);

            //uwaga to wyrażenie łapie dwa typy tekstu
            // z jednym znakiem >
            //oraz z dwoma znakami >>
            //dlatego dobrze działa i zamienia gdy drugi raz klikniemy wiadomość prywatna
            //a dotychczasowa wiadomość jest już prywatna
            Regex regex = new Regex(@"^>.*?:");

            string blipMessage;
            if (regex.IsMatch(messageText))
            {
                blipMessage = regex.Replace(messageText, userFormat);
            }
            else
            {
                blipMessage = messageText.Insert(0, userFormat);
            }

            //string blipMessage = Regex.Replace(messageText, @"^>.*:", userFormat, RegexOptions.IgnoreCase);

            view.TextMessage = blipMessage;
        }

        #endregion

        public void SetMode(UpdateMode updateMode)
        {
            mode = updateMode;

            //todo: załadować od nowa na podstawie typu

            if (updateStatusTimer != null)
                updateStatusTimer.Stop();

            StopConsuerThread();

            statusesLoaded = false;

            LoadStatuses(blipfaceUser.UserName);
        }

        public void ShowArchiv(int page)
        {
            currentPage = page*statusesLimit;
            LoadStatuses(blipfaceUser.UserName);
        }


        /// <summary>
        /// zatrzymuje wątke obsługi kolejki, wstawiają null do kolejki
        /// </summary>
        private void StopConsuerThread()
        {
            lock (lockUpdateQueue)
            {
                statusUpdateQueue.Clear();
                statusUpdateQueue.Enqueue(null);
                Monitor.Pulse(lockUpdateQueue);
            }
        }
    }
}