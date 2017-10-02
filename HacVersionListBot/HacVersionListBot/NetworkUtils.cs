using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using HacVersionListBot.Properties;


namespace HacVersionListBot
{
    class NetworkUtils
    {
        private static X509Certificate ShopNCert = new X509Certificate(Resources.ShopN, Resources.ShopN_Password);
        private static X509Certificate ConsoleCert = new X509Certificate(Resources.ConsoleCert, Resources.ConsoleCert_Password);

        public static byte[] TryDownload(string file)
        {
            try
            {
                return new WebClient().DownloadData(file);
            }
            catch (WebException)
            {
                Program.Log($"Failed to download {file}.");
                return null;
            }
        }

        public static byte[] TryCertifiedDownload(string file, X509Certificate x509, string userAgent = "")
        {
            try
            {
                return new CertificateWebClient(x509, userAgent).DownloadData(file);
            }
            catch (WebException)
            {
                Program.Log($"Failed to download {file}.");
                return null;
            }
        }

        public static byte[] TryConsoleDownload(string file)
        {
            return TryCertifiedDownload(file, ConsoleCert);
        }

        public static string TryMakeCertifiedRequest(string URL, X509Certificate clientCert, bool json = true, string userAgent = "")
        {
            var wr = WebRequest.Create(new Uri(URL)) as HttpWebRequest;
            wr.UserAgent = userAgent;
            if (json)
                wr.Accept = "application/json";
            wr.Method = WebRequestMethods.Http.Get;
            wr.ClientCertificates.Clear();
            wr.ClientCertificates.Add(clientCert);
            try
            {
                using (var resp = wr.GetResponse() as HttpWebResponse)
                {
                   return new StreamReader(resp.GetResponseStream()).ReadToEnd().Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                }
            }
            catch (WebException ex)
            {
                Program.Log($"Error: Failed to make request to {URL} -- WebException {ex.Message}.");
            }
            catch (NullReferenceException nex)
            {
                Program.Log($"Error: Failed to make request to {URL} -- NullReferenceException {nex.Message}.");
            }
            return null;
        }

        public static string TryMakeShogunRequest(string URL)
        {
            return TryMakeCertifiedRequest(URL, ShopNCert);
        }

        public static string TryMakeConsoleRequest(string URL)
        {
            return TryMakeCertifiedRequest(URL, ConsoleCert);
        }

        private class CertificateWebClient : WebClient
        {
            private X509Certificate client_cert;
            private readonly string user_agent;

            public CertificateWebClient(X509Certificate cert, string ua = "") : base()
            {
                client_cert = cert;
                user_agent = ua;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = (HttpWebRequest)WebRequest.Create(address);
                request.ClientCertificates.Clear();
                request.ClientCertificates.Add(client_cert);
                request.UserAgent = user_agent;
                return request;
            }
        }
    }
}
