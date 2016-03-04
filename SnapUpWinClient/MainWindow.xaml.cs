using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;
using System.Threading;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Windows.Media;

namespace SnapUpWinClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BusDestinationList busDestinationList;
        Type[] busDestinationTypes = { typeof(BusDestination) };
        XmlSerializer serializer;

        public int rowCount = 0;
        public Grid selectedRowGrid;

        public MainWindow()
        {
            // PCId = 1
            // Code = 1234abcd
            // Initiate connection, establish notifications
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
                    QueryBus();
                }, null)
            );

            // Start connection
            hubConnection.Start().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    Debug.Write("There was an error opening the connection\n");
                }
                else {
                    Debug.Write("Connected to " + "http://localhost/MVCWebApp/" + "\n");
                }
            }).Wait();

            // Deserialize BusDestinations
            try
            {
                FileStream fs = new FileStream("BusDestinationList.xml", FileMode.Open);
                serializer = new XmlSerializer(typeof(BusDestinationList), busDestinationTypes);
                this.busDestinationList = (BusDestinationList)serializer.Deserialize(fs);
                fs.Close();
                rowCount = this.busDestinationList.busDestinations.Count;
            } catch (FileNotFoundException ex)
            {
                // BusDestinationList.xml not created yet. Create empty list.
                busDestinationList = new BusDestinationList();
            }

            InitializeComponent();

            RenderBusDestinations();
        }

        private void RowGridMouseDown(object sender, RoutedEventArgs e)
        {
            Grid rowGrid = (Grid) sender;
            if (rowGrid.Equals(this.selectedRowGrid))
            {
                rowGrid.Background = Brushes.Transparent;
                this.selectedRowGrid = null;
            } else
            {
                if (this.selectedRowGrid != null)
                {
                    this.selectedRowGrid.Background = Brushes.Transparent;
                }
                rowGrid.Background = Brushes.Beige;
                this.selectedRowGrid = rowGrid;
                Debug.Write("Selected Row Code: " + GetSelectedRowCode() + "\n");
            }
        }

        private String GetSelectedRowCode()
        {
            String identifier = "";
            TextBlock rowIdentifier;
            foreach (UIElement child in this.selectedRowGrid.Children)
            {
                if (child.GetType() == typeof(TextBlock))
                {
                    rowIdentifier = (TextBlock)child;
                    identifier = rowIdentifier.Text;
                }
            }
            return identifier;
        } 

        private void RenderBusDestinations()
        {
            int i = 0;
            foreach (BusDestination bd in busDestinationList.busDestinations)
            {
                Grid rowGrid = RenderRow(bd, i);
                rowGrid.MouseLeftButtonDown += RowGridMouseDown; // Add event mousedown
                i++;
            }

        }

        private Grid RenderRow(BusDestination bd, int i)
        {
            Grid rowGrid = new Grid();
            rowGrid.Height = 50;
            rowGrid.Background = Brushes.Transparent;

            // RowLabel
            Label rowLabel = new Label();
            rowLabel.Name = "BusNameLabel" + i.ToString();
            rowLabel.Content = "Bus Name: " + bd.busName + " | " + bd.code;
            rowLabel.VerticalAlignment = VerticalAlignment.Center;
            rowLabel.Margin = new Thickness(10, 0, 0, 0);

            // rowDownloadLoc
            Label rowDownloadLoc = new Label();
            rowDownloadLoc.Name = "DownloadLoc" + i.ToString();
            rowDownloadLoc.Content = "Open Download Location";
            rowDownloadLoc.VerticalAlignment = VerticalAlignment.Center;
            rowDownloadLoc.Margin = new Thickness(200, 0, 0, 0);

            // rowProgressBar
            ProgressBar rowProgressBar = new ProgressBar();
            rowProgressBar.Name = "ProgressBar" + i.ToString();
            rowProgressBar.Width = 160;
            rowProgressBar.Height = 16;
            rowProgressBar.VerticalAlignment = VerticalAlignment.Center;
            rowProgressBar.Margin = new Thickness(340, 0, 0, 0);

            // rowIdentifier
            TextBlock rowIdentifier = new TextBlock();
            rowIdentifier.Name = "RowIdentifier" + i.ToString();
            rowIdentifier.Text = bd.code;
            rowIdentifier.Visibility = Visibility.Hidden;

            rowGrid.Children.Add(rowLabel);
            rowGrid.Children.Add(rowDownloadLoc);
            rowGrid.Children.Add(rowProgressBar);
            rowGrid.Children.Add(rowIdentifier);
            StackPanel.Height = 50 * this.rowCount;
            StackPanel.Children.Add(rowGrid);

            return rowGrid;
        }

        private void QueryBus()
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

        private void AddBus_Click(object sender, RoutedEventArgs e)
        {
            BusDestination newBusDest = new BusDestination();
            Window AddBusWindow = new AddBusWindow(newBusDest);
            AddBusWindow.Owner = this;
            AddBusWindow.ShowDialog();
            Debug.Write("Received BusDestination from 'Add Bus' window. Code is: " + newBusDest.code + "\n");

            if (newBusDest.code != null)
            {
                this.busDestinationList.AddBusDestination(newBusDest);
                serializer = new XmlSerializer(typeof(BusDestinationList), busDestinationTypes);
                FileStream fs = new FileStream("BusDestinationList.xml", FileMode.Create);
                serializer.Serialize(fs, busDestinationList);
                fs.Close();
            }

        }
    }
}
