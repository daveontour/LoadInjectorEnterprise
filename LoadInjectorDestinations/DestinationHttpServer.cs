using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class DestinationHttpServer : DestinationAbstract {
        private HttpListener listener;
        private string serverURL;
        private readonly Queue<string> messageQueue = new Queue<string>();
        private Thread threadToCancel;
        private CancellationTokenSource source;
        private string name;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);

            name = defn.Attributes["name"]?.Value;
            try {
                serverURL = defn.Attributes["serverURL"]?.Value;
            } catch (Exception ex) {
                serverURL = null;
                Console.WriteLine($"HTTP Server URL Invalid. {ex.Message}");
            }
            if (serverURL == null) {
                return false;
            }
            return true;
        }

        public override string GetDestinationDescription() {
            return $"URL: {serverURL}";
        }

        public override void Prepare() {
            source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            StartSimpleListener().Wait();
            Task.Factory.StartNew(() => {
                while (true) {
                    if (token.IsCancellationRequested && threadToCancel != null) {
                        threadToCancel.Abort();
                        return;
                    }
                }
            });
        }

        public override void Stop() {
            source.Cancel();
            listener.Abort();
        }

        public override bool Send(string val, List<Variable> vars) {
            try {
                messageQueue.Enqueue(val);
                return true;
            } catch (Exception ex) {
                logger.Error(ex);
                return false;
            }
        }

        public async Task<string> StartSimpleListener() {
            if (!HttpListener.IsSupported) {
                logger.Error("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return "Windows XP SP2 or Server 2003 is required to use the HttpListener class.";
            }

            var tcs = new TaskCompletionSource<string>();
            await Task.Factory.StartNew(async () => {
                threadToCancel = Thread.CurrentThread;

                try {
                    listener = new HttpListener();
                    listener.Prefixes.Add(serverURL);
                    listener.Start();
                    logger.Trace("Listening on " + serverURL + "...");
                    Console.WriteLine($"{name} Listening on " + serverURL + "...");
                } catch (Exception ex) {
                    logger.Error($"Could not start HTTP Listener {ex.Message}");
                    Console.WriteLine($"Could not start HTTP Listener {ex.Message}");
                }

                // Received messages are written to a buffer queue
                // The written records are read in the main Listen method and returned
                while (true) {
                    HttpListenerContext context = null;
                    try {
                        context = await listener.GetContextAsync();
                    } catch (Exception) {
                        break;
                    }

                    string message;
                    try {
                        message = messageQueue.Dequeue();
                    } catch (Exception) {
                        Console.WriteLine($"{name} No Data Available for HTTP Request");
                        message = "No Data Available";
                    }

                    // Obtain a response object.
                    HttpListenerResponse response = context.Response;
                    response.StatusCode = 200;

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
                    // Get a response stream and write the response to it.

                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                    if (message != "No Data Available")
                        Console.WriteLine($"HTTP Server: {name}. Message Dispatched to caller. {messageQueue.Count} messages in queue");
                }
            });

            tcs.SetResult("Completed");
            return tcs.Task.Result;
        }
    }
}