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

namespace BlipFace.Presenter
{

    /// <summary>
    /// Klasa prezentera do naszego głównego widoku, zgodnie z wzorcem MVP
    /// </summary>
    public class StatusesPresenter : IPresenter
    {

        enum ConnectivityStatus { Online, Offline };


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
       // private const string ConnectivityStatusOnline = "Online";
        //private const string ConnectivityStatusOffline = "Offline";
        
        /// <summary>
        /// Limity pobierania statusów
        /// </summary>
        private const int Limit = 30;
        private Queue<StatusViewModel> statusQueue = new Queue<StatusViewModel> (Limit+1); 
        
        

        /// <summary>
        /// co ile czasu mamy aktualizować 
        /// </summary>
        private const int UpdateTime = 45;

        private static Regex linkRegex = new Regex(@"(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}\S*");
        private readonly object lockQueue = new object();

       

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

            
            updateStatusTimer = new Timer(UpdateTime * 1000);//time in milisconds
            updateStatusTimer.Elapsed += new ElapsedEventHandler(UpdateStatusTimerElapsed);
        }



        #region IPresenter Members

        public void SetView(IView view)
        {
            if (view is IStatusesView)
            {


                this.view = (IStatusesView)view;
            }
            else
            {
                string message =
                    string.Format(@"Przekazano nieodpowiedni widok, oczekiwano widoku typu {0} a podano {1} ", typeof(ILoginView), view.GetType().ToString());
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

        #endregion

        #region Calbacks

        /// <summary>
        /// Calback do akutalizacji, metoda wywoływana co UpdateTime
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UpdateStatusTimerElapsed(object sender, ElapsedEventArgs e)
        {
            
            LoadUserMainStatus(blipfaceUser.UserName);

            int queueSize = 0;
            lock (lockQueue)
            {
                queueSize = statusQueue.Count;
            }

            if (queueSize < 1)
            {
                StatusViewModel lastStatus = GetLastStatus();


                if (lastStatus != null)
                {

                    UpdateUserDashboard(blipfaceUser.UserName, lastStatus.StatusId);
                }
                else
                {
                    //z powyższych nie udało się pobrać ostatniego statusu,
                    //więc pobieramy cały dashborad od nowa
                    LoadUserDashboard(blipfaceUser.UserName);
                }
            }

            //else
            //{
            //    //pobieramy cały dashborad od nowa
            //    LoadUserDashboard(blipfaceUser.UserName);

            //}

            //trzeba tu uruchomić na nowo czasomierz, nie ma sensu w zdarzeniach bo 
            //gdy nic nie ma do pobrania to nie są wywoływane
            updateStatusTimer.Start();

        }

        private StatusViewModel GetLastStatus()
        {
            StatusViewModel lastStatus = null;
            lock (lockQueue)
            {
                if (statusQueue.Count > 0)
                {
                    //jeśli jest coś w kolejce to pobierz ostatni z kolejki
                    lastStatus = statusQueue.Last();
                }
            }
            
            if (lastStatus==null && view.Statuses != null)
            {
                //jeśli kolejka statusuów jest pusta to pobierz najnowszy z listy
                lastStatus = view.Statuses[0];
            }
            return lastStatus;
        }


        /// <summary>
        /// Callback do zdarzenie gdzie podczas pobierania, dodawania itp wystąpi wyjątek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComExceptionOccure(object sender, ExceptionEventArgs e)
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
        void BlpComCommunicationError(object sender, CommunicationErrorEventArgs e)
        {

            view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Offline);

            //gdy licznik jest zatrzymany to go uruchamiamy
            updateStatusTimer.Start();
        }

        /// <summary>
        /// Callback do zdarzenia gdy statusy zostają zaktualizowane
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComStatusesUpdated(object sender, StatusesLoadingEventArgs e)
        {

            IList<StatusViewModel> sts = ViewModelHelper.MapToViewStatus(e.Statuses, blipfaceUser.UserName);

            //RetriveBlipHyperlinks(sts);
            //blokujemy kolejką gdy dodajemy do niej nowe statusy,
            lock (lockQueue)
            {
                //dodajemy statusy od końca
                for (int i = sts.Count - 1; i >= 0; i--)
                {
                    statusQueue.Enqueue(sts[i]);
                }

                //foreach (var st in sts)
                //{
                //    statusQueue.Enqueue(st);
                //}
            }

            //pobierz z kolejki dodane wyżej statusu i przetworz je
            AddStatusesWithHyperlinks(true);

            //foreach (var st in sts)
            //{
            //    RetriveStatusHyperlinks(st);
            //}
            //view.UpdateStatuses(sts);

            //view.Statuses = statuses.Concat(view.Statuses).ToList();

            view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Online);
            // view.Statuses.Insert(0, statuses[0]);

            updateStatusTimer.Start();
        }

        /// <summary>
        /// calback do zdarzenia gdy statusy zostają załadowane od nowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComStatusesLoaded(object sender, StatusesLoadingEventArgs e)
        {

            //view.Statuses = 

            IList<StatusViewModel> sts = ViewModelHelper.MapToViewStatus(e.Statuses, blipfaceUser.UserName);

            
            lock (lockQueue)
            {
                //tu nie musimy odwracać bo i tak dodajemy na koniec statusy
                //dodajemy do kolejki od razu, lecz od drugiego statusu dopiero, pierwszy o indeksie 0
                //w celu złudzenia przyspieszenia dodajemy od razu

                for (int i = 1; i < sts.Count; i++)
                {
                    statusQueue.Enqueue(sts[i]);
                }


                //foreach (var st in sts)
                //{
                //    statusQueue.Enqueue(st);
                //}
            }
            //jak najszybciej ustawiamy pierwszy status na liście, aby użytkownik nie czekał
            //aż zostaną przetworzone wszystkie statusy
            var oneStatusList = new ObservableCollection<StatusViewModel>();
            StatusViewModel initStatus = sts[0];
            RetriveStatusHyperlinks(initStatus);
            oneStatusList.Add(initStatus);

            //inicjujemy listę statusów tylko jednym statusem, pozostałe będą dodawanie
            //kolejno
            view.Statuses = oneStatusList;

            //chyba to już nie potrzebne będzie
            //sts.RemoveAt(0);

            

            //parametr false - dodajemy na koniec statusy
            AddStatusesWithHyperlinks(false);


            //view.Statuses = new List<StatusViewModel>(sts);
            view.ConnectivityStatus = SetConnectivityStatus(ConnectivityStatus.Online);

            
            //uruchamiamy z normalnym czasem timer
            StartListeningForUpdates(UpdateTime);
            
        }



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


        //tworzy obiekt z informacjami o stanie połaćzenia z blipem
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
        /// callback do zdarzenia gdy status zostanie dodany
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComStatusesAdded(object sender, EventArgs e)
        {
            //tylko powiadomienie że dodał 
            view.TextMessage = string.Empty;
            view.PicturePath = string.Empty;
            //int lastIndex = lstbStatusList.Items.Count;
            //if (view.Statuses != null)
            //{
            //    StatusViewModel lastStatus = view.Statuses[0] as StatusViewModel;

            //    if (lastStatus != null)
            //    {
            //        LoadUserMainStatus(blipfaceUser.UserName);
            //        UpdateUserDashboard(blipfaceUser.UserName, lastStatus.StatusId);
            //    }
            //}

            StatusViewModel lastStatus = GetLastStatus();


            if (lastStatus != null)
            {

                UpdateUserDashboard(blipfaceUser.UserName, lastStatus.StatusId);
            }
            else
            {
                //z powyższych nie udało się pobrać ostatniego statusu,
                //więc pobieramy cały dashborad od nowa
                LoadUserDashboard(blipfaceUser.UserName);
            }



            //gdy licznik jest zatrzymany to go uruchamiamy
            updateStatusTimer.Start();
        }



        /// <summary>
        /// calback do zdarzenia gdy główny status zostanie załadowany od nowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComMainStatusLoaded(object sender, MainStatusLoadingEventArgs e)
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
        /// Ustawia timer który odpytuje blipa czy pojawiły się nowe statusy
        /// </summary>
        /// <param name="updateInterval"></param>
        private void StartListeningForUpdates(int updateInterval)
        {
            //start timera
            updateStatusTimer.Interval = updateInterval * 1000; //time in milisconds
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
            updateStatusTimer.Stop();

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


            updateStatusTimer.Stop();

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

            updateStatusTimer.Stop();
            blpCom.AddUpdateAsync(content, pictureFileName);
        }

        /// <summary>
        /// Tworzy treść wiadomości do cytowania
        /// </summary>
        /// <param name="status"></param>
        /// <param name="text"></param>
        /// <param name="position"></param>
        public void MakeCitation(StatusViewModel status, string text, int position)
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
