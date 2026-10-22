/*
 * Magic Cube Simulator
 * Copyright (C) 2026 Magic Cube Project
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using MagicCube.Services;
using System.Diagnostics;
using System.Windows.Threading;

namespace MagicCube.Infrastructure.Services
{
    public class DispatcherTimerService : ITimerService
    {
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch;

        public event TimerTickEventHandler? Tick;
        public bool IsRunning => _timer.IsEnabled;

        public DispatcherTimerService()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _timer.Tick += OnTimerTick;
            _stopwatch = new Stopwatch();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            Tick?.Invoke(_stopwatch.Elapsed);
        }

        public void Reset()
        {
            _timer.Stop();
            _stopwatch.Reset();
            Tick?.Invoke(TimeSpan.Zero);
        }

        public void Start()
        {
            _stopwatch.Restart();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _stopwatch.Stop();
        }
    }
}
