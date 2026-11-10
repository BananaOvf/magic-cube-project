// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;
using MagicCube.Core.Enums.Pieces;
using MagicCube.Core.Models.Pieces;
using MagicCube.Core.Utils;

namespace MagicCube.Core.Core.Factories
{
    public sealed class RandomCubeStateFactory : IRandomCubeStateFactory
    {
        public IMutableCubeState Create(IRandomSource? source = null)
        {
            source ??= new StandardRandomSource();

            var (corners, edges) = GeneratePieces(source);
            var centers = Enum.GetValues<CenterId>()
                .ToDictionary(id => id, id => new CenterPiece(id, CenterOrientation.Default));

            return new CubeState(centers, edges, corners);
        }

        private static (Dictionary<CornerId, CornerPiece>, Dictionary<EdgeId, EdgePiece>)
            GeneratePieces(IRandomSource source)
        {
            var cornerIds = Enum.GetValues<CornerId>();
            var edgeIds = Enum.GetValues<EdgeId>();
            Shuffle(cornerIds, source);
            Shuffle(edgeIds, source);

            if (Parity(cornerIds) != Parity(edgeIds))
                (edgeIds[0], edgeIds[1]) = (edgeIds[1], edgeIds[0]);

            var cornerOrient = MakeCornerOrientations(source);
            var edgeOrient = MakeEdgeOrientations(source);

            var cornerPositions = Enum.GetValues<CornerId>();
            var corners = new Dictionary<CornerId, CornerPiece>(8);
            for (int i = 0; i < 8; i++)
                corners[cornerPositions[i]] = new CornerPiece(cornerIds[i], cornerOrient[i]);

            var edgePositions = Enum.GetValues<EdgeId>();
            var edges = new Dictionary<EdgeId, EdgePiece>(12);
            for (int i = 0; i < 12; i++)
                edges[edgePositions[i]] = new EdgePiece(edgeIds[i], edgeOrient[i]);

            return (corners, edges);
        }

        private static CornerOrientation[] MakeCornerOrientations(IRandomSource source)
        {
            var r = new CornerOrientation[8];
            int sum = 0;
            for (int i = 0; i < 7; i++) { r[i] = (CornerOrientation)source.Next(3); sum += (int)r[i]; }
            r[7] = (CornerOrientation)((3 - sum % 3) % 3);
            return r;
        }

        private static EdgeOrientation[] MakeEdgeOrientations(IRandomSource source)
        {
            var r = new EdgeOrientation[12];
            int sum = 0;
            for (int i = 0; i < 11; i++) { r[i] = (EdgeOrientation)source.Next(2); sum += (int)r[i]; }
            r[11] = (EdgeOrientation)(sum % 2);
            return r;
        }

        private static void Shuffle<T>(T[] a, IRandomSource source)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = source.Next(i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        private static int Parity<T>(T[] p) where T : struct, Enum
        {
            int inv = 0;
            for (int i = 0; i < p.Length; i++)
                for (int j = i + 1; j < p.Length; j++)
                    if (Comparer<T>.Default.Compare(p[i], p[j]) > 0) inv++;
            return inv & 1;
        }
    }
}