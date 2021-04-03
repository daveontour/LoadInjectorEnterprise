using LoadInjector.Common;
using LoadInjector.RunTime;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class HttpServer : IDestinationType {
        public string name = "HTTPSERVER";
        public string description = "HTTP Server";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new HttpServerPropertyGrid(dataModel, view);
        }

        public SenderAbstract GetDestinationSender() {
            return new DestinationHttpServer();
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("HTTP Server Protocol Configuration")]
    public class HttpServerPropertyGrid : LoadInjectorGridBase {

        public HttpServerPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("HTTP Server URL"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The URL on the local server for clients to call. (including port and server name)")]
        public string ServerURL {
            get => GetAttribute("serverURL");
            set => SetAttribute("serverURL", value);
        }
    }

    internal class DestinationHttpServer : SenderAbstract {
        private HttpListener listener;
        private string serverURL;
        private readonly Queue<string> messageQueue = new Queue<string>();
        private Thread threadToCancel;
        private CancellationTokenSource source;
        private string name;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controlller, Logger logger) {
            base.Configure(defn, controller, logger);

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

        public override void Prepare() {
            source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            StartSimpleListener().Wait();
            Task.Factory.StartNew(() => {
                while (true) {
                    if (token.IsCancellationRequested && threadToCancel != null) {
                        threadToCancel.Abort();
                        Application.Current.Dispatcher.Invoke(delegate {
                            Console.WriteLine($"{name} HTTP Listener Thread aborted");
                        });

                        return;
                    }
                }
            });
        }

        public override void Stop() {
            source.Cancel();
            listener.Abort();
        }

        public override void Send(string val, List<Variable> vars) {
            messageQueue.Enqueue(val);
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

                // Recieved messages are written to a buffer queue
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