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
    public static class MoveParser
    {
        public static List<Move> ParseMany(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation))
                return new List<Move>();

            return notation.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                           .Select(ParseOne)
                           .ToList();
        }

        public static string SerializeMany(IEnumerable<Move> moves)
        {
            return string.Join(" ", moves.Select(m => m.ToNotationString()));
        }

        private static readonly Dictionary<char, Func<char, Direction, Move>> Parsers = new()
        {
            ['R'] = (c, d) => new FaceMove(Face.R, d),
            ['U'] = (c, d) => new FaceMove(Face.U, d),
            ['L'] = (c, d) => new FaceMove(Face.L, d),
            ['D'] = (c, d) => new FaceMove(Face.D, d),
            ['F'] = (c, d) => new FaceMove(Face.F, d),
            ['B'] = (c, d) => new FaceMove(Face.B, d),
            ['M'] = (c, d) => new SliceMove(Slice.M, d),
            ['E'] = (c, d) => new SliceMove(Slice.E, d),
            ['S'] = (c, d) => new SliceMove(Slice.S, d),
            ['X'] = (c, d) => new RotationMove(Axis.X, d),
            ['Y'] = (c, d) => new RotationMove(Axis.Y, d),
            ['Z'] = (c, d) => new RotationMove(Axis.Z, d),
        };

        public static Move ParseOne(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be empty");

            token = token.Trim();
            char first = token[0];
            bool isLower = char.IsLower(first);
            char baseChar = char.ToUpper(first);

            if (!Parsers.ContainsKey(baseChar))
                throw new FormatException($"Invalid move token: {token}");

            Direction direction = token.Length > 1
                ? token[1] switch
                {
                    '\'' => Direction.CounterClockwise,
                    '2' => Direction.Double,
                    _ => Direction.Clockwise
                }
                : Direction.Clockwise;

            if (isLower && baseChar is 'X' or 'Y' or 'Z')
                return new RotationMove(baseChar switch { 'X' => Axis.X, 'Y' => Axis.Y, 'Z' => Axis.Z }, direction);

            if (isLower)
                return new WideMove(Enum.Parse<Face>(baseChar.ToString()), direction);

            return Parsers[baseChar](baseChar, direction);
        }
    }
}