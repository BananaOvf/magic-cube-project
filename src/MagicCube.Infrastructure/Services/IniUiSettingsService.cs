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

using MagicCube.Services;
using System.Globalization;

namespace MagicCube.Infrastructure.Services
{
    public class IniUiSettingsService : IUiSettingsService
    {
        private readonly string _filePath;
        private bool _isLoaded;

        public bool IsCompressedView { get; set; } = true;
        public bool ShowSplashScreen { get; set; } = true;
        public double AnimationTimeScale { get; set; } = 1.0;
        public string Language { get; set; } = "en";

        public IniUiSettingsService(string filePath)
        {
            _filePath = filePath;
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        public Task LoadAsync()
        {
            if (!File.Exists(_filePath))
                return Task.CompletedTask;

            var lines = File.ReadAllLines(_filePath);
            foreach (var line in lines)
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "IsCompressedView":
                        if (bool.TryParse(value, out var compressed))
                            IsCompressedView = compressed;
                        break;
                    case "ShowSplashScreen":
                        if (bool.TryParse(value, out var showSplash))
                            ShowSplashScreen = showSplash;
                        break;
                    case "AnimationTimeScale":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale))
                            AnimationTimeScale = scale;
                        break;
                    case "Language":
                        if (value is not null)
                            Language = value;
                        break;
                }
            }

            _isLoaded = true;
            return Task.CompletedTask;
        }

        public Task SaveAsync()
        {
            var lines = new List<string>
            {
                $"IsCompressedView={IsCompressedView}",
                $"ShowSplashScreen={ShowSplashScreen}",
                $"AnimationTimeScale={AnimationTimeScale.ToString(CultureInfo.InvariantCulture)}",
                $"Language={Language}"
            };

            return File.WriteAllLinesAsync(_filePath, lines);
        }

        public void Save()
        {
            var lines = new List<string>
            {
                $"IsCompressedView={IsCompressedView}",
                $"ShowSplashScreen={ShowSplashScreen}",
                $"AnimationTimeScale={AnimationTimeScale.ToString(CultureInfo.InvariantCulture)}",
                $"Language={Language}"
            };
            File.WriteAllLines(_filePath, lines);
        }

        public void Load()
        {
            if (!File.Exists(_filePath)) return;
            var lines = File.ReadAllLines(_filePath);
            foreach (var line in lines)
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                switch (key)
                {
                    case "IsCompressedView":
                        if (bool.TryParse(value, out var compressed)) IsCompressedView = compressed;
                        break;
                    case "ShowSplashScreen":
                        if (bool.TryParse(value, out var showSplash)) ShowSplashScreen = showSplash;
                        break;
                    case "AnimationTimeScale":
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale))
                            AnimationTimeScale = scale;
                        break;
                    case "Language":
                        if (value is not null)
                            Language = value;
                        break;
                }
            }
        }
    }
}