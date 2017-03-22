using System;
using System.Globalization;
using System.Threading.Tasks;
using Infusion.ServerSentEvents;

namespace Microsoft.AspNetCore.Http
{
    internal static partial class Extensions
    {

        internal static async Task WriteSseRetryAsync(this HttpResponse response, uint reconnectInterval)
        {
            await response.WriteSseEventFieldAsync(Constants.SSE_RETRY_FIELD, reconnectInterval.ToString(CultureInfo.InvariantCulture));
            await response.WriteSseEventBoundaryAsync();
            response.Body.Flush();
        }

        internal static async Task WriteSseEventAsync(this HttpResponse response, string text)
        {
            await response.WriteSseEventFieldAsync(Constants.SSE_DATA_FIELD, text);
            await response.WriteSseEventBoundaryAsync();
            response.Body.Flush();
        }

        internal static async Task WriteSseEventAsync(this HttpResponse response, ServerSentEvent serverSentEvent)
        {
            if (!String.IsNullOrWhiteSpace(serverSentEvent.Id))
            {
                await response.WriteSseEventFieldAsync(Constants.SSE_ID_FIELD, serverSentEvent.Id);
            }

            if (!String.IsNullOrWhiteSpace(serverSentEvent.Type))
            {
                await response.WriteSseEventFieldAsync(Constants.SSE_EVENT_FIELD, serverSentEvent.Type);
            }

            if (serverSentEvent.Data != null)
            {
                foreach(string data in serverSentEvent.Data)
                {
                    await response.WriteSseEventFieldAsync(Constants.SSE_DATA_FIELD, data);
                }
            }

            await response.WriteSseEventBoundaryAsync();
            response.Body.Flush();
        }

        private static Task WriteSseEventFieldAsync(this HttpResponse response, string field, string data)
        {
            return response.WriteAsync($"{field}: {data}\n");
        }

        private static Task WriteSseEventBoundaryAsync(this HttpResponse response)
        {
            return response.WriteAsync("\n");
        }
    }
}