using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace LoadInjector.Runtime.EngineComponents {

    public class SimpleHTTPServer {
        private readonly NLog.Logger logger = NLog.LogManager.GetLogger("LoadInjector");

        private static readonly IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {

        #region extension to MIME type list

        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"},
        #endregion extension to MIME type list
    };

        private Thread _serverThread;
        private string _rootDirectory;
        public string _serverPath;
        private HttpListener _listener;
        private int _port;
        public bool _useHTTPS = false;
        public string _host;

        public SimpleHTTPServer(string path, string serverPath) {
            this._rootDirectory = path;
            this._serverPath = serverPath;

            string temp = serverPath.Substring(serverPath.LastIndexOf(':') + 1);
            temp = temp.Substring(0, temp.IndexOf('/'));
            _port = int.Parse(temp);
            _useHTTPS = serverPath.ToLower().Contains("https:");

            int index = _serverPath.IndexOf('/') + 2;
            string temp2 = _serverPath.Substring(index, _serverPath.Length - index);
            _host = temp2.Substring(0, temp2.IndexOf(':'));

            this.Initialize(path, serverPath);
        }

        private void Initialize(string path, string serverPath) {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_serverPath);

            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }

        public void Stop() {
            if (_listener.IsListening) {
                _listener.Stop();
            }
            _serverThread.Abort();
        }

        private void Listen() {
            try {
                _listener = new HttpListener();
                _listener.Prefixes.Add(_serverPath);
                _listener.Start();
            } catch (Exception ex1) {
                logger.Warn(ex1.Message);
                try {
                    logger.Warn($"Insufficient authority to bind to IP address {_host}. Binding to localhost only");

                    _listener = new HttpListener();
                    _serverPath = _serverPath.Replace(_host, "localhost");
                    _listener.Prefixes.Add(_serverPath);
                    _host = "localhost";

                    _listener.Start();
                } catch (Exception ex) {
                    logger.Error($"Unable to start HTTP Server due to error: {ex.Message}");
                    logger.Error(ex.StackTrace);
                    return;
                }
            }
            while (true) {
                try {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing HTTP Context  {ex.Message}");
                } finally {
                    // Intentionally not doing anything
                }
            }
        }

        private void Context(IAsyncResult result) {
            HttpListener listener = (HttpListener)result.AsyncState;
            try {
                HttpListenerContext context = listener.EndGetContext(result);
                Process(context);

                context.Response.Close();
                _listener.BeginGetContext(Context, listener);
            } catch (ObjectDisposedException) {
                //Intentionally not doing anything with the exception.
            }
        }

        private void Process(HttpListenerContext context) {
            string filename = context.Request.Url.AbsolutePath;

            filename = filename.Substring(1);

            filename = Path.Combine(_rootDirectory, filename);

            if (File.Exists(filename)) {
                try {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Adding permanent http response headers
                    context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out string mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                } catch (Exception) {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            } else {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            context.Response.OutputStream.Close();
        }
    }
}