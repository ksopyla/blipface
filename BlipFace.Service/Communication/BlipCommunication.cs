using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Http;
using System.Runtime.Serialization.Json;
using System.Net;
using BlipFace.Service.Entities;
using System.Runtime.Serialization;

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
        /// Zgłaszane gdy statusy zostaną pobrane z serwisu i mają nadpisać 
        /// obecną zawartość np. przy starcie aplikacji
        /// </summary>
        public event EventHandler<StatusesLoadingEventArgs> StatusesLoaded;


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
        /// Klasa z WCF Rest Starter Kit (http://msdn.microsoft.com/netframework/cc950529(en-us).aspx)
        /// </summary>
        private HttpClient blipHttpClient = new HttpClient("http://api.blip.pl/");


        private string userName;
        private string password;

        /// <summary>
        /// Konstruktor, ustawia dane do autentykacji, oraz niezbędne
        /// nagłówki do komunikacji z blipem
        /// </summary>
        /// <param name="userName">nazwa użytkownika</param>
        /// <param name="password">hasło</param>
        public BlipCommunication(string userName, string password)
        {
            this.userName = userName;
            this.password = password;

            //potrzeba dodać obowiązkowy nagłówek gdy korzystamy z api blip'a
            blipHttpClient.DefaultHeaders.Add("X-Blip-API", "0.02");

            //także wymagane przez blipa
            blipHttpClient.DefaultHeaders.Accept.Add(
                new Microsoft.Http.Headers.StringWithOptionalQuality("application/json"));

            //trzeba zakodować w base64 login:hasło - tak każe blip
            byte[] credentialBuffer = new UTF8Encoding().GetBytes(
                string.Format("{0}:{1}", this.userName, this.password));
            string authHeader = "Basic " + Convert.ToBase64String(credentialBuffer);

            //nagłówek autoryzacja - zakodowane w base64
            blipHttpClient.DefaultHeaders.Add("Authorization", authHeader);

            //ustawienie nagłówka UserAgent - po tym blip rozpoznaje transport
            blipHttpClient.DefaultHeaders.UserAgent.Add(
                new Microsoft.Http.Headers.ProductOrComment("BlipFace/0.1 (http://blipface.pl)"));


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

            HttpResponseMessage resp = blipHttpClient.Get(query);
            //sprawdzamy czy komunikacja się powiodła
            try
            {
                if (resp.StatusCode != HttpStatusCode.Unauthorized)
                {
                    //gdy nie wyrzuci wyjątku znaczy że wszystko jest ok
                    //lecz gdy wyrzuci wyjątek to znaczy że coś nawaliła komunikacja
                    resp.EnsureStatusIsSuccessful();

                    validate = true;
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

        /// <summary>
        /// Pobiera listę statusów, w sposób synchroniczny
        /// </summary>
        /// <param name="limit">limit statusów</param>
        /// <returns></returns>
        public IList<BlipStatus> GetUpdates(int limit)
        {

            string query = string.Format("updates?include=user,user[avatar]&amp;limit={0}", limit.ToString());
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString

            HttpResponseMessage resp = blipHttpClient.Get(query);
            //sprawdzamy czy komunikacja się powiodła
            resp.EnsureStatusIsSuccessful();


            var statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();


            return statuses;
        }


        /// <summary>
        /// Pobiera statusy asynchronicznie, gdy już pobierze to zgłasza że pobrał
        /// i w callbacku ustawia statusy w widoku
        /// </summary>
        /// <param name="limi"></param>
        public void GetUpdatesAsync(int limit)
        {
            string query = string.Format("updates?include=user,user[avatar]&amp;limit={0}", limit.ToString());


            //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
            blipHttpClient.BeginSend(
                new HttpRequestMessage("GET", query), new AsyncCallback(AfterGetUpdates), blipHttpClient);

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
            //todo: Pobierać także typ odbiorcy recipient,recipient[avatar]
            string query = string.Format("/users/{0}/dashboard?include=user,user[avatar],recipient,recipient[avatar]&amp;limit={1}", user, limit.ToString());
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString
            //HttpQueryString query = new HttpQueryString();

            //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
            blipHttpClient.BeginSend(
                new HttpRequestMessage("GET", query), new AsyncCallback(AfterGetUpdates), blipHttpClient);

        }

        /// <summary>
        /// Metoda wywoływana jako callback przy pobieraniu Update'ów, korzysta z niej
        /// metoda <see cref="GetUpdatesAsync"/>
        /// </summary>
        /// <param name="result"></param>
        private void AfterGetUpdates(IAsyncResult result)
        {

            //pobieramy obiekt HttpClient, dzięki któremu został wysłany request
            //przekazaliśmy ten obiekt jako state
            var client = result.AsyncState as HttpClient;

            //pobieramy odpowiedź
            var resp = client.EndSend(result);

            try
            {

                resp.EnsureStatusIsSuccessful();

                //deserializujemy z json
                var statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();

                //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłoszenia wraz z statusami
                if (StatusesLoaded != null)
                {
                    StatusesLoaded(this, new StatusesLoadingEventArgs(statuses));
                }
            }
            catch (Exception ex)
            {
                if (ExceptionOccure != null)
                {
                    ExceptionOccure(this, new ExceptionEventArgs(ex));
                }
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
            string query = string.Format("users/{0}/statuses?include=user,user[avatar]&amp;limit=1", user);
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString
            //HttpQueryString query = new HttpQueryString();

            //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
            blipHttpClient.BeginSend(
                new HttpRequestMessage("GET", query), new AsyncCallback(AfterUserMainStatus), blipHttpClient);

        }

        /// <summary>
        /// Metoda wywoływana jako callback przy pobieraniu głównego statusu, korzysta z niej
        /// metoda <see cref="GetUserMainStatus"/>
        /// </summary>
        /// <param name="result"></param>
        private void AfterUserMainStatus(IAsyncResult result)
        {

            //pobieramy obiekt HttpClient, dzięki któremu został wysłany request
            //przekazaliśmy ten obiekt jako state
            var client = result.AsyncState as HttpClient;

            //pobieramy odpowiedź
            var resp = client.EndSend(result);

            try
            {

                resp.EnsureStatusIsSuccessful();

                //@todo: sprawdzić czy istnieje chociaż jeden status
                //deserializujemy z json
                var status = resp.Content.ReadAsJsonDataContract<StatusesList>()[0];
                
                //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłoszenia wraz ze statusem
                if (MainStatusLoaded != null)
                {
                   MainStatusLoaded(this, new MainStatusLoadingEventArgs(status));
                }
            }
            catch (Exception ex)
            {
                if (ExceptionOccure != null)
                {
                    ExceptionOccure(this, new ExceptionEventArgs(ex));
                }
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
            form.Add("body", content);

            //nowy sposób dodawania statusów
            //blipHttpClient.Post(query, form.CreateHttpContent());

            //stary sposób dodawania elementów
            //blipHttpClient.Post(query,HttpContent.Create(string.Format(@"body={0}",content)) );


            //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
            blipHttpClient.BeginSend(
                 new HttpRequestMessage("POST", new Uri(query, UriKind.Relative), form.CreateHttpContent()),
                 new AsyncCallback(AfterAddStatusAsync), blipHttpClient);

        }


        /// <summary>
        /// callback do <seealso cref="AddStatusAsync"/> wywoływany po dodaniu statusu
        /// </summary>
        /// <param name="result"></param>
        private void AfterAddStatusAsync(IAsyncResult result)
        {

            //pobieramy obiekt HttpClient, dzięki któremu został wysłany request
            //przekazaliśmy ten obiekt jako state
            var client = result.AsyncState as HttpClient;

            //pobieramy odpowiedź
            var resp = client.EndSend(result);

            try
            {
                //sprawdź czy odpowiedź jest poprawna
                resp.EnsureStatusIsSuccessful();

                //to poniżej chyba nie potrzebne
                //deserializujemy z json
                // var status = resp.Content.ReadAsJsonDataContract<BlipStatus>();

                //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłosznie wraz z statusami
                if (StatusesAdded != null)
                {
                    StatusesAdded(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                if (ExceptionOccure != null)
                {
                    ExceptionOccure(this, new ExceptionEventArgs(ex));
                }
            }
        }

        /// <summary>
        /// Asynchronicznie pobiera pulpit użytkownika od zadanego updatu,
        /// gdy są jakieś aktualizacje w nowym wątku zgłaszane jest zdarzenie <see cref="StatusesUpdated"/>
        /// </summary>
        /// <param name="user">login użytkownika</param>
        /// <param name="since">id statusu od którego należy pobrać nowsze wpisy</param>
        public void GetUserDashboardSince(string user, int since)
        {
            string query = string.Format("/users/{0}/dashboard/since/{1}?include=user,user[avatar],recipient,recipient[avatar]", user, since.ToString());
            //todo: zamiast query stringa w postaci stringa to lepiej zastosować klasę HttpQueryString
            //HttpQueryString query = new HttpQueryString();

            //jako state przekazujemy cały obiekt,aby można było pobrać później z niego ResponseMessage
            blipHttpClient.BeginSend(
                new HttpRequestMessage("GET", query), new AsyncCallback(AfterUserDashboardSince), blipHttpClient);

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
            var resp = client.EndSend(result);

            try
            {
                resp.EnsureStatusIsSuccessful();

                //deserializujemy z json
                var statuses = resp.Content.ReadAsJsonDataContract<StatusesList>();

                //gdy zostały zwrócone jakieś statusy
                if ((statuses.Count > 0) && (StatusesUpdated != null))
                {
                    //zgłaszamy zdarzenie że dane załadowaliśmy, przekazując nasze parametry zgłosznie wraz z statusami
                    StatusesUpdated(this, new StatusesLoadingEventArgs(statuses));
                }
            }
            catch (Exception ex)
            {
                if (ExceptionOccure != null)
                {
                    ExceptionOccure(this, new ExceptionEventArgs(ex));
                }
            }
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






}
