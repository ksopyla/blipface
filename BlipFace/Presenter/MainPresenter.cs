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
        }

        void blpCom_StatusesLoaded(object sender, StatusesLoadingEventArgs e)
        {
            view.Statuses = MapToViewStatus(e.Statuses);
        }

        //tymczasowe rozwianie
        private IEnumerable<StatusViewModel> MapToViewStatus(IList<BlipFace.Service.Entities.BlipStatus> iList)
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


       public void LoadUserDashboard(string user)
        {
            blpCom.GetUserDashboard(user, 30); 
           
        }
    }
}
