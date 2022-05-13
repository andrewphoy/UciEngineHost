using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UciEngineHost {
    public static partial class ExtensionMethods {

        public static async Task<string?> ReceiveStringAsync(this WebSocket socket, CancellationToken ct = default(CancellationToken)) {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            try {
                using (var ms = new MemoryStream()) {
                    WebSocketReceiveResult result;
                    do {
                        ct.ThrowIfCancellationRequested();

                        result = await socket.ReceiveAsync(buffer, ct);
                        if (buffer.Array != null) {
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    if (result.MessageType != WebSocketMessageType.Text) {
                        return "";
                    }

                    using (var reader = new StreamReader(ms, Encoding.UTF8)) {
                        return await reader.ReadToEndAsync();
                    }
                }
            } catch (OperationCanceledException) {
                // catch the exception if cancellation requested
                return null;
            } catch {
                return null;
            }
        }

        public static Task SendStringAsync(this WebSocket socket, string data, CancellationToken ct = default(CancellationToken)) {
            var buffer = Encoding.UTF8.GetBytes(data);
            var segment = new ArraySegment<byte>(buffer);
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }

    }
}
