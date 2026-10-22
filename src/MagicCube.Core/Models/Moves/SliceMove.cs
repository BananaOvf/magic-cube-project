// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Enums;
using MagicCube.Core.Extensions;

namespace MagicCube.Core.Models.Moves
{
    public sealed record SliceMove(Slice Slice, Direction Direction) : Move(Direction)
    {
        protected override string GetBaseNotation() => Slice.ToString();

        public override bool IsInverseOf(Move other) =>
            other is SliceMove move && Slice == move.Slice && Direction.Inverse() == move.Direction;
    }
}