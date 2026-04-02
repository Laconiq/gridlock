using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Gridlock.Core
{
    public sealed class Profiler
    {
        public static Profiler Instance { get; private set; } = new();

        private readonly Dictionary<string, SectionData> _sections = new();
        private readonly List<string> _sectionOrder = new();
        private readonly Stopwatch _sw = new();

        private string? _activeSection;
        private long _activeTick;

        private int _frameCount;
        private readonly List<FrameSnapshot> _history = new();
        private readonly Dictionary<string, double> _currentFrame = new();

        private StreamWriter? _csvWriter;
        private bool _csvHeaderWritten;

        public bool Enabled { get; set; }
        public int FrameCount => _frameCount;

        public Profiler()
        {
            _sw.Start();
        }

        public void Begin(string name)
        {
            if (!Enabled) return;

            if (_activeSection != null)
                End();

            if (!_sections.ContainsKey(name))
            {
                _sections[name] = new SectionData();
                _sectionOrder.Add(name);
            }

            _activeSection = name;
            _activeTick = _sw.ElapsedTicks;
        }

        public void End()
        {
            if (!Enabled || _activeSection == null) return;

            long elapsed = _sw.ElapsedTicks - _activeTick;
            double ms = elapsed * 1000.0 / Stopwatch.Frequency;

            var data = _sections[_activeSection];
            data.TotalMs += ms;
            data.Count++;
            if (ms > data.MaxMs) data.MaxMs = ms;

            _currentFrame[_activeSection] = _currentFrame.GetValueOrDefault(_activeSection) + ms;
            _activeSection = null;
        }

        public void EndFrame(double totalFrameMs)
        {
            if (!Enabled) return;

            if (_activeSection != null)
                End();

            _frameCount++;

            var snap = new FrameSnapshot
            {
                FrameIndex = _frameCount,
                TotalMs = totalFrameMs,
                Sections = new Dictionary<string, double>(_currentFrame)
            };
            _history.Add(snap);

            if (_csvWriter != null)
            {
                if (!_csvHeaderWritten)
                {
                    _csvWriter.Write("frame,total_ms");
                    foreach (var name in _sectionOrder)
                        _csvWriter.Write($",{name}");
                    _csvWriter.WriteLine();
                    _csvHeaderWritten = true;
                }

                _csvWriter.Write($"{_frameCount},{totalFrameMs:F3}");
                foreach (var name in _sectionOrder)
                {
                    double v = _currentFrame.GetValueOrDefault(name);
                    _csvWriter.Write($",{v:F3}");
                }
                _csvWriter.WriteLine();
            }

            _currentFrame.Clear();
        }

        public void EnableCsvLog(string path)
        {
            _csvWriter = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = false };
        }

        public string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== PROFILER REPORT ===");
            sb.AppendLine($"Frames: {_frameCount}");

            if (_history.Count > 0)
            {
                var totalTimes = _history.Select(h => h.TotalMs).ToList();
                totalTimes.Sort();
                double avg = totalTimes.Average();
                double p50 = Percentile(totalTimes, 0.50);
                double p95 = Percentile(totalTimes, 0.95);
                double p99 = Percentile(totalTimes, 0.99);
                double max = totalTimes.Last();

                sb.AppendLine();
                sb.AppendLine("--- Frame Time (ms) ---");
                sb.AppendLine($"  Avg:  {avg:F2}  ({1000.0 / avg:F0} FPS)");
                sb.AppendLine($"  P50:  {p50:F2}  ({1000.0 / p50:F0} FPS)");
                sb.AppendLine($"  P95:  {p95:F2}  ({1000.0 / p95:F0} FPS)");
                sb.AppendLine($"  P99:  {p99:F2}  ({1000.0 / p99:F0} FPS)");
                sb.AppendLine($"  Max:  {max:F2}  ({1000.0 / max:F0} FPS)");
            }

            sb.AppendLine();
            sb.AppendLine("--- Per-Section Breakdown (avg ms / frame) ---");

            double totalAvg = _frameCount > 0 ? _history.Average(h => h.TotalMs) : 0;

            foreach (var name in _sectionOrder)
            {
                var data = _sections[name];
                double avgMs = _frameCount > 0 ? data.TotalMs / _frameCount : 0;
                double pct = totalAvg > 0 ? avgMs / totalAvg * 100 : 0;
                var perSection = _history
                    .Select(h => h.Sections.GetValueOrDefault(name))
                    .ToList();
                perSection.Sort();
                double sP95 = perSection.Count > 0 ? Percentile(perSection, 0.95) : 0;

                sb.AppendLine($"  {name,-30} avg:{avgMs,7:F3}  p95:{sP95,7:F3}  max:{data.MaxMs,7:F3}  ({pct:F1}%)");
            }

            // Unaccounted time
            if (_frameCount > 0 && _history.Count > 0)
            {
                double accounted = _sectionOrder.Sum(n => _sections[n].TotalMs / _frameCount);
                double unaccounted = totalAvg - accounted;
                double pct = totalAvg > 0 ? unaccounted / totalAvg * 100 : 0;
                sb.AppendLine($"  {"[unaccounted]",-30} avg:{unaccounted,7:F3}  ({pct:F1}%)");
            }

            sb.AppendLine();
            sb.AppendLine("=== END REPORT ===");
            return sb.ToString();
        }

        public void Shutdown()
        {
            _csvWriter?.Flush();
            _csvWriter?.Dispose();
            _csvWriter = null;
        }

        public void Reset()
        {
            _sections.Clear();
            _sectionOrder.Clear();
            _history.Clear();
            _currentFrame.Clear();
            _frameCount = 0;
            _activeSection = null;
            _csvHeaderWritten = false;
        }

        private static double Percentile(List<double> sorted, double p)
        {
            if (sorted.Count == 0) return 0;
            double idx = p * (sorted.Count - 1);
            int lo = (int)Math.Floor(idx);
            int hi = Math.Min(lo + 1, sorted.Count - 1);
            double frac = idx - lo;
            return sorted[lo] * (1 - frac) + sorted[hi] * frac;
        }

        private class SectionData
        {
            public double TotalMs;
            public double MaxMs;
            public int Count;
        }

        private struct FrameSnapshot
        {
            public int FrameIndex;
            public double TotalMs;
            public Dictionary<string, double> Sections;
        }
    }
}
