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

using Kociemba;
using MagicCube.Core.Abstractions;
using MagicCube.Core.Enums;
using MagicCube.Core.Enums.Pieces;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Utils;
using MagicCube.Services;
using System.Collections.Immutable;

namespace MagicCube.Infrastructure.Solvers
{
    public class KociembaSolver : ICubeSolver
    {
        private static readonly string TablesFolder = Path.Combine(
            AppContext.BaseDirectory,
            "Assets", "Kociemba", "Tables");

        private readonly ImmutableDictionary<(CenterId, CenterId), ImmutableList<RotationMove>> _orientationMap;
        private readonly ImmutableDictionary<RotationMove, ImmutableDictionary<Face, Face>> _facePermutations;

        public string Name => "Kociemba Solver";

        public KociembaSolver(ICubeStateFactory factory)
        {
            _orientationMap = BuildOrientationMap(factory);
            _facePermutations = BuildFacePermutations(factory);
        }

        public async Task<IReadOnlyList<Move>> SolveAsync(
            IReadOnlyCubeState state,
            CancellationToken token = default)
        {
            IMutableCubeState oriented = (IMutableCubeState)state.Clone();

            CenterId whitePos = FindCurrentPosition(oriented, CenterId.U);
            CenterId greenPos = FindCurrentPosition(oriented, CenterId.F);

            if (!_orientationMap.TryGetValue((whitePos, greenPos), out var orientationMoves))
                throw new InvalidOperationException($"Invalid centers: white {whitePos}, green {greenPos}");

            foreach (var rot in orientationMoves)
                oriented.ApplyMove(rot);

            string facelets = oriented.ToFaceletString();

            bool buildTables = !TablesAlreadyExist();

            string rawSolution = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                string info = "";

                if (buildTables)
                    return SearchRunTime.solution(facelets, out info, buildTables: true);
                else
                    return Search.solution(facelets, out info);
            }, token);

            if (string.IsNullOrWhiteSpace(rawSolution) || rawSolution.StartsWith("Error"))
                return Array.Empty<Move>();

            IReadOnlyList<Move> orientedMoves = MoveParser.ParseMany(rawSolution);

            var inverseOrientation = InverseSequence(orientationMoves);

            var faceMap = ComposePermutations(inverseOrientation);

            var result = new List<Move>();
            foreach (Move move in orientedMoves)
            {
                if (move is FaceMove fm)
                {
                    Face newFace = faceMap[fm.Face];
                    result.Add(new FaceMove(newFace, fm.Direction));
                }
                else
                {
                    result.Add(move);
                }
            }

            return result;
        }

        private static bool TablesAlreadyExist()
        {
            string twistPath = Path.Combine(TablesFolder, "twist");
            return File.Exists(twistPath);
        }

        private static ImmutableDictionary<(CenterId, CenterId), ImmutableList<RotationMove>> BuildOrientationMap(ICubeStateFactory factory)
        {
            var map = new Dictionary<(CenterId, CenterId), ImmutableList<RotationMove>>();
            var solved = factory.Create();

            var singleMoves = new List<ImmutableList<RotationMove>>();
            foreach (Axis axis in Enum.GetValues<Axis>())
                foreach (Direction dir in new[] { Direction.Clockwise, Direction.CounterClockwise, Direction.Double })
                    singleMoves.Add(ImmutableList.Create(new RotationMove(axis, dir)));

            map[(CenterId.U, CenterId.F)] = ImmutableList<RotationMove>.Empty;

            foreach (var seq in singleMoves)
                TryAddOrientation(map, seq, solved);

            foreach (var seq1 in singleMoves)
                foreach (var seq2 in singleMoves)
                    TryAddOrientation(map, seq1.AddRange(seq2), solved);

            return map.ToImmutableDictionary();
        }

        private static void TryAddOrientation(
            Dictionary<(CenterId, CenterId), ImmutableList<RotationMove>> map,
            ImmutableList<RotationMove> moveSeq,
            IReadOnlyCubeState solved)
        {
            IMutableCubeState state = (IMutableCubeState)solved.Clone();
            foreach (var m in moveSeq) state.ApplyMove(m);

            CenterId white = FindCurrentPosition(state, CenterId.U);
            CenterId green = FindCurrentPosition(state, CenterId.F);

            if (AreOpposite(white, green) || map.ContainsKey((white, green)))
                return;

            map[(white, green)] = InverseSequence(moveSeq);
        }

        private static ImmutableDictionary<RotationMove, ImmutableDictionary<Face, Face>> BuildFacePermutations(ICubeStateFactory factory)
        {
            var dict = new Dictionary<RotationMove, ImmutableDictionary<Face, Face>>();
            var identityCenters = new Dictionary<CenterId, CenterId>
            {
                {CenterId.U, CenterId.U}, {CenterId.D, CenterId.D}, {CenterId.L, CenterId.L},
                {CenterId.R, CenterId.R}, {CenterId.F, CenterId.F}, {CenterId.B, CenterId.B}
            };

            foreach (Axis axis in Enum.GetValues<Axis>())
                foreach (Direction dir in new[] { Direction.Clockwise, Direction.CounterClockwise, Direction.Double })
                {
                    var move = new RotationMove(axis, dir);
                    var state = factory.Create();
                    state.ApplyMove(move);

                    var mapping = new Dictionary<Face, Face>();
                    foreach (var kv in identityCenters)
                    {
                        CenterId originalPos = kv.Key;
                        CenterId originalId = kv.Value;

                        CenterId newPos = FindCurrentPosition(state, originalId);
                        mapping[CenterToFace(originalPos)] = CenterToFace(newPos);
                    }
                    dict[move] = mapping.ToImmutableDictionary();
                }
            return dict.ToImmutableDictionary();
        }

        private static Face CenterToFace(CenterId id) => id switch
        {
            CenterId.U => Face.U,
            CenterId.D => Face.D,
            CenterId.L => Face.L,
            CenterId.R => Face.R,
            CenterId.F => Face.F,
            CenterId.B => Face.B,
            _ => throw new ArgumentException()
        };


        private ImmutableDictionary<Face, Face> ComposePermutations(IEnumerable<RotationMove> moves)
        {
            var current = new Dictionary<Face, Face>
            {
                {Face.U, Face.U}, {Face.D, Face.D}, {Face.L, Face.L},
                {Face.R, Face.R}, {Face.F, Face.F}, {Face.B, Face.B}
            };

            foreach (var move in moves)
            {
                var perm = _facePermutations[move];
                var next = new Dictionary<Face, Face>();
                foreach (var kv in current)
                    next[kv.Key] = perm[kv.Value];
                current = next;
            }
            return current.ToImmutableDictionary();
        }

        private static ImmutableList<RotationMove> InverseSequence(IEnumerable<RotationMove> seq) =>
            seq.Reverse().Select(r => (RotationMove)r.Inverse()).ToImmutableList();

        private static CenterId FindCurrentPosition(IReadOnlyCubeState state, CenterId targetColor)
        {
            foreach (CenterId pos in Enum.GetValues<CenterId>())
                if (state.GetCenterPiece(pos).Id == targetColor)
                    return pos;
            throw new InvalidOperationException($"Center {targetColor} not found");
        }

        private static bool AreOpposite(CenterId a, CenterId b) =>
            (a, b) switch
            {
                (CenterId.U, CenterId.D) or (CenterId.D, CenterId.U) => true,
                (CenterId.F, CenterId.B) or (CenterId.B, CenterId.F) => true,
                (CenterId.L, CenterId.R) or (CenterId.R, CenterId.L) => true,
                _ => false
            };
    }
}