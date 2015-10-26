using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net;
using System.Windows;

namespace PDS_Client
{

    /*
        This class is the one that uses the internet connection to comunicate with the server. 
        By using this we can make sure the app can create a limitated number of thread so it does not make too many request to the server
    */
    class NetworkHandler
    {
        Thread[] threads;
        static NetworkHandler This = null;
        Queue<Action<Socket>> functions;
        Mutex fsemaphore, d_semaphore;
        volatile Boolean die;

        private NetworkHandler(string username, string password, string path)
        {
            die = false;
            fsemaphore = new Mutex();
            d_semaphore = new Mutex();
            threads = new Thread[1];
            functions = new Queue<Action<Socket>>();
            for (int i = 0; i < 1; i++)
            {
                threads[i] = new Thread(() => {
                    Monitor.Enter(d_semaphore);
                    Boolean value = die;
                    Monitor.Exit(d_semaphore);


                    // connect to the server
                    Socket s;
                    try
                    {
                        s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        s.Connect("127.0.0.1", 7000);

                    }
                    catch (SocketException se)
                    {
                        MessageBox.Show("Errore Nella connessione al server: codice" + se.ErrorCode, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                        // this should never happen because i just checked in the login the fact that it's all ok
                    }
                    logIn(s, username, password);
                    if (path.Length != 0) sendFolder(s, path);

                    while (!value)
                    {
                        Monitor.Enter(fsemaphore);
                        while (functions.Count == 0)
                        {
                            Monitor.Wait(fsemaphore);
                            // i am awake. do i still need to live?
                            Debug.Write("This thread has been awaken ");
                            Monitor.Enter(d_semaphore);
                            value = die;
                            Monitor.Exit(d_semaphore);
                            if (value)
                            {
                                Debug.WriteLine("and has to die");
                                return;
                            }

                        }
                        Action<Socket> f = functions.Dequeue();
                        Monitor.Exit(fsemaphore);
                        try {
                            f(s);
                        }
                        catch(SocketException se)
                        {
                            // this means an error on the network.
                            return; // i make the thread die
                        }

                        Monitor.Enter(d_semaphore);
                        value = die;
                        Monitor.Exit(d_semaphore);
                    }

                });
                threads[i].Start();
            }

        }


        public static void createInstance(string username, string password, string path)
        {
            if (This == null) This = new NetworkHandler(username, password, path);
        }

        public static void createInstance(string username, string password)
        {
            if (This == null)  This = new NetworkHandler(username, password, "");
            
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

        public void addFunction(Action<Socket> f)
        {
            Monitor.Enter(fsemaphore);
            functions.Enqueue(f);
            Monitor.PulseAll(fsemaphore);
            Monitor.Exit(fsemaphore);
        }

        public void killWorkers()
        {
            Debug.WriteLine("killing all the workers");
            Monitor.Enter(d_semaphore);
            die = true;
            Debug.WriteLine("told them to die");
            Monitor.Exit(d_semaphore);

            Monitor.Enter(fsemaphore);
            Monitor.PulseAll(fsemaphore);
            Debug.WriteLine("should awake them now");
            Monitor.Exit(fsemaphore);

        }

        private bool logIn(Socket s, string username, string password)
        {
            int inviati = s.Send(BitConverter.GetBytes(0)); // LOG IN

            string message = username;
            s.Send(Encoding.UTF8.GetBytes(message));

            byte[] buffer = new byte[5];
            s.Receive(buffer);
            message = Encoding.ASCII.GetString(buffer);
            if (message.Contains("ERR"))
            {
                MessageBox.Show("Errore nell'username", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            message = password;

            s.Send(Encoding.UTF8.GetBytes(message));

            s.Receive(buffer);
            message = Encoding.ASCII.GetString(buffer);
            if (message.Contains("ERR"))
            {
                MessageBox.Show("Impossibile completare il login", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // if i am here i am logged!
            return true;
        }

        private void sendFolder(Socket s, string folder)
        {    
            s.Send(BitConverter.GetBytes((int)messages.SEND_PATH));
            s.Send(Encoding.UTF8.GetBytes(folder));   
        }

    }
}
