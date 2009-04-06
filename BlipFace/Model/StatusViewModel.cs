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
        public int StatusId { get; set; }

        public string UserId { get; set; }

        public string UserLogin { get; set; }

        public string UserAvatar50 { get; set; }

        public string Content { get; set; }


        public string CreationDate { get; set; }
    }
}
