using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlipFace.Service.Entities
{

    /*
      
     "user":{
"avatar_path":"\/users\/ksirg\/avatar",
"sex":"m",
"login":"ksirg",
"location":"Olsztyn, Polska","id":26658}
     */
    /// <summary>
    /// Klasa reprezentuje u¿ytkownika blipa
    /// </summary>
    [DataContract]
	public class BlipUser
    {

        /// <summary>
        /// Id u¿ytkownika
        /// </summary>
        [DataMember(Name="id")]
        public string Id { get; set; }

        /// <summary>
        /// Login u¿ytkownika
        /// </summary>
        [DataMember(Name="login")]
        public string Login { get; set; }


        /// <summary>
        /// wzglêdny url do avatara u¿ytkownika
        /// </summary>
        [DataMember(Name="avatar_path")]
        public string AvatarPath { get; set; }

        /// <summary>
        /// wzglêdny url do avatara u¿ytkownika
        /// </summary>
        [DataMember(Name = "avatar")]
        public BlipAvatar Avatar { get; set; }


    }
}
