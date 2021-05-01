using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LoadInjector.Destinations {

    internal class DestinationHttpPut : SenderAbstract {
        private readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        private string httpLogPath;
        private string postURL;
        private int maxRetry = 2;
        private string name;
        public int numHttpOK;
        private int timeout;
        public HttpMethod httpVerb = HttpMethod.Put;

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
                                        logger.Warn($"Destination: {name}, HTTP Put Response Code was not  2XX. HTTP Response Code: {response.StatusCode}. ID = {uuid}");
                                        Console.WriteLine($"Destination: {name}, HTTP Put Response Code was not  2XX. HTTP Response Code: {response.StatusCode}. ID = {uuid}");
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