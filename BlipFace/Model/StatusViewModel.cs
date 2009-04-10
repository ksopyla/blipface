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
        public int StatusId { get; set; }

        public string UserId { get; set; }

        public string UserLogin { get; set; }

        public string UserAvatar50 { get; set; }


        private string recipientAvatar50;
        public string RecipientAvatar50 { 
            get
            {
                return recipientAvatar50;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    //jeśli RecipientAvatar50 jest ustawiony na łańcuch pusty 
                    //to dodatkow ustawiamy właściwość czy ma odbirorcę na false
                    //przydatne to jest przy bindowaniu do kontrolki wyświetlającej
                    //listę statusów i Avatar drugiego użytkownika
                    HasRecipient = false;
                    
                }
                else
                {
                    HasRecipient = true;
                }
                recipientAvatar50 = value;
            }
        }

        public bool HasRecipient { get; set; }

        public string Content { get; set; }


        public string CreationDate { get; set; }
    }
}
