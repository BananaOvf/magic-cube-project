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

namespace MagicCube.UI.Services
{
    public static class HistoryCompressor
    {
        public static List<Move> Compress(IReadOnlyList<Move> history, out Dictionary<int, int> indexMap)
        {
            indexMap = new Dictionary<int, int>();
            var stack = new List<(Move move, List<int> sourceIndices)>();

            for (int i = 0; i < history.Count; i++)
            {
                var current = history[i];

                if (stack.Count > 0 && AreSameType(stack[^1].move, current))
                {
                    var (topMove, topIndices) = stack[^1];
                    int sumDir = DirectionToValue(topMove.Direction) + DirectionToValue(current.Direction);
                    int effective = sumDir % 4;

                    if (effective == 0)
                    {
                        stack.RemoveAt(stack.Count - 1);
                    }
                    else
                    {
                        Direction newDir = effective switch
                        {
                            1 => Direction.Clockwise,
                            2 => Direction.Double,
                            3 => Direction.CounterClockwise,
                            _ => Direction.Clockwise
                        };
                        var newMove = topMove with { Direction = newDir };
                        topIndices.Add(i);
                        stack[^1] = (newMove, topIndices);
                    }
                }
                else
                {
                    stack.Add((current, new List<int> { i }));
                }
            }

            var compressed = new List<Move>();
            for (int cIdx = 0; cIdx < stack.Count; cIdx++)
            {
                var (move, indices) = stack[cIdx];
                compressed.Add(move);
                foreach (var srcIdx in indices)
                    indexMap[srcIdx] = cIdx;
            }

            return compressed;
        }

        private static bool AreSameType(Move a, Move b) => (a, b) switch
        {
            (FaceMove f1, FaceMove f2) => f1.Face == f2.Face,
            (SliceMove s1, SliceMove s2) => s1.Slice == s2.Slice,
            (WideMove w1, WideMove w2) => w1.Face == w2.Face,
            (RotationMove r1, RotationMove r2) => r1.Axis == r2.Axis,
            _ => false
        };

        private static int DirectionToValue(Direction d) => d switch
        {
            Direction.Clockwise => 1,
            Direction.Double => 2,
            Direction.CounterClockwise => 3,
            _ => 0
        };
    }
}