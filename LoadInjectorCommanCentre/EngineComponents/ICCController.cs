using Microsoft.AspNet.SignalR.Hubs;

namespace LoadInjector.Runtime.EngineComponents {

    public interface ICCController {

        void InitialInterrogation(HubCallerContext connectionId);

        void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, HubCallerContext context);

        void RefreshResponse(string processID, string ipAddress, string osversion, string xml, HubCallerContext context);

        void UpdateLine(string executionNodeID, string uuid, string message, int messagesSent, double currentRate, double messagesPerMinute, HubCallerContext context);
    }
}