using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace SnapUpWinClient
{
    public class BusDestination
    {
        public String code;
        public String busName;
        public String downloadLocation;
        public bool openFolder;

        public BusDestination()
        {
            // DownloadLocation is initially default
            this.downloadLocation = (String) Application.Current.Properties["myPicturesLocation"] + @"\SnapUp";
        }
    }
}
