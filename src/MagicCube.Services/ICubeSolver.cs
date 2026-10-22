// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;
using MagicCube.Core.Models.Moves;

namespace MagicCube.Services
{
    public interface ICubeSolver
    {
        string Name { get; }
        Task<IReadOnlyList<Move>> SolveAsync(IReadOnlyCubeState state, CancellationToken token = default);
    }
}
