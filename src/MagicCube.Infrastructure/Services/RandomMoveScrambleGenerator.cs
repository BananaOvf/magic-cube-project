// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;
using MagicCube.Core.Enums;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Utils;

namespace MagicCube.Infrastructure.Services
{
    public sealed class RandomMoveScrambleGenerator : IScrambleGenerator
    {
        private static readonly Face[] Faces =
            { Face.U, Face.D, Face.L, Face.R, Face.F, Face.B };
        private static readonly Direction[] Directions =
            { Direction.Clockwise, Direction.CounterClockwise, Direction.Double };

        private readonly int _length;
        private readonly IRandomSource _source;

        public string Name => "Random moves";

        public RandomMoveScrambleGenerator(int length = 20, IRandomSource? source = null)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            _length = length;
            _source = source ?? new StandardRandomSource();
        }

        public Task<IReadOnlyList<Move>> GenerateAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var moves = new List<Move>(_length);
            Face? lastFace = null;

            for (int i = 0; i < _length; i++)
            {
                Face face;
                do { face = Faces[_source.Next(Faces.Length)]; }
                while (lastFace.HasValue && GetAxis(face) == GetAxis(lastFace.Value));

                var dir = Directions[_source.Next(Directions.Length)];
                moves.Add(new FaceMove(face, dir));
                lastFace = face;
            }

            return Task.FromResult<IReadOnlyList<Move>>(moves);
        }

        private static int GetAxis(Face face) => face switch
        {
            Face.U or Face.D => 0,
            Face.R or Face.L => 1,
            Face.F or Face.B => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };
    }
}