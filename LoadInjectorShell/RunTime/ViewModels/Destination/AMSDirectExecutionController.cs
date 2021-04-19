using IBM.WMQ;
using LoadInjector.RunTime.Models;
using LoadInjector.RunTime.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Messaging;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjector.RunTime {

    public class AmsDirectExecutionController : DestinationControllerAbstract {
        private string qMgr;
        private string qSvrChan;
        private string qHost;
        private string qPort;
        private string qUser;
        private string qPass;
        private readonly Hashtable connectionParams = new Hashtable();
        public string queueName;
        protected object sendLock = new object();

        public Stopwatch stopwatch = new Stopwatch();

        private readonly string destinationDirectory;
        private readonly string protocol;

        private readonly string soapTopTemplate = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ams6=""http://www.sita.aero/ams6-xml-api-webservice"" xmlns:wor=""http://schemas.datacontract.org/2004/07/WorkBridge.Modules.AMS.AMSIntegrationAPI.Mod.Intf.DataTypes"">
   <soapenv:Header/>
   <soapenv:Body>
      <ams6:UpdateFlight>
         <!--Optional:-->
         <ams6:sessionToken>@token</ams6:sessionToken>
         <!--Optional:-->
         <ams6:flightIdentifier>
            <wor:_hasAirportCodes>true</wor:_hasAirportCodes>
            <wor:_hasFlightDesignator>true</wor:_hasFlightDesignator>
            <wor:_hasScheduledTime>true</wor:_hasScheduledTime>
            <wor:airlineDesignatorField>
               <!--Zero or more repetitions:-->
               <wor:LookupCode>
                  <wor:codeContextField>ICAO</wor:codeContextField>  <!-- ICAO = 0, IATA = 1 -->
                  <wor:valueField>@airlineICAO</wor:valueField>
               </wor:LookupCode>
               <wor:LookupCode>
                  <wor:codeContextField>IATA</wor:codeContextField>  <!-- ICAO = 0, IATA = 1 -->
                  <wor:valueField>@airlineIATA</wor:valueField>
               </wor:LookupCode>
           </wor:airlineDesignatorField>
            <wor:airportCodeField>
               <!--Zero or more repetitions:-->
               <wor:LookupCode>
                  <wor:codeContextField>ICAO</wor:codeContextField> <!-- ICAO = 0, IATA = 1 -->
                  <wor:valueField>@airportICAO</wor:valueField>
               </wor:LookupCode>
               <wor:LookupCode>
                  <wor:codeContextField>IATA</wor:codeContextField> <!-- ICAO = 0, IATA = 1 -->
                  <wor:valueField>@airportIATA</wor:valueField>
               </wor:LookupCode>
            </wor:airportCodeField>
            <wor:flightKindField>@kind</wor:flightKindField> <!-- Arrival = 0, Departure = 1 -->
            <wor:flightNumberField>@flightNum</wor:flightNumberField>
            <wor:scheduledDateField>@schedDate</wor:scheduledDateField>
         </ams6:flightIdentifier>
         <!--Optional:-->
         <ams6:updates>";

        private readonly string propertyTemplate = @"
           <wor:PropertyValue>
               <wor:codeContextField>IATA</wor:codeContextField>
               <wor:codeContextFieldSpecified>false</wor:codeContextFieldSpecified> <!-- bool -->
               <wor:propertyNameField>@propName</wor:propertyNameField>
               <wor:valueField>@propValue</wor:valueField>
            </wor:PropertyValue>";

        private readonly string soapBottomTemplate = @"
         </ams6:updates>
      </ams6:UpdateFlight>
   </soapenv:Body>
</soapenv:Envelope>";

        private readonly string mqTopTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<amsx-messages:Envelope xmlns:amsx-messages=""http://www.sita.aero/ams6-xml-api-messages"" xmlns:amsx-datatypes=""http://www.sita.aero/ams6-xml-api-datatypes"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" apiVersion=""2.8"">
    <amsx-messages:Content>
        <amsx-messages:FlightUpdateRequest>
            <amsx-datatypes:Token>@token</amsx-datatypes:Token>
            <amsx-messages:FlightId>
                <amsx-datatypes:FlightKind>@kind</amsx-datatypes:FlightKind>
                <amsx-datatypes:AirlineDesignator codeContext=""IATA"">@airlineIATA</amsx-datatypes:AirlineDesignator>
                <amsx-datatypes:FlightNumber>@flightNum</amsx-datatypes:FlightNumber>
                <amsx-datatypes:ScheduledDate>@schedDate</amsx-datatypes:ScheduledDate>
                <amsx-datatypes:AirportCode codeContext=""IATA"">@airportIATA</amsx-datatypes:AirportCode>
            </amsx-messages:FlightId>
            <amsx-messages:FlightUpdates>";

        private readonly string mqpropTemplate = @"<amsx-messages:Update propertyName=""@propName"" codeContext=""IATA"">@propValue</amsx-messages:Update>";

        private readonly string mqTemplateBottom = @"</amsx-messages:FlightUpdates>
        </amsx-messages:FlightUpdateRequest>
    </amsx-messages:Content>
</amsx-messages:Envelope>";

        public AmsDirectExecutionController(XmlNode node, NgExecutionController executionController) : base(node, executionController) {
            if (!ConfigOK) {
                return;
            }
            this.destinationDirectory = SetVar("destinationDirectory", null);
            this.protocol = SetVar("protocol", "WS");
        }

        internal void TriggerHandler(object sender, TriggerFiredEventArgs e) {
            ProcessIteration(e.record).Wait();
        }

        public override async void Execute() {
            this.messagesSent = 0;
            Report(0, 0);
            stopwatch.Restart();
            foreach (string trigger in triggerIDs) {
                eventDistributor.AddAMSLineHandler(trigger, this);
            }
            SetOutput("Waiting for triggering event");
        }

        public new bool PrePrepare() {
            messagesSent = 0;
            Report(0, 0);
            stopwatch.Stop();
            return base.PrePrepare();
        }

        public override async Task<bool> ProcessIteration(Tuple<Dictionary<string, string>, FlightNode> record) {
            FlightNode flt = record.Item2;
            Dictionary<string, string> data = record.Item1;

            bool ranOutOfData = false;

            try {
                //Two different message formata are produced. One is for sending via the SOAP interface and the other for MQ or MSMQ

                string message = (string)soapTopTemplate.Clone();
                string mqmessage = (string)mqTopTemplate.Clone();
                message = message.Replace("@token", amsToken ?? executionController.token)
                    .Replace("@airlineICAO", flt.airlineCodeICAO)
                    .Replace("@airlineIATA", flt.airlineCode)
                    .Replace("@airportICAO", executionController.apt_icao_code)
                    .Replace("@airportIATA", amsAptCode ?? executionController.apt_code)
                    .Replace("@kind", flt.nature)
                    .Replace("@flightNum", flt.fltNumber)
                    .Replace("@schedDate", flt.schedDate);
                mqmessage = mqmessage.Replace("@token", amsToken ?? executionController.token)
                    .Replace("@airlineICAO", flt.airlineCodeICAO)
                    .Replace("@airlineIATA", flt.airlineCode)
                    .Replace("@airportICAO", executionController.apt_icao_code)
                    .Replace("@airportIATA", amsAptCode ?? executionController.apt_code)
                    .Replace("@kind", flt.nature)
                    .Replace("@flightNum", flt.fltNumber)
                    .Replace("@schedDate", flt.schedDate);

                foreach (Variable v in vars) {
                    try {
                        string prop = (string)propertyTemplate.Clone();
                        string mqprop = (string)mqpropTemplate.Clone();

                        if (v.varSubstitionRequired) {
                            v.value = v.GetValue(flt, data, vars);
                        } else {
                            v.value = v.GetValue(flt, data);
                        }

                        prop = prop.Replace("@propName", v.externalName).Replace("@propValue", v.value);
                        mqprop = mqprop.Replace("@propName", v.externalName).Replace("@propValue", v.value);

                        message += prop;
                        mqmessage += mqprop;
                    } catch (ArgumentException) {
                        ranOutOfData = true;
                        break;
                    } catch (Exception ex) {
                        logger.Warn($"Token Substitution Exception: {ex.Message}");
                        ConsoleMsg($"Token Substitution Exception: {ex.Message}");
                    }
                }

                if (ranOutOfData) {
                    return false;
                }

                message += soapBottomTemplate;
                mqmessage += mqTemplateBottom;

                // Send the message here
                await SendUpdate(message, mqmessage);

                if (saveMessageFile != null) {
                    string fullPath = saveMessageFile.Replace(".", $"{messagesSent}.");
                    try {
                        File.WriteAllText(fullPath, message);
                    } catch (Exception ex) {
                        ConsoleMsg($"Unable to record message to file ${fullPath}. {ex.Message}");
                    }
                }
            } catch (Exception ex) {
                logger.Error($"Send Error: { ex.Message}");
            }

            return true;
        }

        private async Task SendUpdate(string message, string mqmessage) {
            if (protocol == "WS") {
                logger.Trace($"Message to be sent \n{message}");
                try {
                    using (var client = new HttpClient()) {
                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, amsHost ?? executionController.amshost) {
                            Content = new StringContent(message, Encoding.UTF8, "text/xml")
                        };
                        requestMessage.Headers.Add("SOAPAction", "http://www.sita.aero/ams6-xml-api-webservice/IAMSIntegrationService/UpdateFlight");

                        using (HttpResponseMessage response = await client.SendAsync(requestMessage)) {
                            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent) {
                                await ProcessResponse(response);
                                logger.Trace($"AMS Flight Update Result = {response.StatusCode}");
                                ConsoleMsg($"AMS Flight Update Result = {response.StatusCode}");
                            } else {
                                SetOutput($"Flight Update Failure({response.StatusCode})");
                                ConsoleMsg($"Flight Update Failure({response.StatusCode})");
                            }
                        }
                    }
                } catch (Exception ex) {
                    logger.Error(ex.StackTrace);
                    ConsoleMsg($"Flight Update Failure({ex.Message})");
                    return;
                }
            } else if (protocol == "MQ") {
                logger.Trace($"Message to be sent \n{mqmessage}");
                try {
                    ConfigureMQ(config);
                    SendMQ(mqmessage);
                } catch (Exception ex) {
                    logger.Error(ex.StackTrace);
                    ConsoleMsg($"Flight Update Failure({ex.Message})");
                    return;
                }
            } else {
                logger.Trace($"Message to be sent \n{mqmessage}");
                try {
                    SendMSMQ(mqmessage);
                } catch (Exception ex) {
                    logger.Error(ex.StackTrace);
                    ConsoleMsg($"Flight Update Failure({ex.Message})");
                    return;
                }
            }

            ConsoleMsg($"AMS Direct Line: {name}. Message Sent");
            messagesSent++;
            this.avg = stopwatch.Elapsed.TotalMilliseconds / messagesSent;
            double rate = RoundToSignificantDigits(60000 / avg, 2);
            Report(messagesSent, rate);
        }

        public async Task ProcessResponse(HttpResponseMessage response) {
            string res = await response.Content.ReadAsStringAsync();
            logger.Trace(res);

            if (destinationDirectory != null && Directory.Exists(destinationDirectory)) {
                string uuid = Guid.NewGuid().ToString();

                try {
                    await Task.Run(() => File.WriteAllText(destinationDirectory + uuid + ".xml", res));
                } catch (Exception) {
                    logger.Error("Unable to write to AMS Direct Response Director");
                }
            }
        }

        private void ConfigureMQ(XmlNode defn) {
            try {
                try {
                    this.queueName = config.Attributes["queue"].Value;
                } catch (Exception) {
                    ConsoleMsg($"No Queue defined for {name}");
                    return;
                }

                try {
                    this.qMgr = defn.Attributes["queueMgr"].Value;
                } catch (Exception) {
                    ConsoleMsg($"Queue Manager not defined for {name}");
                    return;
                }
                try {
                    this.qSvrChan = defn.Attributes["channel"].Value;
                } catch (Exception) {
                    ConsoleMsg($"Channel not defined for {name}");
                    return;
                }

                try {
                    this.qHost = defn.Attributes["host"].Value;
                } catch (Exception) {
                    ConsoleMsg($"Queue  not defined for {name}");
                    return;
                }

                try {
                    this.qPort = defn.Attributes["port"].Value;
                } catch (Exception) {
                    ConsoleMsg($"Port not defined for {name}");
                    return;
                }

                try {
                    this.qUser = defn.Attributes["username"].Value;
                } catch (Exception) {
                    this.qUser = null;
                    logger.Info($"No username defined for {name}");
                }

                try {
                    this.qPass = defn.Attributes["password"].Value;
                } catch (Exception) {
                    this.qPass = null;
                    logger.Info($"No password defined for {name}");
                }

                try {
                    // Set the connection parameter
                    connectionParams.Add(MQC.CHANNEL_PROPERTY, qSvrChan);
                    connectionParams.Add(MQC.HOST_NAME_PROPERTY, qHost);
                    connectionParams.Add(MQC.PORT_PROPERTY, qPort);
                    connectionParams.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED);

                    if (qUser != null) {
                        connectionParams.Add(MQC.USER_ID_PROPERTY, qUser);
                    }
                    if (qPass != null) {
                        connectionParams.Add(MQC.PASSWORD_PROPERTY, qPass);
                    }
                } catch (Exception) {
                    return;
                }
            } catch (AccessViolationException ex) {
                logger.Info(ex.Message);
                logger.Info(ex.StackTrace);
            } catch (Exception ex) {
                logger.Info("Error configuring MQ queue");
                logger.Info(ex.Message);
                logger.Info(ex.StackTrace);
                ConsoleMsg($"Error configuring MQ access for {name}");
            }
        }

        public void SendMSMQ(string xm) {
            try {
                queueName = config.Attributes["queue"].Value;
            } catch (Exception) {
                ConsoleMsg($"No Queue defined for {name}");
                return;
            }
            try {
                using (MessageQueue msgQueue = new MessageQueue(queueName)) {
                    try {
                        Message myMessage = new Message(Encoding.ASCII.GetBytes(xm), new ActiveXMessageFormatter());
                        msgQueue.Send(myMessage);
                    } catch (Exception ex) {
                        logger.Error(ex.Message);
                        logger.Error(ex.StackTrace);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"MSMQ Error Sending to {queueName}");
            }
        }

        public void SendMQ(string xm) {
            string messageXML = xm;
            bool sent = false;
            int tries = 1;

            lock (sendLock) {
                try {
                    using (MQQueueManager queueManager = new MQQueueManager(qMgr, connectionParams)) {
                        var openOptions = MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING;
                        MQQueue queue = queueManager.AccessQueue(queueName, openOptions);
                        var message = new MQMessage {
                            CharacterSet = 1208 // UTF-8
                        };
                        message.WriteString(messageXML);
                        message.Format = MQC.MQFMT_STRING;
                        MQPutMessageOptions putOptions = new MQPutMessageOptions();
                        queue.Put(message, putOptions);
                        queue.Close();
                        sent = true;
                        logger.Trace($"===Message Sent to {queueName}");
                    }
                } catch (Exception ex) {
                    tries++;
                    logger.Info(ex.Message);
                    logger.Info("Error send MQ Message");
                }
                if (!sent) {
                    logger.Trace($"===Message NOT Sent to  {queueName}");
                }
            }
        }
    }
}