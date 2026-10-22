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
using MagicCube.Core.Models.Moves;

namespace MagicCube.Core.Tests.Models;

public class MoveTests
{
    [Fact]
    public void FaceMove_ToNotationString_ReturnsCorrectString()
    {
        var move = new FaceMove(Face.R, Direction.Clockwise);
        Assert.Equal("R", move.ToNotationString());

        move = new FaceMove(Face.U, Direction.CounterClockwise);
        Assert.Equal("U'", move.ToNotationString());

        move = new FaceMove(Face.F, Direction.Double);
        Assert.Equal("F2", move.ToNotationString());
    }

    [Fact]
    public void FaceMove_Inverse_ReturnsOppositeDirection()
    {
        var move = new FaceMove(Face.L, Direction.Clockwise);
        var inverse = move.Inverse();
        Assert.Equal(Face.L, ((FaceMove)inverse).Face);
        Assert.Equal(Direction.CounterClockwise, inverse.Direction);
    }

    [Fact]
    public void FaceMove_IsInverseOf_ReturnsTrueForOppositeMoves()
    {
        var move1 = new FaceMove(Face.D, Direction.Clockwise);
        var move2 = new FaceMove(Face.D, Direction.CounterClockwise);
        var move3 = new FaceMove(Face.D, Direction.Double);
        var move4 = new FaceMove(Face.U, Direction.CounterClockwise);

        Assert.True(move1.IsInverseOf(move2));
        Assert.False(move1.IsInverseOf(move3));
        Assert.False(move1.IsInverseOf(move4));
    }

    [Fact]
    public void RotationMove_Notation_IsLowercase()
    {
        var move = new RotationMove(Axis.Y, Direction.Clockwise);
        Assert.Equal("y", move.ToNotationString());

        move = new RotationMove(Axis.X, Direction.CounterClockwise);
        Assert.Equal("x'", move.ToNotationString());
    }

    [Fact]
    public void SliceMove_Notation_UsesSliceLetter()
    {
        var move = new SliceMove(Slice.M, Direction.Double);
        Assert.Equal("M2", move.ToNotationString());
    }

    [Fact]
    public void WideMove_Notation_UsesLowercaseFace()
    {
        var move = new WideMove(Face.R, Direction.Clockwise);
        Assert.Equal("r", move.ToNotationString());
    }
}