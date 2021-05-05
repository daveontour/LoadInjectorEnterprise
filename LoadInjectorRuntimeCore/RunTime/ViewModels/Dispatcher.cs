using NLog;
using System;

namespace LoadInjector.RunTime.ViewModels {

    public class Dispatcher {
        public static readonly Logger sourceLogger = LogManager.GetLogger("sourceLogger");

        public void Fire(TriggerFiredEventArgs args) {
            try {
                TriggerFire?.Invoke(this, args);
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Internal Dispatcher Error");
                throw;
            }
        }

        public event EventHandler<TriggerFiredEventArgs> TriggerFire;
    }
}