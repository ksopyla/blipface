using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BlipFace.WebServices
{
    // NOTE: If you change the class name "BlipFaceServices" here, you must also update the reference to "BlipFaceServices" in Web.config.
    public class StatServices : IStatServices
    {

        #region IBlipFaceServices Members

        public void NotifyUseBlipFace(string guid, string version)
        {
            using (DataClassesDataContext db = new DataClassesDataContext())
            {
                CountUse countUse = new CountUse();
                countUse.DateUse = DateTime.Now;
                countUse.UserGuid = guid;
                countUse.Version = version;

                db.CountUses.InsertOnSubmit(countUse);
                db.SubmitChanges();
            }
        }

        public BlipFaceVersion GetLatestVersion()
        {
           return new BlipFaceVersion() { Version = Properties.Settings.Default.LatestVersion, DownloadLink = Properties.Settings.Default.DownloadLink };
            //return new BlipFaceVersion(){  DownloadLink = Properties.Settings.Default.DownloadLink };
        }

        #endregion
    }
}
