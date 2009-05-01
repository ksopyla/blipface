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

namespace BlipFace.Presenter
{

    /// <summary>
    /// Klasa prezentera do naszego głównego widoku, zgodnie z wzorcem MVP
    /// </summary>
    public class StatusesPresenter : IPresenter
    {

        /// <summary>
        /// widok 
        /// </summary>
        private IStatusesView view;

        private UserViewModel user;

        /// <summary>
        /// Klasa do komunikacji z blipem, 
        /// todo: trzeba pomyśleć o innym przechowywaiu hasła
        /// </summary>
        private BlipCommunication blpCom; // = new BlipCommunication("blipface", @"12Faceewq");

        private Timer updateStatusTimer;

        /// <summary>
        /// Konstruktor główny
        /// </summary>
        /// <param name="_view">wikok</param>
        public StatusesPresenter(UserViewModel _user)
        {
            this.user = _user;
            blpCom = new BlipCommunication(user.UserName,user.Password);

            blpCom.StatusesLoaded += new EventHandler<StatusesLoadingEventArgs>(blpCom_StatusesLoaded);

            blpCom.MainStatusLoaded += new EventHandler<MainStatusLoadingEventArgs>(blpCom_MainStatusLoaded);

            blpCom.StatusesAdded += new EventHandler<EventArgs>(blpCom_StatusesAdded);

            blpCom.StatusesUpdated += new EventHandler<StatusesLoadingEventArgs>(blpCom_StatusesUpdated);

            blpCom.ExceptionOccure += new EventHandler<ExceptionEventArgs>(blpCom_ExceptionOccure);

            //domyślnie aktualizacje co 30 sekund
            updateStatusTimer = new Timer(30 * 1000);
            updateStatusTimer.Elapsed += new ElapsedEventHandler(updateStatusTimer_Elapsed);



        }



        #region Calbacks
        void updateStatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //int lastIndex = lstbStatusList.Items.Count;
            //todo: uwaga gdyż może być wyrzucony wyjątek NullReference Exception, gdy za wczesnie tu wejdzie

            if (view.Statuses != null)
            {
                StatusViewModel lastStatus = view.Statuses[0] as StatusViewModel;

                if (lastStatus != null)
                {
                    //todo: zamiast pobierać za każdym razem ostatni status można by najpierw sprawdzić czy się zmienił
                    LoadUserMainStatus(user.UserName);
                    UpdateUserDashboard(user.UserName, lastStatus.StatusId);
                }
            }
        }


        /// <summary>
        /// Callback do zdarzenie gdzie podczas pobierania, dodawania itp wystąpi wyjątek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void blpCom_ExceptionOccure(object sender, ExceptionEventArgs e)
        {
            view.Error = e.Error;
        }

        /// <summary>
        /// Callback do zdarzenia gdy statusy zostają zaktualizowane
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void blpCom_StatusesUpdated(object sender, StatusesLoadingEventArgs e)
        {

            IList<StatusViewModel> statuses = ViewModelHelper.MapToViewStatus(e.Statuses);

            view.Statuses = statuses.Concat(view.Statuses).ToList();
            // view.Statuses.Insert(0, statuses[0]);
        }

        /// <summary>
        /// callback do zdarzenia gdy status zostanie dodany
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void blpCom_StatusesAdded(object sender, EventArgs e)
        {
            //tylko powiadomienie że dodał 
            view.TextMessage = string.Empty;

            //int lastIndex = lstbStatusList.Items.Count;
            if (view.Statuses != null)
            {
                StatusViewModel lastStatus = view.Statuses[0] as StatusViewModel;

                if (lastStatus != null)
                {
                    LoadUserMainStatus(user.UserName);
                    UpdateUserDashboard(user.UserName, lastStatus.StatusId);
                }
            }
        }


        /// <summary>
        /// calback do zdarzenia gdy statusy zostają załadowane od nowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void blpCom_StatusesLoaded(object sender, StatusesLoadingEventArgs e)
        {
            view.Statuses = ViewModelHelper.MapToViewStatus(e.Statuses);
        }

        /// <summary>
        /// calback do zdarzenia gdy główny status zostanie załadowany od nowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void blpCom_MainStatusLoaded(object sender, MainStatusLoadingEventArgs e)
        {
            view.MainStatus = ViewModelHelper.MapToViewStatus(e.MainStatus);
        }


        #endregion
       


        public void StartListeningForUpdates(int updateInterval)
        {
            //start timera
            updateStatusTimer.Interval = updateInterval * 1000; //time in milisconds
            updateStatusTimer.Enabled = true;
        }

        /// <summary>
        /// Pozwala dodać nowy satus
        /// </summary>
        public void AddStatus(string content)
        {
            blpCom.AddUpdateAsync(content);
        }


        /// <summary>
        /// Pobiera listę statusów asynchronicznie, po wysłaniu zdarzenie
        /// statusy będą zwrócone jako zgłosznie zdarzenia StatusesLoaded
        /// </summary>
        public void LoadStatuses()
        {
            //return blpCom.GetUpdates(30);

            //ładuje asynchronicznie listę statusów,
            //po załadowaniu zgłaszane jest zdarzenie StatusesLoaded
            blpCom.GetUpdatesAsync(30);
        }


        /// <summary>
        /// Pobiera główny status asynchronicznie, po wysłaniu
        /// status będzie zwrócony jako zgłosznie zdarzenia MainStatusLoaded
        /// </summary>
        public void LoadUserMainStatus(string user)
        {
            //ładuje asynchronicznie główny status,
            //po załadowaniu zgłaszane jest zdarzenie MainStatusLoaded
            blpCom.GetUserMainStatus(user);
        }


        /// <summary>
        /// ładuje cały Dashboard użytkownika
        /// </summary>
        /// <param name="user">nazwa użytkownika którego dashboard ma załadować</param>
        public void LoadUserDashboard(string user)
        {
            blpCom.GetUserDashboard(user, 30);

        }

        /// <summary>
        /// Aktualizacja, pobranie części updateów z dashborda użytkownika
        /// </summary>
        /// <param name="user">nazwa użytkownika</param>
        /// <param name="since">od jakiego id mamy pobrać</param>
        public void UpdateUserDashboard(string user, int since)
        {
            blpCom.GetUserDashboardSince(user, since);

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
            LoadUserMainStatus(user.UserName);

            //todo: pobrać listę statusów
            LoadUserDashboard(user.UserName);

            StartListeningForUpdates(90);
        }

        public event EventHandler<ActionsEventArgs> WorkDone;

        #endregion

        public void CiteUser(StatusViewModel status, string text, int position)
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
    }
}
