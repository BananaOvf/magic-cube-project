// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Utils;
using MagicCube.Services;

namespace MagicCube.Infrastructure.Services
{
    public sealed class WcaScrambleGenerator : IScrambleGenerator
    {
        private readonly IRandomCubeStateFactory _factory;
        private readonly IOptimalCubeSolver _solver;
        private readonly IRandomSource _source;

        public string Name => "WCA 3x3";

        public WcaScrambleGenerator(
            IRandomCubeStateFactory factory,
            IOptimalCubeSolver solver,
            IRandomSource? source = null)
        {
            _factory = factory;
            _solver = solver;
            _source = source ?? new SecureRandomSource();
        }

        public async Task<IReadOnlyList<Move>> GenerateAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var state = _factory.Create(_source);

            if (state.IsSolved)
                throw new InvalidOperationException(
                    "Random state generator returned a solved state — this should never happen.");

            var solution = await _solver.SolveAsync(state, ct);

            if (solution.Count == 0)
                throw new InvalidOperationException(
                    "Solver returned empty solution for a non-solved state — " +
                    "check Kociemba tables and facelet conversion.");

            return solution.Reverse().Select(m => m.Inverse()).ToList();
        }
    }
}