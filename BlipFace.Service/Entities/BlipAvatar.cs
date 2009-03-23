using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlipFace.Service.Entities
{

    /*
     "avatar":{
     *      "url_15":"\/user_generated\/avatars\/238118_femto.jpg",
     *      "url_30":"\/user_generated\/avatars\/238118_nano.jpg",
     *      "url_50":"\/user_generated\/avatars\/238118_pico.jpg",
     *      "url":"http:\/\/blip.pl\/user_generated\/avatars\/238118.jpg",
     *      "url_90":"\/user_generated\/avatars\/238118_standard.jpg",
     *      "url_120":"\/user_generated\/avatars\/238118_large.jpg",
     *      "id":238118}
     */ 

    /// <summary>
    /// Klasa reprezentująca Avatar na blipie
    /// </summary>
    [DataContract(Name="avatar")]
    public class BlipAvatar
    {
        [DataMember(Name = "id")]
        public string Id { get ; set; }

        [DataMember(Name="url_15")]
        public string Url15 { get; set; }

        [DataMember(Name = "url_30")]
        public string Url30 { get; set; }
        
        [DataMember(Name = "url_50")]
        public string Url50 { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "url_90")]
        public string Url90 { get; set; }

        [DataMember(Name = "url_120")]
        public string Url120 { get; set; }
    }
}
