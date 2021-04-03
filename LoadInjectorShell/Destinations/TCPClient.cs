using LoadInjector.Common;
using LoadInjector.RunTime;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class TCPClient : IDestinationType {
        public string name = "TCPCLIENT";
        public string description = "TCP Client";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new TCPClientPropertyGrid(dataModel, view);
        }

        public SenderAbstract GetDestinationSender() {
            return new DestinationTCPClient();
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("TCP Client Protocol Configuration")]
    public class TCPClientPropertyGrid : LoadInjectorGridBase {

        public TCPClientPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("TCP Server IP"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The IP addres of the TCP Server")]
        public string TCPServerIP {
            get => GetAttribute("tcpServerIP");
            set => SetAttribute("tcpServerIP", value);
        }

        [DisplayName("TCP Server Port"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("The Port number of the TCP Server")]
        public string TCPServerPort {
            get => GetAttribute("tcpServerPort");
            set => SetAttribute("tcpServerPort", value);
        }
    }

    public class DestinationTCPClient : SenderAbstract {
        private string tcpServerIP;
        private int tcpServerPort;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);

            try {
                tcpServerIP = defn.Attributes["tcpServerIP"].Value;
            } catch (Exception) {
                Console.WriteLine($"No TCP Server IP for {defn.Attributes["name"].Value}");
                Console.WriteLine("Using '127.0.0.1' for TCP Server IP");
                tcpServerIP = "127.0.0.1";
            }
            try {
                tcpServerPort = int.Parse(defn.Attributes["tcpServerPort"].Value);
            } catch (Exception) {
                Console.WriteLine($"No TCP Server Port correctly defined for {defn.Attributes["name"].Value}");
                return false;
            }

            return true;
        }

        public override void Send(String message, List<Variable> vars) {
            try {
                TcpClient client = new TcpClient(tcpServerIP, tcpServerPort);

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                logger.Trace("Sent: {0}", message);

                // Close everything.
                stream.Close();
                client.Close();
            } catch (ArgumentNullException e) {
                Console.WriteLine($"ArgumentNullException: {e.Message}");
            } catch (SocketException e) {
                Console.WriteLine($"SocketException: {e.Message}");
            }
        }
    }
}