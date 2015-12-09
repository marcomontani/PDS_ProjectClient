using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PDS_Client
{

    struct element
    {
        public String path;
        public String version;
        public element(String path, String version)
        {
            this.path = path;
            this.version = version;
        }
        
    };

    class DownloadManager
    {
        private static HashSet<element> downloadingFiles = null;
        private static Mutex m;

        public static void init()
        {
            downloadingFiles = new HashSet<element>();
        }

        public static bool isDownloading(String path)
        {
            if (downloadingFiles == null) throw new NullReferenceException();
            Monitor.Enter(m);
            for(int i = 0; i < downloadingFiles.Count; i++)
                if (((element)downloadingFiles.ElementAt(i)).path.Equals(path))
                {
                    Monitor.Exit(m);
                    return true;
                }

            Monitor.Exit(m);
            return false;  
        }



        public static void addFile(String path, String version)
        {
            if (downloadingFiles == null) throw new NullReferenceException();
            Monitor.Enter(m);
            downloadingFiles.Add(new element(path, version));
            Monitor.Exit(m);
        }


        public String getVersionDowloading(String path)
        {
            if (downloadingFiles == null) throw new NullReferenceException();
            Monitor.Enter(m);
            for(int i = 0; i < downloadingFiles.Count; i++)
            {
                element e = downloadingFiles.ElementAt(i);
                if (e.path.Equals(path))
                {
                    Monitor.Exit(m);
                    return e.version;
                }
            }
            Monitor.Exit(m);
            return null;
        }

        public static void removeFile(String path)
        {
            if (downloadingFiles == null) throw new NullReferenceException();
            Monitor.Enter(m);
            for(int i = 0; i < downloadingFiles.Count; i++)
            {
                element e = downloadingFiles.ElementAt(i);
                if (e.path.Equals(path)) downloadingFiles.Remove(e);
            }
            Monitor.Exit(m);
        }
        
    }
}
