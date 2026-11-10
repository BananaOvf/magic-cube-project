// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Models.Moves;

namespace MagicCube.Core.Abstractions
{
    public interface IScrambleGenerator
    {
        string Name { get; }
        Task<IReadOnlyList<Move>> GenerateAsync(CancellationToken ct = default);
    }
}
