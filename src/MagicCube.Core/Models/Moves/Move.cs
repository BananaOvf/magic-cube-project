// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Enums;
using MagicCube.Core.Extensions;

namespace MagicCube.Core.Models.Moves
{
    public abstract record Move(Direction Direction)
    {
        protected abstract string GetBaseNotation();

        public string ToNotationString()
        {
            string baseStr = GetBaseNotation();
            return Direction switch
            {
                Direction.Clockwise => baseStr,
                Direction.CounterClockwise => baseStr + "'",
                Direction.Double => baseStr + "2",
                _ => baseStr
            };
        }

        public Move Inverse() => this with { Direction = Direction.Inverse() };
        public abstract bool IsInverseOf(Move other);
    }
}