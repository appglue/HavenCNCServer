using Microsoft.AspNetCore.SignalR;
using HavenCNCServer.Services;
using HavenCNCServer.Centriod.Events;

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
            
            // // Send current machine position immediately to the newly connected client
            // try
            // {
            //     var currentPosition = MachinePositionService.GetCurrentPosition();

            //     // Create a DRO event with current position
            //     var droEvent = new DROEvent
            //     {
            //         Timestamp = DateTime.Now,
            //         Axis1 = currentPosition.X,
            //         Axis2 = currentPosition.Y,
            //         Axis3 = currentPosition.Z,
            //         Axis4 = currentPosition.A,
            //         Axis5 = 0,
            //         Axis6 = 0,
            //         Axis7 = 0,
            //         Axis8 = 0,
            //         Message = "Initial position on connection",
            //         MessageType = "DROEvent"
            //     };
                
            //     // Send directly to this specific client
            //     await Clients.Caller.SendAsync("DROUpdate", droEvent.ToSignalRData());
                
            //     LoggingService.LogInfo($"Sent initial position to new SignalR client: X={currentPosition.X:F4}, Y={currentPosition.Y:F4}, Z={currentPosition.Z:F4}, A={currentPosition.A:F4}", "SignalR");
            // }
            // catch (Exception ex)
            // {
            //     LoggingService.LogWarning($"Failed to send initial position to SignalR client: {ex.Message}", "SignalR");
            // }
            
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