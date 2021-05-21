using Microsoft.AspNet.SignalR.Hubs;

namespace LoadInjector.Runtime.EngineComponents {

    public interface ICCController {

        void InitialInterrogation(HubCallerContext connectionId);

        void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, string status, HubCallerContext context);

        void RefreshResponse(string processID, string ipAddress, string osversion, string xml, string status, HubCallerContext context);

        void UpdateLine(string executionNodeID, string uuid, string message, int messagesSent, double currentRate, double messagesPerMinute, HubCallerContext context);

        void UpdateLine(string executionNodeID, string uuid, int messagesSent, HubCallerContext context);

        void Disconnect(HubCallerContext context);

        void SetExecutionNodeStatus(string executionNodeID, string message, HubCallerContext context);
        void SetConsoleMessage(string message);
    }
}