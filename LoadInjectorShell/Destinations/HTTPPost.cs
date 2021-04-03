﻿using LoadInjector.Common;
using LoadInjector.GridDefinitions;
using LoadInjector.RunTime;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class HttpPost : IDestinationType {
        public string name = "HTTP";
        public string description = "HTTP Post";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new HttpPostPropertyGrid(dataModel, view);
        }

        public SenderAbstract GetDestinationSender() {
            return new DestinationHttpPost();
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("HTTP Post Protocol Configuration")]
    public class HttpPostPropertyGrid : LoadInjectorGridBase {

        public HttpPostPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Post URL (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(41), DescriptionAttribute("The full URL of the endpoint to post to. The URL can contain tokens which are substituted for the corresponding variable")]
        public string PostURL {
            get => GetAttribute("postURL");
            set => SetAttribute("postURL", value);
        }

        [Editor(typeof(FolderNameSelector), typeof(FolderNameSelector))]
        [DisplayName("HTTP Log Path"), ReadOnly(false), Browsable(true), PropertyOrder(42), DescriptionAttribute("If not null, then the local *directory* path to write the request and response")]
        public string HTTPLogPath {
            get => GetAttribute("httpLogPath");
            set => SetAttribute("httpLogPath", value);
        }

        [DisplayName("Request Timeout (seconds)"), ReadOnly(false), Browsable(true), PropertyOrder(43), DescriptionAttribute("The request timeout in seconds")]
        public int Timeout {
            get {
                int val = GetIntAttribute("timeout");
                if (val == -1) {
                    SetAttribute("timeout", 5);
                }
                return val;
            }
            set => SetAttribute("timeout", value);
        }

        [DisplayName("Number of Retries"), ReadOnly(false), Browsable(true), PropertyOrder(43), DescriptionAttribute("The number of retries to send the message if there is a failure")]
        public int MaxRetry {
            get => GetIntAttribute("maxRetry");
            set => SetAttribute("maxRetry", value);
        }
    }

    internal class DestinationHttpPost : SenderAbstract {
        private readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        private string httpLogPath;
        private string postURL;
        private int maxRetry = 2;
        private string name;
        public int numHttpOK;
        private int timeout;
        public HttpMethod httpVerb = HttpMethod.Post;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);
            USE_ASYNC_SEND = true;
            name = defn.Attributes["name"].Value;

            try {
                postURL = defn.Attributes["postURL"].Value;
            } catch (Exception) {
                Console.WriteLine($"No HTTP URL defined for {name}");
                return false;
            }

            try {
                httpLogPath = defn.Attributes["httpLogPath"].Value;
            } catch (Exception) {
                httpLogPath = null;
            }
            try {
                maxRetry = Convert.ToInt32(defn.Attributes["maxRetry"].Value);
            } catch (Exception) {
                maxRetry = 2;
            }

            try {
                timeout = Convert.ToInt32(defn.Attributes["timeout"].Value);
            } catch (Exception) {
                timeout = 5;
            }

            XElement xElem = XElement.Load(defn.CreateNavigator().ReadSubtree());
            IEnumerable<XElement> headerDefn = from value in xElem.Descendants("header") select value;
            foreach (XElement v in headerDefn) {
                string key = v.Attribute("name").Value;
                string value = v.Attribute("value").Value;
                headers.Add(key, value);
            }

            return true;
        }

        public override async Task<bool> SendAsync(string message, List<Variable> vars) {
            string messageXML = message;
            string url = postURL;

            foreach (Variable v in vars) {
                try {
                    //Before getting here, the value of the variable was set in the template substitution
                    url = url.Replace(v.token, v.value);
                } catch (Exception) {
                    //NO-OP
                }
            }

            logger.Info($"Constructed URL = {url}");

            bool sent = false;
            int tries = 1;

            do {
                if (tries > maxRetry && maxRetry > 0) {
                    return sent;
                }

                try {
                    using (var client = new HttpClient()) {
                        client.Timeout = TimeSpan.FromSeconds(timeout);

                        HttpRequestMessage requestMessage = new HttpRequestMessage(httpVerb, url) {
                            Content = new StringContent(messageXML, Encoding.UTF8, "text/xml")
                        };

                        if (headers != null) {
                            foreach (var item in headers) {
                                requestMessage.Headers.Add(item.Key, item.Value);
                            }
                        }
                        string uuid = Guid.NewGuid().ToString();

                        using (requestMessage) {
                            try {
                                logger.Info($"Destination: {name}, Send HTTP Request. ID = {uuid}");
                                Console.WriteLine($"Destination: {name}, Send HTTP Request. ID = {uuid}");
                                if (httpLogPath != null) {
                                    try {
                                        string logfile = $"{httpLogPath}/{uuid}.Request.log";
                                        await Task.Run(() => System.IO.File.WriteAllText(logfile, $"URL: {url}\nMessage:\n{messageXML}"));
                                    } catch (Exception) {
                                        logger.Info("Unable to write response to log file");
                                    }
                                }
                                using (HttpResponseMessage response = await client.SendAsync(requestMessage)) {
                                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent) {
                                        numHttpOK++;
                                        logger.Info($"Destination: {name}, HTTP Response Code = {response.StatusCode == HttpStatusCode.OK}. ID = {uuid}");
                                        if (httpLogPath != null) {
                                            _ = ProcessResponse(response, uuid, logger, response.StatusCode, url);
                                        }
                                        sent = true;
                                        return sent;
                                    } else {
                                        logger.Warn($"Destination: {name}, HTTP Post Response Code was not  2XX. HTTP Response Code: {response.StatusCode}. ID = {uuid}");
                                        Console.WriteLine($"Destination: {name}, HTTP Post Response Code was not  2XX. HTTP Response Code: {response.StatusCode}. ID = {uuid}");
                                        try {
                                            string logfile = $"{httpLogPath}/{uuid}.Error.log";
                                            await Task.Run(() => System.IO.File.WriteAllText(logfile, $"URL: {url}, HTTP Response Code was not  2XX. HTTP Response {response}. ID = {uuid}"));
                                        } catch (Exception) {
                                            logger.Info("Unable to write response to log file");
                                        }
                                        tries++;
                                    }
                                }
                            } catch (Exception ex) {
                                logger.Warn($"Error posting request {ex.Message}. Probably timeout of request.  ID = {uuid}");
                                Console.WriteLine($"Error posting request {ex.Message}. Probably timeout of request.  ID = {uuid}");
                                try {
                                    string logfile = $"{httpLogPath}/{uuid}.Error.log";
                                    await Task.Run(() => System.IO.File.WriteAllText(logfile, $"Error Sending Request: {ex.Message}. Probably timeout of request.  ID = {uuid}"));
                                } catch (Exception) {
                                    logger.Info("Unable to write response to log file");
                                }
                                tries++;
                            }
                        }
                    }
                } catch (Exception ex) {
                    logger.Info("Unable to send message to : " + name + " : " + ex.Message);
                    tries++;
                }
            } while (!sent && OK_TO_RUN);

            return sent;
        }

        public async Task ProcessResponse(HttpResponseMessage response, string uuid, Logger logger, HttpStatusCode statusCode, string url) {
            string res = await response.Content.ReadAsStringAsync();

            try {
                string logfile = $"{httpLogPath}/{uuid}.Response.log";
                await Task.Run(() => System.IO.File.WriteAllText(logfile, $"URL: {url}\nStatus Code: {statusCode}\nContent:\n{res}"));
            } catch (Exception) {
                logger.Info("Unable to write response to log file");
            }
        }
    }
}