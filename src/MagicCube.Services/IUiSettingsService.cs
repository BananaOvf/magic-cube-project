// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace MagicCube.Services
{
    public interface IUiSettingsService
    {
        bool IsCompressedView { get; set; }
        bool ShowSplashScreen { get; set; }
        double AnimationTimeScale { get; set; }
        string Language { get; set; }

        Task LoadAsync();
        Task SaveAsync();

        void Save();
        void Load();
    }
}
