// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Enums;
using MagicCube.Core.Extensions;

namespace MagicCube.Core.Models.Moves
{
    public sealed record RotationMove(Axis Axis, Direction Direction) : Move(Direction)
    {
        protected override string GetBaseNotation() => Axis.ToString().ToLowerInvariant();

        public override bool IsInverseOf(Move other) =>
            other is RotationMove move && Axis == move.Axis && Direction.Inverse() == move.Direction;
    }
}