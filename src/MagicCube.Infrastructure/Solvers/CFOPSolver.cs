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

using CubeXdotNET;
using MagicCube.Core.Abstractions;
using MagicCube.Core.Enums;
using MagicCube.Core.Enums.Pieces;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Utils;
using MagicCube.Services;
using System.Collections.Immutable;

namespace MagicCube.Infrastructure.Solvers
{
    public class CFOPSolver : ICubeSolver
    {
        private readonly ImmutableList<ImmutableList<RotationMove>> _cubeOrientations;
        private readonly ImmutableDictionary<RotationMove, ImmutableDictionary<Face, Face>> _facePermutations;

        public string Name => "CFOP Solver";

        public CFOPSolver(ICubeStateFactory factory)
        {
            _facePermutations = BuildFacePermutations(factory);
            _cubeOrientations = BuildCubeOrientations();
        }

        public async Task<IReadOnlyList<Move>> SolveAsync(
            IReadOnlyCubeState state, CancellationToken token = default)
        {
            IMutableCubeState workState = (IMutableCubeState)state.Clone();

            // Orientation: white on D, green on F, red on L
            ImmutableList<RotationMove> orientationMoves = FindOrientationMoves(workState);
            foreach (RotationMove rot in orientationMoves)
                workState.ApplyMove(rot);

            // String for FridrichSolver
            string facelets = ConvertToCubeXFacelets(workState);

            // Launching the solver
            var cubeXSolver = new FridrichSolver(facelets);
            int result = await Task.Run(() => cubeXSolver.Solve(), token);

            if (result != 1 || string.IsNullOrWhiteSpace(cubeXSolver.Solution))
                return Array.Empty<Move>();

            // Solution parsing
            IReadOnlyList<Move> orientedMoves = MoveParser.ParseMany(cubeXSolver.Solution);

            // Backtracking moves
            ImmutableList<RotationMove> inverseOrientation = InverseSequence(orientationMoves);
            ImmutableDictionary<Face, Face> faceMap = ComposePermutations(inverseOrientation);

            var finalMoves = new List<Move>();
            foreach (Move move in orientedMoves)
            {
                if (move is FaceMove fm)
                {
                    Face newFace = faceMap[fm.Face];
                    finalMoves.Add(new FaceMove(newFace, fm.Direction));
                }
                else
                {
                    finalMoves.Add(move);
                }
            }

            return finalMoves;
        }

        private ImmutableList<RotationMove> FindOrientationMoves(IMutableCubeState scrambled)
        {
            foreach (var orientation in _cubeOrientations)
            {
                IMutableCubeState clone = (IMutableCubeState)scrambled.Clone();
                foreach (var rot in orientation)
                    clone.ApplyMove(rot);

                CenterId whitePos = GetCenterPosition(clone, CenterId.U);
                CenterId greenPos = GetCenterPosition(clone, CenterId.F);
                CenterId redPos = GetCenterPosition(clone, CenterId.R);

                if (whitePos == CenterId.D && greenPos == CenterId.F && redPos == CenterId.L)
                    return orientation;
            }
            throw new InvalidOperationException("Cannot orient cube for CFOP solver");
        }

        private static string ConvertToCubeXFacelets(IReadOnlyCubeState state)
        {
            // Our format: U, R, F, D, L, B; each face is represented in row-major order (9 characters)
            string src = state.ToFaceletString();
            string u = src.Substring(0, 9);   // after orientation: yellow (y)
            string r = src.Substring(9, 9);   // orange (o)
            string f = src.Substring(18, 9);  // green (g)
            string d = src.Substring(27, 9);  // white (w)
            string l = src.Substring(36, 9);  // red (r)
            string b = src.Substring(45, 9);  // blue (b)

            // Order of faces: F, L, B, R, U, D
            string uRotated = string.Concat(
                u[2], u[5], u[8],
                u[1], u[4], u[7],
                u[0], u[3], u[6]
            );
            string dRotated = string.Concat(
                d[6], d[3], d[0],
                d[7], d[4], d[1],
                d[8], d[5], d[2]
            );
            string reordered = f + r + b + l + uRotated + dRotated;

            var colorMap = new Dictionary<char, char>
            {
                { 'F', 'g' },  // green
                { 'L', 'o' },  // orange
                { 'B', 'b' },  // blue
                { 'R', 'r' },  // red
                { 'U', 'w' },  // yellow
                { 'D', 'y' }   // white
            };

            char[] cubeX = reordered.ToCharArray();
            for (int i = 0; i < cubeX.Length; i++)
                cubeX[i] = colorMap[cubeX[i]];

            return new string(cubeX);
        }

        private static CenterId GetCenterPosition(IMutableCubeState state, CenterId color)
        {
            foreach (CenterId pos in Enum.GetValues<CenterId>())
                if (state.GetCenterPiece(pos).Id == color)
                    return pos;
            throw new InvalidOperationException($"Center {color} not found.");
        }

        private ImmutableDictionary<Face, Face> ComposePermutations(
            IEnumerable<RotationMove> moves)
        {
            var current = new Dictionary<Face, Face>
            {
                { Face.U, Face.U }, { Face.D, Face.D }, { Face.L, Face.L },
                { Face.R, Face.R }, { Face.F, Face.F }, { Face.B, Face.B }
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

        private static ImmutableList<RotationMove> InverseSequence(
            IEnumerable<RotationMove> seq) =>
            seq.Reverse().Select(r => (RotationMove)r.Inverse()).ToImmutableList();

        private static ImmutableList<ImmutableList<RotationMove>> BuildCubeOrientations()
        {
            var baseMoves = new List<RotationMove>();
            foreach (Axis axis in Enum.GetValues<Axis>())
                foreach (Direction dir in Enum.GetValues<Direction>())
                    baseMoves.Add(new RotationMove(axis, dir));

            var list = new List<ImmutableList<RotationMove>>();
            list.AddRange(baseMoves.Select(m => ImmutableList.Create(m)));
            list.AddRange(from m1 in baseMoves from m2 in baseMoves select ImmutableList.Create(m1, m2));
            return list.ToImmutableList();
        }

        private static ImmutableDictionary<RotationMove, ImmutableDictionary<Face, Face>>
            BuildFacePermutations(ICubeStateFactory factory)
        {
            var dict = new Dictionary<RotationMove, ImmutableDictionary<Face, Face>>();
            var identity = new Dictionary<CenterId, CenterId>
            {
                { CenterId.U, CenterId.U }, { CenterId.D, CenterId.D },
                { CenterId.L, CenterId.L }, { CenterId.R, CenterId.R },
                { CenterId.F, CenterId.F }, { CenterId.B, CenterId.B }
            };
            foreach (Axis axis in Enum.GetValues<Axis>())
                foreach (Direction dir in new[] { Direction.Clockwise, Direction.CounterClockwise, Direction.Double })
                {
                    var move = new RotationMove(axis, dir);
                    var state = factory.Create();
                    state.ApplyMove(move);
                    var map = new Dictionary<Face, Face>();
                    foreach (var kv in identity)
                    {
                        CenterId newPos = GetCenterPosition(state, kv.Value);
                        map[CenterToFace(kv.Key)] = CenterToFace(newPos);
                    }
                    dict[move] = map.ToImmutableDictionary();
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
    }
}