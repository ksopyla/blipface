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
    internal class MainPresenter
    {

        /// <summary>
        /// widok 
        /// </summary>
        private IMainView view;


        /// <summary>
        /// Klasa do komunikacji z blipem, 
        /// todo: trzeba pomyśleć o innym przechowywaiu hasła
        /// </summary>
        private BlipCommunication blpCom = new BlipCommunication("blipface", @"12Faceewq");

        private Timer updateStatusTimer;

        /// <summary>
        /// Konstruktor główny
        /// </summary>
        /// <param name="_view">wikok</param>
        public MainPresenter(IMainView _view)
        {
            view = _view;

            blpCom.StatusesLoaded += new EventHandler<StatusesLoadingEventArgs>(blpCom_StatusesLoaded);

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
                    UpdateUserDashboard("blipface", lastStatus.StatusId);
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
                    UpdateUserDashboard("blipface", lastStatus.StatusId);
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
    }
}
