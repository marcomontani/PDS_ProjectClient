using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;

namespace PDS_Client
{

    /*
        This class is the one that uses the internet connection to comunicate with the server. 
        By using this we can make sure the app can create a limitated number of thread so it does not make too many request to the server
    */
    class NetworkHandler
    {
        Thread[] threads;
        Socket s;
        static NetworkHandler This = null;
        
        private NetworkHandler(Socket cs)
        {
            s = cs;
            threads = new Thread[3];
            for(int i = 0; i < 3; i++)
            {
                threads[i] = new Thread(() => {
                    // todo: make the thread code here
                });
            }
        }


        public static void createInstance(Socket cs)
        {
            if (This == null)  This = new NetworkHandler(cs);
        }

        // Implementation of NetworkHandler as singleton object
        public static NetworkHandler getInstance()
        {
            return This;
        }





    }
}
