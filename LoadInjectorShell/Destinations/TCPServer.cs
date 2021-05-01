using LoadInjector.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class TcpServer : IDestinationType {
        public string name = "TCPSERVER";
        public string description = "TCP Server";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new TcpServerPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new TcpServerPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("TCP Server Protocol Configuration")]
    public class TcpServerPropertyGrid : LoadInjectorGridBase {

        public TcpServerPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("TCP Server IP"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The IP addres of the TCP Server")]
        public string TCPServerIP {
            get => GetAttribute("tcpServerIP");
            set {
                if (value == "localhost") {
                    value = "127.0.0.1";
                }
                SetAttribute("tcpServerIP", value);
            }
        }

        [DisplayName("TCP Server Port"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("The Port number of the TCP Server")]
        public string TCPServerPort {
            get => GetAttribute("tcpServerPort");
            set => SetAttribute("tcpServerPort", value);
        }

        [DisplayName("Close Connection"), ReadOnly(false), Browsable(true), PropertyOrder(14), DescriptionAttribute("Close Connection after each message is sent")]
        public bool CloseConnection {
            get => GetBoolDefaultFalseAttribute(_node, "closeConnection");
            set => SetAttribute("closeConnection", value);
        }
    }

    public class AsynchronousSocketListener : IDisposable {

        // Thread signal.
        public ManualResetEvent allDone = new ManualResetEvent(false);

        public bool continueToListen = true;
        public bool closeAfterSend;
        public List<Socket> serverSockets = new List<Socket>();

        public void Stop() {
            foreach (Socket serverSocket in serverSockets) {
                try {
                    serverSocket.Disconnect(false);
                    serverSocket.Dispose();
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
            continueToListen = false;
            allDone.Set();
        }

        public void SendTCPServer(string message, bool closeConnection) {
            foreach (Socket serverSocket in serverSockets.ToArray()) {
                SendTCPServer(serverSocket, message);
                if (closeConnection) {
                    serverSockets.Remove(serverSocket);
                    serverSocket.Disconnect(false);
                    serverSocket.Dispose();
                }
            }
        }

        public void SendTCPServer(Socket serverSocket, string message) {
            if (serverSocket == null) {
                Console.WriteLine("No current client connection to TCP Server");
                return;
            }

            if (serverSocket.Poll(10, SelectMode.SelectWrite)) {
                try {
                    Send(serverSocket, message);
                } catch (Exception e) {
                    Console.WriteLine(e.StackTrace);
                }
            } else {
                Console.WriteLine("SOCKET NOT AVAILABLE");
            }
        }

        public void StartListening(string tcpServerIP, int tcpServerPort, bool closeConnection) {
            // Establish the local endpoint for the socket.
            IPAddress ipAddress = null;
            try {
                closeAfterSend = closeConnection;
                ipAddress = IPAddress.Parse(tcpServerIP);
                Console.WriteLine("*****************************************");
                Console.WriteLine($"* TCP Server Listen on {ipAddress}:{tcpServerPort}");
                Console.WriteLine("*****************************************");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, tcpServerPort);

            // Create a TCP/IP socket.
            using (Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)) {
                // Bind the socket to the local endpoint and listen for incoming connections.
                try {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);

                    while (continueToListen) {
                        // Set the event to nonsignaled state.
                        allDone.Reset();

                        // Start an asynchronous socket to listen for connections.
                        Console.WriteLine("TCP Server Waiting for a connection");
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                        // Wait until a connection is made before continuing.
                        allDone.WaitOne();
                    }
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void AcceptCallback(IAsyncResult ar) {
            // Signal the main thread to continue.
            allDone.Set();
            Console.WriteLine("TCP Server Accepted Connection");
            // Get the socket that handles the client request.

            try {
                Socket listener = (Socket)ar.AsyncState;
                Socket serverSocket = listener.EndAccept(ar);
                serverSockets.Add(serverSocket);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private void Send(Socket serverSocket, String data) {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            if (!serverSocket.Poll(10000, SelectMode.SelectWrite)) {
                Console.WriteLine("Connection No Longer Available");
                try {
                    serverSocket = null;
                    return;
                } catch (Exception) {
                    // NO-OP
                }
            }
            // Begin sending the data to the remote device.
            try {
                int bytesSent = serverSocket.Send(byteData, SocketFlags.None);
                Console.WriteLine($"TCP Server Sent {bytesSent} bytes to client.");
            } catch (Exception) {
                Console.WriteLine("Connection No Longer Available");
                try {
                    serverSocket = null;
                } catch (Exception) {
                    // NO-OP
                }
            }
        }

        public void Dispose() {
            // NO-OP
        }
    }
}