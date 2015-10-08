using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDS_Client
{
    class JSONVersion
    {
        [JsonProperty("lastModified")]
        public string date
        {
            get; set;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null) return false;
            try
            {
                return ((JSONVersion)obj).date.Equals(this.date);
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }
    }

    



}
