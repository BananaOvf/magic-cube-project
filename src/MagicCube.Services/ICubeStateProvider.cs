// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;

namespace MagicCube.Services
{
    public delegate void StateChangedEventHandler(IReadOnlyCubeState newState);

    public interface ICubeStateProvider
    {
        event StateChangedEventHandler? StateChanged;
        IReadOnlyCubeState GetCurrentState();
        void NotifyStateChanged();
    }
}
