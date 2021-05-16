using Microsoft.AspNet.SignalR.Hubs;

namespace LoadInjector.Runtime.EngineComponents {

    public interface ICCController {

        void InitialInterrogation(HubCallerContext connectionId);

        void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, HubCallerContext context);
    }
}