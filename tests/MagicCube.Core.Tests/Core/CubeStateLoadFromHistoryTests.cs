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
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Tests.Factories;

namespace MagicCube.Core.Tests.Core
{
    public class CubeStateLoadFromHistoryTests
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
        public void LoadFromHistory_ValidHistoryAndIndex_ShouldRestoreExactStateAndHistory()
        {
            IMutableCubeState state = GetFactory().Create();
            var moves = MakeMoves(
                (Face.R, Direction.Clockwise),
                (Face.U, Direction.Clockwise),
                (Face.F, Direction.Clockwise)
            );

            IMutableCubeState reference = GetFactory().Create();
            foreach (var m in moves)
                reference.ApplyMove(m);

            state.LoadFromHistory(moves, 2);
            state.IsSolved.Should().Be(reference.IsSolved);
            state.ToFaceletString().Should().Be(reference.ToFaceletString());
            state.MoveHistory.Should().Equal(moves);
            state.CurrentMoveIndex.Should().Be(2);
        }

        [Fact]
        public void LoadFromHistory_NegativeIndex_ShouldResetAndHaveEmptyHistory()
        {
            IMutableCubeState state = GetFactory().Create();
            var moves = MakeMoves((Face.R, Direction.Clockwise));

            state.LoadFromHistory(moves, -1);

            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
            state.MoveHistory.Should().NotBeEmpty();
            state.CurrentMoveIndex.Should().Be(-1);
        }

        [Fact]
        public void LoadFromHistory_IndexBeyondLength_ShouldApplyAllMovesAndSetIndexToLast()
        {
            IMutableCubeState state = GetFactory().Create();
            var moves = MakeMoves(
                (Face.R, Direction.Clockwise),
                (Face.U, Direction.Clockwise)
            );

            state.LoadFromHistory(moves, 5);

            state.MoveHistory.Should().Equal(moves);
            state.CurrentMoveIndex.Should().Be(1);

            IMutableCubeState reference = GetFactory().Create();
            reference.ApplyMove(moves[0]);
            reference.ApplyMove(moves[1]);
            state.ToFaceletString().Should().Be(reference.ToFaceletString());
        }

        [Fact]
        public void LoadFromHistory_EmptyHistory_ShouldResetSolvedState()
        {
            IMutableCubeState state = GetFactory().Create();
            state.ApplyMove(new FaceMove(Face.R, Direction.Clockwise));
            state.LoadFromHistory(new List<Move>(), 0);

            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
            state.MoveHistory.Should().BeEmpty();
            state.CurrentMoveIndex.Should().Be(-1);
        }

        [Fact]
        public void LoadFromHistory_ThenUndo_ShouldMoveToPreviousState()
        {
            IMutableCubeState state = GetFactory().Create();
            var moves = MakeMoves(
                (Face.R, Direction.Clockwise),
                (Face.U, Direction.Clockwise),
                (Face.F, Direction.Clockwise)
            );
            state.LoadFromHistory(moves, 2);

            state.Undo();
            state.CurrentMoveIndex.Should().Be(1);
            state.MoveHistory.Should().Equal(moves);

            IMutableCubeState reference = GetFactory().Create();
            reference.ApplyMove(moves[0]);
            reference.ApplyMove(moves[1]);
            state.ToFaceletString().Should().Be(reference.ToFaceletString());
        }

        [Fact]
        public void LoadFromHistory_ThenRedo_ShouldReapplyMove()
        {
            IMutableCubeState state = GetFactory().Create();
            var moves = MakeMoves(
                (Face.R, Direction.Clockwise),
                (Face.U, Direction.Clockwise)
            );
            state.LoadFromHistory(moves, 0);
            state.Redo();

            state.CurrentMoveIndex.Should().Be(1);
            state.MoveHistory.Should().Equal(moves);
            state.ToFaceletString().Should().Be(
                ApplyThenGetString(GetFactory().Create(), moves[0], moves[1])
            );
        }

        [Fact]
        public void LoadFromHistory_ThenApplyNewMove_ShouldTruncateHistory()
        {
            IMutableCubeState state = GetFactory().Create();
            var moves = MakeMoves(
                (Face.R, Direction.Clockwise),
                (Face.U, Direction.Clockwise),
                (Face.F, Direction.Clockwise)
            );
            state.LoadFromHistory(moves, 1);

            state.ApplyMove(new FaceMove(Face.L, Direction.Clockwise));

            state.MoveHistory.Should().HaveCount(3);
            state.MoveHistory[2].Should().BeOfType<FaceMove>().Which.Face.Should().Be(Face.L);
            state.CurrentMoveIndex.Should().Be(2);
        }

        [Fact]
        public void LoadFromHistory_SolvedSequence_ShouldMakeSolvedTrue()
        {
            IMutableCubeState state = GetFactory().Create();
            var moves = MakeMoves(
                (Face.R, Direction.Clockwise),
                (Face.R, Direction.CounterClockwise)
            );
            state.LoadFromHistory(moves, 1);
            state.IsSolved.Should().BeTrue();
            state.ToFaceletString().Should().Be(SolvedFaceletString);
        }

        private string ApplyThenGetString(IMutableCubeState state, params Move[] moves)
        {
            foreach (var m in moves)
                state.ApplyMove(m);
            return state.ToFaceletString();
        }
    }
}
