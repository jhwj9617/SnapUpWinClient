using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SnapUpWinClient
{
    public class WebHelperFunctions
    {
        public WebHelperFunctions() {}

        #if DEBUG
               public const string RootUrl = "http://localhost/MVCWebApp";
        #else
               public const string RootUrl = "http://snapup.apphb.com";
        #endif

        public Object JSONResponseToObject(WebResponse jsonResponse)
        {
            Stream stream = jsonResponse.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            String responseString = reader.ReadToEnd();
            return JsonConvert.DeserializeObject(responseString);
        }

        public bool CheckConnection()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.google.com/");
                request.Timeout = 10000;
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                response.Close();

                if (response.StatusCode == HttpStatusCode.OK) {
                    return true;
                } else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public string GetRootUrl()
        {
            return RootUrl;
        }
    }
}
