using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Http;
using System.Runtime.Serialization.Json;
using System.Net;
using BlipFace.Service.Entities;

namespace BlipFace.Service.Communication
{

    /// <summary>
    /// Główna klasa służąca do komunikacji z api blip.pl
    /// 
    /// Na razie zastanawiamy się czy to nie będzie singleton,
    /// a może same metody statyczne?
    /// </summary>
    public class BlipCommunication
    {
        /// <summary>
        /// Klasa z WCF Rest Starter Kit (http://msdn.microsoft.com/netframework/cc950529(en-us).aspx)
        /// </summary>
        private HttpClient blipHttpClient = new HttpClient("http://api.blip.pl");


        private string userName;
        private string password;


        public BlipCommunication(string _userName,string _password)
        {

            userName = _userName;
            password = _password;

            blipHttpClient.TransportSettings.Credentials = new NetworkCredential(userName,password);
        }


        public IList<BlipStatus> GetAllStatuses(int limit,bool withDirectMessage)
        {

            HttpResponseMessage resp=  blipHttpClient.Get("updates");
                //, new HttpQueryString().Add("limit", limit.ToString()));

            //resp.Content.ReadAsJsonDataContract

            return null;
        }
    }
}
