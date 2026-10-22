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

using MagicCube.Core.Models;
using MagicCube.Services;
using System.Text.Json;

namespace MagicCube.Infrastructure.Repositories
{
    public class JsonSolvesRepository : ISolvesRepository
    {
        private readonly string _filePath;
        private List<SolveRecord> _records = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public JsonSolvesRepository(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            var folder = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            Load();
        }

        private void Load()
        {
            if (!File.Exists(_filePath)) return;

            string json = File.ReadAllText(_filePath);
            _records = JsonSerializer.Deserialize<List<SolveRecord>>(json) ?? new List<SolveRecord>();
        }

        private async Task SaveAsync()
        {
            string json = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }

        public async Task AddAsync(SolveRecord record, CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token);
            try { _records.Add(record); await SaveAsync(); }
            finally { _semaphore.Release(); }
        }

        public async Task ClearAllAsync(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token);
            try { _records.Clear(); await SaveAsync(); }
            finally { _semaphore.Release(); }
        }

        public async Task DeleteAsync(Guid id, CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token);
            try { _records.RemoveAll(r => r.Id == id); await SaveAsync(); }
            finally { _semaphore.Release(); }
        }

        public async Task<IEnumerable<SolveRecord>> GetAllAsync(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token);
            try { return _records.ToList(); }
            finally { _semaphore.Release(); }
        }
    }
}