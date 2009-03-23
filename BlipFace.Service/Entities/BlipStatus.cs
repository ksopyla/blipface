using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlipFace.Service.Entities
{

    /// <summary>
    /// Klasa reprezentuje pojedynczy status z blipa, któy zwracany jes w postaci json
    /// {"type":"Status",
    ///"user_path":"\/users\/ksirg",
    ///"created_at":"2009-03-22 14:10:13",
    ///"body":"#mix shoct hanselman jak zwykle wymiata, http:\/\/rdir.pl\/ensg",
    ///"id":8185940,
    ///"transport":{"name":"gg","id":5}}
    /// </summary>
    [DataContract()]
    public class BlipStatus
    {
        /// <summary>
        /// status id 
        /// </summary>
        [DataMember(Name = "id")]
        public int Id { get; set; }


        [DataMember(Name = "body")]
        public string Content { get; set; }

        [DataMember(Name = "user_path")]
        public string UserPath { get; set; }
        
        [DataMember(Name = "user")]
        public BlipUser User { get; set; }

        [DataMember(Name = "recipient_path")]
        public string RecipientPath { get; set; }

        [DataMember(Name = "recipient")]
        public BlipUser Recipient { get; set; }
        /// <summary>
        /// Date and time when status was saved
        /// </summary>
        [DataMember(Name = "created_at")]
        public string StatusTime { get; set; }





        [DataMember(Name = "pictures_path")]
        public string PicturesPath { get; set; }

        [DataMember(Name = "recording_path")]
        public string RecordingPath { get; set; }

        [DataMember(Name = "movie_path")]
        public string MoviePath { get; set; }




    }
}
