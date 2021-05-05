using LoadInjector.RunTime;
using LoadInjector.RunTime.EngineComponents;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace LoadInjectorRuntime {

    internal class LoadInjectorRuntimeClient {
        private readonly NLog.Logger logger = NLog.LogManager.GetLogger("LoadInjectorClient");
        public string executeFile = null;

        public string ExecuteFile {
            get => executeFile;
            set => executeFile = value;
        }

        public string server = null;

        public string Server {
            get => server;
            set => server = value;
        }

        public LoadInjectorRuntimeClient(string executeFile, string server) {
            ExecuteFile = executeFile;
            Server = server;
        }

        public void OnStart() {
            if (ExecuteFile == null) {
                Thread mainThread = new Thread(this.OnStartAsync) {
                    IsBackground = false
                };

                try {
                    mainThread.Start();
                } catch (Exception ex) {
                    logger.Info($"Thread start error {ex.Message}");
                }
            } else {
                Thread mainThread = new Thread(this.ExecuteLocal) {
                    IsBackground = false
                };

                try {
                    mainThread.Start();
                } catch (Exception ex) {
                    logger.Info($"Thread start error {ex.Message}");
                }
            }
        }

        private void ExecuteLocal() {
            NgExecutionController cnt = new NgExecutionController(ExecuteFile);
        }

        public void OnStop() {
        }

        private void OnStartAsync() {
            StartSignalRClient();

            //if (!HttpListener.IsSupported) {
            //    Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
            //    return;
            //}
            //// URI prefixes are required,
            //var prefixes = new List<string>() { "http://localhost:8888/" };

            //// Create a listener.
            //HttpListener listener = new HttpListener();
            //// Add the prefixes.
            //foreach (string s in prefixes) {
            //    listener.Prefixes.Add(s);
            //}
            //listener.Start();
            //Console.WriteLine("Listening...");
            //while (true) {
            //    // Note: The GetContext method blocks while waiting for a request.
            //    HttpListenerContext context = listener.GetContext();

            //    HttpListenerRequest request = context.Request;

            //    string documentContents;
            //    using (Stream receiveStream = request.InputStream) {
            //        using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8)) {
            //            documentContents = readStream.ReadToEnd();
            //        }
            //    }
            //    Console.WriteLine($"Recived request for {request.Url}");
            //    Console.WriteLine(documentContents);

            //    // Obtain a response object.
            //    HttpListenerResponse response = context.Response;
            //    // Construct a response.
            //    string responseString = "OK";
            //    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            //    // Get a response stream and write the response to it.
            //    response.ContentLength64 = buffer.Length;
            //    System.IO.Stream output = response.OutputStream;
            //    output.Write(buffer, 0, buffer.Length);
            //    // You must close the output stream.

            //    StartSignalRClient();

            //    output.Close();
            //}
            //listener.Stop();
        }

        private void StartSignalRClient() {
            NgExecutionController controller = new NgExecutionController(6220, false);

            //   int port = GetAvailablePort(49152);
            //   port = 6220;

            string serverURL = "http://localhost:6220";
            if (Server != null) {
                serverURL = Server;
            }

            try {
                ClientHub clientHub = new ClientHub(serverURL, controller);
                Console.WriteLine($"Starting clientHub to connect to {serverURL}");
                controller.clientHub = clientHub;
                controller.slaveMode = true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public static int GetAvailablePort(int startingPort) {
            if (startingPort > ushort.MaxValue) throw new ArgumentException($"Can't be greater than {ushort.MaxValue}", nameof(startingPort));
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            var connectionsEndpoints = ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint);
            var tcpListenersEndpoints = ipGlobalProperties.GetActiveTcpListeners();
            var udpListenersEndpoints = ipGlobalProperties.GetActiveUdpListeners();
            var portsInUse = connectionsEndpoints.Concat(tcpListenersEndpoints)
                .Concat(udpListenersEndpoints)
                .Select(e => e.Port);

            return Enumerable.Range(startingPort, ushort.MaxValue - startingPort + 1).Except(portsInUse).FirstOrDefault();
        }
    }
}