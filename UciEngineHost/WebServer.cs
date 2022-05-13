using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using UciEngineHost.Helpers;

namespace UciEngineHost {
    public class WebServer {

        public Controller Controller { get; internal set; }
        private static readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new ConcurrentDictionary<Guid, WebSocket>();
        private static readonly ConcurrentDictionary<string, SocketClient> _clients = new ConcurrentDictionary<string, SocketClient>();

        // see https://github.com/lichess-org/external-engine#protocol for designed protocol

        public WebServer() { }

        public async Task HandleRequest(HttpContext context) {
            string session = context.Request.Query["session"];
            string secret = context.Request.Query["secret"];

            if (string.IsNullOrWhiteSpace(session) || string.IsNullOrWhiteSpace(secret)) {
                return;
            }

            if (context.WebSockets.IsWebSocketRequest) {
                await this.HandleSocket(context, session);
            } else {
                // show a simple html page with instructions?
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("hello");
            }
        }

        private async Task HandleSocket(HttpContext context, string session) {

            CancellationToken ct = context.RequestAborted;
            var socketId = Guid.NewGuid();
            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            _sockets.TryAdd(socketId, socket);

            SocketClient client = _clients.GetOrAdd(session, (s) => new SocketClient(s, this.Controller));
            client.AddSocket(socketId, socket);

            Console.WriteLine("Socket connected: " + session);

            while (true) {
                if (ct.IsCancellationRequested) {
                    break;
                }

                var response = await socket.ReceiveStringAsync(ct);

                if (string.IsNullOrEmpty(response)) {
                    if (socket.State != WebSocketState.Open) {
                        Console.WriteLine("Socket is no longer open");
                        break;
                    }
                    continue;
                }

                Console.WriteLine($"[Recv][{session}:{socketId}] {response}");

                try {
                    _ = client.OnMessage(response);
                } catch { }
            }

            try {
                client.RemoveSocket(socketId);
                _sockets.TryRemove(socketId, out WebSocket _);

                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                socket.Dispose();
            } catch { }
        }

    }
}
