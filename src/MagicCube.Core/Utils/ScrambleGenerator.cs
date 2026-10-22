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

namespace MagicCube.Core.Utils
{
    public static class ScrambleGenerator
    {
        private static readonly Face[] Faces =
            { Face.U, Face.D, Face.L, Face.R, Face.F, Face.B };
        private static readonly Direction[] Directions =
            { Direction.Clockwise, Direction.CounterClockwise, Direction.Double };

        private static readonly Random Rng = new();
        private static readonly object LockObj = new();

        // Define axes: 0 = U/D, 1 = R/L, 2 = F/B
        private static int GetAxis(Face face)
        {
            return face switch
            {
                Face.U => 0,
                Face.D => 0,
                Face.R => 1,
                Face.L => 1,
                Face.F => 2,
                Face.B => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(face))
            };
        }

        public static List<Move> Generate(int length = 20)
        {
            var moves = new List<Move>(length);
            Face lastFace = Face.U;

            lock (LockObj)
            {
                for (int i = 0; i < length; i++)
                {
                    Face face;
                    int lastAxis = GetAxis(lastFace);
                    do
                    {
                        face = Faces[Rng.Next(Faces.Length)];
                    } while (GetAxis(face) == lastAxis);

                    Direction dir = Directions[Rng.Next(Directions.Length)];
                    moves.Add(new FaceMove(face, dir));
                    lastFace = face;
                }
            }
            return moves;
        }
    }
}