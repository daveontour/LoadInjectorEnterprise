using LoadInjector.RunTime;
using LoadInjector.RunTime.EngineComponents;
using System;
using System.Threading;

namespace LoadInjectorRuntime {

    internal class LoadInjectorRuntimeClient {
        private Thread mainThread;
        private ClientHub clientHub;
        private NgExecutionController controller;
        private readonly NLog.Logger logger = NLog.LogManager.GetLogger("LoadInjectorClient");

        public LoadInjectorRuntimeClient() {
        }

        public void OnStart() {
            mainThread = new Thread(this.OnStartAsync) {
                IsBackground = false
            };

            try {
                mainThread.Start();
            } catch (Exception ex) {
                logger.Info($"Thread start error {ex.Message}");
            }
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
            this.controller = new NgExecutionController(false);

            try {
                clientHub = new ClientHub("http://localhost:6220", controller);
                controller.clientHub = clientHub;
                controller.slaveMode = true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}