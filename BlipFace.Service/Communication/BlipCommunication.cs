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
        private HttpClient blipHttpClient = new HttpClient("http://api.blip.pl/");


        private string userName;
        private string password;


        public BlipCommunication(string _userName, string _password)
        {

            userName = _userName;
            password = _password;
            blipHttpClient.DefaultHeaders.Add("X-Blip-API", "0.02");

            blipHttpClient.DefaultHeaders.Accept.Add(
                new Microsoft.Http.Headers.StringWithOptionalQuality("application/json"));

            //trzeba zakodować w base64 login:hasło - tak każe blip
            byte[] credentialBuffer = new UTF8Encoding().GetBytes(
                string.Format("{0}:{1}", userName, password));
            string authHeader = "Basic " + Convert.ToBase64String(credentialBuffer);

            blipHttpClient.DefaultHeaders.Add("Authorization", authHeader);

            //a3Npcmc6bm92cGFqYWsyNUAl

            blipHttpClient.DefaultHeaders.UserAgent.Add(new Microsoft.Http.Headers.ProductOrComment("BlipFace"));
            
            
            

NetworkCredential networkCredentials =new NetworkCredential(userName,password).GetCredential(new Uri("http://api.blip.pl"),"Basic");

       
            blipHttpClient.TransportSettings.Credentials = new NetworkCredential(userName,password).GetCredential(new Uri("http://api.blip.pl"),"Basic");
            
        }


        public IList<BlipStatus> GetAllStatuses(int limit, bool withDirectMessage)
        {

            HttpResponseMessage resp = blipHttpClient.Get("updates");
            //, new HttpQueryString().Add("limit", limit.ToString()));

            resp.EnsureStatusIsSuccessful();

            var statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();


            return statuses;
        }
    }
}
