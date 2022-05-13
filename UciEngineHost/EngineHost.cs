using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UciEngineHost.Helpers;
using UciEngineHost.Models;

namespace UciEngineHost {
    public class EngineHost {

        private readonly Configuration _configuration;
        private UciEngine? _uci;

        public EngineHost(Configuration configuration) {
            _configuration = configuration;

            this.CurrentEngine = null;
            _uci = null;

            this.ProcessorCount = Environment.ProcessorCount;
            try {
                this.AvailableMemory = (int)(new ComputerInfo().TotalPhysicalMemory / (1024 * 1024));
            } catch { }

            foreach (var engine in _configuration.Engines) {
                Task.Run(() => SetupEngine(engine));
            }
        }

        public EngineDefinition? CurrentEngine { get; private set; }
        public int ProcessorCount { get; private set; }
        public int AvailableMemory { get; private set; }

        private async Task SetupEngine(EngineDefinition engine) {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Engines", engine.Path);
            if (!File.Exists(path)) {
                return;
            }

            engine.ResolvedPath = path;

            try {
                var uci = new UciEngine(path);
                await uci.Start();

                engine.Options = uci.Options;
                engine.UciName = uci.Name;
                engine.UciAuthor = uci.Author;
                uci.Dispose();

                engine.IsReady = true;
                if (engine.Selected) {
                    this.CurrentEngine = engine;
                }
            } catch {
                engine.IsReady = false;
            }
        }

        internal async Task StartAnalysis(AnalysisRequest analysis, Dictionary<string, string>? options, Action<EngineEval> onEvalReceived, Action<EngineEval> onBestMove) {
            //TODO if still setting up, give the engines a chance to load
            Console.WriteLine("Host analyzing");

            if (string.IsNullOrEmpty(analysis.InitialFen)) {
                throw new ArgumentNullException("InitialFen");
            }

            try {
                if (CurrentEngine != null) {
                    if (_uci == null) {
                        _uci = new UciEngine(CurrentEngine.ResolvedPath!);
                        await _uci.Start();
                    }

                    if (_uci.Running) {
                        await _uci.StopAsync();
                    }

                    Console.WriteLine("Setting multipv");
                    await _uci.SetOption("MultiPV", "4");

                    if (analysis.Moves != null && analysis.Moves.Count > 0) {
                        await _uci.SetFenAndMoves(analysis.InitialFen, analysis.Moves);
                    } else {
                        await _uci.SetFenPosition(analysis.InitialFen);
                    }

                    _uci.OnData = eval => {
                        onEvalReceived(eval);
                    };
                    _uci.OnBestMove = eval => {
                        onBestMove(eval);
                    };

                    await _uci.Analyze();

                } else {
                    throw new Exception("Engine could not be loaded");
                }
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        internal async Task StopAsync() {
            if (_uci != null) {
                Console.WriteLine("Engine host stopping");
                await _uci.StopAsync();
            }
        }
    }
}
