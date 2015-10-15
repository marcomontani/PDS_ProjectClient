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
        Queue<Action> functions;
        Mutex fsemaphore, d_semaphore;
        volatile Boolean die;



        private NetworkHandler(Socket cs)
        {
            die = false;
            fsemaphore = new Mutex();
            d_semaphore = new Mutex();
            s = cs;
            threads = new Thread[1];
            functions = new Queue<Action>();
            for (int i = 0; i < 1; i++)
            {
                threads[i] = new Thread(() => {
                    Monitor.Enter(d_semaphore);
                    Boolean value = die;
                    Monitor.Exit(d_semaphore);


                    while (!value)
                    {
                        Monitor.Enter(fsemaphore);
                        while (functions.Count == 0) Monitor.Wait(fsemaphore);
                        Action f = functions.Dequeue();
                        Monitor.Exit(fsemaphore);
                        f();

                        Monitor.Enter(d_semaphore);
                        value = die;
                        Monitor.Exit(d_semaphore);
                    }

                });
                threads[i].Start();
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
        public static void deleteInstance()
        {
            This = null;
        }

        public void addFunction(Action f)
        {
            Monitor.Enter(fsemaphore);
            functions.Enqueue(f);
            Monitor.PulseAll(fsemaphore);
            Monitor.Exit(fsemaphore);
        }

        public void killWorkers()
        {
            Monitor.Enter(d_semaphore);
            die = true;
            Monitor.Exit(d_semaphore);
        }



    }
}
