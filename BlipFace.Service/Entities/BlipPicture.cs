using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlipFace.Service.Entities
{


    [DataContract()]
    public class BlipPicture
    {

        /// <summary>
        /// status id 
        /// </summary>
        [DataMember(Name = "id")]
        public int Id { get; set; }

        /// <summary>
        /// status id 
        /// </summary>
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
