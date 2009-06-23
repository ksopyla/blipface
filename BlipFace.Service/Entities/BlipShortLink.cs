using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlipFace.Service.Entities
{
    [DataContract]
   public class BlipShortLink
    {
        [DataMember(Name = "original_link")]
        public string OriginalLink { get; set; }
    }
}
