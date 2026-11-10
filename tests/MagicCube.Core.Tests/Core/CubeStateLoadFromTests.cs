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
    public class CubeStateLoadFromTests
    {
        private static ICubeStateFactory GetFactory() => new TestCubeStateFactory();

        private const string SolvedFaceletString =
            "UUUUUUUUU" + "RRRRRRRRR" + "FFFFFFFFF" +
            "DDDDDDDDD" + "LLLLLLLLL" + "BBBBBBBBB";

        private static List<Move> MakeMoves(params (Face face, Direction dir)[] moves)
        {
            return moves.Select(m => (Move)new FaceMove(m.face, m.dir)).ToList();
        }

        [Fact]
        public void LoadFrom_NullSource_ShouldThrowArgumentNullException()
        {
            var state = GetFactory().Create();

            Action act = () => state.LoadFrom(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LoadFrom_SolvedState_ShouldCopySolvedStateAndEmptyHistory()
        {
            var source = GetFactory().Create();
            var target = GetFactory().Create();

            target.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));

            target.LoadFrom(source);

            target.IsSolved.Should().BeTrue();
            target.ToFaceletString().Should().Be(SolvedFaceletString);
            target.MoveHistory.Should().BeEmpty();
            target.CurrentMoveIndex.Should().Be(-1);
        }

        [Fact]
        public void LoadFrom_ScrambledState_ShouldCopyAllPiecesAndHistory()
        {
            var source = GetFactory().Create();
            var moves = MakeMoves(
                (Face.R, Direction.Clockwise),
                (Face.U, Direction.Clockwise),
                (Face.F, Direction.CounterClockwise),
                (Face.L, Direction.Double)
            );

            foreach (var m in moves)
                source.ApplyMove(m);

            var target = GetFactory().Create();
            target.LoadFrom(source);

            target.IsSolved.Should().Be(source.IsSolved);
            target.ToFaceletString().Should().Be(source.ToFaceletString());
            target.MoveHistory.Should().Equal(source.MoveHistory);
            target.CurrentMoveIndex.Should().Be(source.CurrentMoveIndex);

            foreach (CenterId id in Enum.GetValues<CenterId>())
            {
                var expected = source.GetCenterPiece(id);
                var actual = target.GetCenterPiece(id);
                actual.Id.Should().Be(expected.Id);
                actual.Orientation.Should().Be(expected.Orientation);
            }

            foreach (EdgeId id in Enum.GetValues<EdgeId>())
            {
                var expected = source.GetEdgePiece(id);
                var actual = target.GetEdgePiece(id);
                actual.Id.Should().Be(expected.Id);
                actual.Orientation.Should().Be(expected.Orientation);
            }

            foreach (CornerId id in Enum.GetValues<CornerId>())
            {
                var expected = source.GetCornerPiece(id);
                var actual = target.GetCornerPiece(id);
                actual.Id.Should().Be(expected.Id);
                actual.Orientation.Should().Be(expected.Orientation);
            }
        }

        [Fact]
        public void LoadFrom_StateWithUndoneHistory_ShouldPreserveCurrentIndex()
        {
            var source = GetFactory().Create();
            source.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            source.ApplyMove(new FaceMove(Face.U, Direction.Clockwise));
            source.ApplyMove(new FaceMove(Face.F, Direction.Clockwise));

            source.Undo();

            var target = GetFactory().Create();
            target.LoadFrom(source);

            target.MoveHistory.Should().HaveCount(3);
            target.CurrentMoveIndex.Should().Be(1);
            target.ToFaceletString().Should().Be(source.ToFaceletString());
        }

        [Fact]
        public void LoadFrom_ShouldNotCreateReferenceAliases()
        {
            var source = GetFactory().Create();
            source.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));

            var target = GetFactory().Create();
            target.LoadFrom(source);

            var initialTargetFacelets = target.ToFaceletString();

            source.ApplyMove(new FaceMove(Face.U, Direction.Clockwise));

            target.ToFaceletString().Should().Be(initialTargetFacelets);
            target.MoveHistory.Should().HaveCount(1);
            target.CurrentMoveIndex.Should().Be(0);
        }

        [Fact]
        public void LoadFrom_ShouldOverwriteExistingDirtyStateCompletely()
        {
            var target = GetFactory().Create();

            target.ApplyMove(new FaceMove(Face.D, Direction.Clockwise));
            target.ApplyMove(new FaceMove(Face.B, Direction.Clockwise));

            var source = GetFactory().Create();
            source.ApplyMove(new FaceMove(Face.L, Direction.Clockwise));

            target.LoadFrom(source);

            target.MoveHistory.Should().HaveCount(1);
            target.MoveHistory[0].Should().BeOfType<FaceMove>().Which.Face.Should().Be(Face.L);
            target.CurrentMoveIndex.Should().Be(0);
            target.ToFaceletString().Should().Be(source.ToFaceletString());
        }
    }
}