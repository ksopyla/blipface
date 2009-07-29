﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Http;
using System.Runtime.Serialization.Json;
using System.Net;
using BlipFace.Service.Entities;
using System.Runtime.Serialization;
using System.Security;
using System.Drawing;
using System.IO;

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
        #region Events

        /// <summary>
        /// Zgłaszane gdy statusy zostaną pobrane z serwisu i mają nadpisać 
        /// obecną zawartość np. przy starcie aplikacji
        /// </summary>
        public event EventHandler<StatusesLoadingEventArgs> StatusesLoaded;


        public delegate void BoolDelegate(bool value);

        public event BoolDelegate AuthorizationComplete;

        /// <summary>
        /// Zgłaszane gdy główny status zostanie pobrany z serwisu i ma nadpisać 
        /// obecną zawartość np. przy starcie aplikacji
        /// </summary>
        public event EventHandler<MainStatusLoadingEventArgs> MainStatusLoaded;


        /// <summary>
        /// Zdarzenie zgłaszane gdy statusy zostaną pobrane z serwisu i mają zostać
        /// dołączone do obecnej zawartości np. po dodaniu statusu, lub co jakiś czas
        /// </summary>
        public event EventHandler<StatusesLoadingEventArgs> StatusesUpdated;

        /// <summary>
        /// zdarzenie zgłaszane gdy status został dodany
        /// </summary>
        public event EventHandler<EventArgs> StatusesAdded;


        /// <summary>
        /// zdarzenie zgłaszane gdy wystąpi wyjątek
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionOccure;

        /// <summary>
        /// zdarzenie zgłaszane gdy nie można się skomunikować z blipem
        /// </summary>
        public event EventHandler<CommunicationErrorEventArgs> CommunicationError;

        #endregion

        /// <summary>
        /// Klasa z WCF Rest Starter Kit (http://msdn.microsoft.com/netframework/cc950529(en-us).aspx)
        /// </summary>
        private readonly HttpClient blipHttpClient = new HttpClient("http://api.blip.pl/");


        //todo: trochę to brzydko w kodzie coś na stałe wpisywać, do poprawy
        private string userAgent = "BlipFace";


        private string userLogin;


        private string password;

        /// <summary>
        /// obiekt "lock" do blokowania konkurującym wątką dostępu do obiektu blipHttpClient
        /// </summary>
        private object httpClientLock = new object();

        private TimeSpan WebGetTimout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Konstruktor, ustawia dane do autentykacji, oraz niezbędne
        /// nagłówki do komunikacji z blipem
        /// </summary>
        /// <param name="userName">nazwa użytkownika</param>
        /// <param name="password">hasło</param>
        public BlipCommunication(string userName, string password)
        {
            //potrzeba dodać obowiązkowy nagłówek gdy korzystamy z api blip'a
            SetDefaultHeaders();


            this.userLogin = userName;
            this.password = password;
            //trzeba zakodować w base64 login:hasło - tak każe blip

            //ustawiamy nagłówki do autoryzacji na bazie hasła i loginu
            SetAuthHeader();
        }

        /// <summary>
        /// Domyślny konstruktor, ustawia podstawowe nagłówki
        /// </summary>
        public BlipCommunication()
        {
            SetDefaultHeaders();
        }


        /// <summary>
        /// ustawia nagłówek Auth do autoryzacji, dokonuje kodowania base64
        /// </summary>
        /// <returns></returns>
        private void SetAuthHeader()
        {
            byte[] credentialBuffer = new UTF8Encoding().GetBytes(
                string.Format("{0}:{1}", this.userLogin, this.password));


            //nagłówek autoryzacja - zakodowane w base64
            blipHttpClient.DefaultHeaders.Remove("Authorization");
            blipHttpClient.DefaultHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(credentialBuffer));
        }


        //ustawia domyślne nagłówki dla blipa
        private void SetDefaultHeaders()
        {
            //To było ustawiane, nie wiem dlaczego, zbadać
            System.Net.ServicePointManager.Expect100Continue = false;

            blipHttpClient.TransportSettings.ConnectionTimeout = new TimeSpan(0, 30, 0);
            blipHttpClient.TransportSettings.ReadWriteTimeout = new TimeSpan(0, 15, 0);


            blipHttpClient.DefaultHeaders.Add("X-Blip-API", "0.02");

            //także wymagane przez blipa
            blipHttpClient.DefaultHeaders.Accept.Add(
                new Microsoft.Http.Headers.StringWithOptionalQuality("application/json"));


            blipHttpClient.DefaultHeaders.Add("User-Agent", userAgent);
        }


        /// <summary>
        /// Metoda służy do walidacji danych użytkownika
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            /// z racji że blip nie daje metody do autoryzacji trzeba posłuzyć się 
            /// sztuczką i spróbować pobrać statusy, jak się nie uda to znaczy że nie udało się 
            /// zalogować
            string query = "updates?limit=1";

            bool validate = false;
            try
            {
                using (HttpResponseMessage resp = blipHttpClient.Get(query))
                {
                    //sprawdzamy czy komunikacja się powiodła

                    if (resp.StatusCode != HttpStatusCode.Unauthorized)
                    {
                        //gdy nie wyrzuci wyjątku znaczy że wszystko jest ok
                        //lecz gdy wyrzuci wyjątek to znaczy że coś nawaliła komunikacja
                        resp.EnsureStatusIsSuccessful();

                        validate = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ExceptionOccure != null)
                {
                    ExceptionOccure(this, new ExceptionEventArgs(ex));
                }
            }

            return validate;
        }


        public void ValideteAsync()
        {
            /// z racji że blip nie daje metody do autoryzacji trzeba posłuzyć się 
            /// sztuczką i spróbować pobrać statusy, jak się nie uda to znaczy że nie udało się 
            /// zalogować
            string query = "updates?limit=1";

            lock (httpClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                blipHttpClient.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterValidate), blipHttpClient);
            }
        }

        /// <summary>
        /// Metoda wywoływana jako callback przy pobieraniu Update'ów, korzysta z niej
        /// metoda <see cref="GetUpdatesAsync"/>
        /// </summary>
        /// <param name="result"></param>
        private void AfterValidate(IAsyncResult result)
        {
            BlipCommunicationState state = BlipCommunicationState.OK;
            Exception exp = null;
            HttpStatusCode httpCode = HttpStatusCode.OK;

            bool validate = false;

            HttpResponseMessage resp = null;
            //sprawdzamy czy komunikacja się powiodła
            try
            {
                //pobieramy obiekt HttpClient, dzięki któremu został wysłany request
                //przekazaliśmy ten obiekt jako state
                var client = result.AsyncState as HttpClient;


                //pobieramy odpowiedź
                using (resp = client.EndSend(result))
                {
                    httpCode = resp.StatusCode;

                    if (resp.StatusCode != HttpStatusCode.Unauthorized)
                    {
                        //gdy nie wyrzuci wyjątku znaczy że wszystko jest ok
                        //lecz gdy wyrzuci wyjątek to znaczy że coś nawaliła komunikacja
                        resp.EnsureStatusIsSuccessful();

                        validate = true;
                    }
                }
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                state = BlipCommunicationState.CommunicationError;
            }
            catch (Exception ex)
            {
                state = BlipCommunicationState.Error;
                exp = ex;
            }


            //gdy wystąpiły jakieś błędy w komunikacji
            if (state == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(this, new ExceptionEventArgs(exp));
                return;
            }


            if (state == BlipCommunicationState.CommunicationError && CommunicationError != null)
            {
                CommunicationError(this, new CommunicationErrorEventArgs(httpCode));
                return;
            }

            //zgłaszamy zdarzenie że walidacja zakńczona
            if (AuthorizationComplete != null)
            {
                AuthorizationComplete(validate);
            }
        }


        /// <summary>
        /// Pobiera listę statusów, w sposób synchroniczny
        /// </summary>
        /// <param name="limit">limit statusów</param>
        /// <returns></returns>
        public IList<BlipStatus> GetUpdates(int limit)
        {
            string query = string.Format("updates?include=user,user[avatar]&amp;limit={0}", limit.ToString());
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString

            HttpResponseMessage resp;
            lock (httpClientLock)
            {
                resp = blipHttpClient.Get(query);
            }

            //sprawdzamy czy komunikacja się powiodła

            resp.EnsureStatusIsSuccessful();


            var statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();


            return statuses;
        }


        /// <summary>
        /// Pobiera status o podanym ID, w sposób synchroniczny
        /// </summary>
        /// <param name="limit">limit statusów</param>
        /// <returns></returns>
        public BlipStatus GetUpdate(string id)
        {
            string query = string.Format("updates/{0}?include=user", id);


            HttpResponseMessage resp = null;
            BlipStatus status = null;

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            try
            {
                //lock (httpClientLock)
                //{
                //to może długo trwać, a przebywanie w lock
                //za duługo może spowodować zakleszczenie 

                

                ThreadPool.QueueUserWorkItem(
                    c =>
                        {
                            resp = blipHttpClient.Get(query);
                            waitHandle.Set();
                        }
                    );

                waitHandle.WaitOne(WebGetTimout);
               
                
                ////Register the timeout callback
                //ThreadPool.RegisterWaitForSingleObject(
                //    waitHandle,
                //    delegate(object state, bool timeout)
                //    {
                //        if (timeout)
                //        {
                //            resp = null;
                //            return;
                //        }
                //    },
                //    null,
                //    TimeSpan.FromSeconds(10), // 30 second timeout
                //    true
                //    );

                if (resp != null && resp.StatusCode == HttpStatusCode.OK)
                {
                    status = resp.Content.ReadAsJsonDataContract<BlipStatus>();
                }
             
               
                /*
                 * 
                IAsyncResult asyncResult;
                lock (httpClientLock)
                {
                    //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                    asyncResult = blipHttpClient.BeginSend(
                        new HttpRequestMessage("GET", query),
                        delegate(IAsyncResult async)
                            {
                                lock (httpClientLock)
                                {
                                    resp = blipHttpClient.EndSend(async);
                                }

                                if (resp != null &&
                                    resp.StatusCode == HttpStatusCode.OK)
                                {
                                    status =resp.Content.ReadAsJsonDataContract<BlipStatus>();
                                }
                            }
                        , null);
                }


                //Register the timeout callback
                ThreadPool.RegisterWaitForSingleObject(
                    asyncResult.AsyncWaitHandle,
                    delegate(object state, bool timeout)
                        {
                            if (timeout)
                            {
                                resp = null;
                                return;
                            }
                        },
                    asyncResult,
                    TimeSpan.FromSeconds(15), // 30 second timeout
                    true
                    );
                */

                //resp = blipHttpClient.Get(query);
                //}

                //sprawdzamy czy komunikacja się powiodła
                //todo: trochę to niebezpieczne, na razie to zostawiam

                //if (resp!=null && resp.StatusCode == HttpStatusCode.OK)
                //{
                //    var status = resp.Content.ReadAsJsonDataContract<BlipStatus>();
                //    return status;
                //}
            }
            catch (Exception)
            {
                //jeśli nie uda się pobrać statusu to zwróć null
                return null;
            }

            
            return status;
        }


        /// <summary>
        /// Pobiera statusy asynchronicznie, gdy już pobierze to zgłasza że pobrał
        /// i w callbacku ustawia statusy w widoku
        /// </summary>
        /// <param name="limi"></param>
        public void GetUpdatesAsync(int limit)
        {
            string query = string.Format("updates?include=user,user[avatar]&amp;limit={0}", limit.ToString());

            lock (httpClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                blipHttpClient.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterGetUpdates), blipHttpClient);
            }
            //blipHttpClient.SendCompleted+=new EventHandler<SendCompletedEventArgs>(blipHttpClient_SendCompleted);
            //blipHttpClient.SendAsync(new HttpRequestMessage("GET",query);
        }

        /// <summary>
        /// Metoda asynchronicznie pobiera statusy z dashboardu użytkownika, gdy 
        /// zostaną pobrane zgłaszane jest zdarzenie 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="limit"></param>
        public void GetUserDashboard(string user, int limit)
        {
            Uri query = new Uri(string.Format(
                                    "/users/{0}/dashboard?include=user,user[avatar],recipient,recipient[avatar],pictures&amp;limit={1}",
                                    user,
                                    limit.ToString()), UriKind.Relative);
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString


            lock (httpClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                blipHttpClient.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterGetUpdates), blipHttpClient);
            }
        }

        /// <summary>
        /// Metoda wywoływana jako callback przy pobieraniu Update'ów, korzysta z niej
        /// metoda <see cref="GetUpdatesAsync"/>
        /// </summary>
        /// <param name="result"></param>
        private void AfterGetUpdates(IAsyncResult result)
        {
            #region stara implementacja

            /*
            //pobieramy obiekt HttpClient, dzięki któremu został wysłany request
            //przekazaliśmy ten obiekt jako state
            //var client = result.AsyncState as HttpClient;


            //HttpResponseMessage resp = null;

            //try
            //{
            //    lock (httpClientLock)
            //    {

            //        resp = client.EndSend(result);
            //    }

            //    //pobieramy odpowiedź

            //    resp.EnsureStatusIsSuccessful();

            //    if (resp.Content.GetLength() > 2)
            //    {
            //        //deserializujemy z json
            //        var statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();

            //        //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłoszenia wraz z statusami
            //        if (StatusesLoaded != null)
            //        {
            //            StatusesLoaded(this, new StatusesLoadingEventArgs(statuses));
            //        }
            //    }

            //}
            //catch (ArgumentOutOfRangeException aorEx)
            //{
            //    //gdy wystąpiły jakieś błędy w komunikacji
            //    if (CommunicationError != null)
            //    {
            //        CommunicationError(this, new CommunicationErrorEventArgs(resp.StatusCode));
            //    }
            //}
            ////catch (HttpStageProcessingException timeEx)
            ////{
            ////    //gdy wystąpiły jakieś błędy w komunikacji
            ////    if (CommunicationError != null)
            ////    {
            ////        CommunicationError(this, new CommunicationErrorEventArgs());
            ////    }
            ////}
            //catch (Exception ex)
            //{
            //    if (ExceptionOccure != null)
            //    {
            //        ExceptionOccure(this, new ExceptionEventArgs(ex));
            //    }
            //}
            //finally
            //{
            //    if (resp != null)
            //    {
            //        resp.Dispose();
            //        resp = null;
            //    }
            //}

            */

            #endregion

            var client = result.AsyncState as HttpClient;

            //pobieramy odpowiedź
            HttpResponseMessage resp = null;
            StatusesList statuses = null;
            BlipCommunicationState state = BlipCommunicationState.OK;
            Exception exp = null;
            HttpStatusCode httpCode = HttpStatusCode.OK;

            try
            {
                lock (httpClientLock)
                {
                    resp = client.EndSend(result);
                }


                httpCode = resp.StatusCode;
                resp.EnsureStatusIsSuccessful();

                //deserializujemy z json

                if (resp.Content.GetLength() > 2)
                {
                    statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();
                }
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                state = BlipCommunicationState.CommunicationError;

                //gdy wystąpiły jakieś błędy w komunikacji
            }
                //catch (HttpStageProcessingException timeEx)
                //{
                //    //gdy wystąpiły jakieś błędy w komunikacji
                //    if (CommunicationError != null)
                //    {
                //        CommunicationError(this, new CommunicationErrorEventArgs());
                //    }
                //}
            catch (Exception ex)
            {
                state = BlipCommunicationState.Error;
                exp = ex;
            }
            finally
            {
                if (resp != null)
                {
                    resp.Dispose();
                    resp = null;
                }
            }

            if (state == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(this, new ExceptionEventArgs(exp));
                return;
            }


            if (state == BlipCommunicationState.CommunicationError && CommunicationError != null)
            {
                CommunicationError(this, new CommunicationErrorEventArgs(httpCode));
                return;
            }


            if (statuses == null)
                return;

            //gdy zostały zwrócone jakieś statusy
            if ((statuses.Count > 0) && (StatusesLoaded != null))
            {
                StatusesLoaded(this, new StatusesLoadingEventArgs(statuses));
            }
        }

        /// <summary>
        /// Metoda asynchronicznie pobiera główny status użytkownika, gdy 
        /// zostanie pobrane zgłaszane jest zdarzenie 
        /// </summary>
        /// <param name="user"></param>
        public void GetUserMainStatus(string user)
        {
            //users/{0}/dashboard?include=user,user[avatar],recipient,recipient[avatar]&amp;limit={1}

            Uri query = new Uri(string.Format("users/{0}/statuses?include=user,user[avatar]&amp;limit=1", user),
                                UriKind.Relative);


            //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
            lock (httpClientLock)
            {
                blipHttpClient.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterUserMainStatus), blipHttpClient);
            }
        }

        /// <summary>
        /// Metoda wywoływana jako callback przy pobieraniu głównego statusu, korzysta z niej
        /// metoda <see cref="GetUserMainStatus"/>
        /// </summary>
        /// <param name="result"></param>
        private void AfterUserMainStatus(IAsyncResult result)
        {
            HttpResponseMessage resp = null;
            BlipStatus status = null;
            BlipCommunicationState state = BlipCommunicationState.OK;
            Exception exp = null;
            HttpStatusCode httpCode = HttpStatusCode.OK;

            try
            {
                //pobieramy obiekt HttpClient, dzięki któremu został wysłany request
                //przekazaliśmy ten obiekt jako state
                var client = result.AsyncState as HttpClient;

                //pobieramy odpowiedź
                lock (httpClientLock)
                {
                    resp = client.EndSend(result);
                }

                resp.EnsureStatusIsSuccessful();


                //deserializujemy z json
                status = resp.Content.ReadAsJsonDataContract<StatusesList>()[0];

                //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłoszenia wraz ze statusem
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                //gdy wystąpiły jakieś błędy w komunikacji
                state = BlipCommunicationState.CommunicationError;
                httpCode = resp.StatusCode;
            }
                //catch (HttpStageProcessingException timeEx)
                //{
                //    //gdy wystąpiły jakieś błędy w komunikacji
                //    if (CommunicationError != null)
                //    {
                //        CommunicationError(this, new CommunicationErrorEventArgs());
                //    }
                //}
            catch (Exception ex)
            {
                state = BlipCommunicationState.Error;
                exp = ex;
            }
            finally
            {
                if (resp != null)
                {
                    resp.Dispose();
                    resp = null;
                }
            }

            if (state == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(this, new ExceptionEventArgs(exp));
                return;
            }


            if (state == BlipCommunicationState.CommunicationError && CommunicationError != null)
            {
                CommunicationError(this, new CommunicationErrorEventArgs(httpCode));
                return;
            }


            if (status != null && MainStatusLoaded != null)
            {
                MainStatusLoaded(this, new MainStatusLoadingEventArgs(status));
            }
        }

        /// <summary>
        /// Asynchronicznie dodaje status do blipa
        /// </summary>
        /// <param name="content">treść</param>
        public void AddUpdateAsync(string content)
        {
            string query = "/updates";

            HttpUrlEncodedForm form = new HttpUrlEncodedForm();
            form.Add("update[body]", content);

            //nowy sposób dodawania statusów
            //blipHttpClient.Post(query, form.CreateHttpContent());

            //stary sposób dodawania elementów
            //blipHttpClient.Post(query,HttpContent.Create(string.Format(@"body={0}",content)) );

            lock (httpClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                blipHttpClient.BeginSend(
                    new HttpRequestMessage("POST", new Uri(query, UriKind.Relative), form.CreateHttpContent()),
                    new AsyncCallback(AfterAddStatusAsync), blipHttpClient);
            }
        }

        /// <summary>
        /// Asynchronicznie dodaje status do blipa
        /// </summary>
        /// <param name="content">treść statusu</param>
        /// <param name="imagePath">ścieżka do pliku z obrazem</param>
        public void AddUpdateAsync(string content, string imagePath)
        {
            string query = "/updates";

            string fileName = Path.GetFileName(imagePath);


            HttpMultipartMimeForm form = new HttpMultipartMimeForm();
            form.Add("update[body]", content);

            using (MemoryStream ms = new MemoryStream())
            {
                Image img = Image.FromFile(imagePath);

                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                byte[] imageBytes = ms.ToArray();

                HttpContent pic = HttpContent.Create(imageBytes, "application/octet-stream");

                form.Add("update[picture]", fileName, pic);

                lock (httpClientLock)
                {
                    //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                    blipHttpClient.BeginSend(
                        new HttpRequestMessage("POST", new Uri(query, UriKind.Relative), form.CreateHttpContent()),
                        new AsyncCallback(AfterAddStatusAsync), blipHttpClient);
                }
            }
        }


        /// <summary>
        /// callback do <seealso cref="AddUpdateAsync"/> wywoływany po dodaniu statusu
        /// </summary>
        /// <param name="result"></param>
        private void AfterAddStatusAsync(IAsyncResult result)
        {
            //pobieramy obiekt HttpClient, dzięki któremu został wysłany request
            //przekazaliśmy ten obiekt jako state
            var client = result.AsyncState as HttpClient;
            HttpResponseMessage resp = null;
            StatusesList statuses = null;
            BlipCommunicationState state = BlipCommunicationState.OK;
            Exception exp = null;
            HttpStatusCode httpCode = HttpStatusCode.OK;

            try
            {
                //pobieramy odpowiedź
                lock (httpClientLock)
                {
                    resp = client.EndSend(result);
                }

                //sprawdź czy odpowiedź jest poprawna
                resp.EnsureStatusIsSuccessful();

                //jeżeli status ok to znaczy że dodano status
                httpCode = HttpStatusCode.OK;
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                //gdy wystąpiły jakieś błędy w komunikacji
                state = BlipCommunicationState.CommunicationError;
                if (resp != null)
                    httpCode = resp.StatusCode;
            }
                //catch (HttpStageProcessingException timeEx)
                //{
                //    //gdy wystąpiły jakieś błędy w komunikacji
                //    if (CommunicationError != null)
                //    {
                //        CommunicationError(this, new CommunicationErrorEventArgs());
                //    }
                //}
            catch (Exception ex)
            {
                state = BlipCommunicationState.Error;
                exp = ex;
            }
            finally
            {
                if (resp != null)
                {
                    resp.Dispose();
                    resp = null;
                }
            }


            if (state == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(this, new ExceptionEventArgs(exp));
                return;
            }


            if (state == BlipCommunicationState.CommunicationError && CommunicationError != null)
            {
                CommunicationError(this, new CommunicationErrorEventArgs(httpCode));
                return;
            }

            //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłosznie wraz z statusami
            if (StatusesAdded != null)
            {
                StatusesAdded(this, EventArgs.Empty);
            }
        }


        /// <summary>
        /// Asynchronicznie pobiera pulpit użytkownika od zadanego updatu,
        /// gdy są jakieś aktualizacje w nowym wątku zgłaszane jest zdarzenie <see cref="StatusesUpdated"/>
        /// </summary>
        /// <param name="user">login użytkownika</param>
        /// <param name="since">id statusu od którego należy pobrać nowsze wpisy</param>
        /// <param name="limit"></param>
        public void GetUserDashboardSince(string user, uint since, int limit)
        {
            Uri query = new Uri(
                string.Format(
                    "/users/{0}/dashboard/since/{1}?include=user,user[avatar],recipient,recipient[avatar],pictures&amp;limit={2}",
                    user, since, limit), UriKind.Relative);
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString
            //HttpQueryString query = new HttpQueryString();

            //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
            lock (httpClientLock)
            {
                blipHttpClient.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterUserDashboardSince), blipHttpClient);
            }
        }


        /// <summary>
        /// Wywoływana jako callback po metodzie <see cref="GetUserDashboardSince"/>
        /// </summary>
        /// <param name="result"></param>
        private void AfterUserDashboardSince(IAsyncResult result)
        {
            //pobieramy obiekt HttpClient, dzięki któremu został wysłany request
            //przekazaliśmy ten obiekt jako state
            var client = result.AsyncState as HttpClient;

            //pobieramy odpowiedź
            HttpResponseMessage resp = null;
            StatusesList statuses = null;
            BlipCommunicationState state = BlipCommunicationState.OK;
            Exception exp = null;
            HttpStatusCode httpCode = HttpStatusCode.OK;

            try
            {
                lock (httpClientLock)
                {
                    resp = client.EndSend(result);
                }

                resp.EnsureStatusIsSuccessful();

                //deserializujemy z json

                //todo: do usunięcia testowo opóźniam przetworzenie żadania
                //Thread.Sleep(10*1000);

                if (resp.Content.GetLength() > 2)
                {
                    statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();
                }
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                state = BlipCommunicationState.CommunicationError;
                httpCode = resp.StatusCode;
                //gdy wystąpiły jakieś błędy w komunikacji
            }
                //catch (HttpStageProcessingException timeEx)
                //{
                //    //gdy wystąpiły jakieś błędy w komunikacji
                //    if (CommunicationError != null)
                //    {
                //        CommunicationError(this, new CommunicationErrorEventArgs());
                //    }
                //}
            catch (Exception ex)
            {
                state = BlipCommunicationState.Error;
                exp = ex;
            }
            finally
            {
                if (resp != null)
                {
                    resp.Dispose();
                    resp = null;
                }
            }

            if (state == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(this, new ExceptionEventArgs(exp));
                return;
            }


            if (state == BlipCommunicationState.CommunicationError && CommunicationError != null)
            {
                CommunicationError(this, new CommunicationErrorEventArgs(httpCode));
                return;
            }


            if (statuses == null)
                return;

            //gdy zostały zwrócone jakieś statusy
            if ((statuses.Count > 0) && (StatusesUpdated != null))
            {
                //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłosznie wraz z statusami
                StatusesUpdated(this, new StatusesLoadingEventArgs(statuses));
            }
        }


        /// <summary>
        /// Zmienia dane login i hasło do komunikacji z blipem 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="s"></param>
        public void SetAuthorizationCredential(string user, string password)
        {
            userLogin = user;
            this.password = password;

            SetAuthHeader();
        }

        ////
        /// <summary>
        /// ta metoda ma na celu tylko połączenie się i ustanowienie
        /// kanału TCP, 
        /// </summary>
        public void Connect()
        {
            lock (httpClientLock)
            {
                blipHttpClient.BeginSend(
                    new HttpRequestMessage("GET", "/bliposphere?limit=1"), null, null);
            }
        }

        public void ConnectAsync()
        {
            Thread t = new Thread(delegate() { this.Connect(); });
            t.Start();
        }


        /// <summary>
        /// Pobiera prawdziwego linka dla podanego skrótu
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public string GetShortLink(string code)
        {
            string query = string.Format("shortlinks/{0}", code);
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString

            HttpResponseMessage resp= null;
            string link = null;
            EventWaitHandle waitHandle = new AutoResetEvent(false);
            try
            {
                //lock (httpClientLock)
                //{
                //to może długo trwać, a przebywanie w lock
                //za duługo może spowodować zakleszczenie
               // resp = blipHttpClient.Get(query);
                //}
                ThreadPool.QueueUserWorkItem(
                   c =>
                   {
                       resp = blipHttpClient.Get(query);
                       waitHandle.Set();
                   }
                   );

                waitHandle.WaitOne(WebGetTimout);


                //sprawdzamy czy komunikacja się powiodła
                //todo: trochę to niebezpiecznie, na razie zostawiam
                if (resp!=null &&resp.StatusCode == HttpStatusCode.OK)
                {
                   var blipLink = resp.Content.ReadAsJsonDataContract<BlipShortLink>();
                    link = blipLink.OriginalLink;
                }
            }
            catch (Exception)
            {
                //specjalnie pomijamy wyjątek gdyż gdy nie uda się rozwinąć linka
                //to nie jest to wielki problem, można zwrócić null
                return null;
            }

            //zwróc link, nie ważcne czy będzie null czy miał ustawioną wartość
            return link;
        }

        
    }

    internal enum BlipCommunicationState
    {
        OK,
        CommunicationError,
        Error
    }


    /// <summary>
    /// klasa reprezentująca Statusy przekazane jako argumenty wywołania zdarzenia
    /// </summary>
    public class StatusesLoadingEventArgs : EventArgs
    {
        public IList<BlipStatus> Statuses { get; private set; }

        public StatusesLoadingEventArgs(IList<BlipStatus> statuses)
        {
            Statuses = statuses;
        }
    }

    /// <summary>
    /// klasa reprezentująca główny status przekazany jako argumenty wywołania zdarzenia
    /// </summary>
    public class MainStatusLoadingEventArgs : EventArgs
    {
        public BlipStatus MainStatus { get; private set; }

        public MainStatusLoadingEventArgs(BlipStatus status)
        {
            MainStatus = status;
        }
    }


    public class ExceptionEventArgs : EventArgs
    {
        public Exception Error { get; private set; }

        public ExceptionEventArgs(Exception _error)
        {
            this.Error = _error;
        }
    }


    public class CommunicationErrorEventArgs : EventArgs
    {
        public HttpStatusCode Code { get; private set; }

        public string Message { get; private set; }

        public CommunicationErrorEventArgs()
        {
            Code = HttpStatusCode.RequestTimeout;
            MakeMessage();
        }

        public CommunicationErrorEventArgs(HttpStatusCode code)
        {
            Code = code;
            MakeMessage();
        }

        private void MakeMessage()
        {
            /*
            400 Bad Request
            401 Unauthorized
            404 Missing
            422 Unprocessable Entity
            503 Server Unavailable
             * */
            switch (Code)
            {
                case HttpStatusCode.BadRequest:
                    Message = "Http:400 Blip odrzucił żądanie jako nieprawidłowe";
                    break;
                case HttpStatusCode.Unauthorized:
                    Message = "Http:401 Błędne dane login i hasło";
                    break;

                case HttpStatusCode.Forbidden:
                    Message = "Http:403 Serwer odrzucił żądanie jako zabronione";
                    break;
                case HttpStatusCode.NotFound:
                    Message = "Http:404 Nie znaleziono zasobu, lub nie można nawiązać komunikacji z blipem";
                    break;
                case HttpStatusCode.RequestTimeout:
                    Message = "Http:408 RequestTimeout";
                    break;

                case HttpStatusCode.ServiceUnavailable:
                    Message = "Http:503 Blip jest przeciążony";
                    break;


                case HttpStatusCode.GatewayTimeout:
                    Message = "Http:504 GatewayTimeout";
                    break;

                default:
                    Message = Code.ToString();
                    break;
            }
        }
    }
}