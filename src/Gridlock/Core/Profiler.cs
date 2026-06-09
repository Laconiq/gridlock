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

        private readonly Frame[] _stack = new Frame[32];
        private int _depth;

        private int _frameCount;
        private readonly List<FrameSnapshot> _history = new();
        private readonly Dictionary<string, double> _currentFrame = new();

        private StreamWriter? _csvWriter;
        private bool _csvHeaderWritten;

        public bool Enabled { get; set; }
        public int FrameCount => _frameCount;

        public Profiler()
        {
            for (int i = 0; i < _stack.Length; i++)
                _stack[i] = new Frame();
            _sw.Start();
        }

        public void Begin(string name)
        {
            if (!Enabled) return;

            long now = _sw.ElapsedTicks;

            // Pause the parent so its measured time excludes this nested child's duration
            // (sections report exclusive self-time, so the report's totals don't double count).
            if (_depth > 0)
            {
                var parent = _stack[_depth - 1];
                parent.SelfMs += TicksToMs(now - parent.ResumeTick);
            }

            if (!_sections.ContainsKey(name))
            {
                _sections[name] = new SectionData();
                _sectionOrder.Add(name);
            }

            if (_depth >= _stack.Length) return;
            var frame = _stack[_depth++];
            frame.Name = name;
            frame.SelfMs = 0.0;
            frame.ResumeTick = now;
        }

        public void End()
        {
            if (!Enabled || _depth == 0) return;

            long now = _sw.ElapsedTicks;
            var frame = _stack[--_depth];
            frame.SelfMs += TicksToMs(now - frame.ResumeTick);

            var data = _sections[frame.Name];
            data.TotalMs += frame.SelfMs;
            data.Count++;
            if (frame.SelfMs > data.MaxMs) data.MaxMs = frame.SelfMs;

            _currentFrame[frame.Name] = _currentFrame.GetValueOrDefault(frame.Name) + frame.SelfMs;

            // Resume the parent now that this child has finished.
            if (_depth > 0)
                _stack[_depth - 1].ResumeTick = now;
        }

        public void EndFrame(double totalFrameMs)
        {
            if (!Enabled) return;

            while (_depth > 0)
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

        private static double Percentile(List<double> sorted, double p)
        {
            if (sorted.Count == 0) return 0;
            double idx = p * (sorted.Count - 1);
            int lo = (int)Math.Floor(idx);
            int hi = Math.Min(lo + 1, sorted.Count - 1);
            double frac = idx - lo;
            return sorted[lo] * (1 - frac) + sorted[hi] * frac;
        }

        private static double TicksToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        private sealed class Frame
        {
            public string Name = "";
            public long ResumeTick;
            public double SelfMs;
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
