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
using System.ComponentModel;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Input;

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
            hubProxy.On("BusUpdate", (string code) =>
                // Context is a reference to SynchronizationContext.Current
                syncContext.Post(delegate
                {
                    Debug.Write("Notified! New asset in bus: " + code + ".\n");
                    QueryBus(code);
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
            } catch (FileNotFoundException ex)
            {
                // BusDestinationList.xml not created yet. Create empty list.
                busDestinationList = new BusDestinationList();
            }

            // Find default download location
            string myPicturesLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            Application.Current.Properties["myPicturesLocation"] = myPicturesLocation;
            Application.Current.Properties["defaultDownloadLocation"] = Path.Combine(myPicturesLocation, "SnapUp");

            InitializeComponent();

            RenderBusDestinations();
        }

        private void QueryBus(string code)
        {
            string assetURL = String.Empty;
            using (var webClient = new WebClient())
            {
                assetURL = webClient.DownloadString("http://localhost/MVCWebApp/Buses/QueryBus?code=" + code);
            }
            BusDestination busDest = this.FindBusDestination(code);
            Debug.Write("Received URL: " + assetURL + "\n");
            string downloadLocation = busDest.downloadLocation != null ? busDest.downloadLocation : (String)Application.Current.Properties["myPicturesLocation"];
            string localFilename = Path.Combine(downloadLocation, @"SnapUp\newFile.jpg");
            new FileInfo(localFilename).Directory.Create();
            using (WebClient client = new WebClient())
            {
                client.DownloadFileCompleted += new AsyncCompletedEventHandler((sender, e) => DownloadCompleted(sender, e, busDest));
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) => ProgressChanged(sender, e, busDest));
                client.DownloadFileAsync(new Uri(assetURL), localFilename, (String) Application.Current.Properties["defaultDownloadLocation"]);
            }
            
        }

        private void OpenBusProperties()
        {
            BusDestination busDest = FindSelectedBusDestination();
            BusPropertiesWindow BusPropertiesWindow = new BusPropertiesWindow(busDest);
            BusPropertiesWindow.Owner = this;
            BusPropertiesWindow.ShowDialog();
            if (BusPropertiesWindow.GetPropertiesChanged())
            {
                serializer = new XmlSerializer(typeof(BusDestinationList), busDestinationTypes);
                FileStream fs = new FileStream("BusDestinationList.xml", FileMode.Create);
                serializer.Serialize(fs, this.busDestinationList);
                fs.Close();

                Label rowDownloadLoc = (Label)LogicalTreeHelper.FindLogicalNode(StackPanel, "DownloadLoc" + busDest.code);
                Hyperlink newHyperlink = (Hyperlink)rowDownloadLoc.Content;
                newHyperlink.NavigateUri = busDest.downloadLocation != null ?
                    new Uri(busDest.downloadLocation) :
                    new Uri((String)Application.Current.Properties["defaultDownloadLocation"]);
            }
        }

        // ** RENDERING ** //
        private void RenderBusDestination(BusDestination newBusDest)
        {
            Grid rowGrid = RenderRow(newBusDest);
            rowGrid.MouseDown += RowGridMouseDown; // Add event mousedown
        }

        private void RenderBusDestinations()
        {
            foreach (BusDestination bd in busDestinationList.busDestinations)
            {
                RenderBusDestination(bd);
            }
        }

        private Grid RenderRow(BusDestination bd)
        {
            this.rowCount += 1;
            Grid rowGrid = new Grid();
            rowGrid.Name = "_" + bd.code;
            rowGrid.Height = 50;
            rowGrid.Background = Brushes.Transparent;

            // ContextMenu
            MenuItem busPropertiesItem = new MenuItem();
            busPropertiesItem.Header = "Bus Properties";
            busPropertiesItem.Click += this.EditBus_Click;
            MenuItem deleteBusItem = new MenuItem();
            deleteBusItem.Header = "Delete Bus";
            deleteBusItem.Click += this.DeleteBus_Click;
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.Items.Add(busPropertiesItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteBusItem);
            rowGrid.ContextMenu = contextMenu;

            // RowLabel
            Label rowLabel = new Label();
            rowLabel.Name = "BusNameLabel" + bd.code;
            rowLabel.Content = "Bus Name: " + bd.busName + " | " + bd.code;
            rowLabel.Width = 180;
            rowLabel.VerticalAlignment = VerticalAlignment.Center;
            rowLabel.HorizontalAlignment = HorizontalAlignment.Left;
            rowLabel.Margin = new Thickness(10, 0, 0, 0);

            // rowDownloadLoc
            Hyperlink hyperlink = new Hyperlink();
            hyperlink.NavigateUri = bd.downloadLocation != null ?
                new Uri(bd.downloadLocation) :
                new Uri((String)Application.Current.Properties["defaultDownloadLocation"]);
            hyperlink.Inlines.Add(new Run("Open File Location"));
            hyperlink.RequestNavigate += OpenFileLocation_RequestNavigate;
            Label rowDownloadLoc = new Label();
            rowDownloadLoc.Name = "DownloadLoc" + bd.code;
            rowDownloadLoc.Content = hyperlink;
            rowDownloadLoc.VerticalAlignment = VerticalAlignment.Center;
            rowDownloadLoc.Margin = new Thickness(200, 0, 0, 0);

            // rowProgressBar
            ProgressBar rowProgressBar = new ProgressBar();
            rowProgressBar.Name = "ProgressBar" + bd.code;
            rowProgressBar.Width = 160;
            rowProgressBar.Height = 16;
            rowProgressBar.VerticalAlignment = VerticalAlignment.Center;
            rowProgressBar.Margin = new Thickness(340, 0, 0, 0);

            rowGrid.Children.Add(rowLabel);
            rowGrid.Children.Add(rowDownloadLoc);
            rowGrid.Children.Add(rowProgressBar);
            StackPanel.Height = 50 * this.rowCount;
            StackPanel.Children.Add(rowGrid);

            return rowGrid;
        }

        private void RemoveBusDestination(BusDestination busDest)
        {
            Grid rowGrid = (Grid)LogicalTreeHelper.FindLogicalNode(StackPanel, "_" + busDest.code);
            StackPanel.Children.Remove(rowGrid);
        }

        // ** USER CONTROL EVENTS ** //
        private void RowGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                OpenBusProperties();
            }
            Grid rowGrid = (Grid)sender;
            if (!rowGrid.Equals(this.selectedRowGrid))
            {
                if (this.selectedRowGrid != null)
                {
                    this.selectedRowGrid.Background = Brushes.Transparent;
                }
                rowGrid.Background = Brushes.Beige;
                this.selectedRowGrid = rowGrid;
                this.BusProperties.IsEnabled = true;
                this.DeleteBus.IsEnabled = true;
                Debug.Write("Selected Row Code: " + GetSelectedRowCode() + "\n");
            }
        }

        private void AddBus_Click(object sender, RoutedEventArgs e)
        {
            BusDestination newBusDest = new BusDestination();
            AddBusWindow AddBusWindow = new AddBusWindow(newBusDest);
            AddBusWindow.Owner = this;
            AddBusWindow.ShowDialog();
            Debug.Write("Received BusDestination from 'Add Bus' window. Code is: " + newBusDest.code + "\n");

            if (newBusDest.code != null)
            {
                this.busDestinationList.AddBusDestination(newBusDest);
                serializer = new XmlSerializer(typeof(BusDestinationList), busDestinationTypes);
                FileStream fs = new FileStream("BusDestinationList.xml", FileMode.Create);
                serializer.Serialize(fs, this.busDestinationList);
                fs.Close();
                this.RenderBusDestination(newBusDest);
            }
        }

        private void EditBus_Click(object sender, RoutedEventArgs e)
        {
            OpenBusProperties();
        }

        private void DeleteBus_Click(object sender, RoutedEventArgs e)
        {
            BusDestination busDest = FindSelectedBusDestination();
            DeleteBusWindow deleteBusWindow = new DeleteBusWindow(busDest);
            deleteBusWindow.Owner = this;
            deleteBusWindow.ShowDialog();
            if (deleteBusWindow.GetMessageBoxResult())
            {
                this.busDestinationList.busDestinations.Remove(busDest);
                serializer = new XmlSerializer(typeof(BusDestinationList), busDestinationTypes);
                FileStream fs = new FileStream("BusDestinationList.xml", FileMode.Create);
                serializer.Serialize(fs, this.busDestinationList);
                fs.Close();
                this.RemoveBusDestination(busDest);
            }
        }

        private void OpenFileLocation_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e, BusDestination busDest)
        {
            string downloadDestination = (String) e.UserState;
            if (busDest.openFolder == true)
            {
                Process.Start(downloadDestination);
            }
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e, BusDestination busDest)
        {
            ProgressBar pb = (ProgressBar) LogicalTreeHelper.FindLogicalNode(StackPanel, "ProgressBar" + busDest.code);
            pb.Value = e.ProgressPercentage;
        }

        // ** HELPER FUNCTIONS ** //
        private BusDestination FindBusDestination(string code)
        {
            return this.busDestinationList.busDestinations.Find(bd => bd.code == code);
        }

        private BusDestination FindSelectedBusDestination()
        {
            return FindBusDestination(this.GetSelectedRowCode());
        }

        private String GetSelectedRowCode()
        {
            string code = this.selectedRowGrid.Name;
            // remove the underscore e.g. _1234abcd to 1234abcd
            return code.Substring(1, code.Length - 1);
        }
    }
}
