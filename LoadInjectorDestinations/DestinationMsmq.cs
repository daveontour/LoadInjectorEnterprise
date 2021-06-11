using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Management;
using System.Messaging;
using System.Text;
using System.Xml;

namespace LoadInjector.Destinations {

    public class DestinationMsmq : DestinationAbstract {
        public string queueName;
        private readonly int getTimeout = 2000;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);

            try {
                queueName = defn.Attributes["queue"].Value;
            } catch (Exception) {
                Console.WriteLine($"No Queue defined for {defn.Attributes["name"].Value}");
                return false;
            }

            return true;
        }

        public override string GetDestinationDescription() {
            return $"Queue: {queueName}";
        }

        public override bool Send(string val, List<Variable> vars) {
            try {
                using (MessageQueue msgQueue = new MessageQueue(queueName)) {
                    try {
                        var body = Encoding.ASCII.GetBytes(val);
                        Message myMessage = new Message(body, new ActiveXMessageFormatter());
                        msgQueue.Send(myMessage);
                    } catch (Exception ex) {
                        logger.Error(ex.Message);
                        logger.Error(ex.StackTrace);
                        return false;
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"MSMQ Error Sending to {queueName}");
                return false;
            }
            return true;
        }

        public override string Listen() {
            using (MessageQueue msgQueue = new MessageQueue(queueName)) {
                while (true) {
                    try {
                        using (Message msg = msgQueue.Receive(new TimeSpan(0, 0, 0, 0, getTimeout))) {
                            msg.Formatter = new ActiveXMessageFormatter();
                            using (var reader = new System.IO.StreamReader(msg.BodyStream)) {
                                return reader.ReadToEnd();
                            }
                        }
                    } catch (MessageQueueException e) {
                        // Handle no message arriving in the queue.
                        if (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout) {
                            //Keep loopping around the read until there is a message available
                        }
                    } catch (Exception ex) {
                        logger.Trace(ex.Message);
                        logger.Info(ex, "Unhandled MSMQ listen Error");
                    }
                }
            }
        }

        public void ClearQueue(MessageQueue msgQueue) {
            try {
                long count;
                try {
                    count = GetMSMessaegCount(queueName);
                } catch (Exception e) {
                    logger.Error(e.Message);
                    logger.Error(e.StackTrace);
                    count = msgQueue.GetAllMessages().Length;
                }

                logger.Trace($"Maintenance for Queue {queueName} Length = {count}");
                for (int i = 0; i < count; i++) {
                    msgQueue.Receive();
                }
            } catch (Exception ex) {
                logger.Error(ex, $"Error Maininting the Queue Size {queueName}");
            }

            logger.Info($"Maintenance for monitor {defn.Attributes["name"].Value} complete");
        }

        private long GetMSMessaegCount(string msmqName) {
            msmqName = msmqName.Split('\\')[2].ToLower();
            ManagementObjectCollection wmiCollection = null;
            using (ManagementObjectSearcher wmiSearch = new ManagementObjectSearcher("SELECT Name,MessagesinQueue FROM Win32_PerfRawdata_MSMQ_MSMQQueue")) {
                try {
                    wmiCollection = wmiSearch.Get();
                } catch (Exception ex) {
                    logger.Error(ex.Message);
                }
            }

            foreach (ManagementObject wmiObject in wmiCollection) {
                if (!wmiObject.Path.Path.Contains(msmqName)) {
                    continue;
                }
                foreach (PropertyData wmiProperty in wmiObject.Properties) {
                    if (wmiProperty.Name.Equals("MessagesinQueue", StringComparison.InvariantCultureIgnoreCase)) {
                        return long.Parse(wmiProperty.Value.ToString());
                    }
                }
            }

            return 0;
        }
    }
}