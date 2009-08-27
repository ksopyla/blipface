using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security;
using BlipFace.Service.Communication;

namespace BlipFace.Helpers
{
    public class UseageStatistics
    {
        private const string FileBlipFaceGuid = "BlipFaceGuid.bup";

        public void Notyfi()
        {
            Thread thread = new Thread(new ThreadStart(NotyfiInNewThread));
            thread.IsBackground = true;
            thread.Start();
        }

        private void NotyfiInNewThread()
        {
            try
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                Guid blipFaceGuid = Guid.Empty;

                using (IsolatedStorageAccess isoAccess = new IsolatedStorageAccess(FileBlipFaceGuid))
                {
                    string[] credenctial = isoAccess.ReadAll();

                    if (credenctial != null && credenctial.Length > 0)
                    {
                        //odczytujemy Guid danej instancji aplikacji BlipFace
                        var stringGuid = credenctial[0].Decrypt().ToInsecureString();
                        blipFaceGuid = new Guid(stringGuid);
                    }
                }

                //gdy nie ma jeszcze ustawionego Guida aplikacji (np. gdy BlipFace jest uruchamiany poraz pierwszy
                if (blipFaceGuid == Guid.Empty)
                {
                    blipFaceGuid = Guid.NewGuid();

                    SecureString secureGuid = blipFaceGuid.ToString().ToSecureString();

                    using (IsolatedStorageAccess isoAccess = new IsolatedStorageAccess(FileBlipFaceGuid))
                    {
                        string[] credenctial = new[] {secureGuid.Encrypt()};

                        isoAccess.WriteStrings(credenctial);
                    }
                }

                BlipFaceServicesCommunication communication = new BlipFaceServicesCommunication();
                communication.NotifyUseBlipFace(blipFaceGuid, version.ToString());
            }catch(Exception ex)
            {
                string mes = ex.Message;
                if(ex.InnerException!=null)
                {
                    mes += Environment.NewLine + "Inner exp: " + ex.InnerException.Message;
                }
                System.Windows.Forms.MessageBox.Show(mes);
            }
        }
    }
}
