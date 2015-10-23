using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PDS_Client
{
    class JsonPaths
    {
        public JsonPaths(string username, string path)
        {
            this.path = path;
            this.name = name;
        }
        [JsonProperty("path")]
        public string path
        {
            get; set;
        }

        [JsonProperty("username")]
        public string name
        {
            get; set;
        }
    }
}
