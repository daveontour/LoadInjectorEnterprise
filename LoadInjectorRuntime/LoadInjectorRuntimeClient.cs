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
        private NgExecutionController controller;

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

        public void OnStop() {
            controller.ProgramStop();
        }

        public void OnStart() {
            if (ExecuteFile == null) {
                Thread mainThread = new Thread(this.StartClientMode) {
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
            controller = new NgExecutionController(ExecutionControllerType.StandAlone, executeFile: ExecuteFile);
        }

        private void StartClientMode() {
            controller = new NgExecutionController(ExecutionControllerType.Client, serverHub: Server);
        }
    }
}