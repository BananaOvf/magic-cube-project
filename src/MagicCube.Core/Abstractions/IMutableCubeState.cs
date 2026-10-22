// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Models.Moves;

namespace MagicCube.Core.Abstractions
{
    public interface IMutableCubeState : IReadOnlyCubeState
    {
        void ApplyMove(Move move);
        void Undo();
        void Redo();
        void Reset(); // Reset to assembled state
        void LoadFromHistory(IReadOnlyList<Move> history, int currentIndex);
    }
}
