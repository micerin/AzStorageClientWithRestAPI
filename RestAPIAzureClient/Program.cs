using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
//using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace RestAPIAzureClient
{
    class Program
    {
        /// <summary>
        /// Construct authentication string for given account
        /// </summary>
        /// <param name="StringToSign"></param>
        /// <param name="Key"></param>
        /// <param name="Account"></param>
        /// An authenticated request requires two headers: the Date or x-ms-date header and the Authorization header
        /// Signature is a Hash-based Message Authentication Code (HMAC)
        private static String SignThis(String StringToSign, string Key, string Account)
        {
            String signature = string.Empty;
            byte[] unicodeKey = Convert.FromBase64String(Key);
            using (HMACSHA256 hmacSha256 = new HMACSHA256(unicodeKey))
            {
                Byte[] dataToHmac = System.Text.Encoding.UTF8.GetBytes(StringToSign);
                signature = Convert.ToBase64String(hmacSha256.ComputeHash(dataToHmac));
            }

            String authorizationHeader = String.Format(
                  CultureInfo.InvariantCulture,
                  "{0} {1}:{2}",
                  "SharedKey",
                  Account,
                  signature);

            return authorizationHeader;
        }

        /// <summary>
        /// List all Blobs for given container
        /// </summary>
        /// <param name="account"></param>
        /// <param name="key"></param>
        /// <param name="container"></param>
        static void ListContainerBlobs(string account, string key, string container)
        {
            DateTime dt = DateTime.UtcNow;
            string StringToSign = String.Format("GET\n"
                + "\n" // content encoding
                + "\n" // content language
                + "\n" // content length
                + "\n" // content md5
                + "\n" // content type
                + "\n" // date
                + "\n" // if modified since
                + "\n" // if match
                + "\n" // if none match
                + "\n" // if unmodified since
                + "\n" // range
                + "x-ms-date:" + dt.ToString("R") + "\nx-ms-version:2012-02-12\n" // headers
                + "/{0}/{1}\ncomp:list\nrestype:container", account, container);

            string auth = SignThis(StringToSign, key, account);
            string method = "GET";
            string urlPath = string.Format("https://{0}.blob.core.windows.net/{1}?restype=container&comp=list", account, container);
            Uri uri = new Uri(urlPath);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = method;
            request.Headers.Add("x-ms-date", dt.ToString("R"));
            request.Headers.Add("x-ms-version", "2012-02-12");
            request.Headers.Add("Authorization", auth);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if ((int)response.StatusCode == 200)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();

                        XElement x = XElement.Parse(result);

                        foreach (XElement zcontainer in x.Element("Blobs").Elements("Blob"))
                        {
                            Console.WriteLine(zcontainer.Element("Name").Value);
                            Console.WriteLine(zcontainer.Element("Properties").Element("BlobType").Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Main entry for application
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string Account = "weixin";
            string Key = "";
            string Container = "office";
            ListContainerBlobs(Account, Key, Container);
        }
    }
}
