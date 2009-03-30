using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace BlipFace.Service.Entities
{

    [CollectionDataContract]
	public class StatusesList : List<BlipStatus>
    {
    }
}
