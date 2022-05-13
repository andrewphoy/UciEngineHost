using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UciEngineHost.Helpers;
using UciEngineHost.Models;

namespace UciEngineHost {
    public class Controller {

        public Controller(WebServer server, EngineHost engine, HostApplicationContext applicationContext) {
            this.Server = server;
            this.Server.Controller = this;

            this.Engine = engine;
            this.ApplicationContext = applicationContext;

            this.IsAnalyzing = false;
            this.CurrentAnalysisSession = null;
        }

        public WebServer Server { get; }
        public EngineHost Engine { get; }
        public HostApplicationContext ApplicationContext { get; }
        public bool IsAnalyzing { get; private set; }
        public string? CurrentAnalysisSession { get; private set; }


        internal async Task SendUciHeader(SocketClient socket) {
            var engine = this.Engine.CurrentEngine;
            if (engine != null && engine.IsReady) {
                // the name of the engine from the config file, not the UCI name
                await socket.SendStringAsync("id name " + engine.Name);
                await socket.SendStringAsync("id author " + engine.UciAuthor);

                foreach (var opt in engine.Options.Values) {
                    string line = $"option name {opt.Name} type {opt.Type}";
                    if (!string.IsNullOrEmpty(opt.Default)) {
                        line += " default " + opt.Default;
                    }
                    if (opt.MinValue.HasValue) {
                        line += " min " + opt.MinValue.ToString();
                    }
                    if (opt.MaxValue.HasValue) {
                        line += " min " + opt.MaxValue.ToString();
                    }
                    if (opt.Options != null && opt.Options.Count > 0) {
                        foreach (var o in opt.Options) {
                            line += " var " + o;
                        }
                    }
                    await socket.SendStringAsync(line);
                }

                await socket.SendStringAsync("uciok");
            }
        }

        internal async Task SendIsReady(SocketClient socket) {
            var engine = this.Engine.CurrentEngine;
            if (engine != null && engine.IsReady) {
                await socket.SendStringAsync("readyok");
            }
        }

        internal async Task StartAnalysis(SocketClient socket, Analysis analysis) {
            Console.WriteLine("Controller.StartAnalysis");
            this.IsAnalyzing = true;
            var def = this.Engine.CurrentEngine;
            if (def != null && analysis.Request != null && analysis.OnDataReceived != null && !string.IsNullOrEmpty(analysis.Request.InitialFen)) {
                this.CurrentAnalysisSession = socket.Session;
                Console.WriteLine("awaiting engine host start");
                await this.Engine.StartAnalysis(
                    analysis.Request, 
                    analysis.Parameters, 
                    async (eval) => await analysis.OnDataReceived(def, eval),
                    async (eval) => {
                        if (analysis.OnStop != null) {
                            await analysis.OnStop(def, eval);
                        }
                    }
                );
            }
        }

        internal async Task StartAnalysis(SocketClient socket, AnalysisRequest analysis, Dictionary<string, string>? options = null) {
            // if is running, send stop and then queue the next request


            this.IsAnalyzing = true;
            this.CurrentAnalysisSession = socket.Session;
            await this.Engine.StartAnalysis(
                analysis,
                options,
                async (EngineEval ee) => await this.SendEvaluation(socket, ee, analysis.VerboseEvaluation),
                (EngineEval ee) => throw new NotImplementedException()
            );
        }

        internal async Task StopAnalysis(SocketClient client) {
            if (client.Session.Equals(CurrentAnalysisSession)) {
                await this.Engine.StopAsync();
            }
        }

        internal async Task SendEvaluation(SocketClient socket, EngineEval eval, bool jsonMode) {
            if (jsonMode) {
                string json = JsonSerializer.Serialize(eval);
                await socket.SendStringAsync(json);

            } else {
                // uci protocol
                // use a strict format (instead of passing back what we receive)

                foreach (var pv in eval.Variations.Values) {
                    if (pv.ScoreCp.HasValue || pv.ScoreMate.HasValue) {
                        string evalString = pv.ScoreCp.HasValue ? "cp " + pv.ScoreCp.Value.ToString() : "mate " + pv.ScoreMate.Value.ToString();
                        await socket.SendStringAsync($"info depth {pv.Depth} seldepth {pv.Seldepth} multipv {pv.MultiPv} score {evalString} nodes {pv.Nodes} nps {pv.NodesPerSecond} hashfull {pv.HashFull} tbhits {pv.TableBaseHits} time {pv.ElapsedMs} pv {pv.Variation}");
                    }
                }

            }
        }
    }
}
