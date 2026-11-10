// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;

namespace MagicCube.Core.Utils
{
    public sealed class StandardRandomSource : IRandomSource
    {
        private readonly Random _rng;

        public StandardRandomSource(Random? rng = null) => _rng = rng ?? Random.Shared;

        public int Next(int maxExclusive) => _rng.Next(maxExclusive);
    }
}
