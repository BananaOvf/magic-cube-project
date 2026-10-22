// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;
using MagicCube.Services;

namespace MagicCube.Infrastructure.Providers
{
    public class ManualCubeStateProvider : ICubeStateProvider
    {
        private readonly IMutableCubeState _state;

        public event StateChangedEventHandler? StateChanged;

        public ManualCubeStateProvider(IMutableCubeState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public IReadOnlyCubeState GetCurrentState() => _state;

        public void NotifyStateChanged() => StateChanged?.Invoke(_state);
    }
}
