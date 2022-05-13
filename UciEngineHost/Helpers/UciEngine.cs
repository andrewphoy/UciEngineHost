using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UciEngineHost.Models;

namespace UciEngineHost.Helpers {
    internal class UciEngine {

        private readonly Process _process;
        private readonly SemaphoreSlim _syncStart = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _syncReady = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _syncAnalyzing = new SemaphoreSlim(0, 1);

        public Dictionary<string, UciOption> Options { get; private set; }
        public bool Running { get; private set; }

        public bool WhiteToMove { get; private set; }
        private EngineEval? _eval;
        private int _multiPv = 1;

        public Action<EngineEval>? OnData { get; set; }
        public Action<EngineEval>? OnBestMove { get; set; }

        public UciEngine(string uciPath) {
            this.Options = new Dictionary<string, UciOption>();
            this.Running = false;
            _eval = null;

            var si = new ProcessStartInfo {
                FileName = uciPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            _process = new Process {
                StartInfo = si
            };

            _process.ErrorDataReceived += Process_ErrorDataReceived;
            _process.OutputDataReceived += Process_OutputDataReceived;

            _process.Exited += (s, e) => {
                Console.WriteLine("Engine exited");
            };
        }

        /// <summary>
        /// Starts the UCI engine
        /// Sends the "uci" command and waits for the engine to be ready ("isready"/"readyok")
        /// </summary>
        /// <returns></returns>
        public async Task Start() {
            _process.Start();
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();

            await SendLineAsync("uci");
            await _syncStart.WaitAsync();

            await SendLineAsync("isready");
            await _syncReady.WaitAsync();
        }

        public async Task StopAsync() {
            await SendLineAsync("stop");
            Running = false;
        }

        #region ReadOnly Properties
        public string? Name { get; private set; }
        public string? Author { get; private set; }
        public bool IsAlive {
            get {
                try {
                    return !_process.HasExited;
                } catch {
                    this.Dispose();
                    return false;
                }
            }
        }
        //public int Depth { get { return _depth; } }
        //public int Nodes { get { return _cntNodes; } }
        //public int Time { get { return _elapsedTime; } }
        //public float Score { get { return myPVs[0].RawEval; } }
        //public EngineLine[] PVs { get { return myPVs; } }
        #endregion

        public void Dispose() {
            try {
                if (Running) {
                    SendLineAsync("stop").Wait();
                }
                SendLineAsync("quit").Wait();
                _process.WaitForExit();
            } catch { }
            // todo kill other child processes

            try {
                if (!_process.HasExited) {
                    _process.Close();
                    _process.Kill();
                }
            } catch { }

            try {
                _process.Dispose();
            } catch { }
        }

        #region Sending Data
        private async Task SendLineAsync(string command) {
            Console.WriteLine("[Engine][Send] " + command);
            await _process.StandardInput.WriteLineAsync(command);
            await _process.StandardInput.FlushAsync();
        }
        #endregion

        #region Receiving Data
        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            // Leela sometimes writes on the error stream
            //if (Debugger.IsAttached) { Debugger.Break(); }
            Console.WriteLine(e.Data);
        }

        private readonly object _lockOutputData = new object();
        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            lock (_lockOutputData) {
                ParseReceivedLine(e.Data);
            }
        }

        private void ParseReceivedLine(string? line) {
            try {
                if (string.IsNullOrWhiteSpace(line)) {
                    return;
                }
                string[] words = line.Split(new char[] { '\t', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) {
                    return;
                }
                ParseWords(words);
            } catch { }
        }

        /*
        info depth 20 seldepth 30 multipv 1 score cp 76 nodes 3501156 nps 1309822 hashfull 899 tbhits 0 time 2673 pv d2d4 d7d5 g1f3 g8f6 c2c4 e7e6 b1c3 c7c5 c4d5 e6d5 c1g5 f8e7 d4c5 e8g8 e2e3 e7c5 f1d3 c8e6 e1g1 b8c6 a2a3 c5d6 c3b5
        info depth 20 seldepth 29 multipv 2 score cp 70 nodes 3501156 nps 1309822 hashfull 899 tbhits 0 time 2673 pv e2e4 e7e6 d2d4 d7d5 b1c3 f8b4 e4d5 e6d5 f1d3 b8c6 g1f3 g8f6 d1e2 c8e6 a2a3 b4c3 b2c3 e8g8 a1b1
        info depth 20 seldepth 29 multipv 3 score cp 67 nodes 3501156 nps 1309822 hashfull 899 tbhits 0 time 2673 pv e2e3 g8f6 g1f3 d7d5 c2c4 e7e6 f1e2 c7c5 d2d4 c5d4 e3d4 b8c6 b1c3 f8e7 e1g1 e8g8 a2a3 c8d7 f3e5 d5c4 c1f4 c6e5 d4e5
        info depth 20 seldepth 25 multipv 4 score cp 61 nodes 3501156 nps 1309822 hashfull 899 tbhits 0 time 2673 pv g1f3 e7e6 d2d4 c7c5 e2e3 g8f6 c2c4 d7d5 f1d3 f8d6 b1c3 e8g8 e1g1 b8c6 c4d5 e6d5 d4c5 d6c5 c1d2 f8e8 a1c1 a7a6 a2a3 c5d6
        bestmove d2d4 ponder d7d5
        */

        private void ParseWords(string[] words) {
            if (words.Length == 0) { return; }

            switch (words[0].ToLower()) {
                case "id":
                    if (words.Length > 3) {
                        if (words[1].ToLower() == "name") {
                            this.Name = string.Join(" ", words.Skip(2));
                        } else if (words[1].ToLower() == "author") {
                            this.Author = string.Join(" ", words.Skip(2));
                        }
                    }
                    break;

                case "uciok":
                    _syncStart.Release();
                    break;

                case "readyok":
                    _syncReady.Release();
                    break;

                case "bestmove":
                    if (_eval != null && words.Length >= 2) {
                        _eval.BestMove = words[1];
                    }
                    if (_eval != null) {
                        Task.Run(() => this.OnBestMove?.Invoke(_eval));
                    }
                    _syncAnalyzing.Release();
                    break;

                case "copyprotection":
                    break;

                case "registration":
                    break;

                case "info":
                    ExtractInfo(words.Skip(1).ToArray());
                    break;

                case "option":
                    RegisterOption(words.Skip(1).ToArray());
                    break;

                default:
                    // not sure what to do, try the remaining words
                    if (words.Length > 1) {
                        ParseWords(words.Skip(1).ToArray());
                    } else {
                        // no more words, quietly ignore
                    }
                    break;
            }
        }

        private void ExtractInfo(string[] words) {
            int i = 0;

            string linePV = string.Empty;
            bool hasLine = false;
            int intResult;
            long longResult;

            EngineInfo info = new EngineInfo();
            info.WhiteToMove = this.WhiteToMove;

            bool done = false;
            while (!done) {
                switch (words[i].ToLower()) {
                    case "depth":
                        if (int.TryParse(words[i + 1], out intResult)) {
                            info.Depth = intResult;
                        }
                        i += 2;
                        break;

                    case "seldepth":
                        if (int.TryParse(words[i + 1], out intResult)) {
                            info.Seldepth = intResult;
                        }
                        i += 2;
                        break;

                    case "time":
                        if (long.TryParse(words[i + 1], out longResult)) {
                            info.ElapsedMs = longResult;
                        }
                        i += 2;
                        break;

                    case "nodes":
                        if (long.TryParse(words[i + 1], out longResult)) {
                            info.Nodes = longResult;
                        }
                        i += 2;
                        break;

                    case "pv":
                        hasLine = true;
                        info.Variation = string.Join(" ", words.Skip(i + 1));
                        done = true;
                        break;

                    case "multipv":
                        hasLine = true;
                        info.MultiPv = int.Parse(words[i + 1]);
                        i += 2;
                        break;

                    case "wdl":
                        info.WdlWin = int.Parse(words[i + 1]);
                        info.WdlDraw = int.Parse(words[i + 2]);
                        info.WdlLoss = int.Parse(words[i + 3]);
                        i += 4;
                        break;

                    case "score":
                        if (int.TryParse(words[i + 2], out intResult)) {
                            if (string.Compare(words[i + 1], "mate", true) == 0) {
                                info.ScoreMate = intResult;
                            } else if (string.Compare(words[i + 1], "cp", true) == 0) {
                                info.ScoreCp = intResult;
                            } else {
                                info.ScoreCp = intResult;
                            }
                        }
                        i += 3;
                        break;

                    case "nps":
                        if (long.TryParse(words[i + 1], out longResult)) {
                            info.NodesPerSecond = longResult;
                        }
                        i += 2;
                        break;

                    case "tbhits":
                        if (int.TryParse(words[i + 1], out intResult)) {
                            info.TableBaseHits = intResult;
                        }
                        i += 2;
                        break;

                    case "hashfull":
                        if (int.TryParse(words[i + 1], out intResult)) {
                            info.HashFull = intResult;
                        }
                        i += 2;
                        break;

                    case "string":
                        done = true;
                        break;

                    default:
                        if (hasLine) {
                            linePV += " " + words[i].Trim();
                        }
                        i++;
                        break;
                }

                done = done || i >= words.Length;
            }

            // would it be better to batch this instead of keeping a single object?
            if (info.MultiPv > 0) {
                if (_eval != null) {
                    _eval.Variations[info.MultiPv] = info;
                }

                if (info.MultiPv == _multiPv) {
                    if (_eval != null) {
                        var swap = _eval;
                        _eval = new EngineEval {
                            WhiteToMove = this.WhiteToMove,
                            Counter = swap.Counter + 1,
                        };
                        Task.Run(() => this.OnData?.Invoke(swap));
                    }
                }
            }
        }

        /*
        option name Debug Log File type string default
        option name Threads type spin default 1 min 1 max 512
        option name Hash type spin default 16 min 1 max 33554432
        option name Clear Hash type button
        option name Ponder type check default false
        option name MultiPV type spin default 1 min 1 max 500
        option name Skill Level type spin default 20 min 0 max 20
        option name Move Overhead type spin default 10 min 0 max 5000
        option name Slow Mover type spin default 100 min 10 max 1000
        option name nodestime type spin default 0 min 0 max 10000
        option name UCI_Chess960 type check default false
        option name UCI_AnalyseMode type check default false
        option name UCI_LimitStrength type check default false
        option name UCI_Elo type spin default 1350 min 1350 max 2850
        option name UCI_ShowWDL type check default false
        option name SyzygyPath type string default <empty>
        option name SyzygyProbeDepth type spin default 1 min 1 max 100
        option name Syzygy50MoveRule type check default true
        option name SyzygyProbeLimit type spin default 7 min 0 max 7
        option name Use NNUE type check default true
        option name EvalFile type string default nn-6877cd24400e.nnue
        option name Style type combo default Normal var Solid var Normal var Risky
        */

        private void RegisterOption(string[] words) {
            var opt = new UciOption();
            StringBuilder sb = new StringBuilder();
            int intResult;

            int i = 0;
            bool readingName = false;
            bool done = false;
            while (!done) {
                switch (words[i].ToLower()) {
                    case "name":
                        readingName = true;
                        sb.Append(words[i + 1]);
                        i += 2;
                        break;

                    case "type":
                        readingName = false;
                        opt.Type = words[i + 1];
                        i += 2;
                        break;

                    case "default":
                        readingName = false;
                        if (words.Length > (i + 1)) {
                            opt.Default = words[i + 1];
                        }
                        i += 2;
                        break;

                    case "min":
                        readingName = false;
                        if (int.TryParse(words[i + 1], out intResult)) {
                            opt.MinValue = intResult;
                        }
                        i += 2;
                        break;

                    case "max":
                        readingName = false;
                        if (int.TryParse(words[i + 1], out intResult)) {
                            opt.MaxValue = intResult;
                        }
                        i += 2;
                        break;

                    case "var":
                        readingName = false;
                        if (opt.Options == null) {
                            opt.Options = new List<string>();
                        }
                        opt.Options.Add(words[i + 1]);
                        i += 2;
                        break;

                    default:
                        if (readingName) {
                            sb.Append(" " + words[i].Trim());
                        }
                        i++;
                        break;
                }

                done = done || i >= words.Length;
            }

            opt.Name = sb.ToString();
            this.Options[opt.Name] = opt;
        }
        #endregion

        public async Task UciNewGame() {
            this.WhiteToMove = true;
            await SendLineAsync("ucinewgame");
        }

        private bool ValidateFen(string fen) {
            var fenRegex = new Regex(@"\s*([rnbqkpRNBQKP1-8]+\/){7}([rnbqkpRNBQKP1-8]+)\s[bw-]\s(([a-hkqA-HKQ]{1,4})|(-))\s(([a-h][36])|(-))\s\d+\s\d+\s*");
            return fenRegex.IsMatch(fen);
        }

        public async Task SetFenPosition(string fen) {
            //if (!ValidateFen(fen)) {
            //    throw new ArgumentException("Invalid FEN, cannot continue", nameof(fen));
            //}
            var parts = fen.Split(' ');
            if (parts.Length < 3) {
                throw new ArgumentException("Invalid FEN, not enough parts", nameof(fen));
            }

            if (parts[1].ToLowerInvariant() == "w") {
                this.WhiteToMove = true;
            } else if (parts[1].ToLowerInvariant() == "b") {
                this.WhiteToMove = false;
            } else {
                throw new ArgumentOutOfRangeException(nameof(fen), "Invalid FEN, color to move not recognized");
            }

            await SendLineAsync("position fen " + fen);
        }

        public async Task SetFenAndMoves(string fen, IEnumerable<string> moves) {
            var parts = fen.Split(' ');
            if (parts.Length < 3) {
                throw new ArgumentException("Invalid FEN, not enough parts", nameof(fen));
            }

            bool whiteToMove;
            if (parts[1].ToLowerInvariant() == "w") {
                whiteToMove = true;
            } else if (parts[1].ToLowerInvariant() == "b") {
                whiteToMove = false;
            } else {
                throw new ArgumentOutOfRangeException(nameof(fen), "Invalid FEN, color to move not recognized");
            }

            string strMoves = string.Join(' ', moves);

            if (moves.Count() % 2 == 1) {
                whiteToMove = !whiteToMove;
            }
            this.WhiteToMove = whiteToMove;

            await SendLineAsync($"position fen {fen} moves {strMoves}");
        }

        public async Task<EngineEval> Analyze(int? nodes = null) {
            _eval = new EngineEval {
                WhiteToMove = this.WhiteToMove,
                Counter = 1
            };

            if (nodes.HasValue && nodes.Value > 0) {
                await SendLineAsync($"go nodes {nodes.Value}");
                await _syncAnalyzing.WaitAsync();
            } else {
                await SendLineAsync("go infinite");
            }

            return _eval;
        }

        public async Task SetOption(string name, string value) {
            if (name.Equals("MultiPV", StringComparison.OrdinalIgnoreCase)) {
                _multiPv = int.Parse(value);
            }
            await SendLineAsync($"setoption name {name} value {value}");
        }
    }
}
