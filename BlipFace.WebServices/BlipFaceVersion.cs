using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace BlipFace.WebServices
{
    [DataContract]
    public class BlipFaceVersion
    {
        [DataMember]
        public Version Version { get; set; }

        [DataMember]
        public string DownloadLink { get; set; }
    }
}
