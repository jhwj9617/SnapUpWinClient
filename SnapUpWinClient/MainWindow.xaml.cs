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
using System.Configuration;
using Newtonsoft.Json;
using Hardcodet.Wpf.TaskbarNotification;

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
        TaskbarIcon taskbarIcon;

        public int rowCount = 0;
        public Grid selectedRowGrid;

        public MainWindow()
        {
            InitializeClientId();
            // Initiate connection, establish notifications
            var querystringData = new Dictionary<string, string>();
            querystringData.Add("PCId", (String) Application.Current.Properties["PCId"]);
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

            // Get TaskbarIcon
            InitializeTaskbarIcon();
            RenderBusDestinations();
        }

        private void InitializeClientId()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            int PCId = Int32.Parse(config.AppSettings.Settings["PCId"].Value);
            if (PCId == -1)
            {
                Debug.Write("NO CLIENT ID: REQUESTING NEW ID");
                // Wipe BusDestinationList
                serializer = new XmlSerializer(typeof(BusDestinationList), busDestinationTypes);
                FileStream fs = new FileStream("BusDestinationList.xml", FileMode.Create);
                serializer.Serialize(fs, new BusDestinationList());
                fs.Close();
                string requestResponse = String.Empty;
                this.IsEnabled = false;
                using (var webClient = new WebClient())
                {
                    requestResponse = webClient.DownloadString("http://localhost/MVCWebApp/PersonalComputers/Create");
                }
                this.IsEnabled = true;
                config.AppSettings.Settings["PCId"].Value = requestResponse;
                config.Save(ConfigurationSaveMode.Modified);
                Application.Current.Properties["PCId"] = requestResponse;
            } else
            {
                Application.Current.Properties["PCId"] = PCId.ToString();
            }
        }

        private void InitializeTaskbarIcon()
        {
            taskbarIcon = (TaskbarIcon)FindResource("TaskbarIcon");
            taskbarIcon.TrayLeftMouseDown += TaskbarIcon_Click;
            MenuItem exitApp = new MenuItem();
            exitApp.Header = "Exit";
            exitApp.Click += this.ExitApp;
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.Items.Add(exitApp);
            taskbarIcon.ContextMenu = contextMenu;
            taskbarIcon.Visibility = Visibility.Visible;
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            taskbarIcon.Dispose();
            Application.Current.Shutdown();
        }

        // Prevent application from closing
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void QueryBus(string code)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create("http://localhost/MVCWebApp/Buses/QueryBus?code=" + code);
            string assetURL = String.Empty;
            string assetName = String.Empty;
            using (WebResponse jsonResponse = request.GetResponse())
            {
                StreamReader streamReader = new StreamReader(jsonResponse.GetResponseStream());
                String responseData = streamReader.ReadToEnd();
                QueryBusResponse jsonData = JsonConvert.DeserializeObject<QueryBusResponse>(responseData);
                // process your data
                assetURL = jsonData.assetUrl;
                assetName = jsonData.assetName;
            }
            BusDestination busDest = this.FindBusDestination(code);
            Debug.Write("Received URL: " + assetURL + "\n");
            string downloadLocation = busDest.downloadLocation;
            string localFilename = Path.Combine(downloadLocation, assetName);
            new FileInfo(localFilename).Directory.Create();
            using (WebClient client = new WebClient())
            {
                taskbarIcon.CloseBalloon();
                taskbarIcon.ShowBalloonTip("Downloading...", assetName, BalloonIcon.None);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler((sender, e) => DownloadCompleted(sender, e, busDest, assetName));
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
                newHyperlink.NavigateUri = new Uri(busDest.downloadLocation);
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
            hyperlink.NavigateUri = new Uri(bd.downloadLocation);
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
                this.selectedRowGrid = null;
                this.BusProperties.IsEnabled = false;
                this.DeleteBus.IsEnabled = false;
            }
        }

        private void OpenFileLocation_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e, BusDestination busDest, string assetName)
        {
            taskbarIcon.CloseBalloon();
            taskbarIcon.ShowBalloonTip("Download Complete", "Successfully downloaded " + assetName, BalloonIcon.None);
            taskbarIcon.TrayBalloonTipClicked += new RoutedEventHandler((sender1, e1) => taskbarIcon_TrayBalloonTipClicked(sender1, e1, busDest.downloadLocation));
            if (busDest.openFolder == true)
            {
                Process.Start(busDest.downloadLocation);
            }
        }

        private void taskbarIcon_TrayBalloonTipClicked(object sender, RoutedEventArgs e, string downloadLocation)
        {
            Process.Start(downloadLocation);
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e, BusDestination busDest)
        {
            ProgressBar pb = (ProgressBar) LogicalTreeHelper.FindLogicalNode(StackPanel, "ProgressBar" + busDest.code);
            pb.Value = e.ProgressPercentage;
        }

        private void TaskbarIcon_Click(object sender, EventArgs e)
        {
            this.Show();
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
