using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using BlipFace.Service.Entities;
using BlipFace.Model;

namespace BlipFace.View
{

    /// <summary>
    /// Interfejs głównego okna,
    /// Definiuje podstawową odpowiedzialność naszego widoku, jakie dane powienien 
    /// przechowywać, oraz jakie operacje powinien wystawiać
    /// 
    /// </summary>
    public interface IStatusesView: IView
    {
        /// <summary>
        /// Lista wszystkich statusów użytkowników w oknie
        /// </summary>
        IList<StatusViewModel> Statuses { get; set; }

        /// <summary>
        /// Główny(ostatni) status użytkownika
        /// </summary>
        StatusViewModel MainStatus { get; set; }




        /// <summary>
        /// tekst wiadomości
        /// </summary>
        string TextMessage { get; set; }

        /// <summary>
        /// ścieżka do załączonego pliku
        /// </summary>
        string PicturePath { get; set; }

        /// <summary>
        /// Powiadomienie że nastąpił błąd aplikacji
        /// </summary>
        Exception Error { get; set; }

        TitleMessageViewModel ConnectivityStatus { get; set; }
        void UpdateStatuses(IList<StatusViewModel> collection);

        void AddStatus(StatusViewModel status,bool insertAtBeginning);
        void ShowInfo(string message);
    }
}
