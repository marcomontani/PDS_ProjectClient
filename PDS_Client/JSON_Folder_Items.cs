using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDS_Client
{
    class JSON_Folder_Items
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

        [JsonProperty("checksum")]
        public string checksum
        {
            get; set;
        }

        public static bool operator ==(JSON_Folder_Items a, JSON_Folder_Items b)
        {
            if(a.name == b.name && a.path == b.path) return true;
            return false;
        }
        public static bool operator !=(JSON_Folder_Items a, JSON_Folder_Items b)
        {
            return !(a== b);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            return ((JSON_Folder_Items)obj) == this;
        }
    }
}
