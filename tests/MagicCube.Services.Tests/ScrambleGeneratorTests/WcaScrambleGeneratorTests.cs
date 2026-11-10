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
using MagicCube.Core.Core.Factories;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Utils;
using MagicCube.Infrastructure.Services;
using MagicCube.Infrastructure.Solvers;

namespace MagicCube.Services.Tests.ScrambleGeneratorTests
{
    public class WcaScrambleGeneratorTests
    {
        private static WcaScrambleGenerator CreateGenerator(IRandomSource source)
        {
            var factory = new CubeStateFactory();
            return new WcaScrambleGenerator(
                new RandomCubeStateFactory(),
                new KociembaSolver(factory),
                source);
        }

        [Fact]
        public async Task GenerateAsync_SolutionAppliedToSolved_ProducesRandomState()
        {
            var generator = CreateGenerator(new StandardRandomSource(new Random(42)));
            var scramble = await generator.GenerateAsync();

            var state = new CubeStateFactory().Create();
            foreach (var m in scramble) state.ApplyMove(m);

            state.IsSolved.Should().BeFalse();
        }

        [Fact]
        public async Task GenerateAsync_SolutionAppliedToSolved_SolvableByKociemba()
        {
            var factory = new CubeStateFactory();
            var kociemba = new KociembaSolver(factory);
            var generator = new WcaScrambleGenerator(
                new RandomCubeStateFactory(),
                kociemba,
                new StandardRandomSource(new Random(42)));

            var scramble = await generator.GenerateAsync();
            var state = factory.Create();
            foreach (var m in scramble) state.ApplyMove(m);

            var solution = await kociemba.SolveAsync(state);
            foreach (var m in solution) state.ApplyMove(m);

            state.IsSolved.Should().BeTrue();
        }

        [Fact]
        public async Task GenerateAsync_ProducesOnlyFaceMoves()
        {
            var generator = CreateGenerator(new StandardRandomSource(new Random(42)));
            var scramble = await generator.GenerateAsync();

            scramble.Should().AllBeOfType<FaceMove>(
                "WCA 3x3 scrambles are expressed in standard face-turn notation");
        }
    }
}
