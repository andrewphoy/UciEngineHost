using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UciEngineHost.Models;

namespace UciEngineHost.Helpers {
    internal class SocketClient {

        private static JsonSerializerOptions _serializerOptions = new JsonSerializerOptions {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static Regex _regexOption = new Regex(
            @"setoption\s+name\s+(?<name>.+)\s+value\s+(?<value>.+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
        );

        private static Regex _regexPosition = new Regex(
            @"position\s+fen\s+(?<fen>.+?)\s+moves\s*(?<moves>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
        );

        private readonly object _lockUci = new object();
        private readonly Controller _controller;
        private readonly ConcurrentDictionary<Guid, WebSocket> _sockets;
        private readonly Dictionary<string, string> _options;
        private AnalysisRequest? _nextAnalysis;
        private long _lastCounter;

        public SocketClient(string session, Controller controller) {
            _sockets = new ConcurrentDictionary<Guid, WebSocket>();
            _options = new Dictionary<string, string>();
            _controller = controller;
            _lastCounter = 0;
            this.Session = session;
        }

        public string Session { get; }

        public void AddSocket(Guid id, WebSocket ws) {
            _sockets[id] = ws;
        }

        public void RemoveSocket(Guid id) {
            _sockets.TryRemove(id, out _);
        }

        public async Task SendStringAsync(string message) {
            var tasks = _sockets.Values.Select(async s => await s.SendStringAsync(message));
            await Task.WhenAll(tasks);
        }

        public async Task OnMessage(string message) {
            // allow uci commands or a json object
            if (Regex.IsMatch(@"\s*\{", message, RegexOptions.Compiled)) {
                try {
                    var request = JsonSerializer.Deserialize<AnalysisRequest>(message, _serializerOptions);
                    if (request != null && !string.IsNullOrEmpty(request.InitialFen)) {
                        // start analyzing with the requested options
                        request.Session = this.Session;
                        request.VerboseEvaluation = true;

                        await this.StartAnalysis(request, null, false);
                        
                    }
                } catch { }
            } else {
                message = message.Trim();
                if (message.Equals("uci")) {
                    // reply with options for the current engine
                    await _controller.SendUciHeader(this);

                } else if (message.Equals("isready")) {
                    await _controller.SendIsReady(this);

                } else if (message.StartsWith("setoption", StringComparison.OrdinalIgnoreCase)) {
                    var match = _regexOption.Match(message);
                    if (match.Success) {
                        lock (_lockUci) {
                            _options[match.Groups["name"].Value] = match.Groups["value"].Value; ;
                        }
                    }

                } else if (message.StartsWith("ucinewgame", StringComparison.OrdinalIgnoreCase)) {
                    // force ucinewgame with the next request
                    lock (_lockUci) {
                        if (_nextAnalysis == null) {
                            _nextAnalysis = new AnalysisRequest {
                                Session = this.Session
                            };
                        }
                        _nextAnalysis.UciNewGame = true;
                    }

                } else if (message.StartsWith("position", StringComparison.OrdinalIgnoreCase)) {
                    string? fen = null;
                    string? moves = null;

                    if (message.StartsWith("position startpos", StringComparison.OrdinalIgnoreCase)) {
                        fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
                        string movesPortion = message.Substring("position startpos".Length).Trim();
                        if (movesPortion.StartsWith("moves")) {
                            moves = movesPortion.Substring(5).Trim();
                        }

                    } else {
                        if (message.Contains("moves", StringComparison.OrdinalIgnoreCase)) {
                            var match = _regexPosition.Match(message);
                            if (match.Success) {
                                fen = match.Groups["fen"].Value;
                                moves = match.Groups["moves"].Value?.Trim();
                            }
                        } else {
                            fen = message.Substring("position fen ".Length).Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(fen)) {
                        lock (_lockUci) {
                            if (_nextAnalysis == null) {
                                _nextAnalysis = new AnalysisRequest {
                                    Session = this.Session
                                };
                            }
                            _nextAnalysis.InitialFen = fen;
                            if (!string.IsNullOrEmpty(moves)) {
                                _nextAnalysis.Moves = moves.Split(' ').ToList();
                            }
                        }
                    }

                } else if (message.StartsWith("go ", StringComparison.OrdinalIgnoreCase)) {
                    // do we want to respect the movetime/depth/nodes from the browser?
                    AnalysisRequest? analysis;
                    Dictionary<string, string> options;

                    lock (_lockUci) {
                        options = _options.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        analysis = _nextAnalysis;
                        _nextAnalysis = null;
                    }

                    Console.WriteLine("Starting analysis");

                    if (analysis != null) {
                        await this.StartAnalysis(analysis, options, true);
                    }
                } else if (message.StartsWith("stop", StringComparison.OrdinalIgnoreCase)) {
                    await _controller.StopAnalysis(this);
                }
            }
        }

        private async Task StartAnalysis(AnalysisRequest request, Dictionary<string, string> options, bool uciProtocol) {
            _lastCounter = 0;
            var analysis = new Analysis {
                Request = request,
            };

            if (uciProtocol) {
                analysis.OnDataReceived = async (engine, eval) => {
                    //Console.Write("On data received");
                    if (eval.Counter < _lastCounter) {
                        //Console.WriteLine(": skipping " + eval.Counter);
                        return;
                    } else {
                        //Console.WriteLine(": using " + eval.Counter);
                        _lastCounter = eval.Counter;
                    }

                    foreach (var pv in eval.Variations.Values) {
                        if (pv.ScoreCp.HasValue || pv.ScoreMate.HasValue) {
                            string evalString = pv.ScoreCp.HasValue ? "cp " + pv.ScoreCp.Value.ToString() : "mate " + pv.ScoreMate.Value.ToString();
                            await this.SendStringAsync($"info depth {pv.Depth} seldepth {pv.Seldepth} multipv {pv.MultiPv} score {evalString} nodes {pv.Nodes} nps {pv.NodesPerSecond} hashfull {pv.HashFull} tbhits {pv.TableBaseHits} time {pv.ElapsedMs} pv {pv.Variation}");
                        }
                    }
                };
                analysis.OnStop = async (engine, eval) => {
                    if (!string.IsNullOrEmpty(eval.BestMove)) {
                        await analysis.OnDataReceived(engine, eval);
                        Console.WriteLine("Sending best move");
                        await this.SendStringAsync("bestmove " + eval.BestMove);
                    }
                };
            } else {
                analysis.OnDataReceived = async (engine, eval) => {
                    string json = JsonSerializer.Serialize(eval);
                    await this.SendStringAsync(json);
                };
                analysis.OnStop = async (engine, eval) => {
                    if (!string.IsNullOrEmpty(eval.BestMove)) {
                        await analysis.OnDataReceived(engine, eval);
                        await this.SendStringAsync("bestmove " + eval.BestMove);
                    }
                };
            }

            _ = Task.Run(() => this._controller.StartAnalysis(this, analysis));
        }


    }
}
