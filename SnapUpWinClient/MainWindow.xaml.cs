using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;
using System.Threading;

namespace SnapUpWinClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // PCId = 1
            // Code = 1234abcd

            var querystringData = new Dictionary<string, string>();
            querystringData.Add("PCId", "1"); // PCId is "1"
            var hubConnection = new HubConnection("http://localhost/MVCWebApp/", querystringData);
            IHubProxy hubProxy = hubConnection.CreateHubProxy("SnapUpServer");
            var syncContext = SynchronizationContext.Current;
            hubProxy.On("notify", () =>
                // Context is a reference to SynchronizationContext.Current
                syncContext.Post(delegate
                {
                    Debug.Write("Notified!\n");
                    queryBus();
                }, null)
            );

            hubConnection.Start().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    Debug.Write("There was an error opening the connection\n");
                }
                else {
                    Debug.Write("Connected to " + "http://localhost/MVCWebApp/" + "\n");
                }
            }).Wait();


            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            queryBus();
        }

        private void queryBus()
        {
            var assetURL = string.Empty;
            using (var webClient = new WebClient())
            {
                assetURL = webClient.DownloadString("http://localhost/MVCWebApp/Buses/QueryBus?code=1234abcd");
            }
            Debug.Write("Received URL: " + assetURL + "\n");
            string locationMyPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string downloadDestination = Path.Combine(locationMyPictures, "SnapUp");
            string localFilename = Path.Combine(locationMyPictures, @"SnapUp\newFile.jpg");
            new FileInfo(localFilename).Directory.Create();
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(assetURL, localFilename);
            }
            Process.Start(downloadDestination);
        }
    }
}
