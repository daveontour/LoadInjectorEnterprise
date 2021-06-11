using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace LoadInjector.Destinations {

    public class DestinationFtp : DestinationAbstract {
        private string ftpURL;
        private string ftpUser;
        private string ftpPass;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);

            try {
                ftpURL = defn.Attributes["ftpURL"].Value;
            } catch (Exception) {
                Console.WriteLine($"No FTP URL defined for { defn.Attributes["name"].Value}");
                return false;
            }

            try {
                ftpUser = defn.Attributes["ftpUser"].Value;
            } catch (Exception) {
                ftpUser = null;
            }

            try {
                ftpPass = defn.Attributes["ftpPass"].Value;
            } catch (Exception) {
                ftpPass = null;
            }

            return true;
        }

        public override string GetDestinationDescription() {
            return $"FTP Destination: {ftpURL}";
        }

        public override bool Send(string val, List<Variable> vars) {
            string uri = string.Copy(ftpURL);

            foreach (Variable v in vars) {
                try {
                    uri = uri.Replace(v.token, v.value);
                } catch (Exception) {
                    // NO-OP
                }
            }

            try {
                using (var client = new WebClient()) {
                    client.Credentials = new NetworkCredential(ftpUser, ftpPass);
                    client.UploadString(uri, WebRequestMethods.Ftp.UploadFile, val);
                }
            } catch (Exception) {
                return false;
            }

            return true;
        }
    }
}