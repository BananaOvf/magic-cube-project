/*
 * Magic Cube Simulator
 * Copyright (C) 2026 Magic Cube Project
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using MagicCube.Core.Enums;
using MagicCube.Core.Extensions;

namespace MagicCube.Core.Tests.Extensions
{
    public class DirectionExtensionsTests
    {
        [Theory]
        [InlineData(Direction.Clockwise, Direction.CounterClockwise)]
        [InlineData(Direction.CounterClockwise, Direction.Clockwise)]
        [InlineData(Direction.Double, Direction.Double)]
        public void Inverse_ReturnsCorrectDirection(Direction input, Direction expected)
        {
            var result = input.Inverse();
            Assert.Equal(expected, result);
        }
    }
}
