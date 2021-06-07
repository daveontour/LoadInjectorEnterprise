using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Xml;

namespace LoadInjector.Destinations {

    public class DestinationTCPClient : DestinationAbstract {
        private string tcpServerIP;
        private int tcpServerPort;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);

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

        public override bool Send(string message, List<Variable> vars) {
            try {
                TcpClient client = new TcpClient(tcpServerIP, tcpServerPort);

                // Translate the passed message into ASCII and store it as a Byte array.
                byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

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
                return false;
            } catch (SocketException e) {
                Console.WriteLine($"SocketException: {e.Message}");
                return false;
            }
            return true;
        }
    }
}