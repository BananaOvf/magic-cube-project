// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace MagicCube.Services
{
    public delegate void TimerTickEventHandler(TimeSpan elapsed);

    public interface ITimerService
    {
        event TimerTickEventHandler? Tick;
        bool IsRunning { get; }
        void Start();
        void Stop();
        void Reset();

    }
}
