﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;

namespace LoadInjectorBase.Common {

    public class Utils {

        public static XmlDocument ExtractArchiveToDirectory(string archiveFile, string archiveRoot, string archiveFileName, bool addUniqueID = true) {
            XmlDocument document = new XmlDocument();
            try {
                Directory.Delete(archiveRoot, true);
            } catch (Exception ex) {
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

        public static XmlDocument ExtractArchiveToDirectory(byte[] archiveByteArray, string archiveRoot, string archiveFileName, bool addUniqueID = true) {
            string temmpArchiveFileName = Path.GetTempFileName();

            File.WriteAllBytes(temmpArchiveFileName, archiveByteArray);

            XmlDocument document = ExtractArchiveToDirectory(temmpArchiveFileName, archiveRoot, archiveFileName, addUniqueID);
            File.Delete(temmpArchiveFileName);
            return document;
        }
    }
}