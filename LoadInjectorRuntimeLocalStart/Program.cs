using LoadInjector.RunTime;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadInjectorRuntimeLocalStart {

    public class Program {
        private static NgExecutionController controller;
        private static string executeFile = null;
        private static string server = null;
        private static string report = null;
        private static string port = null;
        private static string localStart = null;
        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");

        private static void Main(string[] args) {
            if (args != null) {
                foreach (string arg in args) {
                    if (arg.StartsWith("-port:")) {
                        port = arg.Split(':')[1];
                    }
                    if (arg.StartsWith("-execute:")) {
                        executeFile = arg.Split(':')[1];
                    }
                    if (arg.StartsWith("-server:")) {
                        server = arg.Split(':')[1];
                    }
                    if (arg.StartsWith("-report:")) {
                        report = arg.Split(':')[1];
                    }
                    if (arg.StartsWith("-localStart:")) {
                        localStart = arg.Split(':')[1];
                    }
                }
            }

            Thread backgroundThread = new Thread(new ThreadStart(OnStart)) {
                IsBackground = false
            };
            backgroundThread.Start();
            backgroundThread.Join();

            logger.Info("End");
        }

        public static void OnStart() {
            if (executeFile == null) {
                Thread mainThread = new Thread(StartClientMode) {
                    IsBackground = false
                };

                try {
                    mainThread.Start();
                } catch (Exception ex) {
                    logger.Info($"Thread start error {ex.Message}");
                }
            } else {
                Thread mainThread = new Thread(ExecuteLocal) {
                    IsBackground = false
                };

                try {
                    mainThread.Start();
                } catch (Exception ex) {
                    logger.Info($"Thread start error {ex.Message}");
                }
            }

            Task.Delay(-1).Wait();
        }

        private static void ExecuteLocal() {
            controller = new NgExecutionController(ExecutionControllerType.StandAlone, executeFile: executeFile, reportFile: report, localStart: localStart);
        }

        private static void StartClientMode() {
            controller = new NgExecutionController(ExecutionControllerType.Client, serverHub: $"http://{server}:{port}/", localStart: localStart);
        }

        //private static void DoSomeHeavyLifting() {
        //    logger.Info("Thread Start");

        //    controller = new NgExecutionController(ExecutionControllerType.Client, serverHub: $"http://localhost:6220/", localStart: "true");

        //    Task.Delay(-1).Wait();

        //    logger.Info("Thread End");
        //}
    }
}