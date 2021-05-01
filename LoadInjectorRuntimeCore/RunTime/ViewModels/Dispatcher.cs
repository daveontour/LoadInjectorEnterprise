using System;

namespace LoadInjector.RunTime.ViewModels {
    public class Dispatcher {
        public void Fire(TriggerFiredEventArgs args) {
            try {
                TriggerFire?.Invoke(this, args);
            } catch (Exception ex) {
                Console.WriteLine($"Internal Dispatcher Error {ex.Message}");
                throw;
            }
        }
        public event EventHandler<TriggerFiredEventArgs> TriggerFire;
    }
}
