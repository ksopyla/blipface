using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlipFace.Model
{
    /// <summary>
    /// Grupuje dane z modelu do wyświetlenia w widoku
    /// </summary>
    public class StatusViewModel
    {
        /// <summary>
        /// Identyfikator statusu
        /// </summary>
        public uint StatusId { get; set; }

        public string UserId { get; set; }

        public string UserLogin { get; set; }

        public string UserAvatar50 { get; set; }



        public string BlipFaceUser { get; set; }
        
        
        /// <summary>
        /// Czy osoba zalogowana do blipface nie jest właścicielem tego statusu
        /// potrzebne przy chowaniu opcji takich jak Odpowiedz czy Wiadomość prywatna
        /// tak aby nie można było odpowiadać na swoje wiadomości
        /// </summary>
        public bool IsNotStatusOwner
        {
            get
            {
                return !(UserLogin == BlipFaceUser);
            }
        }

        public string FirstPictureUrl { get; set; }

        
        public string RecipientAvatar50 { get; set; }

        public bool HasRecipient { get; set; }

        public bool DirectedMessage { get; set; }

        public bool PrivateMessage { get; set; }

        public string RecipientLogin { get; set; }

        public string Content { get; set; }

        public string Type { get; set; }

        public string CreationDate { get; set; }


        /// <summary>
        /// Słownik linków , shortlink=>originalLink
        /// </summary>
        public Dictionary<string, string> Links { get; set; }

        /// <summary>
        /// Słownik cytowań innych użytkowników blipLing=>message
        /// </summary>
        public Dictionary<string, string> Cites { get; set; }


        public override bool Equals(object obj)
        {
            StatusViewModel other = (StatusViewModel) obj;

            bool test = this.StatusId == other.StatusId;

            return test;

            
        }


       
    }
}
