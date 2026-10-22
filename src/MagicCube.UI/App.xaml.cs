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
using MagicCube.Core.Core;
using MagicCube.Infrastructure.Exporters;
using MagicCube.Infrastructure.Providers;
using MagicCube.Infrastructure.Repositories;
using MagicCube.Infrastructure.Services;
using MagicCube.Infrastructure.Solvers;
using MagicCube.Services;
using MagicCube.UI.Renderers;
using MagicCube.UI.Services;
using MagicCube.UI.ViewModels;
using MagicCube.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace MagicCube.UI
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override async void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var uiSettings = _serviceProvider.GetRequiredService<IUiSettingsService>();
            await uiSettings.LoadAsync();

            if (uiSettings.ShowSplashScreen)
            {
                var splash = _serviceProvider.GetRequiredService<SplashScreenWindow>();
                splash.Show();

                var loadDataTask = Task.Run(async () =>
                {
                    var settings = _serviceProvider.GetRequiredService<ISettingsService>();
                    var mutable = _serviceProvider.GetRequiredService<IMutableCubeState>();

                    var loadedState = await settings.LoadStateAsync();
                    if (loadedState != null)
                    {
                        mutable.LoadFromHistory(loadedState.MoveHistory, loadedState.CurrentMoveIndex);
                    }
                });

                var delayTask = Task.Delay(1500);

                await Task.WhenAll(loadDataTask, delayTask);

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

                await splash.CloseWithAnimationAsync();
                await Task.Delay(300);

                mainWindow.Show();
            }
            else
            {
                var settings = _serviceProvider.GetRequiredService<ISettingsService>();
                var mutable = _serviceProvider.GetRequiredService<IMutableCubeState>();
                var loadedState = await settings.LoadStateAsync();
                if (loadedState != null)
                {
                    mutable.LoadFromHistory(loadedState.MoveHistory, loadedState.CurrentMoveIndex);
                }

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core
            services.AddSingleton<ICubeStateFactory, CubeStateFactory>();
            services.AddSingleton<IMutableCubeState>(sp =>
            {
                var factory = sp.GetRequiredService<ICubeStateFactory>();
                return factory.Create();
            });
            services.AddSingleton<IReadOnlyCubeState>(sp => sp.GetRequiredService<IMutableCubeState>());
            services.AddSingleton<ICubeStateProvider, ManualCubeStateProvider>();

            // Infrastructure paths
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MagicCube");
            Directory.CreateDirectory(appData);

            services.AddSingleton<ISolvesRepository>(new JsonSolvesRepository(
                Path.Combine(appData, "solves.json")));

            services.AddSingleton<ISettingsService>(sp =>
                new IniSettingsService(
                    Path.Combine(appData, "state.ini"),
                    sp.GetRequiredService<ICubeStateFactory>()));

            services.AddSingleton<IUiSettingsService>(
                new IniUiSettingsService(
                    Path.Combine(appData, "ui_settings.ini")));

            // Services
            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddSingleton<ISolvesExporter, ExcelSolvesExporter>();
            services.AddSingleton<ITimerService, DispatcherTimerService>();
            services.AddSingleton<ILocalizationService, WpfLocalizationService>();

            // Solvers
            services.AddSingleton<ICubeSolver, CFOPSolver>();
            services.AddSingleton<ICubeSolver, KociembaSolver>();

            // Renderers
            services.AddSingleton<I3DRenderer, PrimitiveCubeRenderer>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            // Windows
            services.AddTransient<MainWindow>(sp =>
            {
                var renderer = sp.GetRequiredService<I3DRenderer>();
                var state = sp.GetRequiredService<IReadOnlyCubeState>();
                var vm = sp.GetRequiredService<MainViewModel>();

                var window = new MainWindow(renderer, state, vm);
                return window;
            });

            services.AddSingleton<SplashScreenWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider != null)
            {
                var uiSettings = _serviceProvider.GetRequiredService<IUiSettingsService>();
                uiSettings.Save();

                var settings = _serviceProvider.GetRequiredService<ISettingsService>();
                var cubeState = _serviceProvider.GetRequiredService<IReadOnlyCubeState>();
                settings.SaveState(cubeState);
            }

            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}