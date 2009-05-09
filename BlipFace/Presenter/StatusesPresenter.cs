using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlipFace.View;
using BlipFace.Service.Communication;
using BlipFace.Service.Entities;
using BlipFace.Model;
using System.Timers;
using BlipFace.Helpers;
using System.Text.RegularExpressions;

namespace BlipFace.Presenter
{

    /// <summary>
    /// Klasa prezentera do naszego głównego widoku, zgodnie z wzorcem MVP
    /// </summary>
    public class StatusesPresenter : IPresenter
    {

        //zmienna wskazuje czy jesetśmy online
        private bool isOnLine = false;

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
        private const string ConnectivityStatusOnline="Online";
        private const string ConnectivityStatusOffline = "Offline";


        /// <summary>
        /// co ile czasu mamy aktualizować 
        /// </summary>
        private const int UpdateTime=45;

        /// <summary>
        /// Limity pobierania statusów
        /// </summary>
        private const int Limit = 30;

        /// <summary>
        /// Konstruktor główny
        /// </summary>
        /// <param name="user">zalogowany użytkownik</param>
        public StatusesPresenter(UserViewModel user)
        {
            this.blipfaceUser = user;
            blpCom = new BlipCommunication(blipfaceUser.UserName,blipfaceUser.Password);

            blpCom.StatusesLoaded += new EventHandler<StatusesLoadingEventArgs>(BlpComStatusesLoaded);

            blpCom.MainStatusLoaded += new EventHandler<MainStatusLoadingEventArgs>(BlpComMainStatusLoaded);

            blpCom.StatusesAdded += new EventHandler<EventArgs>(BlpComStatusesAdded);

            blpCom.StatusesUpdated += new EventHandler<StatusesLoadingEventArgs>(BlpComStatusesUpdated);

            blpCom.ExceptionOccure += new EventHandler<ExceptionEventArgs>(BlpComExceptionOccure);

            blpCom.CommunicationError += new EventHandler<CommunicationErrorEventArgs>(BlpComCommunicationError);

            //domyślnie aktualizacje co 30 sekund
            updateStatusTimer = new Timer(UpdateTime * 1000);
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
            LoadUserMainStatus(blipfaceUser.UserName);

            //todo: pobrać listę statusów
            LoadUserDashboard(blipfaceUser.UserName);

            StartListeningForUpdates(UpdateTime);
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
            //int lastIndex = lstbStatusList.Items.Count;
            //todo: uwaga gdyż może być wyrzucony wyjątek NullReference Exception, gdy za wczesnie tu wejdzie

            if (view.Statuses != null)
            {
                StatusViewModel lastStatus = view.Statuses[0];

                if (lastStatus != null)
                {
                    //todo: zamiast pobierać za każdym razem ostatni status można by najpierw sprawdzić czy się zmienił
                    LoadUserMainStatus(blipfaceUser.UserName);
                    UpdateUserDashboard(blipfaceUser.UserName, lastStatus.StatusId);
                }
            }
        }


        /// <summary>
        /// Callback do zdarzenie gdzie podczas pobierania, dodawania itp wystąpi wyjątek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComExceptionOccure(object sender, ExceptionEventArgs e)
        {
            

            view.Error = e.Error;
        }


        /// <summary>
        /// handler Gdy nie mozemy się skomunikować z blipem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComCommunicationError(object sender, CommunicationErrorEventArgs e)
        {
            view.ConnectivityStatus = ConnectivityStatusOffline;
        }

        /// <summary>
        /// Callback do zdarzenia gdy statusy zostają zaktualizowane
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComStatusesUpdated(object sender, StatusesLoadingEventArgs e)
        {

            IList<StatusViewModel> statuses = ViewModelHelper.MapToViewStatus(e.Statuses,blipfaceUser.UserName);

            view.Statuses = statuses.Concat(view.Statuses).ToList();

            view.ConnectivityStatus = ConnectivityStatusOnline;
            // view.Statuses.Insert(0, statuses[0]);
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

            //int lastIndex = lstbStatusList.Items.Count;
            if (view.Statuses != null)
            {
                StatusViewModel lastStatus = view.Statuses[0] as StatusViewModel;

                if (lastStatus != null)
                {
                    LoadUserMainStatus(blipfaceUser.UserName);
                    UpdateUserDashboard(blipfaceUser.UserName, lastStatus.StatusId);
                }
            }
        }


        /// <summary>
        /// calback do zdarzenia gdy statusy zostają załadowane od nowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComStatusesLoaded(object sender, StatusesLoadingEventArgs e)
        {
            
            view.Statuses = ViewModelHelper.MapToViewStatus(e.Statuses, blipfaceUser.UserName);

            view.ConnectivityStatus = ConnectivityStatusOnline;
        }

        /// <summary>
        /// calback do zdarzenia gdy główny status zostanie załadowany od nowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BlpComMainStatusLoaded(object sender, MainStatusLoadingEventArgs e)
        {
            view.MainStatus = ViewModelHelper.MapToViewStatus(e.MainStatus);

            view.ConnectivityStatus = ConnectivityStatusOnline;
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
            blpCom.GetUserDashboardSince(user, since);

        }
        #endregion
       
        #region Metody Publiczne

        /// <summary>
        /// Pozwala dodać nowy satus
        /// </summary>
        public void AddStatus(string content)
        {

            //todo: zwrócić uwagę na to co może się dziać w trakcie dodawania statusu
            //szczególnie gdy jest błąd dodawania, a inne updaty (np pobieranie głównego statusu)
            //mogą odblokować panel do wpisywania wiadomości
            //zatrzymujemy licznik czasu
            //updateStatusTimer.Enabled = false;

            blpCom.AddUpdateAsync(content);
        }
       
        /// <summary>
        /// Tworzy treść wiadomości do cytowania
        /// </summary>
        /// <param name="status"></param>
        /// <param name="text"></param>
        /// <param name="position"></param>
        public void MakeCitation(StatusViewModel status, string text, int position)
        {
            
            StringBuilder blipLink = new StringBuilder("http://blip.pl",26);

            if(status.Type == "DirectedMessage")
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
            if(regex.IsMatch(messageText))
            {
               blipMessage= regex.Replace(messageText, userFormat);
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
