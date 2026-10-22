// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace MagicCube.Core.Models
{
    public record SolveRecord(Guid Id, DateTime Timestamp, TimeSpan Duration, string Scramble, string Solution);
}
