using System;
using BlipFace.Service.StatServices;


namespace BlipFace.Service.Communication
{
    public class BlipFaceServicesCommunication
    {
        /// <summary>
        /// Metoda wysyła do webservices BlipFace informacje o użyciu BlipFace
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="version"></param>
        public void NotifyUseBlipFace(Guid guid, string version)
        {
            //try{
            StatServicesClient client = new StatServicesClient();
            
                client.NotifyUseBlipFace(guid.ToString(), version);
                client.Close();
            //}
            //catch (Exception ex)
            //{
                //jeśli były jakieś problemy z połączeniem z webservices to zignorować to
                //todo: trzeba pomyśleć nad czymś inny, a nie ignorowaniem
                //System.Windows.Forms.MessageBox.Show(ex.Message);
            //}
        }

        private BlipFaceVersion blipFaceVersion;

        private void GetBlipFaceVersion()
        {
            //blipFaceVersion = new BlipFaceVersion() { Version = new Version("0.0.0.0"), DownloadLink = "http://blipface.pl" };

            //try
            //{
            StatServicesClient client = new StatServicesClient();
                blipFaceVersion = client.GetLatestVersion();

                
            //}
            //catch (Exception ex)
            //{
                //trzeba tylko logować zdarzenie bo w przypadku wyrzucenia wyjątku do BlipFace się zwiesza
                //throw new Exception("Problem z połączeniem z BlipFaceWebservices");
               // System.Windows.Forms.MessageBox.Show(ex.Message);
            //}
        }

        public string LatestVersion
        {
            get
            {
                if (blipFaceVersion == null)
                    GetBlipFaceVersion();
                return blipFaceVersion.Version;
            }
        }

        public Uri DownloadLink{
            get
            {
                if (blipFaceVersion == null)
                    GetBlipFaceVersion();
                return new Uri(blipFaceVersion.DownloadLink);
            }
        }

    }
}
