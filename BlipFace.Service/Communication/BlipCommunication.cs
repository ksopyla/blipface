using System;
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
    public enum BlipActions
    {
        AfterValidate,
        AfterUpdate,
        AfterLoad,
        AfterStatusAdd,
        AfterGetUpdate,
        AfterGetShortLink,
        AfterMainStatus
    }

    [Flags]
    internal enum BlipCommunicationState
    {
        OK,
        CommunicationError,
        Error
    }

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
        private readonly HttpClient blipHttpClientAsync = new HttpClient("http://api.blip.pl/");

        private readonly HttpClient blipHttpClientSync = new HttpClient("http://api.blip.pl/");

        //todo: trochę to brzydko w kodzie coś na stałe wpisywać, do poprawy

        private const string BlipfaceUserAgent = "BlipFace";
        private const string BlipApiHeader = "X-Blip-API";
        private const string BlipApiHeaderVersion = "0.02";
        private const string BlipAcceptHeader = "application/json";

        private string userLogin;
        private string password;

        /// <summary>
        /// obiekt "lock" do blokowania konkurującym wątką dostępu do obiektu blipHttpClient
        /// </summary>
        private object httpAsyncClientLock = new object();

        private object httpSyncClientLock = new object();


        private TimeSpan webGetTimout = TimeSpan.FromSeconds(30);


        public TimeSpan WebGetTimout
        {
            get { return webGetTimout; }
            set
            {
                webGetTimout = value;
                blipHttpClientAsync.TransportSettings.ConnectionTimeout = WebGetTimout;
                blipHttpClientAsync.TransportSettings.ReadWriteTimeout = WebGetTimout;

                blipHttpClientSync.TransportSettings.ConnectionTimeout = WebGetTimout;
                blipHttpClientSync.TransportSettings.ReadWriteTimeout = WebGetTimout;
            }
        }

        /// <summary>
        /// Konstruktor, ustawia dane do autentykacji, oraz niezbędne
        /// nagłówki do komunikacji z blipem
        /// </summary>
        /// <param name="userName">nazwa użytkownika</param>
        /// <param name="password">hasło</param>
        public BlipCommunication(string userName, string password) : this()
        {
            //potrzeba dodać obowiązkowy nagłówek gdy korzystamy z api blip'a
            //SetDefaultHeaders();


            this.userLogin = userName;
            this.password = password;
            //trzeba zakodować w base64 login:hasło - tak każe blip

            //ustawiamy nagłówki do autoryzacji na bazie hasła i loginu
            SetAuthHeader();
        }

        public BlipCommunication(string userName, string password, int webTimout) : this(userName, password)
        {
            WebGetTimout = TimeSpan.FromSeconds(webTimout);
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
            string auth = "Basic " + Convert.ToBase64String(credentialBuffer);

            //nagłówek autoryzacja - zakodowane w base64
            blipHttpClientAsync.DefaultHeaders.Remove("Authorization");
            blipHttpClientAsync.DefaultHeaders.Add("Authorization", auth);

            blipHttpClientSync.DefaultHeaders.Remove("Authorization");
            blipHttpClientSync.DefaultHeaders.Add("Authorization", auth);
        }


        //ustawia domyślne nagłówki dla blipa
        private void SetDefaultHeaders()
        {
            WebGetTimout = TimeSpan.FromSeconds(30);
            //To było ustawiane, nie wiem dlaczego, zbadać
            System.Net.ServicePointManager.Expect100Continue = false;

            blipHttpClientAsync.DefaultHeaders.Add(BlipApiHeader, BlipApiHeaderVersion);

            blipHttpClientAsync.DefaultHeaders.Accept.Add(
                new Microsoft.Http.Headers.StringWithOptionalQuality(BlipAcceptHeader));

            blipHttpClientAsync.DefaultHeaders.Add("User-Agent", BlipfaceUserAgent);


            blipHttpClientSync.DefaultHeaders.Add(BlipApiHeader, BlipApiHeaderVersion);

            blipHttpClientSync.DefaultHeaders.Accept.Add(
                new Microsoft.Http.Headers.StringWithOptionalQuality(BlipAcceptHeader));

            blipHttpClientSync.DefaultHeaders.Add("User-Agent", BlipfaceUserAgent);
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
                using (HttpResponseMessage resp = blipHttpClientAsync.Get(query))
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
                    ExceptionOccure(null, new ExceptionEventArgs(ex));
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

            lock (httpAsyncClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterValidate), blipHttpClientAsync);
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
                ExceptionOccure(null, new ExceptionEventArgs(exp));
                return;
            }


            if (state == BlipCommunicationState.CommunicationError && CommunicationError != null)
            {
                CommunicationError(null, new CommunicationErrorEventArgs(httpCode, BlipActions.AfterValidate));
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
            lock (httpAsyncClientLock)
            {
                resp = blipHttpClientSync.Get(query);
            }

            //sprawdzamy czy komunikacja się powiodła

            resp.EnsureStatusIsSuccessful();


            var statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();


            return statuses;
        }


        /*
        public IList<BlipStatus> GetBothDirectAndPrivateMessages(string user, int limit)
        {
            Uri query1 = new Uri(string.Format(
                                     "/users/{0}/directed_messages?include=user,user[avatar],recipient,recipient[avatar],pictures&amp;limit={1}",
                                     user,
                                     limit), UriKind.Relative);
            Uri query2 = new Uri(string.Format(
                                     "/private_messages?include=user,user[avatar],recipient,recipient[avatar],pictures&amp;limit={1}",
                                     limit), UriKind.Relative);

            BlipCommunicationState state = BlipCommunicationState.OK;
            Exception exp = null;
            HttpStatusCode httpCode = HttpStatusCode.OK;
            HttpResponseMessage resp1 = null;
            HttpResponseMessage resp2 = null;

            IList<BlipStatus> statusesPriv = null;
            IList<BlipStatus> statusesDirect = null;

            EventWaitHandle[] waitHandles = new[]
                                                {
                                                    new AutoResetEvent(false),
                                                    new AutoResetEvent(false)
                                                };

            try
            {
                ThreadPool.QueueUserWorkItem(
                    c =>
                        {
                            try
                            {
                                lock (httpClientLock)
                                {
                                    //to może długo trwać, a przebywanie w lock
                                    //za duługo może spowodować zakleszczenie 
                                    resp1 = blipHttpClient.Get(query1);
                                }

                                //to może długo trwać, a przebywanie w lock
                                //za duługo może spowodować zakleszczenie 
                                //resp = blipHttpClient.Get(query);
                            }
                            catch (ArgumentOutOfRangeException aorEx)
                            {
                                state = BlipCommunicationState.CommunicationError;
                                httpCode = resp1.StatusCode;
                                //gdy wystąpiły jakieś błędy w komunikacji
                            }
                            finally
                            {
                                waitHandles[0].Set();
                            }
                        }
                    );

                ThreadPool.QueueUserWorkItem(
                    c =>
                        {
                            try
                            {
                                lock (httpClientLock)
                                {
                                    //to może długo trwać, a przebywanie w lock
                                    //za duługo może spowodować zakleszczenie 
                                    resp2 = blipHttpClient.Get(query2);
                                }

                                //to może długo trwać, a przebywanie w lock
                                //za duługo może spowodować zakleszczenie 
                                //resp = blipHttpClient.Get(query);
                            }
                            catch (ArgumentOutOfRangeException aorEx)
                            {
                                state = BlipCommunicationState.CommunicationError;
                                httpCode = resp2.StatusCode;
                                //gdy wystąpiły jakieś błędy w komunikacji
                            }
                            finally
                            {
                                waitHandles[1].Set();
                            }
                        }
                    );

                WaitHandle.WaitAll(waitHandles, WebGetTimout);


                if (resp1 != null && resp1.StatusCode == HttpStatusCode.OK)
                {
                    resp1.EnsureStatusIsSuccessful();
                    statusesDirect = resp1.Content.ReadAsJsonDataContract<StatusesList>();
                }

                if (resp2 != null && resp2.StatusCode == HttpStatusCode.OK)
                {
                    resp2.EnsureStatusIsSuccessful();
                    statusesPriv = resp2.Content.ReadAsJsonDataContract<StatusesList>();
                }
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                state = BlipCommunicationState.CommunicationError;
                httpCode =
                //gdy wystąpiły jakieś błędy w komunikacji
            }
            catch (SerializationException serEx)
            {
                state = BlipCommunicationState.CommunicationError | BlipCommunicationState.Error;
                httpCode = resp.StatusCode;
                exp = serEx;
            }
            catch (Exception)
            {
                //jeśli nie uda się pobrać statusu to zwróć null
                return null;
            }
            finally
            {
                if (resp != null)
                {
                    resp.Dispose();
                    resp = null;
                }
            }
            return statusesPriv.Concat(statusesDirect).ToList();
        }
        */

        /// <summary>
        /// Pobiera status o podanym ID, w sposób synchroniczny
        /// </summary>
        /// <returns></returns>
        public BlipStatus GetUpdate(string id)
        {
            string query = string.Format("updates/{0}?include=user", id);

            //pobieramy odpowiedź

            BlipCommunicationState state = BlipCommunicationState.OK;
            Exception exp = null;
            HttpStatusCode httpCode = HttpStatusCode.OK;
            HttpResponseMessage resp = null;
            BlipStatus status = null;

            EventWaitHandle waitHandle = new AutoResetEvent(false);
            try
            {
                ThreadPool.QueueUserWorkItem(
                    c =>
                        {
                            try
                            {
                                lock (httpSyncClientLock)
                                {
                                    //to może długo trwać, a przebywanie w lock
                                    //za duługo może spowodować zakleszczenie 
                                    resp = blipHttpClientSync.Get(query);
                                }

                                //to może długo trwać, a przebywanie w lock
                                //za duługo może spowodować zakleszczenie 
                                //resp = blipHttpClient.Get(query);
                            }
                            catch (Exception)
                            {
                                //gdy wystąpiły jakieś błędy w komunikacji
                            }
                            finally
                            {
                                waitHandle.Set();
                            }
                        }
                    );

                waitHandle.WaitOne(WebGetTimout);


                if (resp != null && resp.StatusCode == HttpStatusCode.OK)
                {
                    resp.EnsureStatusIsSuccessful();
                    status = resp.Content.ReadAsJsonDataContract<BlipStatus>();
                }
            }
            catch (HttpStageProcessingException stageEx)
            {
                state = BlipCommunicationState.CommunicationError;
                httpCode = resp.StatusCode;
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                state = BlipCommunicationState.CommunicationError;
                httpCode = resp.StatusCode;
                //gdy wystąpiły jakieś błędy w komunikacji
            }
            catch (SerializationException serEx)
            {
                state = BlipCommunicationState.CommunicationError | BlipCommunicationState.Error;
                httpCode = resp.StatusCode;
                exp = serEx;
            }
            catch (Exception)
            {
                //jeśli nie uda się pobrać statusu to zwróć null
                return null;
            }
            finally
            {
                if (resp != null)
                {
                    resp.Dispose();
                    resp = null;
                }
            }
            if (((state & BlipCommunicationState.CommunicationError) == BlipCommunicationState.CommunicationError)
                && CommunicationError != null)
            {
                CommunicationError(null, new CommunicationErrorEventArgs(httpCode, BlipActions.AfterGetUpdate));
            }


            if ((state & BlipCommunicationState.Error) == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(null, new ExceptionEventArgs(exp));
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

            lock (httpAsyncClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterStatusesLoaded), blipHttpClientAsync);
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
        /// <param name="page"></param>
        public void GetUserDashboard(string user, int limit, int page)
        {
            Uri query = new Uri(string.Format(
                                    "/users/{0}/dashboard?include=user,user[avatar],recipient,recipient[avatar],pictures&amp;limit={1}&amp;offset={2}",
                                    user,
                                    limit, page), UriKind.Relative);
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString


            IAsyncResult asyncResult;
            lock (httpAsyncClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                asyncResult = blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterStatusesLoaded), blipHttpClientAsync);
            }


            //if (asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(8)))
            //{
            //    int a = 5;
            //}
            //else
            //{
            //   // blipHttpClient.EndSend(asyncResult);

            //  //  blipHttpClient.SendAsyncCancel(asyncResult.AsyncState);
            //}
        }


        /// <summary>
        /// Metoda wywoływana jako callback przy pobieraniu Update'ów, korzysta z niej
        /// metoda <see cref="GetUpdatesAsync"/>
        /// </summary>
        /// <param name="result"></param>
        private void AfterStatusesLoaded(IAsyncResult result)
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
                lock (httpAsyncClientLock)
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
            catch (HttpStageProcessingException stageEx)
            {
                state = BlipCommunicationState.CommunicationError;
                if (resp != null) httpCode = resp.StatusCode;
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                state = BlipCommunicationState.CommunicationError;
                httpCode = resp.StatusCode;
                //gdy wystąpiły jakieś błędy w komunikacji
            }
            catch (SerializationException serEx)
            {
                state = BlipCommunicationState.CommunicationError | BlipCommunicationState.Error;
                httpCode = resp.StatusCode;
                exp = serEx;
            }
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

            if (((state & BlipCommunicationState.CommunicationError) == BlipCommunicationState.CommunicationError)
                && CommunicationError != null)
            {
                CommunicationError(null, new CommunicationErrorEventArgs(httpCode, BlipActions.AfterLoad));
                return;
            }


            if ((state & BlipCommunicationState.Error) == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(null, new ExceptionEventArgs(exp));
                return;
            }

            if (statuses == null)
                return;

            //gdy zostały zwrócone jakieś statusy
            if ((statuses.Count > 0) && (StatusesLoaded != null))
            {
                StatusesLoaded(null, new StatusesLoadingEventArgs(statuses));
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
            lock (httpAsyncClientLock)
            {
                blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterUserMainStatus), blipHttpClientAsync);
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
                lock (httpAsyncClientLock)
                {
                    resp = client.EndSend(result);
                }

                resp.EnsureStatusIsSuccessful();


                //deserializujemy z json
                status = resp.Content.ReadAsJsonDataContract<StatusesList>()[0];

                //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłoszenia wraz ze statusem
            } 
            catch (HttpStageProcessingException timeEx)
                {
                    //gdy wystąpiły jakieś błędy w komunikacji
                    state = BlipCommunicationState.CommunicationError;
                    if (resp != null) httpCode = resp.StatusCode;
                }
            catch (ArgumentOutOfRangeException aorEx)
            {
                //gdy wystąpiły jakieś błędy w komunikacji
                state = BlipCommunicationState.CommunicationError;
                if (resp != null) httpCode = resp.StatusCode;
            }

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

            if (((state & BlipCommunicationState.CommunicationError) == BlipCommunicationState.CommunicationError)
                && CommunicationError != null)
            {
                CommunicationError(null, new CommunicationErrorEventArgs(httpCode, BlipActions.AfterMainStatus));
                return;
            }


            if ((state & BlipCommunicationState.Error) == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(null, new ExceptionEventArgs(exp));
                return;
            }


            if (status != null && MainStatusLoaded != null)
            {
                MainStatusLoaded(null, new MainStatusLoadingEventArgs(status));
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

            lock (httpAsyncClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("POST", new Uri(query, UriKind.Relative), form.CreateHttpContent()),
                    new AsyncCallback(AfterAddStatusAsync), blipHttpClientAsync);
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

                lock (httpAsyncClientLock)
                {
                    //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                    blipHttpClientAsync.BeginSend(
                        new HttpRequestMessage("POST", new Uri(query, UriKind.Relative), form.CreateHttpContent()),
                        new AsyncCallback(AfterAddStatusAsync), blipHttpClientAsync);
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
                lock (httpAsyncClientLock)
                {
                    resp = client.EndSend(result);
                }

                //sprawdź czy odpowiedź jest poprawna
                //resp.EnsureStatusIsSuccessful();

                resp.EnsureStatusIs(200, new int[] {201, 204});

                //jeżeli status ok to znaczy że dodano status
                httpCode = resp.StatusCode;
            }
            catch (HttpStageProcessingException stageEx)
            {
                state = BlipCommunicationState.CommunicationError;
                if (resp != null)
                    httpCode = resp.StatusCode;
            }

            catch (ArgumentOutOfRangeException aorEx)
            {
                //gdy wystąpiły jakieś błędy w komunikacji
                state = BlipCommunicationState.CommunicationError;
                if (resp != null)
                    httpCode = resp.StatusCode;
            }
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


            if (((state & BlipCommunicationState.CommunicationError) == BlipCommunicationState.CommunicationError)
                && CommunicationError != null)
            {
                CommunicationError(null, new CommunicationErrorEventArgs(httpCode, BlipActions.AfterStatusAdd));
                return;
            }


            if ((state & BlipCommunicationState.Error) == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(null, new ExceptionEventArgs(exp));
                return;
            }

            //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłosznie wraz z statusami
            if (StatusesAdded != null)
            {
                StatusesAdded(null, EventArgs.Empty);
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
            lock (httpAsyncClientLock)
            {
                blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterStatusesUpdatedSince),
                    blipHttpClientAsync);
            }
        }


        /// <summary>
        /// Asynchronicznie pobiera wiadomości kierowane do użytkownika od zadanego statusu,
        /// nadaje się jako update
        /// </summary>
        /// <param name="user"></param>
        /// <param name="since"></param>
        /// <param name="limit"></param>
        public void GetDirectMessagesSince(string user, uint since, int limit)
        {
            Uri query = new Uri(
                string.Format(
                    "/users/{0}/directed_messages/{1}/since?include=user,user[avatar],recipient,recipient[avatar],pictures&amp;limit={2}",
                    user, since, limit), UriKind.Relative);
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString
            //HttpQueryString query = new HttpQueryString();

            //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
            lock (httpAsyncClientLock)
            {
                blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterStatusesUpdatedSince),
                    blipHttpClientAsync);
            }
        }


        /// <summary>
        /// Asynchronicznie pobiera wiadomości kierowane do użytkownika, pobiera <paramref name="limit"/> ostatnich
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="limit"></param>
        public void GetDirectMessages(string user, int limit)
        {
            Uri query = new Uri(string.Format(
                                    "/users/{0}/directed_messages?include=user,user[avatar],recipient,recipient[avatar],pictures&amp;limit={1}",
                                    user,
                                    limit.ToString()), UriKind.Relative);
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString


            lock (httpAsyncClientLock)
            {
                //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
                blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("GET", query), new AsyncCallback(AfterStatusesLoaded), blipHttpClientAsync);
            }
        }

        /// <summary>
        /// Wywoływana jako callback po metodzie <see cref="GetUserDashboardSince"/>
        /// </summary>
        /// <param name="result"></param>
        private void AfterStatusesUpdatedSince(IAsyncResult result)
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
                lock (httpAsyncClientLock)
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
            catch (HttpStageProcessingException stageEx)
            {
                state = BlipCommunicationState.CommunicationError;
                if (resp != null)
                {
                    httpCode = resp.StatusCode;
                }
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                state = BlipCommunicationState.CommunicationError;
                if (resp != null)
                    httpCode = resp.StatusCode;
                //gdy wystąpiły jakieś błędy w komunikacji
            }
            catch (SerializationException serEx)
            {
                state = BlipCommunicationState.CommunicationError | BlipCommunicationState.Error;
                httpCode = resp.StatusCode;
                exp = serEx;
            }
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


            if (((state & BlipCommunicationState.CommunicationError) == BlipCommunicationState.CommunicationError)
                && CommunicationError != null)
            {
                CommunicationError(null, new CommunicationErrorEventArgs(httpCode, BlipActions.AfterUpdate));
                return;
            }


            if ((state & BlipCommunicationState.Error) == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(null, new ExceptionEventArgs(exp));
                return;
            }


            if (statuses == null)
                return;

            //gdy zostały zwrócone jakieś statusy
            if ((statuses.Count > 0) && (StatusesUpdated != null))
            {
                //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłosznie wraz z statusami
                StatusesUpdated(null, new StatusesLoadingEventArgs(statuses));
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
            lock (httpAsyncClientLock)
            {
                blipHttpClientAsync.BeginSend(
                    new HttpRequestMessage("GET", "/bliposphere?limit=1"), null, null);
            }
        }

        public void ConnectAsync()
        {
            ThreadPool.QueueUserWorkItem(
                c => Connect());

            //Thread t = new Thread(delegate() { this.Connect(); });
            //t.Start();
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


            BlipCommunicationState state = BlipCommunicationState.OK;
            Exception exp = null;
            HttpStatusCode httpCode = HttpStatusCode.OK;

            HttpResponseMessage resp = null;
            string link = null;
            EventWaitHandle waitHandle = new AutoResetEvent(false);
            try
            {
                ThreadPool.QueueUserWorkItem(
                    c =>
                        {
                            //po testach wydaje się że ten lock nie potrzebny
                            // a może jednak potrzebny - 
                            //todo: zbadać to lepiej
                            try
                            {
                                lock (httpSyncClientLock)
                                {
                                    resp = blipHttpClientSync.Get(query);
                                }
                            }

                            catch (Exception)
                            {
                                //gdy wystąpiły jakieś błędy w komunikacji
                            }
                            finally
                            {
                                waitHandle.Set();
                            }
                        }
                    );

                waitHandle.WaitOne(WebGetTimout);


                //sprawdzamy czy komunikacja się powiodła
                //todo: trochę to niebezpiecznie, na razie zostawiam
                if (resp != null && resp.StatusCode == HttpStatusCode.OK)
                {
                    resp.EnsureStatusIsSuccessful();

                    var blipLink = resp.Content.ReadAsJsonDataContract<BlipShortLink>();
                    link = blipLink.OriginalLink;
                }
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                state = BlipCommunicationState.CommunicationError;
                if (resp != null) httpCode = resp.StatusCode;
                //gdy wystąpiły jakieś błędy w komunikacji
            }
            catch (SerializationException serEx)
            {
                state = BlipCommunicationState.CommunicationError | BlipCommunicationState.Error;
                if (resp != null) httpCode = resp.StatusCode;
                exp = serEx;
            }
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


            if (((state & BlipCommunicationState.CommunicationError) == BlipCommunicationState.CommunicationError)
                && CommunicationError != null)
            {
                CommunicationError(null, new CommunicationErrorEventArgs(httpCode, BlipActions.AfterGetShortLink));
            }


            if ((state & BlipCommunicationState.Error) == BlipCommunicationState.Error && ExceptionOccure != null)
            {
                ExceptionOccure(null, new ExceptionEventArgs(exp));
            }


            //zwróc link, nie ważcne czy będzie null czy miał ustawioną wartość
            return link;
        }
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

        public BlipActions AfterAction { get; private set; }

        public string Message { get; private set; }


        public CommunicationErrorEventArgs()
        {
            Code = HttpStatusCode.RequestTimeout;
            MakeMessage();
        }

        public CommunicationErrorEventArgs(HttpStatusCode code, BlipActions action)
        {
            Code = code;
            MakeMessage();
            AfterAction = action;
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