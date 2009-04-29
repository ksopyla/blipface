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
    /// Klasa reprezentuj¹ca Avatar na blipie
    /// </summary>
    [DataContract(Name = "avatar")]
    public class BlipAvatar
    {
        private const int StaticAddreses = 3;

        /// <summary>
        /// lista statycznych adresów do zasobów blipa
        /// </summary>
        private static readonly List<string> BlipStaticHostAddress =
            new List<string>(StaticAddreses)
                {
                    @"http://static0.blip.pl",
                    @"http://static1.blip.pl",
                    @"http://static2.blip.pl"
                };

        //@"http://static3.blip.pl"


        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "url_15")]
        public string Url15 { get; set; }

        [DataMember(Name = "url_30")]
        public string Url30 { get; set; }


        private string url50;

        [DataMember(Name = "url_50")]
        public string Url50
        {
            get
            {
                Random r = new Random(Environment.TickCount);
                int index = r.Next(StaticAddreses);

                StringBuilder _url = new StringBuilder(80);
                _url.Append(BlipStaticHostAddress[index]);
                _url.Append(url50);

                return _url.ToString();
            }
            set { url50 = value; }
        }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "url_90")]
        public string Url90 { get; set; }

        [DataMember(Name = "url_120")]
        public string Url120 { get; set; }
    }
}