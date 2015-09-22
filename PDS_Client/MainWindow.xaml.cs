﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Text;

namespace PDS_Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// 


    struct queueObject
    {
        public string file;
        public WatcherChangeTypes type;
    }


    public partial class MainWindow : Window
    {
        Queue<queueObject> eventsArray = new Queue<queueObject>();
        BindingList<FileSystemElement> currentWorkDirectory = new BindingList<FileSystemElement>();
        Socket s;
        
        public MainWindow()
        {
            InitializeComponent();
            syncFolder();
            watchFolder();
            
            addCurrentFoderInfo();

            this.DataContext = currentWorkDirectory;
        }

        private void syncFolder()
        {
            s.Send(BitConverter.GetBytes(8)); 
            byte[] buffer = new byte[1024];
            
            s.Receive(buffer, 1024, SocketFlags.None);
            string serverFolderDescription = Encoding.ASCII.GetString(buffer);
            // now in the string we have the JSON string description. it is "folder: [{"path":"...", "name":"......"}]"

            List<JSON_Folder_Items> items = JsonConvert.DeserializeObject<List<JSON_Folder_Items>>(serverFolderDescription);
            // todo : check the filenames and compare them with the selected folder. 
            // todo: add date of last update and compare it with date of last modify

        }

        public void setSocket(Socket sock)
        {
            if (!sock.Connected) throw new SocketException();
            s = sock;
        }

        private void watchFolder()
        {
            FileSystemWatcher fs = new FileSystemWatcher("C:\\Users\\Gaetano\\Documents\\Malnati");
            fs.Changed += new FileSystemEventHandler(OnChanged);
            fs.NotifyFilter = NotifyFilters.LastWrite;
            fs.EnableRaisingEvents = true;

        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            MessageBox.Show("File: " + e.FullPath + " " + e.ChangeType);
            queueObject q; q.file = e.FullPath; q.type = e.ChangeType;
            if (!this.eventsArray.Contains(q)) eventsArray.Enqueue(q);
            
        }


        private void addCurrentFoderInfo()
        {
            StackPanel g = (StackPanel)this.FindName("fs_grid");
            StackPanel Malnati = new StackPanel();
            Label labmal = new Label();
            labmal.Content = "MALNATI";
            Malnati.Orientation = Orientation.Horizontal;
            Malnati.Children.Add(labmal);

            Label labmal2 = new Label();
            labmal2.Content = "FILE";            
            Malnati.Children.Add(labmal2);


            g.Children.Add(Malnati);


            StackPanel Cabodi = new StackPanel();
            Label labcab = new Label();
            labcab.Content = "CABODI";
            Cabodi.Orientation = Orientation.Horizontal;
            Cabodi.Children.Add(labcab);
            Label labcab2 = new Label();
            labcab2.Content = "FILE";
            Cabodi.Children.Add(labcab2);


            g.Children.Add(Cabodi);



        }

    }
}
