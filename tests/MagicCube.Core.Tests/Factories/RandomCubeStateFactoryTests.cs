// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentAssertions;
using MagicCube.Core.Abstractions;
using MagicCube.Core.Core.Factories;
using MagicCube.Core.Enums.Pieces;
using MagicCube.Core.Utils;

namespace MagicCube.Core.Tests.Factories
{
    public class RandomCubeStateFactoryTests
    {
        private const string SolvedFaceletString =
            "UUUUUUUUU" + "RRRRRRRRR" + "FFFFFFFFF" +
            "DDDDDDDDD" + "LLLLLLLLL" + "BBBBBBBBB";

        private static IRandomCubeStateFactory CreateFactory() => new RandomCubeStateFactory();

        private static IRandomSource SeededSource(int seed) =>
            new StandardRandomSource(new Random(seed));

        [Fact]
        public void Create_ReturnsCentersInSolvedConfiguration()
        {
            var state = CreateFactory().Create(SeededSource(42));

            foreach (CenterId id in Enum.GetValues<CenterId>())
            {
                state.GetCenterPiece(id).Id.Should().Be(id);
                state.GetCenterPiece(id).Orientation.Should().Be(CenterOrientation.Default);
            }
        }

        [Fact]
        public void Create_ReturnsEmptyHistory()
        {
            var state = CreateFactory().Create(SeededSource(42));

            state.MoveHistory.Should().BeEmpty();
            state.CurrentMoveIndex.Should().Be(-1);
        }

        [Fact]
        public void Create_SameSeed_ProducesIdenticalStates()
        {
            var a = CreateFactory().Create(SeededSource(42)).ToFaceletString();
            var b = CreateFactory().Create(SeededSource(42)).ToFaceletString();

            a.Should().Be(b);
        }

        [Fact]
        public void Create_DifferentSeeds_ProduceDifferentStates()
        {
            var a = CreateFactory().Create(SeededSource(42)).ToFaceletString();
            var b = CreateFactory().Create(SeededSource(43)).ToFaceletString();

            a.Should().NotBe(b);
        }

        [Fact]
        public void Create_AlmostNeverReturnsSolvedState()
        {
            int solvedCount = 0;

            for (int seed = 0; seed < 1000; seed++)
            {
                if (CreateFactory().Create(SeededSource(seed)).IsSolved)
                    solvedCount++;
            }

            solvedCount.Should().Be(0);
        }

        [Fact]
        public void Create_DefaultSource_ProducesDifferentStatesAcrossCalls()
        {
            var factory = CreateFactory();
            var a = factory.Create().ToFaceletString();
            var b = factory.Create().ToFaceletString();

            a.Should().NotBe(b);
        }

        [Fact]
        public void Create_CornerIdsFormValidPermutation()
        {
            var state = CreateFactory().Create(SeededSource(7));

            var ids = Enum.GetValues<CornerId>()
                .Select(pos => state.GetCornerPiece(pos).Id)
                .ToList();

            ids.Should().BeEquivalentTo(Enum.GetValues<CornerId>());
            ids.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void Create_EdgeIdsFormValidPermutation()
        {
            var state = CreateFactory().Create(SeededSource(7));

            var ids = Enum.GetValues<EdgeId>()
                .Select(pos => state.GetEdgePiece(pos).Id)
                .ToList();

            ids.Should().BeEquivalentTo(Enum.GetValues<EdgeId>());
            ids.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void Create_PermutationParity_CornersMatchesEdges()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var state = CreateFactory().Create(SeededSource(seed));

                var cornerPerm = Enum.GetValues<CornerId>()
                    .Select(pos => state.GetCornerPiece(pos).Id).ToArray();
                var edgePerm = Enum.GetValues<EdgeId>()
                    .Select(pos => state.GetEdgePiece(pos).Id).ToArray();

                Parity(cornerPerm).Should().Be(Parity(edgePerm),
                    $"seed {seed}: parity must match for state to be solvable");
            }
        }

        [Fact]
        public void Create_CornerOrientationSum_DivisibleByThree()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var state = CreateFactory().Create(SeededSource(seed));

                int sum = Enum.GetValues<CornerId>()
                    .Sum(pos => (int)state.GetCornerPiece(pos).Orientation);

                (sum % 3).Should().Be(0, $"seed {seed}");
            }
        }

        [Fact]
        public void Create_EdgeOrientationSum_Even()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var state = CreateFactory().Create(SeededSource(seed));

                int sum = Enum.GetValues<EdgeId>()
                    .Sum(pos => (int)state.GetEdgePiece(pos).Orientation);

                (sum % 2).Should().Be(0, $"seed {seed}");
            }
        }

        [Fact]
        public void Create_OverManySamples_EveryEdgeIdAppearsAtEveryPosition()
        {
            var seen = new HashSet<(EdgeId pos, EdgeId id)>();
            var positions = Enum.GetValues<EdgeId>();

            for (int seed = 0; seed < 500; seed++)
            {
                var state = CreateFactory().Create(SeededSource(seed));
                foreach (var pos in positions)
                    seen.Add((pos, state.GetEdgePiece(pos).Id));
            }

            seen.Count.Should().Be(12 * 12, "all 144 (position, piece) pairs should appear");
        }

        [Fact]
        public void Create_OverManySamples_EveryCornerIdAppearsAtEveryPosition()
        {
            var seen = new HashSet<(CornerId pos, CornerId id)>();
            var positions = Enum.GetValues<CornerId>();

            for (int seed = 0; seed < 500; seed++)
            {
                var state = CreateFactory().Create(SeededSource(seed));
                foreach (var pos in positions)
                    seen.Add((pos, state.GetCornerPiece(pos).Id));
            }

            seen.Count.Should().Be(8 * 8, "all 64 (position, piece) pairs should appear");
        }

        [Fact]
        public void Create_OverManySamples_AllCornerOrientationsOccur()
        {
            var seen = new HashSet<CornerOrientation>();

            for (int seed = 0; seed < 100; seed++)
            {
                var state = CreateFactory().Create(SeededSource(seed));
                foreach (var pos in Enum.GetValues<CornerId>())
                    seen.Add(state.GetCornerPiece(pos).Orientation);
            }

            seen.Should().BeEquivalentTo(new[]
            {
                CornerOrientation.Default,
                CornerOrientation.Clockwise,
                CornerOrientation.CounterClockwise
            });
        }

        [Fact]
        public void Create_OverManySamples_BothEdgeOrientationsOccur()
        {
            var seen = new HashSet<EdgeOrientation>();

            for (int seed = 0; seed < 100; seed++)
            {
                var state = CreateFactory().Create(SeededSource(seed));
                foreach (var pos in Enum.GetValues<EdgeId>())
                    seen.Add(state.GetEdgePiece(pos).Orientation);
            }

            seen.Should().BeEquivalentTo(new[]
            {
                EdgeOrientation.Default,
                EdgeOrientation.Flipped
            });
        }

        [Fact]
        public void Create_GeneratedFaceletString_IsAlways54Chars()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                CreateFactory().Create(SeededSource(seed))
                    .ToFaceletString().Should().HaveLength(54);
            }
        }

        [Fact]
        public void Create_GeneratedFaceletString_HasNineOfEachColor()
        {
            var state = CreateFactory().Create(SeededSource(123));
            var str = state.ToFaceletString();

            foreach (var c in new[] { 'U', 'R', 'F', 'D', 'L', 'B' })
                str.Count(x => x == c).Should().Be(9);
        }

        private static int Parity<T>(T[] permutation) where T : struct, Enum
        {
            int inversions = 0;
            for (int i = 0; i < permutation.Length; i++)
                for (int j = i + 1; j < permutation.Length; j++)
                    if (Comparer<T>.Default.Compare(permutation[i], permutation[j]) > 0)
                        inversions++;
            return inversions & 1;
        }
    }
}