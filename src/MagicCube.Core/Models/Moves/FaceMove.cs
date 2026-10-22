// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Enums;
using MagicCube.Core.Extensions;

namespace MagicCube.Core.Models.Moves
{
    public sealed record FaceMove(Face Face, Direction Direction) : Move(Direction)
    {
        protected override string GetBaseNotation() => Face.ToString();

        public override bool IsInverseOf(Move other) =>
            other is FaceMove move && Face == move.Face && Direction.Inverse() == move.Direction;

    }
}