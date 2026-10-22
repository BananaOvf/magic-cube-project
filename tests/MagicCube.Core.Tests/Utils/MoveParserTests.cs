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

using FluentAssertions;
using MagicCube.Core.Enums;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Utils;

namespace MagicCube.Core.Tests.Utils
{
    public class MoveParserTests
    {
        // ---------- ParseOne ----------

        [Theory]
        [InlineData("R", Face.R, Direction.Clockwise)]
        [InlineData("U", Face.U, Direction.Clockwise)]
        [InlineData("L", Face.L, Direction.Clockwise)]
        [InlineData("D", Face.D, Direction.Clockwise)]
        [InlineData("F", Face.F, Direction.Clockwise)]
        [InlineData("B", Face.B, Direction.Clockwise)]
        public void ParseOne_FaceMove_NoSuffix_ReturnsClockwise(string token, Face face, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<FaceMove>()
                .Which.Should().BeEquivalentTo(new FaceMove(face, dir));
        }

        [Theory]
        [InlineData("R'", Face.R, Direction.CounterClockwise)]
        [InlineData("U'", Face.U, Direction.CounterClockwise)]
        [InlineData("L'", Face.L, Direction.CounterClockwise)]
        [InlineData("D'", Face.D, Direction.CounterClockwise)]
        [InlineData("F'", Face.F, Direction.CounterClockwise)]
        [InlineData("B'", Face.B, Direction.CounterClockwise)]
        public void ParseOne_FaceMove_CounterClockwise(string token, Face face, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<FaceMove>()
                .Which.Should().BeEquivalentTo(new FaceMove(face, dir));
        }

        [Theory]
        [InlineData("R2", Face.R, Direction.Double)]
        [InlineData("U2", Face.U, Direction.Double)]
        [InlineData("L2", Face.L, Direction.Double)]
        [InlineData("D2", Face.D, Direction.Double)]
        [InlineData("F2", Face.F, Direction.Double)]
        [InlineData("B2", Face.B, Direction.Double)]
        public void ParseOne_FaceMove_Double(string token, Face face, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<FaceMove>()
                .Which.Should().BeEquivalentTo(new FaceMove(face, dir));
        }

        [Theory]
        [InlineData("M", Slice.M, Direction.Clockwise)]
        [InlineData("E", Slice.E, Direction.Clockwise)]
        [InlineData("S", Slice.S, Direction.Clockwise)]
        public void ParseOne_SliceMove_Clockwise(string token, Slice slice, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<SliceMove>()
                .Which.Should().BeEquivalentTo(new SliceMove(slice, dir));
        }

        [Theory]
        [InlineData("M'", Slice.M, Direction.CounterClockwise)]
        [InlineData("E'", Slice.E, Direction.CounterClockwise)]
        [InlineData("S'", Slice.S, Direction.CounterClockwise)]
        public void ParseOne_SliceMove_CounterClockwise(string token, Slice slice, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<SliceMove>()
                .Which.Should().BeEquivalentTo(new SliceMove(slice, dir));
        }

        [Theory]
        [InlineData("M2", Slice.M, Direction.Double)]
        [InlineData("E2", Slice.E, Direction.Double)]
        [InlineData("S2", Slice.S, Direction.Double)]
        public void ParseOne_SliceMove_Double(string token, Slice slice, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<SliceMove>()
                .Which.Should().BeEquivalentTo(new SliceMove(slice, dir));
        }

        [Theory]
        [InlineData("x", Axis.X, Direction.Clockwise)]
        [InlineData("y", Axis.Y, Direction.Clockwise)]
        [InlineData("z", Axis.Z, Direction.Clockwise)]
        public void ParseOne_RotationMove_Clockwise(string token, Axis axis, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<RotationMove>()
                .Which.Should().BeEquivalentTo(new RotationMove(axis, dir));
        }

        [Theory]
        [InlineData("x'", Axis.X, Direction.CounterClockwise)]
        [InlineData("y'", Axis.Y, Direction.CounterClockwise)]
        [InlineData("z'", Axis.Z, Direction.CounterClockwise)]
        public void ParseOne_RotationMove_CounterClockwise(string token, Axis axis, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<RotationMove>()
                .Which.Should().BeEquivalentTo(new RotationMove(axis, dir));
        }

        [Theory]
        [InlineData("x2", Axis.X, Direction.Double)]
        [InlineData("y2", Axis.Y, Direction.Double)]
        [InlineData("z2", Axis.Z, Direction.Double)]
        public void ParseOne_RotationMove_Double(string token, Axis axis, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<RotationMove>()
                .Which.Should().BeEquivalentTo(new RotationMove(axis, dir));
        }

        [Theory]
        [InlineData("r", Face.R, Direction.Clockwise)]
        [InlineData("u", Face.U, Direction.Clockwise)]
        [InlineData("l", Face.L, Direction.Clockwise)]
        [InlineData("d", Face.D, Direction.Clockwise)]
        [InlineData("f", Face.F, Direction.Clockwise)]
        [InlineData("b", Face.B, Direction.Clockwise)]
        public void ParseOne_WideMove_Clockwise(string token, Face face, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<WideMove>()
                .Which.Should().BeEquivalentTo(new WideMove(face, dir));
        }

        [Theory]
        [InlineData("r'", Face.R, Direction.CounterClockwise)]
        [InlineData("u'", Face.U, Direction.CounterClockwise)]
        public void ParseOne_WideMove_CounterClockwise(string token, Face face, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<WideMove>()
                .Which.Should().BeEquivalentTo(new WideMove(face, dir));
        }

        [Theory]
        [InlineData("r2", Face.R, Direction.Double)]
        [InlineData("u2", Face.U, Direction.Double)]
        public void ParseOne_WideMove_Double(string token, Face face, Direction dir)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<WideMove>()
                .Which.Should().BeEquivalentTo(new WideMove(face, dir));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ParseOne_InvalidToken_ThrowsArgumentException(string token)
        {
            Action act = () => MoveParser.ParseOne(token);
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("X2")]  // uppercase rotation
        [InlineData("Y")]
        [InlineData("Z'")]
        public void ParseOne_UppercaseRotation_ShouldWork(string token)
        {
            var move = MoveParser.ParseOne(token);
            move.Should().BeOfType<RotationMove>();
        }

        [Fact]
        public void ParseOne_InvalidLetter_ThrowsFormatException()
        {
            Action act = () => MoveParser.ParseOne("W");
            act.Should().Throw<FormatException>();
        }

        // ---------- ParseMany ----------

        [Fact]
        public void ParseMany_EmptyString_ReturnsEmptyList()
        {
            var result = MoveParser.ParseMany("");
            result.Should().BeEmpty();
        }

        [Fact]
        public void ParseMany_NullOrWhitespace_ReturnsEmptyList()
        {
            MoveParser.ParseMany(null).Should().BeEmpty();
            MoveParser.ParseMany("   ").Should().BeEmpty();
        }

        [Fact]
        public void ParseMany_SingleMove_ReturnsOneElement()
        {
            var moves = MoveParser.ParseMany("R");
            moves.Should().HaveCount(1);
            moves[0].Should().BeOfType<FaceMove>().Which.Face.Should().Be(Face.R);
        }

        [Fact]
        public void ParseMany_MultipleMovesSpaceSeparated_ReturnsCorrectOrder()
        {
            var moves = MoveParser.ParseMany("R U' F2");
            moves.Should().HaveCount(3);
            moves[0].Should().BeOfType<FaceMove>().Which.Face.Should().Be(Face.R);
            moves[1].Should().BeOfType<FaceMove>().Which.Face.Should().Be(Face.U);
            moves[1].Direction.Should().Be(Direction.CounterClockwise);
            moves[2].Should().BeOfType<FaceMove>().Which.Face.Should().Be(Face.F);
            moves[2].Direction.Should().Be(Direction.Double);
        }

        [Fact]
        public void ParseMany_MixedTypes_ReturnsCorrectMoves()
        {
            var moves = MoveParser.ParseMany("R r M x y' u2 S'");
            moves.Should().HaveCount(7);
            moves[0].Should().BeOfType<FaceMove>();
            moves[1].Should().BeOfType<WideMove>();
            moves[2].Should().BeOfType<SliceMove>();
            moves[3].Should().BeOfType<RotationMove>();
            moves[4].Should().BeOfType<RotationMove>();
            moves[4].Direction.Should().Be(Direction.CounterClockwise);
            moves[5].Should().BeOfType<WideMove>();
            moves[5].Direction.Should().Be(Direction.Double);
            moves[6].Should().BeOfType<SliceMove>();
            moves[6].Direction.Should().Be(Direction.CounterClockwise);
        }

        [Fact]
        public void ParseMany_ExtraSpaces_AreTrimmed()
        {
            var moves = MoveParser.ParseMany("  R    U'   F2  ");
            moves.Should().HaveCount(3);
        }

        // ---------- SerializeMany ----------

        [Fact]
        public void SerializeMany_EmptyList_ReturnsEmptyString()
        {
            MoveParser.SerializeMany(new List<Move>()).Should().Be("");
        }

        [Fact]
        public void SerializeMany_SingleMove_ReturnsNotation()
        {
            var moves = new List<Move> { new FaceMove(Face.R, Direction.Clockwise) };
            MoveParser.SerializeMany(moves).Should().Be("R");
        }

        [Fact]
        public void SerializeMany_MixedMoves_ReturnsSpaceSeparated()
        {
            var moves = new List<Move>
            {
                new FaceMove(Face.R, Direction.Clockwise),
                new FaceMove(Face.U, Direction.CounterClockwise),
                new FaceMove(Face.F, Direction.Double),
                new SliceMove(Slice.M, Direction.Clockwise),
                new WideMove(Face.L, Direction.CounterClockwise),
                new RotationMove(Axis.Y, Direction.Double)
            };
            string expected = "R U' F2 M l' y2";
            MoveParser.SerializeMany(moves).Should().Be(expected);
        }

        [Fact]
        public void SerializeMany_Roundtrip_WithParseMany_ReturnsSameMoves()
        {
            string original = "R U' F2 M l' y2";
            var moves = MoveParser.ParseMany(original);
            string reserialized = MoveParser.SerializeMany(moves);
            reserialized.Should().Be(original);
        }

        [Fact]
        public void ParseOne_ThenToNotationString_ShouldMatchOriginalToken()
        {
            var tokens = new[] { "R", "U'", "F2", "M", "E'", "S2", "x", "y'", "z2", "r", "u'", "d2" };
            foreach (var token in tokens)
            {
                var move = MoveParser.ParseOne(token);
                move.ToNotationString().Should().Be(token, $"token '{token}' did not roundtrip");
            }
        }
    }
}