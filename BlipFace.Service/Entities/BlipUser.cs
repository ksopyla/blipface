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
    [DataContract(Name="user")]
    public class BlipUser
    {
        [DataMember(Name="login")]
        public string Login { get; set; }

    }
}
