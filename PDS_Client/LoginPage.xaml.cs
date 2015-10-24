using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace PDS_Client
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {

        Socket s = null;

        public Window1()
        {
            InitializeComponent();

            // todo: this shit does not work
            RegistryKey rk = Registry.CurrentUser.OpenSubKey ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.SetValue("PoliHub", Directory.GetCurrentDirectory() + "PDS_Client.exe"); // name , executablePath

            if (File.Exists("./polihub.settings"))
            {
                byte[] chifer = File.ReadAllBytes("./polihub.settings");
                string plain = Encoding.ASCII.GetString(ProtectedData.Unprotect(chifer, null, DataProtectionScope.CurrentUser));
                Debug.WriteLine(plain);
                string[] credentials = plain.Split('\n');
                if (credentials.Length == 3)
                {
                    try
                    {
                        createSocket();
                        if (doLogin(credentials[0], credentials[1]))
                        {
                            s.Close();
                            NetworkHandler.createInstance(credentials[0], credentials[1], credentials[2]);
                            MainWindow main = new MainWindow();
                            main.setCurrentDirectory(credentials[2]);
                            main.updateFolders();
                            main.Show();
                            main.sync();
                            this.Close();
                        }
                    }
                    catch (SocketException)
                    {
                        s = null;
                    }
                }
            }
            else
                Debug.WriteLine("polihub.settings does not exist");
        }


        public void createSocket()
        {
            
            try
            {
                s = new Socket(SocketType.Stream, ProtocolType.Tcp);
                IPAddress sAddr = IPAddress.Parse("127.0.0.1");
                s.Connect(sAddr, 7000);
                if (!s.Connected) throw new SocketException();
            }
            catch(SocketException se)
            {
                MessageBox.Show(se.Message);
                throw;
            }
            
        }

        private void btn_login_Click(object sender, RoutedEventArgs e)
        {
            if (s == null)
            {
                try {
                    createSocket(); // the socket is already connected
                }
                catch (SocketException)
                {
                    s = null;
                    return; // i already printed the error for the user. now i just do nothing 
                }
            }
            
            string username = ((TextBox)this.FindName("text_user")).Text;
            string password = ((PasswordBox)this.FindName("text_pass")).Password;

            if (!doLogin(username, password))
                return;



            // i am logged. so let's try to read the path from path.settings

            string path = null;
            if (File.Exists("./paths.settings"))
            {
                byte[] msg = File.ReadAllBytes("./paths.settings");
                List<JsonPaths> paths = JsonConvert.DeserializeObject<List<JsonPaths>>(Encoding.UTF8.GetString(msg));
                foreach (JsonPaths p in paths)
                {
                    Debug.WriteLine("username : " + p.name + " - path :" + p.path);
                    if (p.name.Equals(username))
                    {
                        path = p.path;
                        break;
                    }
                }
            }
            else
            {
                Debug.WriteLine("./path.settings non esiste");
            }

            if(path == null)
            {
                MessageBoxResult res = 
                    MessageBox.Show("Errore : la cartella dell'utente scelto è stata rimossa. Vuoi scegliere un nuovo percorso per essa?", "Attenzione", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if(res == MessageBoxResult.No)
                {
                    s.Close();
                    s = null;
                    return;
                }
                else
                {
                    System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        path = dialog.SelectedPath;
                        if(Directory.GetDirectories(path).Length != 0 || Directory.GetFiles(path).Length != 0)
                        {
                            path = null;
                            MessageBox.Show("Errore : la cartella seleziona è vuota.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                            s.Close();
                            s = null;
                            return;
                        }
                    }
                }
            }

            // if i am gere path must be not null
            if (path == null) throw new ArgumentNullException("path is null");
            // write it on the settings file
            string stats = username + "\n" + password + "\n" + path;
            byte[] protectedData = ProtectedData.Protect(Encoding.ASCII.GetBytes(stats), null, DataProtectionScope.CurrentUser);
            FileStream str = File.OpenWrite("./polihub.settings");
            str.Write(protectedData, 0, protectedData.Length);
            str.Close();


            s.Close();
            MainWindow main = new MainWindow();
            NetworkHandler.createInstance(username, password, path);
            main.setCurrentDirectory(path);
            main.updateFolders();
            main.Show();
            main.sync();
            this.Close();

        }

        private void mouse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_x_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_register_Click(object sender, RoutedEventArgs e)
        {
            Registration reg = new Registration();
            reg.Show();
            this.Close();
        }

        private bool doLogin(string username, string password)
        {
            s.Send(BitConverter.GetBytes((int)messages.LOGIN), SocketFlags.None); // LOGIN

            s.Send(Encoding.ASCII.GetBytes(username));
            byte[] buffer = new byte[255];
            s.Receive(buffer);
            if (Encoding.ASCII.GetString(buffer).Contains("ERR"))
            {
                MessageBox.Show("Errore: username non ricevuto correttamente", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            s.Send(Encoding.ASCII.GetBytes(password));
            s.Receive(buffer);
            string message = Encoding.ASCII.GetString(buffer);
            if (message.Contains("OK"))
                return true;
            else
            {
                MessageBox.Show("Errore: credenziali errate", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

        }
    }
}
