using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace LoadInjectorBase.Common {

    public class Utils {

        public static string GetTemporaryDirectory() {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static XmlDocument ExtractArchiveToDirectory(string archiveFile, string archiveRoot, string archiveFileName, bool addUniqueID = true) {
            XmlDocument document = new XmlDocument();
            try {
                Directory.Delete(archiveRoot, true);
            } catch (Exception) {
                // NO-OP
            }

            ZipFile.ExtractToDirectory(archiveFile, archiveRoot);
            File.Copy(archiveFile, $"{archiveRoot}/{archiveFileName}");

            document.Load(archiveRoot + "/config.xml");

            string executionNodeID = Guid.NewGuid().ToString();

            foreach (XmlNode node in document.SelectNodes(".//*")) {
                if (node.Attributes["dataFile"]?.Value != null) {
                    string fullFile = node.Attributes["dataFile"]?.Value;
                    string[] f2 = fullFile.Split('/');
                    string filename = f2[f2.Length - 1];
                    node.Attributes["dataFile"].Value = $"{archiveRoot}/DATA/{filename}";
                }

                if (node.Attributes["templateFile"]?.Value != null) {
                    string fullFile = node.Attributes["templateFile"]?.Value;
                    string[] f2 = fullFile.Split('/');
                    string filename = f2[f2.Length - 1];
                    node.Attributes["templateFile"].Value = $"{archiveRoot}/TEMPLATES/{filename}";
                }

                if (addUniqueID && (node.Name == "destination" || node.Name == "ratedriven" || node.Name.Contains("datadriven") || node.Name == "chained")) {
                    XmlAttribute newAttribute = document.CreateAttribute("uuid");
                    newAttribute.Value = Guid.NewGuid().ToString();
                    node.Attributes.Append(newAttribute);

                    XmlAttribute newAttribute2 = document.CreateAttribute("executionNodeUuid");
                    newAttribute2.Value = executionNodeID;
                    node.Attributes.Append(newAttribute2);
                }
            }

            File.WriteAllText(archiveRoot + "/config.xml", document.OuterXml);

            return document;
        }

        public static XmlDocument ExtractArchiveToDirectoryForEdit(string archiveFile, string archiveRoot, string archiveFileName, bool addUniqueID = true) {
            string temmpArchiveFileName = Path.GetTempFileName();

            var archiveByteArray = File.ReadAllBytes(archiveFile);
            File.WriteAllBytes(temmpArchiveFileName, archiveByteArray);

            XmlDocument document = ExtractArchiveToDirectory(temmpArchiveFileName, archiveRoot, archiveFileName, addUniqueID);
            File.Delete(temmpArchiveFileName);
            return document;
        }

        public static XmlDocument ExtractArchiveToDirectory(byte[] archiveByteArray, string archiveRoot, string archiveFileName, bool addUniqueID = true) {
            string temmpArchiveFileName = Path.GetTempFileName();

            File.WriteAllBytes(temmpArchiveFileName, archiveByteArray);

            XmlDocument document = ExtractArchiveToDirectory(temmpArchiveFileName, archiveRoot, archiveFileName, addUniqueID);
            File.Delete(temmpArchiveFileName);
            return document;
        }

        public static int GetAvailablePort(int startingPort) {
            if (startingPort > ushort.MaxValue) throw new ArgumentException($"Can't be greater than {ushort.MaxValue}", nameof(startingPort));
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            var connectionsEndpoints = ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint);
            var tcpListenersEndpoints = ipGlobalProperties.GetActiveTcpListeners();
            var udpListenersEndpoints = ipGlobalProperties.GetActiveUdpListeners();
            var portsInUse = connectionsEndpoints.Concat(tcpListenersEndpoints)
                .Concat(udpListenersEndpoints)
                .Select(e => e.Port);

            return Enumerable.Range(startingPort, ushort.MaxValue - startingPort + 1).Except(portsInUse).FirstOrDefault();
        }

        public static string FormatXML(string xml) {
            if (xml == null) {
                return null;
            }

            try {
                StringBuilder sb = new StringBuilder();
                TextWriter tr = new StringWriter(sb);
                XmlTextWriter wr = new XmlTextWriter(tr) {
                    Formatting = Formatting.Indented
                };

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                doc.Save(wr);
                wr.Close();
                return sb.ToString();
            } catch (Exception) {
                return null;
            }
        }

        public static string GetLocalIPAddress() {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}