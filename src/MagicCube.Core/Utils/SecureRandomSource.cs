// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;
using System.Security.Cryptography;

namespace MagicCube.Core.Utils
{
    public sealed class SecureRandomSource : IRandomSource
    {
        public int Next(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);
    }
}
