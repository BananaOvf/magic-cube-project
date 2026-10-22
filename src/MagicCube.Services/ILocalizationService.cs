// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace MagicCube.Services
{
    public interface ILocalizationService
    {
        string GetString(string key);
        string GetLongText(string fileName);
        void SetLanguage(string languageCode);
        string CurrentLanguage { get; }
    }
}
