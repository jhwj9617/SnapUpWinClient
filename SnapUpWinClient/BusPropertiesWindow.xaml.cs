using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SnapUpWinClient
{
    /// <summary>
    /// Interaction logic for BusPropertiesWindow.xaml
    /// </summary>
    public partial class BusPropertiesWindow : Window
    {
        private BusDestination busDestination;

        private bool propertiesChanged = false;
        private string originalDownloadLoc;
        private bool originalOpenFolder;

        public BusPropertiesWindow(BusDestination busDestination)
        {
            this.busDestination = busDestination;
            InitializeComponent();

            this.BusNameContent.Text = busDestination.busName;
            this.CodeContent.Text = busDestination.code;
            this.originalDownloadLoc = busDestination.downloadLocation == null ?
                (String)Application.Current.Properties["defaultDownloadLocation"] :
                busDestination.downloadLocation;
            this.DownloadLocationContent.Text = this.originalDownloadLoc;

            this.originalOpenFolder = busDestination.openFolder;
            this.AutoOpenFolderCheckBox.IsChecked = busDestination.openFolder;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (this.originalDownloadLoc != DownloadLocationContent.Text)
            {
                this.busDestination.downloadLocation = DownloadLocationContent.Text;
                this.propertiesChanged = true;
            }
            if (this.originalOpenFolder != AutoOpenFolderCheckBox.IsChecked.Value)
            {
                this.busDestination.openFolder = AutoOpenFolderCheckBox.IsChecked.Value;
                this.propertiesChanged = true;
            }
            this.Close();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(this.originalDownloadLoc));
            e.Handled = true;
        }

        private void DownloadLocationChange_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Select a Download Location";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = this.originalDownloadLoc;
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = this.originalDownloadLoc;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.DownloadLocationContent.Text = dlg.FileName;
            }
        }

        public bool GetPropertiesChanged()
        {
            return this.propertiesChanged;
        }
    }
}
