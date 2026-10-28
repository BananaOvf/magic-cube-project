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

using MagicCube.Core.Abstractions;
using MagicCube.Core.Enums;
using MagicCube.Core.Enums.Pieces;
using MagicCube.Core.Extensions;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Models.Pieces;
using System.Text;

namespace MagicCube.Core.Core
{
    public class CubeState : IMutableCubeState
    {
        private readonly Dictionary<CenterId, CenterPiece> _centers = new();
        private readonly Dictionary<EdgeId, EdgePiece> _edges = new();
        private readonly Dictionary<CornerId, CornerPiece> _corners = new();

        private readonly List<Move> _history = new();
        private int _current = -1;

        public IReadOnlyList<Move> MoveHistory => _history.AsReadOnly();
        public int CurrentMoveIndex => _current;

        public bool IsSolved
        {
            get
            {
                foreach (var edge in _edges)
                    if (edge.Key != edge.Value.Id || edge.Value.Orientation != EdgeOrientation.Default)
                        return false;
                foreach (var corner in _corners)
                    if (corner.Key != corner.Value.Id || corner.Value.Orientation != CornerOrientation.Default)
                        return false;
                foreach (var center in _centers)
                    if (center.Key != center.Value.Id || center.Value.Orientation != CenterOrientation.Default)
                        return false;
                return true;
            }
        }

        public CubeState() => Reset();

        private CubeState(bool skipReset) { if (!skipReset) Reset(); }

        public void LoadFromHistory(IReadOnlyList<Move> history, int currentIndex)
        {
            Reset();
            _history.AddRange(history);
            _current = Math.Min(currentIndex, history.Count - 1);
            for (int i = 0; i <= _current; i++)
                ApplyMoveInternal(history[i]);
        }

        public void ApplyMove(Move move)
        {
            if (_current < _history.Count - 1)
                _history.RemoveRange(_current + 1, _history.Count - _current - 1);
            _history.Add(move);
            _current++;
            ApplyMoveInternal(move);
        }

        public CenterPiece GetCenterPiece(CenterId position) => _centers[position];
        public CornerPiece GetCornerPiece(CornerId position) => _corners[position];
        public EdgePiece GetEdgePiece(EdgeId position) => _edges[position];

        public void Undo()
        {
            if (_current < 0) return;
            ApplyMoveInternal(_history[_current].Inverse());
            _current--;
        }

        public void Redo()
        {
            if (_current >= _history.Count - 1) return;
            _current++;
            ApplyMoveInternal(_history[_current]);
        }

        public void Reset()
        {
            _centers.Clear();
            foreach (CenterId id in Enum.GetValues(typeof(CenterId)))
                _centers[id] = new CenterPiece(id, CenterOrientation.Default);

            _edges.Clear();
            foreach (EdgeId id in Enum.GetValues(typeof(EdgeId)))
                _edges[id] = new EdgePiece(id, EdgeOrientation.Default);

            _corners.Clear();
            foreach (CornerId id in Enum.GetValues(typeof(CornerId)))
                _corners[id] = new CornerPiece(id, CornerOrientation.Default);

            _history.Clear();
            _current = -1;
        }

        public string ToFaceletString()
        {
            var sb = new StringBuilder(54);
            AppendFacelets(sb, Face.U);
            AppendFacelets(sb, Face.R);
            AppendFacelets(sb, Face.F);
            AppendFacelets(sb, Face.D);
            AppendFacelets(sb, Face.L);
            AppendFacelets(sb, Face.B);
            return sb.ToString();
        }

        private void AppendFacelets(StringBuilder sb, Face face)
        {
            foreach (var pos in GetFaceletPositions())
                sb.Append(GetFaceletColor(face, pos));
        }

        private static IEnumerable<(int row, int col)> GetFaceletPositions()
        {
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    yield return (r, c);
        }

        private Face GetCenterFace(CenterId position) =>
            _centers[position].Id switch
            {
                CenterId.U => Face.U,
                CenterId.D => Face.D,
                CenterId.L => Face.L,
                CenterId.R => Face.R,
                CenterId.F => Face.F,
                CenterId.B => Face.B,
                _ => throw new InvalidOperationException()
            };

        private char GetFaceletColor(Face face, (int row, int col) pos)
        {
            var (elementId, stickerIndex) = GetElementAtFacelet(face, pos);
            return elementId switch
            {
                CenterId centerId => FaceToChar(_centers[centerId].Id switch
                {
                    CenterId.U => Face.U,
                    CenterId.D => Face.D,
                    CenterId.L => Face.L,
                    CenterId.R => Face.R,
                    CenterId.F => Face.F,
                    CenterId.B => Face.B,
                    _ => throw new InvalidOperationException()
                }),
                EdgeId edgeId => FaceToChar(GetEdgeFaceColor(_edges[edgeId], stickerIndex)),
                CornerId cornerId => FaceToChar(GetCornerFaceColor(_corners[cornerId], stickerIndex)),
                _ => '?'
            };
        }

        private static (object elementId, int stickerIndex) GetElementAtFacelet(Face face, (int row, int col) pos)
        {
            // Order of stickers for corners: URF = (U, R, F) indices 0, 1, 2; for edges: UR = (U, R) indices 0, 1
            return face switch
            {
                Face.U => pos switch
                {
                    (0, 0) => (CornerId.ULB, 0),
                    (0, 1) => (EdgeId.UB, 0),
                    (0, 2) => (CornerId.UBR, 0),
                    (1, 0) => (EdgeId.UL, 0),
                    (1, 1) => (CenterId.U, 0),
                    (1, 2) => (EdgeId.UR, 0),
                    (2, 0) => (CornerId.UFL, 0),
                    (2, 1) => (EdgeId.UF, 0),
                    (2, 2) => (CornerId.URF, 0),
                    _ => throw new ArgumentOutOfRangeException()
                },
                Face.R => pos switch
                {
                    (0, 0) => (CornerId.URF, 1),
                    (0, 1) => (EdgeId.UR, 1),
                    (0, 2) => (CornerId.UBR, 2),
                    (1, 0) => (EdgeId.FR, 1),
                    (1, 1) => (CenterId.R, 0),
                    (1, 2) => (EdgeId.BR, 1),
                    (2, 0) => (CornerId.DFR, 2),
                    (2, 1) => (EdgeId.DR, 1),
                    (2, 2) => (CornerId.DRB, 1),
                    _ => throw new ArgumentOutOfRangeException()
                },
                Face.F => pos switch
                {
                    (0, 0) => (CornerId.UFL, 1),
                    (0, 1) => (EdgeId.UF, 1),
                    (0, 2) => (CornerId.URF, 2),
                    (1, 0) => (EdgeId.FL, 0),
                    (1, 1) => (CenterId.F, 0),
                    (1, 2) => (EdgeId.FR, 0),
                    (2, 0) => (CornerId.DLF, 2),
                    (2, 1) => (EdgeId.DF, 1),
                    (2, 2) => (CornerId.DFR, 1),
                    _ => throw new ArgumentOutOfRangeException()
                },
                Face.D => pos switch
                {
                    (0, 0) => (CornerId.DLF, 0),
                    (0, 1) => (EdgeId.DF, 0),
                    (0, 2) => (CornerId.DFR, 0),
                    (1, 0) => (EdgeId.DL, 0),
                    (1, 1) => (CenterId.D, 0),
                    (1, 2) => (EdgeId.DR, 0),
                    (2, 0) => (CornerId.DBL, 0),
                    (2, 1) => (EdgeId.DB, 0),
                    (2, 2) => (CornerId.DRB, 0),
                    _ => throw new ArgumentOutOfRangeException()
                },
                Face.L => pos switch
                {
                    (0, 0) => (CornerId.ULB, 1),
                    (0, 1) => (EdgeId.UL, 1),
                    (0, 2) => (CornerId.UFL, 2),
                    (1, 0) => (EdgeId.BL, 1),
                    (1, 1) => (CenterId.L, 0),
                    (1, 2) => (EdgeId.FL, 1),
                    (2, 0) => (CornerId.DBL, 2),
                    (2, 1) => (EdgeId.DL, 1),
                    (2, 2) => (CornerId.DLF, 1),
                    _ => throw new ArgumentOutOfRangeException()
                },
                Face.B => pos switch
                {
                    (0, 0) => (CornerId.UBR, 1),
                    (0, 1) => (EdgeId.UB, 1),
                    (0, 2) => (CornerId.ULB, 2),
                    (1, 0) => (EdgeId.BR, 0),
                    (1, 1) => (CenterId.B, 0),
                    (1, 2) => (EdgeId.BL, 0),
                    (2, 0) => (CornerId.DRB, 2),
                    (2, 1) => (EdgeId.DB, 1),
                    (2, 2) => (CornerId.DBL, 1),
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentException()
            };
        }

        private static Face GetEdgeFaceColor(EdgePiece edge, int stickerIndex)
        {
            // In the assembled state, the edge ID contains two face letters (e.g., UR -> U and R)
            // Index 0 corresponds to the first letter, 1 to the second.
            // Default orientation: stickerIndex = 0 -> first letter, 1 -> second.
            var id = edge.Id.ToString();
            var face0 = Enum.Parse<Face>(id[0].ToString());
            var face1 = Enum.Parse<Face>(id[1].ToString());
            return edge.Orientation == EdgeOrientation.Default
                ? (stickerIndex == 0 ? face0 : face1)
                : (stickerIndex == 0 ? face1 : face0);
        }

        private static Face GetCornerFaceColor(CornerPiece corner, int stickerIndex)
        {
            var id = corner.Id;
            var faces = id switch
            {
                CornerId.URF => new[] { Face.U, Face.R, Face.F },
                CornerId.UFL => new[] { Face.U, Face.F, Face.L },
                CornerId.ULB => new[] { Face.U, Face.L, Face.B },
                CornerId.UBR => new[] { Face.U, Face.B, Face.R },
                CornerId.DFR => new[] { Face.D, Face.F, Face.R },
                CornerId.DLF => new[] { Face.D, Face.L, Face.F },
                CornerId.DBL => new[] { Face.D, Face.B, Face.L },
                CornerId.DRB => new[] { Face.D, Face.R, Face.B },
                _ => throw new ArgumentException()
            };

            return corner.Orientation switch
            {
                CornerOrientation.Default => faces[stickerIndex],
                CornerOrientation.Clockwise => faces[(stickerIndex + 2) % 3],
                CornerOrientation.CounterClockwise => faces[(stickerIndex + 1) % 3],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static Face[] GetCornerBaseFaces(CornerId id) => id switch
        {
            CornerId.URF => new[] { Face.U, Face.R, Face.F },
            CornerId.UFL => new[] { Face.U, Face.F, Face.L },
            CornerId.ULB => new[] { Face.U, Face.L, Face.B },
            CornerId.UBR => new[] { Face.U, Face.B, Face.R },
            CornerId.DFR => new[] { Face.D, Face.F, Face.R },
            CornerId.DLF => new[] { Face.D, Face.L, Face.F },
            CornerId.DBL => new[] { Face.D, Face.B, Face.L },
            CornerId.DRB => new[] { Face.D, Face.R, Face.B },
            _ => throw new ArgumentException()
        };

        private static char FaceToChar(Face face) => face.ToString()[0];

        // ---------- Apply move internal ----------
        private void ApplyMoveInternal(Move move)
        {
            switch (move)
            {
                case FaceMove fm: TurnFace(fm.Face, fm.Direction); break;
                case SliceMove sm: TurnSlice(sm.Slice, sm.Direction); break;
                case WideMove wm: TurnWide(wm.Face, wm.Direction); break;
                case RotationMove rm: RotateCube(rm.Axis, rm.Direction); break;
                default: throw new NotSupportedException($"Unknown move type: {move.GetType()}");
            }
        }

        private void TurnFace(Face face, Direction direction)
        {
            int times = DirectionToCount(direction);
            TurnFaceTimes(face, times);
        }

        private void TurnFaceTimes(Face face, int times)
        {
            switch (face)
            {
                case Face.U: TurnU(times); break;
                case Face.D: TurnD(times); break;
                case Face.L: TurnL(times); break;
                case Face.R: TurnR(times); break;
                case Face.F: TurnF(times); break;
                case Face.B: TurnB(times); break;
            }
        }

        // ---------- Face rotations with cycles and orientations ----------
        private void TurnU(int times)
        {
            var cornerCycle = new[] { CornerId.URF, CornerId.UFL, CornerId.ULB, CornerId.UBR };
            CycleCorners(cornerCycle, times);

            var edgeCycle = new[] { EdgeId.UR, EdgeId.UF, EdgeId.UL, EdgeId.UB };
            CycleEdges(edgeCycle, times);
        }

        private void TurnD(int times)
        {
            var cornerCycle = new[] { CornerId.DFR, CornerId.DRB, CornerId.DBL, CornerId.DLF };
            CycleCorners(cornerCycle, times);

            var edgeCycle = new[] { EdgeId.DR, EdgeId.DB, EdgeId.DL, EdgeId.DF };
            CycleEdges(edgeCycle, times);
        }

        private void TurnR(int times)
        {
            for (int t = 0; t < times; t++)
            {
                var edgeCycle = new[] { EdgeId.UR, EdgeId.BR, EdgeId.DR, EdgeId.FR };
                var edgePieces = new EdgePiece[4];
                for (int i = 0; i < 4; i++) edgePieces[i] = _edges[edgeCycle[i]];
                for (int i = 0; i < 4; i++)
                {
                    int nextIdx = (i + 1) % 4;
                    _edges[edgeCycle[nextIdx]] = edgePieces[i];
                }
            }

            for (int t = 0; t < times; t++)
                RotateCornersForFace(Face.R);
        }

        private void TurnL(int times)
        {
            for (int t = 0; t < times; t++)
            {
                var edgeCycle = new[] { EdgeId.UL, EdgeId.FL, EdgeId.DL, EdgeId.BL };
                var edgePieces = new EdgePiece[4];
                for (int i = 0; i < 4; i++) edgePieces[i] = _edges[edgeCycle[i]];
                for (int i = 0; i < 4; i++)
                {
                    int nextIdx = (i + 1) % 4;
                    _edges[edgeCycle[nextIdx]] = edgePieces[i];
                }
            }
            for (int t = 0; t < times; t++)
                RotateCornersForFace(Face.L);
        }

        private void TurnF(int times)
        {
            for (int t = 0; t < times; t++)
            {
                var edgeCycle = new[] { EdgeId.UF, EdgeId.FR, EdgeId.DF, EdgeId.FL };
                var edgePieces = new EdgePiece[4];
                for (int i = 0; i < 4; i++) edgePieces[i] = _edges[edgeCycle[i]];
                for (int i = 0; i < 4; i++)
                {
                    int nextIdx = (i + 1) % 4;
                    var moved = edgePieces[i] with
                    {
                        Orientation = edgePieces[i].Orientation == EdgeOrientation.Default
                            ? EdgeOrientation.Flipped
                            : EdgeOrientation.Default
                    };
                    _edges[edgeCycle[nextIdx]] = moved;
                }
            }
            for (int t = 0; t < times; t++)
                RotateCornersForFace(Face.F);
        }

        private void TurnB(int times)
        {
            for (int t = 0; t < times; t++)
            {
                var edgeCycle = new[] { EdgeId.UB, EdgeId.BL, EdgeId.DB, EdgeId.BR };
                var edgePieces = new EdgePiece[4];
                for (int i = 0; i < 4; i++) edgePieces[i] = _edges[edgeCycle[i]];
                for (int i = 0; i < 4; i++)
                {
                    int nextIdx = (i + 1) % 4;
                    var moved = edgePieces[i] with
                    {
                        Orientation = edgePieces[i].Orientation == EdgeOrientation.Default
                            ? EdgeOrientation.Flipped
                            : EdgeOrientation.Default
                    };
                    _edges[edgeCycle[nextIdx]] = moved;
                }
            }
            for (int t = 0; t < times; t++)
                RotateCornersForFace(Face.B);
        }

        // ---------- Turns of slices ----------
        private void TurnSlice(Slice slice, Direction direction)
        {
            int times = DirectionToCount(direction);
            switch (slice)
            {
                case Slice.M: TurnSliceM(times); break; // In L direction
                case Slice.E: TurnSliceE(times); break; // In D direction
                case Slice.S: TurnSliceS(times); break; // In F direction
            }
        }

        private void TurnSliceM(int times)
        {
            var edgeCycle = new[] { EdgeId.UB, EdgeId.UF, EdgeId.DF, EdgeId.DB };
            CycleEdges(edgeCycle, times, changeOrientation: true, slice: Slice.M);

            var centerCycle = new[] { CenterId.U, CenterId.F, CenterId.D, CenterId.B };
            CycleCenters(centerCycle, times);
        }

        private void TurnSliceE(int times)
        {
            var edgeCycle = new[] { EdgeId.FR, EdgeId.BR, EdgeId.BL, EdgeId.FL };
            CycleEdges(edgeCycle, times, changeOrientation: true, slice: Slice.E);

            var centerCycle = new[] { CenterId.F, CenterId.R, CenterId.B, CenterId.L };
            CycleCenters(centerCycle, times);
        }

        private void TurnSliceS(int times)
        {
            var edgeCycle = new[] { EdgeId.UR, EdgeId.DR, EdgeId.DL, EdgeId.UL };
            CycleEdges(edgeCycle, times, changeOrientation: true, slice: Slice.S);

            var centerCycle = new[] { CenterId.U, CenterId.R, CenterId.D, CenterId.L };
            CycleCenters(centerCycle, times);
        }

        // ---------- Wide moves ----------
        private void TurnWide(Face face, Direction direction)
        {
            switch (face)
            {
                case Face.U: TurnFace(Face.U, direction); TurnSlice(Slice.E, direction.Inverse()); break;
                case Face.D: TurnFace(Face.D, direction); TurnSlice(Slice.E, direction); break;
                case Face.L: TurnFace(Face.L, direction); TurnSlice(Slice.M, direction); break;
                case Face.R: TurnFace(Face.R, direction); TurnSlice(Slice.M, direction.Inverse()); break;
                case Face.F: TurnFace(Face.F, direction); TurnSlice(Slice.S, direction); break;
                case Face.B: TurnFace(Face.B, direction); TurnSlice(Slice.S, direction.Inverse()); break;
            }
        }

        // ---------- Cube rotations ----------
        private void RotateCube(Axis axis, Direction direction)
        {
            int times = DirectionToCount(direction);
            for (int i = 0; i < times; i++)
                RotateCubeOnce(axis);
        }

        private void RotateCubeOnce(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: RotateX(); break;
                case Axis.Y: RotateY(); break;
                case Axis.Z: RotateZ(); break;
            }
        }

        private void RotateX()
        {
            TurnFace(Face.R, Direction.Clockwise);
            TurnSlice(Slice.M, Direction.CounterClockwise);
            TurnFace(Face.L, Direction.CounterClockwise);
        }

        private void RotateY()
        {
            TurnFace(Face.U, Direction.Clockwise);
            TurnSlice(Slice.E, Direction.CounterClockwise);
            TurnFace(Face.D, Direction.CounterClockwise);
        }

        private void RotateZ()
        {
            TurnFace(Face.F, Direction.Clockwise);
            TurnSlice(Slice.S, Direction.Clockwise);
            TurnFace(Face.B, Direction.CounterClockwise);
        }

        // ---------- Auxiliary methods of permutations ----------
        private void CycleCorners(CornerId[] cycle, int times)
        {
            if (cycle.Length == 0) return;

            for (int j = 0; j < times; j++)
            {
                var last = _corners[cycle[^1]];

                for (int i = cycle.Length - 1; i > 0; i--)
                    _corners[cycle[i]] = _corners[cycle[i - 1]];

                _corners[cycle[0]] = last;
            }
        }

        private void RotateCornersForFace(Face face)
        {
            (CornerId id, int delta)[] corners = face switch
            {
                Face.L => new[] {
                    (CornerId.ULB, 1), (CornerId.UFL, 2),
                    (CornerId.DLF, 1), (CornerId.DBL, 2)
                },
                Face.R => new[] {
                    (CornerId.URF, 1), (CornerId.UBR, 2),
                    (CornerId.DRB, 1), (CornerId.DFR, 2)
                },
                Face.F => new[] {
                    (CornerId.UFL, 1), (CornerId.URF, 2),
                    (CornerId.DFR, 1), (CornerId.DLF, 2)
                },
                Face.B => new[] {
                    (CornerId.UBR, 1), (CornerId.ULB, 2),
                    (CornerId.DBL, 1), (CornerId.DRB, 2)
                },
                _ => throw new ArgumentException()
            };

            var temp = new CornerPiece[corners.Length];

            for (int i = 0; i < corners.Length; i++)
                temp[i] = _corners[corners[i].id];

            for (int i = 0; i < corners.Length; i++)
            {
                int next = (i + 1) % 4;

                var piece = temp[i];

                var newOrientation =
                    (CornerOrientation)(((int)piece.Orientation + corners[i].delta) % 3);

                _corners[corners[next].id] = piece with
                {
                    Orientation = newOrientation
                };
            }
        }

        private void CycleEdges(
            EdgeId[] cycle,
            int times,
            bool changeOrientation = false,
            Face face = default,
            Slice slice = default)
        {
            if (cycle.Length == 0) return;

            for (int j = 0; j < times; j++)
            {
                var last = _edges[cycle[^1]];

                for (int i = cycle.Length - 1; i > 0; i--)
                    _edges[cycle[i]] = _edges[cycle[i - 1]];

                _edges[cycle[0]] = last;

                if (changeOrientation)
                {
                    foreach (var id in cycle)
                    {
                        var edge = _edges[id];

                        var newOrientation =
                            edge.Orientation == EdgeOrientation.Default
                                ? EdgeOrientation.Flipped
                                : EdgeOrientation.Default;

                        _edges[id] = edge with
                        {
                            Orientation = newOrientation
                        };
                    }
                }
            }
        }

        private void CycleCenters(CenterId[] cycle, int times)
        {
            if (cycle.Length == 0) return;

            for (int j = 0; j < times; j++)
            {
                var last = _centers[cycle[^1]];

                for (int i = cycle.Length - 1; i > 0; i--)
                    _centers[cycle[i]] = _centers[cycle[i - 1]];

                _centers[cycle[0]] = last;

                // The orientation of the centers is not taken into account
            }
        }

        private int DirectionToCount(Direction direction) => direction switch
        {
            Direction.Clockwise => 1,
            Direction.CounterClockwise => 3,
            Direction.Double => 2,
            _ => 0
        };

        public IReadOnlyCubeState Clone()
        {
            var clone = new CubeState(skipReset: false);
            foreach (var kv in _centers) clone._centers[kv.Key] = kv.Value;
            foreach (var kv in _edges) clone._edges[kv.Key] = kv.Value;
            foreach (var kv in _corners) clone._corners[kv.Key] = kv.Value;
            clone._history.AddRange(_history);
            clone._current = _current;
            return clone;
        }
    }
}