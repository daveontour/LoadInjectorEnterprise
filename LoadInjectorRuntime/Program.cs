﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace LoadInjectorRuntime {

    internal class Program {

        private static void Main(string[] args) {
            NLog.Logger logger = NLog.LogManager.GetLogger("LoadInjectorClient");

            try {
                var exitCode = HostFactory.Run(x => {
                    try {
                        x.Service<LoadInjectorRuntimeClient>(s => {
                            s.ConstructUsing(core => new LoadInjectorRuntimeClient());
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