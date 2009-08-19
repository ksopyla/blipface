using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BlipFace.WebServices
{
    // NOTE: If you change the interface name "ICountUseBlipFace" here, you must also update the reference to "ICountUseBlipFace" in Web.config.
    [ServiceContract]
    public interface ICountUseBlipFace
    {
        [OperationContract]
        void Notify(string guid, string version);
    }
}
