using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlipFace.Service.Communication;

namespace BlipFace.Helpers
{
    public class CheckLatestVersion
    {
        public void Check()
        {
            Thread thread = new Thread(new ThreadStart(CheckInNewThread));
            thread.IsBackground = true;
            thread.Start();
        }

        private void CheckInNewThread()
        {
            try
            {
                BlipFaceServicesCommunication com = new BlipFaceServicesCommunication();

                
                LatestVersionChecked(null, new BlipFaceVersionEventArgs(com.LatestVersion, com.DownloadLink));
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                if (ex.InnerException != null)
                {
                    mes += Environment.NewLine + "Inner exp: " + ex.InnerException.Message;
                }
                System.Windows.Forms.MessageBox.Show(mes);
            }
        }

        /// <summary>
        /// Zgłaszane gdy zostanie sprawdzona najnowsza dostępna wersja BlipFace
        /// </summary>
        public event EventHandler<BlipFaceVersionEventArgs> LatestVersionChecked;
    }

    /// <summary>
    /// klasa reprezentująca informację o nowej wersji przekazanej jako argumenty wywołania zdarzenia
    /// </summary>
    public class BlipFaceVersionEventArgs : EventArgs
    {
        public Version Version { get; private set; }

        public Uri DownloadLink { get; private set; }

        public BlipFaceVersionEventArgs(Version version, Uri downloadLink)
        {
            Version = version;
            DownloadLink = downloadLink;
        }
    }
}
