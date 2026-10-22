// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Enums;

namespace MagicCube.Core.Extensions
{
    public static class DirectionExtensions
    {
        public static Direction Inverse(this Direction direction) => direction switch
        {
            Direction.Clockwise => Direction.CounterClockwise,
            Direction.CounterClockwise => Direction.Clockwise,
            _ => direction
        };
    }
}
