// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace MagicCube.Services
{
    public interface IDialogService
    {
        void ShowMessage(string text, string title);
        bool? ShowConfirmation(string text, string title);
        string? ShowSaveFileDialog(string filter, string defaultExt);
    }
}
