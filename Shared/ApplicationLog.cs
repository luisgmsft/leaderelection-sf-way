using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    [DataContract]
    public class ApplicationLog
    {
        [DataMember]
        public int Total { get; set; }
    }
}
