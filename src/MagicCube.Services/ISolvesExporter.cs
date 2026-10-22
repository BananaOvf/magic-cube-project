// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Models;

namespace MagicCube.Services
{
    public interface ISolvesExporter
    {
        Task ExportAsync(IEnumerable<SolveRecord> records, string filePath, CancellationToken token = default);
    }
}
