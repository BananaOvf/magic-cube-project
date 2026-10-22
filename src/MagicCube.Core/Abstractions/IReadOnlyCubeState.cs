// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Enums.Pieces;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Models.Pieces;

namespace MagicCube.Core.Abstractions
{
    public interface IReadOnlyCubeState
    {
        CenterPiece GetCenterPiece(CenterId position);
        EdgePiece GetEdgePiece(EdgeId position);
        CornerPiece GetCornerPiece(CornerId position);

        IReadOnlyList<Move> MoveHistory { get; }
        int CurrentMoveIndex { get; }
        bool IsSolved { get; }
        string ToFaceletString();
        IReadOnlyCubeState Clone();
    }
}
