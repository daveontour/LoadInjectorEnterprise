using System;
using Topshelf;

namespace LoadInjectorRuntime {

    // -execute:C:\Users\dave_\Desktop\pulse15sec.xml
    // -server:http://localhost:6220
    public class Program {

        private static void Main(string[] args) {
            NLog.Logger logger = NLog.LogManager.GetLogger("LoadInjectorClient");

            try {
                var exitCode = HostFactory.Run(x => {
                    string executeFile = null;
                    string server = null;
                    string report = null;
                    string port = null;
                    string localStart = null;
                    x.AddCommandLineDefinition("execute", f => { executeFile = f; });
                    x.AddCommandLineDefinition("server", srv => { server = srv; });
                    x.AddCommandLineDefinition("port", prt => { port = prt; });
                    x.AddCommandLineDefinition("report", rep => { report = rep; });
                    x.AddCommandLineDefinition("localStart", ls => { localStart = ls; });
                    x.ApplyCommandLine();
                    try {
                        x.Service<LoadInjectorRuntimeClient>(s => {
                            s.ConstructUsing(core => new LoadInjectorRuntimeClient(executeFile, server, port, report, localStart));
                            s.WhenStarted(core => core.OnStart());
                            s.WhenStopped(core => core.OnStop());
                        });
                    } catch (Exception ex) {
                        logger.Info($"Starting Load Injector Runtime Client Failed, Error {ex.Message}");
                    }
                    x.RunAsLocalSystem();

                    x.SetServiceName($"Load Injector Runtime Client");
                    x.SetDisplayName("Load Injector Runtime Client");
                    x.SetDescription($"Load Injector Runtime Client");
                });

                int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
                Environment.ExitCode = exitCodeValue;
            } catch (Exception e) {
                logger.Info($"Starting HOST Factory - failed (Outer), Error {e.Message}");
            }
        }
    }
}