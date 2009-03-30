using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlipFace.View;
using BlipFace.Service.Communication;
using BlipFace.Service.Entities;
using BlipFace.Model;

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
        BlipCommunication blpCom = new BlipCommunication("blipface", @"12Faceewq");


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
        }

        void blpCom_ExceptionOccure(object sender, ExceptionEventArgs e)
        {
            view.Error = e.Error;
        }

        void blpCom_StatusesUpdated(object sender, StatusesLoadingEventArgs e)
        {

            IList<StatusViewModel> statuses = MapToViewStatus(e.Statuses);
            view.Statuses= statuses.Concat(view.Statuses).ToList();
           // view.Statuses.Insert(0, statuses[0]);
        }

        void blpCom_StatusesAdded(object sender, EventArgs e)
        {
            //tylko powiadomienie że dodał 
            view.TextMessage = string.Empty;

            //int lastIndex = lstbStatusList.Items.Count;
            StatusViewModel since = view.Statuses[0] as StatusViewModel;


            UpdateUserDashboard("blipface", since.StatusId);
        }

       

        void blpCom_StatusesLoaded(object sender, StatusesLoadingEventArgs e)
        {
            view.Statuses = MapToViewStatus(e.Statuses);
        }

        
        private IList<StatusViewModel> MapToViewStatus(IList<BlipFace.Service.Entities.BlipStatus> iList)
        {
            IList<StatusViewModel> sts = new List<StatusViewModel>(20);
            foreach (BlipStatus status in iList)
            {
                sts.Add(new StatusViewModel()
                {
                    StatusId = status.Id,
                    UserId = status.User.Id,
                    Content = status.Content,
                    UserAvatar50 = @"http://blip.pl" + status.User.Avatar.Url50,
                    CreationDate = status.StatusTime,
                    UserLogin = status.User.Login
                });
            }

            return sts;
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
