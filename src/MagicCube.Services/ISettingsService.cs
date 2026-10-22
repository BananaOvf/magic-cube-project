// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;

namespace MagicCube.Services
{
    public interface ISettingsService
    {
        Task SaveStateAsync(IReadOnlyCubeState state, CancellationToken token = default);
        Task<IMutableCubeState> LoadStateAsync(CancellationToken token = default);

        void SaveState(IReadOnlyCubeState state);
        void LoadState(IMutableCubeState intoState);
    }
}
