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

        /// <summary>
        /// Limity pobierania statusów
        /// </summary>
        private const int Limit = 40;

        private readonly Queue<StatusViewModel> statusQueue = new Queue<StatusViewModel>(Limit + 1);

        private readonly object lockLastStatus = new object();


        /// <summary>
        /// Wątek do konsumpcji statusów z kolejki
        /// </summary>
        private Thread consumeStatusesThread;

        /// <summary>
        /// przechowuje ostatni pobrany status
        /// </summary>
        private StatusViewModel newestStatus;

        /// <summary>
        /// informuje czy na początku statusy zostały pobrane
        /// </summary>
        private bool statusesLoaded = false;

        /// <summary>
        /// co ile czasu mamy aktualizować 
        /// </summary>
        private const int UpdateTime = 15;

        private static readonly Regex linkRegex = new Regex(@"(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}\S*");
        private readonly object lockQueue = new object();


        private readonly AutoResetEvent queueResetEvent = new AutoResetEvent(false);

        /// <summary>
        /// Konstruktor główny
        /// </summary>
        /// <param name="user">zalogowany użytkownik</param>
        public StatusesPresenter(UserViewModel user)
        {
            this.blipfaceUser = user;
            blpCom = new BlipCommunication(blipfaceUser.UserName, blipfaceUser.Password);

            blpCom.StatusesLoaded += new EventHandler<StatusesLoadingEventArgs>(BlpComStatusesLoaded);

            blpCom.MainStatusLoaded += new EventHandler<MainStatusLoadingEventArgs>(BlpComMainStatusLoaded);

            blpCom.StatusesAdded += new EventHandler<EventArgs>(BlpComStatusesAdded);

            blpCom.StatusesUpdated += new EventHandler<StatusesLoadingEventArgs>(BlpComStatusesUpdated);

            blpCom.ExceptionOccure += new EventHandler<ExceptionEventArgs>(BlpComExceptionOccure);

            blpCom.CommunicationError += new EventHandler<CommunicationErrorEventArgs>(BlpComCommunicationError);


            updateStatusTimer = new Timer(UpdateTime*1000); //time in milisconds
            updateStatusTimer.Elapsed += new ElapsedEventHandler(UpdateStatusTimerElapsed);
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
            LoadUserDashboard(blipfaceUser.UserName);

            //włączamy timer, lecz początkowo z wydłużonym czasem
            StartListeningForUpdates(3*UpdateTime);
        }

        public event EventHandler<ActionsEventArgs> WorkDone;

        public void Close()
        {
            if (consumeStatusesThread != null)
                consumeStatusesThread.Abort();

            if (updateStatusTimer != null)
                updateStatusTimer.Stop();


            //if(WorkDone!=null)
            //{
            //    WorkDone(this,new ActionsEventArgs(Actions.Close));
            //}
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

            //gdy licznik jest zatrzymany to go uruchamiamy
            updateStatusTimer.Start();
        }


        /// <summary>
        /// Calback do akutalizacji, metoda wywoływana co UpdateTime
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
            updateStatusTimer.Start();
        }


        /// <summary>
        /// Callback do zdarzenia gdy statusy zostają zaktualizowane
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlpComStatusesUpdated(object sender, StatusesLoadingEventArgs e)
        {
            try
            {
                IList<StatusViewModel> sts = ViewModelHelper.MapToViewStatus(e.Statuses, blipfaceUser.UserName);

                lock (lockLastStatus)
                {
                    if (sts.Count < 1)
                        return;

                    //jeżeli pobrało jakieś statusu i pierwszy(najnowszy) ma id większe 
                    //od dotychczaoswego to przypisz
                    if (newestStatus == null || (sts[0].StatusId > newestStatus.StatusId))
                    {
                        newestStatus = sts[0];
                    }
                    else
                    {
                        //jeżeli nie ma statusów lub najnowszy pobrany status ma id mniejsze
                        //to można przerwać przetwarzanie gdyż one już są
                        return;
                    }
                }


                //blokujemy kolejką gdy dodajemy do niej nowe statusy,
                EnqueueStatuses(sts);

                //pobierz z kolejki dodane wyżej statusu i przetworz je
                //AddStatusesWithHyperlinks(true);


                view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Online);
                // view.Statuses.Insert(0, statuses[0]);

                updateStatusTimer.Start();
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
            lock (lockQueue)
            {
                //dodajemy statusy od końca
                for (int i = sts.Count - 1; i >= 0; i--)
                {
                    statusQueue.Enqueue(sts[i]);
                }
            }

            //dajemy sygnał że można
            queueResetEvent.Set();
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
                IList<StatusViewModel> sts = ViewModelHelper.MapToViewStatus(e.Statuses, blipfaceUser.UserName);
                lock (lockLastStatus)
                {
                    //jeżeli pobrało jakieś statusu i pierwszy(najnowszy) ma id większe 
                    //od dotychczaoswego to przypisz
                    if (sts.Count > 0)
                    {
                        if (newestStatus == null || (sts[0].StatusId > newestStatus.StatusId))
                            newestStatus = sts[0];

                        //informacja że załadowano statusy
                        statusesLoaded = true;
                    }
                    else
                    {
                        //jeżeli nie ma statusów lub najnowszy pobrany status ma id mniejsze
                        //to można przerwać przetwarzanie gdyż one już są
                        return;
                    }
                }
                //ostatni ststus
                StatusViewModel initStatus = sts[0];
                sts.RemoveAt(0);


                //blokujemy kolejką gdy dodajemy do niej nowe statusy,
                //EnqueueStatuses(sts);


                //jak najszybciej ustawiamy status na liście, aby użytkownik nie czekał
                //aż zostaną przetworzone wszystkie statusy
                var oneStatusList = new ObservableCollection<StatusViewModel>();
                RetriveStatusHyperlinks(initStatus);
                oneStatusList.Add(initStatus);

                //inicjujemy listę statusów tylko jednym statusem, pozostałe będą dodawanie
                //kolejno
                view.Statuses = oneStatusList;

                for (int i = 0; i < sts.Count; i++)
                {
                    RetriveStatusHyperlinks(sts[i]);
                    view.AddStatus(sts[i], false);
                }


                //view.Statuses = new List<StatusViewModel>(sts);
                view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Online);

                //uruchamiamy wątek z przetważaniem statusów

                consumeStatusesThread = new Thread(ConsumeStatuses);
                consumeStatusesThread.IsBackground = true;
                consumeStatusesThread.Start();

                //uruchamiamy z normalnym czasem timer
                StartListeningForUpdates(UpdateTime);
            }
            catch (Exception exeption)
            {
                view.Error = exeption;
            }
        }


/*
        private void AddStatusesWithHyperlinks(bool insertAtBeginning)
        {
            int queueSize = 0;
            lock (lockQueue)
            {
                queueSize = statusQueue.Count;


                while (queueSize > 0)
                {
                    StatusViewModel status;


                    status = statusQueue.Dequeue();

                    if (status == null)
                    {
                        continue;
                    }

                    RetriveStatusHyperlinks(status);
                    view.AddStatus(status, insertAtBeginning);

                    //lock (lockQueue)
                    //{
                    //    queueSize = statusQueue.Count;
                    //}
                    queueSize = statusQueue.Count;
                }
            }

            //foreach (var status in sts)
            //{
            //    RetriveStatusHyperlinks(status);
            //    view.AddStatus(status);
            //}
        }
*/

        private void RetriveStatusHyperlinks(StatusViewModel status)
        {
            var linkMatches = linkRegex.Matches(status.Content);

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


        private void ConsumeStatuses()
        {
            StatusViewModel status = null;

            while (true)
            {

                queueResetEvent.WaitOne();
                
                    
                
                try
                {
                    while (statusQueue.Count > 0)
                    {
                        lock (lockQueue)
                        {
                            status = statusQueue.Dequeue();

                            //if (statusQueue.Count > 0)
                            //{
                            //    status = statusQueue.Dequeue();
                            //}
                        }

                        //if (status == null)
                        //{
                        //    //czekamy połowę czasu co jaki aktualizujemy
                        //    // Thread.Sleep(UpdateTime*1000/2);
                        //    continue;
                        //}

                        RetriveStatusHyperlinks(status);

                        
                        view.AddStatus(status, true);
                        status = null;
                    }
                }
                catch (Exception exp)
                {
                    view.Error = exp;
                }
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
            updateStatusTimer.Start();
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
            int ststusId = 0;

            //blokujemy dostęp do ostatniego statusu
            lock (lockLastStatus)
            {
                if (newestStatus != null)
                {
                    //zczytujemy ostatnie ID
                    ststusId = newestStatus.StatusId;
                }
            }

            //jeśli udało się zczytać ostatnie id
            if (ststusId > 0)
            {
                //ualtualinij dashboard
                UpdateUserDashboard(blipfaceUser.UserName, ststusId);
            }
            else if (!statusesLoaded)
            {
                LoadUserDashboard(blipfaceUser.UserName);
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
                case ConnectivityStatus.Online:
                    return new TitleMessageViewModel()
                               {
                                   Title = AppMessages.OnlineTitle,
                                   Message = AppMessages.OnlineMessage
                               };
                case ConnectivityStatus.Offline:

                    return new TitleMessageViewModel()
                               {
                                   Title = AppMessages.OfflineTitle,
                                   Message = AppMessages.OfflineMessage
                               };

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
            updateStatusTimer.Interval = updateInterval*1000; //time in milisconds
            updateStatusTimer.Enabled = true;
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
        private void LoadUserDashboard(string user)
        {
            blpCom.GetUserDashboard(user, Limit);
        }

        /// <summary>
        /// Aktualizacja, pobranie części updateów z dashborda użytkownika
        /// </summary>
        /// <param name="user">nazwa użytkownika</param>
        /// <param name="since">od jakiego id mamy pobrać</param>
        private void UpdateUserDashboard(string user, int since)
        {
            // updateStatusTimer.Stop();

            blpCom.GetUserDashboardSince(user, since);
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
    }
}