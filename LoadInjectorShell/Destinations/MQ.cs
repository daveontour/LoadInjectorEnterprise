using IBM.WMQ;
using LoadInjector.Common;
using LoadInjector.RunTime;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    public class MQ : IDestinationType {
        private readonly string name = "MQ";
        private readonly string description = "IBM MQ";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new MQPropertyGrid(dataModel, view);
        }

        public SenderAbstract GetDestinationSender() {
            return new DestinationMQ();
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("IBM MQ Protocol Configuration")]
    public class MQPropertyGrid : LoadInjectorGridBase {

        public MQPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Host"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("Host name")]
        public string HostName {
            get => GetAttribute("host");
            set => SetAttribute("host", value);
        }

        [DisplayName("Port"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("TCP Port Number of Queue Manager")]
        public string Port {
            get => GetAttribute("port");
            set => SetAttribute("port", value);
        }

        [DisplayName("Queue"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("Queue Name")]
        public string Queue {
            get => GetAttribute("queue");
            set => SetAttribute("queue", value);
        }

        [DisplayName("Queue Manager"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("IBM MQ Queue Manager Name")]
        public string QManager {
            get => GetAttribute("queueMgr");
            set => SetAttribute("queueMgr", value);
        }

        [DisplayName("Channel"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("IBM MQ Server Connection Channel name")]
        public string Channel {
            get => GetAttribute("channel");
            set => SetAttribute("channel", value);
        }

        [DisplayName("Message Put Timeout"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("Timeout for the message put")]
        public int PutTimeout {
            get => GetIntAttribute("putTimeout");
            set => SetAttribute("putTimeout", value);
        }

        [DisplayName("Use Send Locking"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("Serialization lock on sending messages - required for high  volumes")]
        public bool SendLocking {
            get => GetBoolAttribute("useSendLocking");
            set => SetAttribute("useSendLocking", value);
        }

        [DisplayName("User Name"), ReadOnly(false), Browsable(true), PropertyOrder(16), DescriptionAttribute("MQ User Name - if rquired for the connection")]
        public string UserName {
            get => GetAttribute("username");
            set => SetAttribute("username", value);
        }

        [DisplayName("User Password"), ReadOnly(false), Browsable(true), PropertyOrder(17), DescriptionAttribute("Connection password for this user - if required for the connection")]
        public string UserPass {
            get => GetAttribute("password");
            set => SetAttribute("password", value);
        }
    }

    internal class DestinationMQ : SenderAbstract {
        private string qMgr;
        private string qSvrChan;
        private string qHost;
        private string qPort;
        private string qUser;
        private string qPass;
        private int putTimeout;
        public string queueName;
        private bool useSendLocking;
        private readonly Hashtable connectionParams = new Hashtable();
        private readonly object sendLock = new object();
        public static MQQueueManager queueManager;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);
            try {
                try {
                    queueName = defn.Attributes["queue"].Value;
                } catch (Exception) {
                    Console.WriteLine($"No Queue defined for {defn.Attributes["name"].Value}");
                    return false;
                }

                try {
                    qMgr = defn.Attributes["queueMgr"].Value;
                } catch (Exception) {
                    Console.WriteLine($"Queue Manager not defined for {defn.Attributes["name"].Value}");
                    return false;
                }
                try {
                    qSvrChan = defn.Attributes["channel"].Value;
                } catch (Exception) {
                    Console.WriteLine($"Channel not defined for {defn.Attributes["name"].Value}");
                    return false;
                }

                try {
                    qHost = defn.Attributes["host"].Value;
                } catch (Exception) {
                    Console.WriteLine($"Queue  not defined for {defn.Attributes["name"].Value}");
                    return false;
                }

                try {
                    qPort = defn.Attributes["port"].Value;
                } catch (Exception) {
                    Console.WriteLine($"Port not defined for {defn.Attributes["name"].Value}");
                    return false;
                }

                try {
                    qUser = defn.Attributes["username"].Value;
                } catch (Exception) {
                    qUser = null;
                    logger.Info($"No username defined for {defn.Attributes["name"].Value}");
                }

                try {
                    putTimeout = int.Parse(defn.Attributes["putTimeout"].Value);
                } catch (Exception) {
                    putTimeout = 10;
                    logger.Info("MQ Message Put Timeout set to default");
                }

                try {
                    qPass = defn.Attributes["password"].Value;
                } catch (Exception) {
                    qPass = null;
                    logger.Info($"No password defined for {defn.Attributes["name"].Value}");
                }
                try {
                    useSendLocking = bool.Parse(defn.Attributes["useSendLocking"].Value);
                } catch (Exception) {
                    useSendLocking = false;
                }

                try {
                    // Set the connection parameter
                    connectionParams.Add(MQC.CHANNEL_PROPERTY, qSvrChan);
                    connectionParams.Add(MQC.HOST_NAME_PROPERTY, qHost);
                    connectionParams.Add(MQC.PORT_PROPERTY, qPort);

                    if (qUser != null) {
                        connectionParams.Add(MQC.USER_ID_PROPERTY, qUser);
                    }
                    if (qPass != null) {
                        connectionParams.Add(MQC.PASSWORD_PROPERTY, qPass);
                    }
                } catch (Exception) {
                    return false;
                }
            } catch (AccessViolationException ex) {
                logger.Info(ex.Message);
                logger.Info(ex.StackTrace);
                return false;
            } catch (Exception ex) {
                logger.Info("Error configuring MQ queue");
                logger.Info(ex.Message);
                logger.Info(ex.StackTrace);
                Console.WriteLine($"Error configuring MQ access for {defn.Attributes["name"].Value}");
                return false;
            }

            return true;
        }

        public override void Prepare() {
            base.Prepare();

            try {
                if (queueManager == null) {
                    Console.WriteLine($"Creating Queue Manager Access Object:  {qMgr}, {queueName}");
                    logger.Trace($"Creating Queue Manager Access Object:  {qMgr}, {queueName}");

                    queueManager = new MQQueueManager(qMgr, connectionParams);

                    Console.WriteLine($"Created Queue Manager Access Object OK:  {qMgr}, {queueName}");
                    logger.Trace($"Created Queue Manager Access Object OK:  {qMgr}, {queueName}");
                } else {
                    Console.WriteLine($"Queue Manager Already Available:  {qMgr}, {queueName}");
                    logger.Trace($"Queue Manager Already Available:  {qMgr}, {queueName}");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error connecting to Queue Manager: {ex.Message}");
            }
        }

        public override void Send(String val, List<Variable> vars) {
            string messageXML = val;

            if (useSendLocking) {
                lock (sendLock) {
                    Console.WriteLine($"MQ Begin sendinng with locking enabled: {qMgr}, {queueName}");
                    logger.Trace($"MQ Begin sendinng with locking enabled: {qMgr}, {queueName}");
                    SendInternal(messageXML);
                }
            } else {
                Console.WriteLine($"MQ Begin sendinng with locking NOT enabled: {qMgr}, {queueName}");
                logger.Trace($"MQ Begin sendinng with locking NOT enabled: {qMgr}, {queueName}");
                SendInternal(messageXML);
            }
        }

        public void SendInternal(String mess) {
            string messageXML = mess;
            bool sent = false;

            try {
                var openOptions = MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING;
                MQQueue queue = queueManager.AccessQueue(queueName, openOptions);

                Console.WriteLine($"MQ Accessed Queue:  {queueName}");
                logger.Trace($"MQ Accessed Queue:  {queueName}");

                var message = new MQMessage {
                    CharacterSet = 1208 // UTF-8
                };
                message.WriteString(messageXML);
                message.Format = MQC.MQFMT_STRING;

                MQPutMessageOptions putOptions = new MQPutMessageOptions {
                    Timeout = putTimeout
                };

                Console.WriteLine($"MQ Putting Message to Queue:  {queueName} with timeout {putTimeout}");
                logger.Trace($"MQ Putting Message to Queue:  {queueName} with timeout {putTimeout}");

                queue.Put(message, putOptions);

                Console.WriteLine($"MQ Message Sent to {queueName}");
                logger.Trace($"MQ Message Sent to {queueName}");

                queue.Close();

                Console.WriteLine($"MQ Queue Closed {queueName}");
                logger.Trace($"MQ Queue Closed {queueName}");

                sent = true;
            } catch (Exception ex) {
                Console.WriteLine($"Error sending MQ Message: {ex.Message}");
                logger.Trace($"Error sending MQ Message: {ex.Message}");
            }
            if (!sent) {
                logger.Trace($"Warning: Message NOT Sent to  {queueName}");
            }
        }

        public override void Stop() {
            //NO-OP
        }
    }
}