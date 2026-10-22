// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Models;

namespace MagicCube.Services
{
    public interface ISolvesRepository
    {
        Task AddAsync(SolveRecord record, CancellationToken token = default);
        Task<IEnumerable<SolveRecord>> GetAllAsync(CancellationToken token = default);
        Task DeleteAsync(Guid id, CancellationToken token = default);
        Task ClearAllAsync(CancellationToken token = default);
    }
}
