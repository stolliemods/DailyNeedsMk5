using System.Diagnostics;
using Digi;

/* Pulled from DarkStar weaponCore - all credit for code is his */

namespace Stollie.DailyNeeds
{
    internal class StopWatch
    {
        private double _last;
        private string _message;
        private bool _time;
        private Stopwatch Sw { get; } = new Stopwatch();

        public void Start(string message, bool time = true)
        {
            _message = message;
            _time = time;
            Sw.Restart();
        }

        public void Complete(bool display = false)
        {
            Sw.Stop();
            var ticks = Sw.ElapsedTicks;
            var ns = 1000000000.0 * ticks / Stopwatch.Frequency;
            var ms = ns / 1000000.0;
            var s = ms / 1000;
            Sw.Reset();
            var message = $"{_message} ms:{(float)ms} last-ms:{(float)_last} s:{(int)s}";
            if (_time && display) Log.Info(message);
            else if (display) Log.Info(message);
            _last = ms;
        }
    }
}
    