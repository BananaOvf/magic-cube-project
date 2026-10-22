# Magic Cube Simulator

**Magic Cube Simulator** is a native 3D Rubik's Cube application built with C# and WPF.

Our vision is to evolve this project from a simple 3D simulator into a **comprehensive, open-source tool for speedcubers**. We aim to provide a lightweight, highly customizable environment for practicing, timing, and analyzing solves without the bloat of modern web-based timers.

[![.NET Build and Test](https://github.com/BananaOvf/magic-cube-project/actions/workflows/dotnet.yml/badge.svg)](https://github.com/BananaOvf/magic-cube-project/actions/workflows/dotnet.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

![Magic Cube Simulator Main Window](docs/images/main-window.png)

## 📥 Download

You don't need to build the project to use it! Go to the [**Releases page**](https://github.com/BananaOvf/magic-cube-project/releases/latest) and download the latest version. 

*   **`standalone.zip`** – Recommended for most users. No installation required.
*   **`win-x64.zip`** – Lightweight version. Requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0/runtime).

## Features (Current Status)

* **Interactive 3D Cube:** Full 3D rotation and zooming using WPF's native `Viewport3D`.
* **Multiple Solvers:** Includes both the classic CFOP method and Kociemba's Two-Phase Algorithm for auto-solving.
* **Smooth Animations:** Every move is animated with configurable speed.
* **Timer & Statistics:** Built-in timer with hold-to-start functionality and a solve history list.
* **Localization:** Full support for both English and Russian languages.
* **Excel Export:** Export your solve history to an `.xlsx` file for external analysis.

![Solving animation demo](docs/images/solving-demo.gif)

## Getting Started

### Prerequisites

* **Operating System:** Windows 10 or later.
* **SDK:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or later).
* **IDE:** Visual Studio 2022+ (recommended) or JetBrains Rider.

### Build & Run

1. **Clone the repository:**

```bash
git clone https://github.com/BananaOvf/magic-cube-project.git
cd magic-cube-project
```

2. **Restore and build:**

```bash
dotnet build
```

3. **Run the application:**

```bash
dotnet run --project src/MagicCube.UI
```

Alternatively, open `MagicCube.slnx` in Visual Studio and press `F5`.

## Architecture

The solution is split into several projects:

* `MagicCube.Core`: The core logic of the Rubik's Cube (state, moves, history).
* `MagicCube.Services`: Interfaces for services.
* `MagicCube.Infrastructure`: Implementations for services (solvers, repositories, settings).
* `MagicCube.UI`: The WPF application (views, viewmodels, renderers).
* `tests/`: Unit tests for the core logic.

## Roadmap

> **Note:** This roadmap represents our current priorities, not a strict timeline. **We welcome Pull Requests for ANY feature at ANY time!** If you see something you'd like to implement, or have an idea of your own, go for it!

Here is what we are planning to build. You can help with any of these blocks:

### 🎯 Speedcubing Core (Current Focus)
*The essential features to make this a serious training tool.*
- [ ] **WCA Compliant Scrambles:** Standard random-state generator.
- [ ] **Inspection Timer & Penalties:** 15-second countdown and support for `+2`/`DNF`.
- [ ] **Advanced Statistics:** Implement Ao5, Ao12, and Mo3.

### 🎨 UI/UX Improvements
- [ ] **Interactive Mouse Controls:** Click and drag the cube's faces to perform turns.
- [ ] **Dark Theme:** Modern UI redesign for comfortable practice.

### 🔮 Future Explorations (Help Wanted!)
*These are long-term ideas that require significant research or hardware. If you have experience in these areas, we'd love your help!*
- [ ] **Computer Vision:** Webcam state recognition (Requires C++ / OpenCV).
- [ ] **Hardware Integration:** Smart Cube (BLE) support.
- [ ] **Other Puzzles:** 2x2, 4x4, 5x5, and Pyraminx (Requires core rewrite).

## Contributing

Contributions are welcome! Please open an issue or submit a pull request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the **GNU General Public License v3.0 (GPLv3)**. See the [LICENSE](LICENSE) file for the full text.
This project also includes third-party code. See [THIRD\_PARTY\_LICENSES.md](THIRD_PARTY_LICENSES.md) for details.

## Acknowledgments

* **Kociemba Two-Phase Algorithm:** Herbert Kociemba (http://kociemba.org/)
* **C# Port of Kociemba:** Megalomatt (https://github.com/Megalomatt/Kociemba)
* **CFOP Solver (CubeXdotNET):** Divin Mathew (divinsmathew)