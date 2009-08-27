using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BlipFace.WebServices
{
    // NOTE: If you change the interface name "IBlipFaceServices" here, you must also update the reference to "IBlipFaceServices" in Web.config.
    [ServiceContract]
    public interface IStatServices
    {
        [OperationContract]
        void NotifyUseBlipFace(string guid, string version);

        [OperationContract]
        BlipFaceVersion GetLatestVersion();
    }
}
