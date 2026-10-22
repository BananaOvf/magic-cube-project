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

using MagicCube.Core.Abstractions;
using MagicCube.Core.Utils;
using MagicCube.Services;

namespace MagicCube.Infrastructure.Services
{
    public class IniSettingsService : ISettingsService
    {
        private readonly ICubeStateFactory _factory;
        private readonly string _filePath;

        public IniSettingsService(string filePath, ICubeStateFactory factory)
        {
            _factory = factory;
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        public Task SaveStateAsync(IReadOnlyCubeState state, CancellationToken token = default)
        {
            var facelet = state.ToFaceletString();
            var historyStr = MoveParser.SerializeMany(state.MoveHistory);
            int currentIndex = state.CurrentMoveIndex;

            string content = $"[State]\nFaceletString={facelet}\nMoveHistory={historyStr}\nCurrentMoveIndex={currentIndex}\n";
            return File.WriteAllTextAsync(_filePath, content, token);
        }

        public Task<IMutableCubeState> LoadStateAsync(CancellationToken token = default)
        {
            var cubeState = _factory.Create();
            if (!File.Exists(_filePath))
                return Task.FromResult<IMutableCubeState>(cubeState);

            string content = File.ReadAllText(_filePath);
            string facelet = string.Empty;
            string historyStr = string.Empty;
            int currentIndex = -1;

            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("FaceletString="))
                    facelet = trimmed.Substring(14);
                else if (trimmed.StartsWith("MoveHistory="))
                    historyStr = trimmed.Substring(12);
                else if (trimmed.StartsWith("CurrentMoveIndex="))
                    int.TryParse(trimmed.Substring(17), out currentIndex);
            }

            if (string.IsNullOrEmpty(facelet))
                return Task.FromResult<IMutableCubeState>(cubeState);

            if (string.IsNullOrEmpty(historyStr))
                return Task.FromResult<IMutableCubeState>(cubeState);

            var moves = MoveParser.ParseMany(historyStr);
            cubeState.LoadFromHistory(moves, currentIndex);
            return Task.FromResult<IMutableCubeState>(cubeState);
        }

        public void SaveState(IReadOnlyCubeState state)
        {
            var facelet = state.ToFaceletString();
            var historyStr = MoveParser.SerializeMany(state.MoveHistory);
            int currentIndex = state.CurrentMoveIndex;
            string content = $"[State]\nFaceletString={facelet}\nMoveHistory={historyStr}\nCurrentMoveIndex={currentIndex}\n";
            File.WriteAllText(_filePath, content);
        }

        public void LoadState(IMutableCubeState intoState)
        {
            if (!File.Exists(_filePath)) return;
            string content = File.ReadAllText(_filePath);
            string facelet = string.Empty;
            string historyStr = string.Empty;
            int currentIndex = -1;

            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("FaceletString="))
                    facelet = trimmed.Substring(14);
                else if (trimmed.StartsWith("MoveHistory="))
                    historyStr = trimmed.Substring(12);
                else if (trimmed.StartsWith("CurrentMoveIndex="))
                    int.TryParse(trimmed.Substring(17), out currentIndex);
            }

            if (string.IsNullOrEmpty(facelet))
                return;

            if (string.IsNullOrEmpty(historyStr))
                return;

            var moves = MoveParser.ParseMany(historyStr);
            intoState.LoadFromHistory(moves, currentIndex);
            return;
        }
    }
}