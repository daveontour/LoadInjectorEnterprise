using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadInjectorBase.Commom {

    public class ClientState {

        private ClientState(string value) {
            Value = value;
        }

        public string Value { get; set; }

        public static ClientState Reset { get { return new ClientState("Reset"); } }
        public static ClientState UnAssigned { get { return new ClientState("Un Assigned"); } }
        public static ClientState NoActive { get { return new ClientState("No Active Destination"); } }
        public static ClientState Assigned { get { return new ClientState("Work Package Assigned"); } }
        public static ClientState Ready { get { return new ClientState("Ready to Execute"); } }
        public static ClientState ConfigInvalid { get { return new ClientState("Configuration Invalid"); } }
        public static ClientState ExecutionPending { get { return new ClientState("Waiting for Start Time"); } }
        public static ClientState Executing { get { return new ClientState("Executing"); } }
        public static ClientState ExecutionComplete { get { return new ClientState("Execution Complete"); } }
        public static ClientState Stopped { get { return new ClientState("Stopped"); } }
        public static ClientState WaitingNextIteration { get { return new ClientState("Waiting for next iteration"); } }
    }
}