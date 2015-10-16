using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace PDS_Client
{
    class JSONDeletedFile
    {
        [JsonProperty("path")]
        public string path
        {
            get; set;
        }

        [JsonProperty("name")]
        public string name
        {
            get; set;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            return ((JSONDeletedFile)obj) == this;
        }
    }
}
