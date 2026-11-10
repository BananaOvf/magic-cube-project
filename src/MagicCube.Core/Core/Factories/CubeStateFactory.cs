// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;

namespace MagicCube.Core.Core.Factories
{
    public sealed class CubeStateFactory : ICubeStateFactory
    {
        public IMutableCubeState Create() => new CubeState();
    }
}