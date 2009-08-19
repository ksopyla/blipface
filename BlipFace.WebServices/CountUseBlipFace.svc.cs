using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BlipFace.WebServices
{
    // NOTE: If you change the class name "CountUseBlipFace" here, you must also update the reference to "CountUseBlipFace" in Web.config.
    public class CountUseBlipFace : ICountUseBlipFace
    {

        #region ICountUseBlipFace Members

        public void Notify(string guid, string version)
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

        #endregion
    }
}
