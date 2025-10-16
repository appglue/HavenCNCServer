using Microsoft.AspNetCore.SignalR;

namespace HavenCNCServer.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time CNC message distribution to clients
    /// </summary>
    public class CNCMessageHub : Hub
    {
        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "CNCClients");
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "CNCClients");
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Allow clients to join specific message type groups
        /// </summary>
        /// <param name="messageType">Type of messages to subscribe to</param>
        public async Task SubscribeToMessageType(string messageType)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"MessageType_{messageType}");
        }

        /// <summary>
        /// Allow clients to leave specific message type groups
        /// </summary>
        /// <param name="messageType">Type of messages to unsubscribe from</param>
        public async Task UnsubscribeFromMessageType(string messageType)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"MessageType_{messageType}");
        }
    }
}