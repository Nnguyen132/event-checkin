using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Infusion.ServerSentEvents
{
    /// <summary>
    /// Service which provides operations over Server-Sent Events protocol.
    /// </summary>
    public class ServerSentEventsService : IServerSentEventsService
    {
        #region Fields
        private readonly ConcurrentDictionary<Guid, ServerSentEventsClient> _clients = new ConcurrentDictionary<Guid, ServerSentEventsClient>();
        private readonly ILogger _logger;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the interval after which clients will attempt to reestablish failed connections.
        /// </summary>
        public uint? ReconnectInterval { get; private set; }
        #endregion

        public ServerSentEventsService(ILogger<IServerSentEventsService> logger){
            _logger = logger;
        }

        #region Methods
        /// <summary>
        /// Changes the interval after which clients will attempt to reestablish failed connections.
        /// </summary>
        /// <param name="reconnectInterval">The reconnect interval.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task ChangeReconnectIntervalAsync(uint reconnectInterval)
        {
            ReconnectInterval = reconnectInterval;

            return ForAllClientsAsync(client => client.ChangeReconnectIntervalAsync(reconnectInterval));
        }

        /// <summary>
        /// Sends event to all clients.
        /// </summary>
        /// <param name="text">The simple text event.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task SendEventAsync(string text)
        {
            _logger.LogDebug(1100, "SSE initiating send for all clients with data '{0}'", text);
            return ForAllClientsAsync(client => client.SendEventAsync(text));
        }

        /// <summary>
        /// Sends event to all clients.
        /// </summary>
        /// <param name="serverSentEvent">The event.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task SendEventAsync(ServerSentEvent serverSentEvent)
        {
            _logger.LogDebug(1100, "SSE initiating send for all clients with data '{0}'", serverSentEvent.ToString());
            return ForAllClientsAsync(client => client.SendEventAsync(serverSentEvent));
        }

        /// <summary>
        /// When overriden in delivered class allows for recovery when client has reestablished the connection.
        /// </summary>
        /// <param name="client">The client who has reestablished the connection.</param>
        /// <param name="lastEventId">The identifier of last event which client has received.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual Task OnReconnectAsync(IServerSentEventsClient client, string lastEventId)
        {
            _logger.LogDebug(1100, "SSE client reconnect");
            return System.Threading.TaskHelper.GetCompletedTask();
        }
 
        internal Guid AddClient(ServerSentEventsClient client)
        {
            Guid clientId = Guid.NewGuid();
            _clients.TryAdd(clientId, client);
            _logger.LogDebug(1100, "SSE added client with id '{0}'. '{1}' clients are registered.", clientId, _clients.Count);
            return clientId;
        }

        internal void RemoveClient(Guid clientId)
        {
            ServerSentEventsClient client;
            _clients.TryRemove(clientId, out client);
            if(client != null) _logger.LogDebug(1100, "SSE removed client with id '{0}'. '{1}' clients are registered.", clientId, _clients.Count);
        }

        private Task ForAllClientsAsync(Func<ServerSentEventsClient, Task> clientOperationAsync)
        {
            List<Task> clientsTasks = new List<Task>();
            foreach (ServerSentEventsClient client in _clients.Values)
            {
                clientsTasks.Add(clientOperationAsync(client));
            }
            return Task.WhenAll(clientsTasks);
        }
        #endregion
    }
}