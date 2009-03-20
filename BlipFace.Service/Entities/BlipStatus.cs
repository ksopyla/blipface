using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlipFace.Service.Entities
{
    public class BlipStatus
    {
        /// <summary>
        /// status id 
        /// </summary>
        public int Id { get; set; }

        public string Content { get; set; }
        
        public string Autor { get; set; }

        /// <summary>
        /// Name of user whoom autor reply to
        /// </summary>
        public string ReplyTo { get; set; }
        
        /// <summary>
        /// Date and time when status was saved
        /// </summary>
        public DateTime StatusTime { get; set; }
        
        /// <summary>
        /// Program which was used to save a status e.g. gadu-gadu, www,
        /// </summary>
        public string Medium { get; set; }
        
        /// <summary>
        /// Url to status
        /// </summary>
        public Uri StatusUrl { get; set; }

    }
}
