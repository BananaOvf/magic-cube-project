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
using System.IO;
using System.Windows;

namespace MagicCube.UI.Services
{
    public class WpfLocalizationService : ILocalizationService
    {
        public string CurrentLanguage { get; private set; } = string.Empty;

        public string GetString(string key)
        {
            if (Application.Current.TryFindResource(key) is string result)
                return result;
            return $"[{key}]";
        }

        public string GetLongText(string fileName)
        {
            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "Localization",
                $"{fileName}.{CurrentLanguage}.txt");

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }

            return $"File {fileName} not found for language {CurrentLanguage}.";
        }

        public void SetLanguage(string languageCode)
        {
            if (CurrentLanguage == languageCode) return;

            var dict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/MagicCubeSimulator;component/Resources/Localization/Strings.{languageCode}.xaml")
            };

            var appResources = Application.Current.Resources.MergedDictionaries;

            for (int i = appResources.Count - 1; i >= 0; i--)
            {
                if (appResources[i].Source != null && appResources[i].Source.OriginalString.Contains("Localization/Strings."))
                {
                    appResources.RemoveAt(i);
                }
            }

            appResources.Add(dict);
            CurrentLanguage = languageCode;
        }
    }
}