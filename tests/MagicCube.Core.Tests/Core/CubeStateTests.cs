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
using MagicCube.Core.Abstractions;
using MagicCube.Core.Enums;
using MagicCube.Core.Enums.Pieces;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Tests.Factories;

namespace MagicCube.Core.Tests.Core
{
    public class CubeStateTests
    {
        private static ICubeStateFactory GetFactory() => new TestCubeStateFactory();

        private const string SolvedFaceletString =
            "UUUUUUUUU" + "RRRRRRRRR" + "FFFFFFFFF" +
            "DDDDDDDDD" + "LLLLLLLLL" + "BBBBBBBBB";

        [Fact]
        public void Reset_ShouldCreateSolvedState()
        {
            var state = GetFactory().Create();
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
            state.MoveHistory.Should().BeEmpty();
            state.CurrentMoveIndex.Should().Be(-1);
        }

        [Fact]
        public void ApplyMove_SingleFaceTurn_ChangesState()
        {
            var state = GetFactory().Create();
            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.IsSolved.Should().BeFalse();
            state.ToFaceletString().Should().NotBe(SolvedFaceletString);
        }

        [Fact]
        public void ApplySliceMoves_IsSolved_ShouldBeFalse()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new SliceMove(Slice.M, Direction.Clockwise));
            state.ApplyMove(new SliceMove(Slice.E, Direction.Clockwise));
            state.ApplyMove(new SliceMove(Slice.M, Direction.CounterClockwise));
            state.ApplyMove(new SliceMove(Slice.E, Direction.CounterClockwise));

            state.IsSolved.Should().BeFalse();
            state.ToFaceletString().Should().NotBe(SolvedFaceletString);
        }

        [Theory]
        [InlineData(Face.U)]
        [InlineData(Face.D)]
        [InlineData(Face.L)]
        [InlineData(Face.R)]
        [InlineData(Face.F)]
        [InlineData(Face.B)]
        public void ApplyMove_FaceTurnThenInverse_ReturnsToSolved(Face face)
        {
            var state = GetFactory().Create();
            state.ApplyMove(new FaceMove(face, Direction.Clockwise));
            state.ApplyMove(new FaceMove(face, Direction.CounterClockwise));
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
        }

        [Theory]
        [InlineData(Face.U)]
        [InlineData(Face.D)]
        [InlineData(Face.L)]
        [InlineData(Face.R)]
        [InlineData(Face.F)]
        [InlineData(Face.B)]
        public void ApplyMove_DoubleFaceTurn_EquivalentToTwoClockwise(Face face)
        {
            var state1 = GetFactory().Create();
            state1.ApplyMove(new FaceMove(face, Direction.Double));

            var state2 = GetFactory().Create();
            state2.ApplyMove(new FaceMove(face, Direction.Clockwise));
            state2.ApplyMove(new FaceMove(face, Direction.Clockwise));

            state1.ToFaceletString().Should().Be(state2.ToFaceletString());
            state1.IsSolved.Should().Be(state2.IsSolved);
        }

        [Fact]
        public void ApplyMove_FourSameFaceTurns_ReturnsToSolved()
        {
            var state = GetFactory().Create();
            for (int i = 0; i < 4; i++)
                state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
        }

        [Fact]
        public void ApplyMove_SequenceRURpUp_ProducesExpectedFacelet()
        {
            var state = GetFactory().Create();
            var moves = new Move[]
            {
                new FaceMove(Face.R, Direction.Clockwise),
                new FaceMove(Face.U, Direction.Clockwise),
                new FaceMove(Face.R, Direction.CounterClockwise),
                new FaceMove(Face.U, Direction.CounterClockwise)
            };

            for (int i = 0; i < 6; i++)
                foreach (var move in moves)
                    state.ApplyMove(move);

            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
        }

        [Fact]
        public void Undo_AfterSingleMove_RestoresSolvedState()
        {
            var state = GetFactory().Create();
            state.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));
            state.Undo();
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
            state.CurrentMoveIndex.Should().Be(-1);
        }

        [Fact]
        public void Redo_AfterUndo_ReappliesMove()
        {
            var state = GetFactory().Create();
            var move = new FaceMove(Face.F, Direction.Clockwise);
            state.ApplyMove(move);
            var afterApply = state.ToFaceletString();

            state.Undo();
            state.ToFaceletString().Should().Be(SolvedFaceletString);

            state.Redo();
            state.ToFaceletString().Should().Be(afterApply);
            state.CurrentMoveIndex.Should().Be(0);
        }

        [Fact]
        public void History_ClearsRedoAfterNewMove()
        {
            var state = GetFactory().Create();
            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.U, Direction.Clockwise));
            state.Undo();
            state.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));

            state.MoveHistory.Should().HaveCount(2);
            state.MoveHistory[0].Should().BeOfType<FaceMove>().Which.Face.Should().Be(Face.R);
            state.MoveHistory[1].Should().BeOfType<FaceMove>().Which.Face.Should().Be(Face.F);
            state.CurrentMoveIndex.Should().Be(1);
        }

        [Theory]
        [InlineData(Slice.M)]
        [InlineData(Slice.E)]
        [InlineData(Slice.S)]
        public void SliceMove_ChangesStateAndInverseWorks(Slice slice)
        {
            var state = GetFactory().Create();
            state.ApplyMove(new SliceMove(slice, Direction.Clockwise));
            state.IsSolved.Should().BeFalse();

            var afterSlice = state.ToFaceletString();
            state.ApplyMove(new SliceMove(slice, Direction.CounterClockwise));
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
        }

        [Fact]
        public void SliceM_DoubleMove_ReturnsToSolved()
        {
            var state = GetFactory().Create();
            state.ApplyMove(new SliceMove(Slice.M, Direction.Double));
            state.ApplyMove(new SliceMove(Slice.M, Direction.Double));
            state.IsSolved.Should().BeTrue();
        }

        [Theory]
        [InlineData(Face.U)]
        [InlineData(Face.D)]
        [InlineData(Face.L)]
        [InlineData(Face.R)]
        [InlineData(Face.F)]
        [InlineData(Face.B)]
        public void WideMove_ChangesStateAndInverseWorks(Face face)
        {
            var state = GetFactory().Create();
            state.ApplyMove(new WideMove(face, Direction.Clockwise));
            state.IsSolved.Should().BeFalse();

            state.ApplyMove(new WideMove(face, Direction.CounterClockwise));
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
        }

        [Theory]
        [InlineData(Axis.X)]
        [InlineData(Axis.Y)]
        [InlineData(Axis.Z)]
        public void RotationMove_ChangesStateAndFourTimesReturnsToSolved(Axis axis)
        {
            var state = GetFactory().Create();
            for (int i = 0; i < 4; i++)
                state.ApplyMove(new RotationMove(axis, Direction.Clockwise));
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
        }

        [Fact]
        public void RotationX_ThenInverse_ReturnsToSolved()
        {
            var state = GetFactory().Create();
            state.ApplyMove(new RotationMove(Axis.X, Direction.Clockwise));
            state.ApplyMove(new RotationMove(Axis.X, Direction.CounterClockwise));
            state.IsSolved.Should().BeTrue();
        }

        [Fact]
        public void ToFaceletString_AlwaysLength54()
        {
            var state = GetFactory().Create();
            state.ToFaceletString().Should().HaveLength(54);

            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.ToFaceletString().Should().HaveLength(54);
        }

        [Fact]
        public void ToFaceletString_ContainsOnlyValidColors()
        {
            var state = GetFactory().Create();
            var validChars = new HashSet<char> { 'U', 'R', 'F', 'D', 'L', 'B' };
            for (int i = 0; i < 10; i++)
            {
                state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
                var str = state.ToFaceletString();
                str.All(c => validChars.Contains(c)).Should().BeTrue();
            }
        }

        [Fact]
        public void Centers_RemainFixedDuringFaceTurns()
        {
            var state = GetFactory().Create();
            var originalCenters = Enum.GetValues<CenterId>()
                .ToDictionary(id => id, id => state.GetCenterPiece(id));

            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.U, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));

            foreach (var id in Enum.GetValues<CenterId>())
                state.GetCenterPiece(id).Id.Should().Be(originalCenters[id].Id);
        }

        [Fact]
        public void EdgePiece_PositionsAndOrientations_AreCorrectAfterKnownSequence()
        {
            var state = GetFactory().Create();
            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.U, Direction.Clockwise));

            var edge = state.GetEdgePiece(EdgeId.UF);
            edge.Id.Should().Be(EdgeId.FR);
            edge.Orientation.Should().Be(EdgeOrientation.Default);
        }

        [Fact]
        public void CornerOrientation_ShouldChangeAfterRMove()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));

            var corner = state.GetCornerPiece(CornerId.UBR);

            corner.Id.Should().Be(CornerId.URF);
            corner.Orientation.Should().NotBe(CornerOrientation.Default);
        }

        [Fact]
        public void IsSolved_AfterScrambleAndReset_ReturnsTrue()
        {
            var state = GetFactory().Create();
            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.U, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.L, Direction.Clockwise));
            state.IsSolved.Should().BeFalse();

            state.Reset();
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
        }

        [Fact]
        public void MoveHistory_RecordsMovesInOrder()
        {
            var state = GetFactory().Create();
            var move1 = new FaceMove(Face.R, Direction.Clockwise);
            var move2 = new FaceMove(Face.U, Direction.Clockwise);
            state.ApplyMove(move1);
            state.ApplyMove(move2);

            state.MoveHistory.Should().Equal(move1, move2);
            state.CurrentMoveIndex.Should().Be(1);
        }

        [Fact]
        public void Undo_AtBeginning_DoesNothing()
        {
            var state = GetFactory().Create();
            state.Undo();
            state.IsSolved.Should().BeTrue();
            state.CurrentMoveIndex.Should().Be(-1);
        }

        [Fact]
        public void Redo_AtEnd_DoesNothing()
        {
            var state = GetFactory().Create();
            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.Redo();
            state.CurrentMoveIndex.Should().Be(0);
            state.MoveHistory.Should().HaveCount(1);
        }

        [Theory]
        [InlineData(Face.U)]
        [InlineData(Face.D)]
        [InlineData(Face.L)]
        [InlineData(Face.R)]
        [InlineData(Face.F)]
        [InlineData(Face.B)]
        public void FourFaceTurns_ShouldRestoreAllPieceOrientations(Face face)
        {
            var state = GetFactory().Create();

            for (int i = 0; i < 4; i++)
                state.ApplyMove(new FaceMove(face, Direction.Clockwise));

            foreach (EdgeId edgeId in Enum.GetValues<EdgeId>())
            {
                var edge = state.GetEdgePiece(edgeId);

                edge.Id.Should().Be(edgeId);
                edge.Orientation.Should().Be(EdgeOrientation.Default);
            }

            foreach (CornerId cornerId in Enum.GetValues<CornerId>())
            {
                var corner = state.GetCornerPiece(cornerId);

                corner.Id.Should().Be(cornerId);
                corner.Orientation.Should().Be(CornerOrientation.Default);
            }
        }

        [Fact]
        public void EdgeOrientationParity_ShouldAlwaysBeEven()
        {
            var state = GetFactory().Create();

            var moves = new Move[]
            {
                new FaceMove(Face.R, Direction.Clockwise),
                new FaceMove(Face.U, Direction.Clockwise),
                new FaceMove(Face.F, Direction.Clockwise),
                new FaceMove(Face.L, Direction.CounterClockwise),
                new FaceMove(Face.B, Direction.Double),
                new FaceMove(Face.D, Direction.Clockwise)
            };

            foreach (var move in moves)
                state.ApplyMove(move);

            int flippedCount = 0;

            foreach (EdgeId edgeId in Enum.GetValues<EdgeId>())
            {
                if (state.GetEdgePiece(edgeId).Orientation == EdgeOrientation.Flipped)
                    flippedCount++;
            }

            (flippedCount % 2).Should().Be(0);
        }

        [Fact]
        public void CornerOrientationParity_ShouldAlwaysBeValid()
        {
            var state = GetFactory().Create();

            var moves = new Move[]
            {
                new FaceMove(Face.R, Direction.Clockwise),
                new FaceMove(Face.U, Direction.Clockwise),
                new FaceMove(Face.F, Direction.Clockwise),
                new FaceMove(Face.L, Direction.CounterClockwise),
                new FaceMove(Face.B, Direction.Double),
                new FaceMove(Face.D, Direction.Clockwise)
            };

            foreach (var move in moves)
                state.ApplyMove(move);

            int total = 0;

            foreach (CornerId cornerId in Enum.GetValues<CornerId>())
            {
                total += (int)state.GetCornerPiece(cornerId).Orientation;
            }

            (total % 3).Should().Be(0);
        }

        [Fact]
        public void RMove_ShouldNotFlipEdges()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));

            foreach (EdgeId edgeId in Enum.GetValues<EdgeId>())
            {
                state.GetEdgePiece(edgeId)
                    .Orientation
                    .Should()
                    .Be(EdgeOrientation.Default);
            }
        }

        [Fact]
        public void FMove_ShouldFlipExactlyFourEdges()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));

            int flipped = 0;

            foreach (EdgeId edgeId in Enum.GetValues<EdgeId>())
            {
                if (state.GetEdgePiece(edgeId).Orientation == EdgeOrientation.Flipped)
                    flipped++;
            }

            flipped.Should().Be(4);
        }

        [Theory]
        [InlineData(Face.U)]
        [InlineData(Face.D)]
        public void UAndD_ShouldNotChangeAnyOrientation(Face face)
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(face, Direction.Clockwise));

            foreach (EdgeId edgeId in Enum.GetValues<EdgeId>())
            {
                state.GetEdgePiece(edgeId)
                    .Orientation
                    .Should()
                    .Be(EdgeOrientation.Default);
            }

            foreach (CornerId cornerId in Enum.GetValues<CornerId>())
            {
                state.GetCornerPiece(cornerId)
                    .Orientation
                    .Should()
                    .Be(CornerOrientation.Default);
            }
        }

        [Fact]
        public void RandomSequence_ThenInverse_ShouldRestoreSolvedState()
        {
            var state = GetFactory().Create();

            var moves = new List<Move>
            {
                new FaceMove(Face.R, Direction.Clockwise),
                new FaceMove(Face.U, Direction.Clockwise),
                new FaceMove(Face.F, Direction.CounterClockwise),
                new FaceMove(Face.L, Direction.Double),
                new FaceMove(Face.B, Direction.Clockwise),
                new FaceMove(Face.D, Direction.CounterClockwise)
            };

            foreach (var move in moves)
                state.ApplyMove(move);

            for (int i = moves.Count - 1; i >= 0; i--)
                state.ApplyMove(moves[i].Inverse());

            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);

            foreach (EdgeId edgeId in Enum.GetValues<EdgeId>())
            {
                var edge = state.GetEdgePiece(edgeId);

                edge.Id.Should().Be(edgeId);
                edge.Orientation.Should().Be(EdgeOrientation.Default);
            }

            foreach (CornerId cornerId in Enum.GetValues<CornerId>())
            {
                var corner = state.GetCornerPiece(cornerId);

                corner.Id.Should().Be(cornerId);
                corner.Orientation.Should().Be(CornerOrientation.Default);
            }
        }

        [Fact]
        public void RandomMoves_ThenInverse_ShouldRestoreSolved()
        {
            var rng = new Random(1234);

            for (int t = 0; t < 100; t++)
            {
                var state = GetFactory().Create();

                var moves = new List<Move>();

                for (int i = 0; i < 100; i++)
                {
                    var move = RandomMove(rng);

                    moves.Add(move);
                    state.ApplyMove(move);
                }

                for (int i = moves.Count - 1; i >= 0; i--)
                    state.ApplyMove(moves[i].Inverse());

                state.IsSolved.Should().BeTrue();
            }
        }

        private static Move RandomMove(Random rng)
        {
            int type = rng.Next(4);

            var directions = new[]
            {
                Direction.Clockwise,
                Direction.CounterClockwise,
                Direction.Double
            };

            var direction = directions[rng.Next(directions.Length)];

            return type switch
            {
                0 => new FaceMove(
                    Enum.GetValues<Face>()[rng.Next(Enum.GetValues<Face>().Length)],
                    direction),

                1 => new SliceMove(
                    Enum.GetValues<Slice>()[rng.Next(Enum.GetValues<Slice>().Length)],
                    direction),

                2 => new WideMove(
                    Enum.GetValues<Face>()[rng.Next(Enum.GetValues<Face>().Length)],
                    direction),

                3 => new RotationMove(
                    Enum.GetValues<Axis>()[rng.Next(Enum.GetValues<Axis>().Length)],
                    direction),

                _ => throw new InvalidOperationException()
            };
        }

        [Fact]
        public void FaceletString_ShouldContainExactlyNineOfEachColor()
        {
            var state = GetFactory().Create();

            for (int i = 0; i < 100; i++)
                state.ApplyMove(RandomMove(new Random(i)));

            var str = state.ToFaceletString();

            str.Count(c => c == 'U').Should().Be(9);
            str.Count(c => c == 'R').Should().Be(9);
            str.Count(c => c == 'F').Should().Be(9);
            str.Count(c => c == 'D').Should().Be(9);
            str.Count(c => c == 'L').Should().Be(9);
            str.Count(c => c == 'B').Should().Be(9);
        }

        [Fact]
        public void ComplexSequence_ShouldProduceExpectedFaceletString()
        {
            var state = GetFactory().Create();

            var moves = new Move[]
            {
                new FaceMove(Face.U, Direction.Clockwise),
                new FaceMove(Face.L, Direction.Clockwise),
                new FaceMove(Face.U, Direction.Clockwise),
                new FaceMove(Face.F, Direction.Clockwise),
                new FaceMove(Face.B, Direction.Double),
                new FaceMove(Face.D, Direction.Clockwise),
                new FaceMove(Face.F, Direction.Double),
                new FaceMove(Face.U, Direction.CounterClockwise),
                new FaceMove(Face.B, Direction.CounterClockwise),
                new FaceMove(Face.R, Direction.Double),
                new FaceMove(Face.B, Direction.Double),
                new FaceMove(Face.D, Direction.Double),
                new FaceMove(Face.F, Direction.CounterClockwise),
                new FaceMove(Face.R, Direction.Double),
                new FaceMove(Face.U, Direction.Double),
                new FaceMove(Face.R, Direction.Double),
                new FaceMove(Face.L, Direction.Double),
                new FaceMove(Face.F, Direction.Clockwise),
                new FaceMove(Face.U, Direction.Double),
                new FaceMove(Face.L, Direction.Double)
            };

            foreach (var move in moves)
                state.ApplyMove(move);

            var facelets = state.ToFaceletString();

            facelets.Should().Be(
                "DDUBUFLUBDLRLRDRBDULRRFDLBUFRFRDDBFRBLFULULFDBFLRBBFUU"
            );
        }

        [Fact]
        public void RMove_ShouldProduceCorrectFacelets()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));

            state.ToFaceletString().Should().Be(
                "UUFUUFUUFRRRRRRRRRFFDFFDFFDDDBDDBDDBLLLLLLLLLUBBUBBUBB"
            );
        }

        [Fact]
        public void LMove_ShouldProduceCorrectFacelets()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.L, Direction.Clockwise));

            state.ToFaceletString().Should().Be(
                "BUUBUUBUURRRRRRRRRUFFUFFUFFFDDFDDFDDLLLLLLLLLBBDBBDBBD"
            );
        }

        [Fact]
        public void UMove_ShouldProduceCorrectFacelets()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.U, Direction.Clockwise));

            state.ToFaceletString().Should().Be(
                "UUUUUUUUUBBBRRRRRRRRRFFFFFFDDDDDDDDDFFFLLLLLLLLLBBBBBB"
            );
        }

        [Fact]
        public void FMove_ShouldProduceCorrectFacelets()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));

            state.ToFaceletString().Should().Be(
                "UUUUUULLLURRURRURRFFFFFFFFFRRRDDDDDDLLDLLDLLDBBBBBBBBB"
            );
        }

        [Fact]
        public void EMove_ShouldProduceCorrectFacelets()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new SliceMove(Slice.E, Direction.Clockwise));

            state.ToFaceletString().Should().Be(
                "UUUUUUUUURRRFFFRRRFFFLLLFFFDDDDDDDDDLLLBBBLLLBBBRRRBBB"
            );
        }

        [Fact]
        public void MMove_ShouldProduceCorrectFacelets()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new SliceMove(Slice.M, Direction.Clockwise));

            state.ToFaceletString().Should().Be(
                "UBUUBUUBURRRRRRRRRFUFFUFFUFDFDDFDDFDLLLLLLLLLBDBBDBBDB"
            );
        }

        [Fact]
        public void SMove_ShouldProduceCorrectFacelets()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new SliceMove(Slice.S, Direction.Clockwise));

            state.ToFaceletString().Should().Be(
                "UUULLLUUURURRURRURFFFFFFFFFDDDRRRDDDLDLLDLLDLBBBBBBBBB"
            );
        }

        [Theory]
        [InlineData(Face.R)]
        [InlineData(Face.L)]
        [InlineData(Face.U)]
        [InlineData(Face.D)]
        [InlineData(Face.F)]
        [InlineData(Face.B)]
        public void WideMove_ShouldEqual_FacePlusSlice(Face face)
        {
            var a = GetFactory().Create();
            var b = GetFactory().Create();

            a.ApplyMove(new WideMove(face, Direction.Clockwise));

            switch (face)
            {
                case Face.R:
                    b.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
                    b.ApplyMove(new SliceMove(Slice.M, Direction.CounterClockwise));
                    break;
                case Face.L:
                    b.ApplyMove(new FaceMove(Face.L, Direction.Clockwise));
                    b.ApplyMove(new SliceMove(Slice.M, Direction.Clockwise));
                    break;
                case Face.U:
                    b.ApplyMove(new FaceMove(Face.U, Direction.Clockwise));
                    b.ApplyMove(new SliceMove(Slice.E, Direction.CounterClockwise));
                    break;
                case Face.D:
                    b.ApplyMove(new FaceMove(Face.D, Direction.Clockwise));
                    b.ApplyMove(new SliceMove(Slice.E, Direction.Clockwise));
                    break;
                case Face.F:
                    b.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));
                    b.ApplyMove(new SliceMove(Slice.S, Direction.Clockwise));
                    break;
                case Face.B:
                    b.ApplyMove(new FaceMove(Face.B, Direction.Clockwise));
                    b.ApplyMove(new SliceMove(Slice.S, Direction.CounterClockwise));
                    break;
            }

            a.ToFaceletString().Should().Be(b.ToFaceletString());
        }

        [Fact]
        public void Rotation_X_ShouldEqual_R_Mp_Lp()
        {
            var a = GetFactory().Create();
            var b = GetFactory().Create();

            a.ApplyMove(new RotationMove(Axis.X, Direction.Clockwise));

            b.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            b.ApplyMove(new SliceMove(Slice.M, Direction.CounterClockwise));
            b.ApplyMove(new FaceMove(Face.L, Direction.CounterClockwise));

            a.ToFaceletString().Should().Be(b.ToFaceletString());
        }

        [Fact]
        public void CornerOrientation_ShouldBeMod3()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));

            foreach (CornerId id in Enum.GetValues<CornerId>())
            {
                var c = state.GetCornerPiece(id);
                ((int)c.Orientation % 3).Should().BeInRange(0, 2);
            }
        }

        [Fact]
        public void EdgeFlip_Twice_ShouldReturnOriginal()
        {
            var state = GetFactory().Create();

            state.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));
            state.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));

            foreach (EdgeId id in Enum.GetValues<EdgeId>())
                state.GetEdgePiece(id).Orientation.Should().Be(EdgeOrientation.Default);
        }
    }
}