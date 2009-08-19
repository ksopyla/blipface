using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlipFace.Service.BlipFaceWebServices;

namespace BlipFace.Service.Communication
{
    public class BlipFaceWebServicesCommunication
    {
        /// <summary>
        /// Metoda wysyła do webservices BlipFace informacje o użyciu BlipFace
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="version"></param>
        public void NotifyUseBlipFace(Guid guid, string version)
        {
            CountUseBlipFaceClient client = new CountUseBlipFaceClient();
            try
            {
                client.Notify(guid.ToString(), version);
                client.Close();
            }
            catch (Exception ex)
            {
                //jeśli były jakieś problemy z połączeniem z webservices to zignorować to
                //todo: trzeba pomyśleć nad czymś inny, a nie ignorowaniem
            }
        }
    }
}
