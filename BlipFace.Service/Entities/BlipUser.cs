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
    /// Klasa reprezentuje użytkownika blipa
    /// </summary>
    [DataContract]
    public class BlipUser
    {

        /// <summary>
        /// Id użytkownika
        /// </summary>
        [DataMember(Name="id")]
        public string Id { get; set; }

        /// <summary>
        /// Login użytkownika
        /// </summary>
        [DataMember(Name="login")]
        public string Login { get; set; }


        /// <summary>
        /// względny url do avatara użytkownika
        /// </summary>
        [DataMember(Name="avatar_path")]
        public string AvatarPath { get; set; }

        /// <summary>
        /// względny url do avatara użytkownika
        /// </summary>
        [DataMember(Name = "avatar")]
        public BlipAvatar Avatar { get; set; }


    }
}
