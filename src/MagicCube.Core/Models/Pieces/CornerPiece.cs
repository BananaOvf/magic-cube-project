// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Enums.Pieces;

namespace MagicCube.Core.Models.Pieces
{
    public readonly record struct CornerPiece(CornerId Id, CornerOrientation Orientation);
}
