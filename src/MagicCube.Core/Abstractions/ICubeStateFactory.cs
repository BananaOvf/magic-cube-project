// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace MagicCube.Core.Abstractions
{
    public interface ICubeStateFactory
    {
        IMutableCubeState Create();
    }
}
