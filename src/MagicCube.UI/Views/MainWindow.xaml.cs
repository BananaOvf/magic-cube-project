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
using MagicCube.Services;
using MagicCube.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MagicCube.UI
{
    public partial class MainWindow : Window
    {
        private readonly I3DRenderer _renderer;
        private readonly IReadOnlyCubeState _cubeState;
        private readonly ITimerKeyHandler _keyHandler;
        private readonly MainViewModel _viewModel;

        private Point _lastMousePos;
        private bool _isRotating;

        public MainWindow(I3DRenderer renderer, IReadOnlyCubeState cubeState, MainViewModel viewModel)
        {
            InitializeComponent();

            _renderer = renderer;
            _cubeState = cubeState;
            _keyHandler = viewModel;
            _viewModel = viewModel;
            DataContext = viewModel;

            Loaded += OnLoaded;
            PreviewKeyDown += OnPreviewKeyDown;
            PreviewKeyUp += OnPreviewKeyUp;

            _viewModel.MoveHistory.CollectionChanged += (_, _) => CenterHistory();

            CubeViewport.MouseLeftButtonDown += OnMouseDown;
            CubeViewport.MouseMove += OnMouseMove;
            CubeViewport.MouseLeftButtonUp += OnMouseUp;
            CubeViewport.MouseWheel += OnMouseWheel;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _renderer.Initialize(CubeViewport);
            _renderer.UpdateStateImmediately(_cubeState);
            CenterHistory();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                _keyHandler.OnSpaceKeyDown();
                e.Handled = true;
            }
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                _keyHandler.OnSpaceKeyUp();
                e.Handled = true;
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isRotating = true;
            _lastMousePos = e.GetPosition(CubeViewport);
            CubeViewport.CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRotating) return;

            Point currentPos = e.GetPosition(CubeViewport);
            double dx = currentPos.X - _lastMousePos.X;
            double dy = currentPos.Y - _lastMousePos.Y;

            double sensitivity = 0.010;
            _renderer.RotateCamera(-dx * sensitivity, dy * sensitivity);

            _lastMousePos = currentPos;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isRotating = false;
            CubeViewport.ReleaseMouseCapture();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomDelta = e.Delta / 120.0;
            _renderer.ZoomCamera(zoomDelta * 0.5);
        }

        private void CenterHistory()
        {
            if (HistoryCanvas == null) return;

            var history = _viewModel.MoveHistory.ToList();
            int currentIndex = history.FindIndex(x => x.IsCurrent);
            if (currentIndex < 0) currentIndex = 0;

            double centerX = HistoryViewport.ActualWidth / 2.0;
            double spacing = 40;

            while (HistoryCanvas.Children.Count > history.Count)
            {
                HistoryCanvas.Children.RemoveAt(HistoryCanvas.Children.Count - 1);
            }

            for (int i = 0; i < history.Count; i++)
            {
                var item = history[i];
                TextBlock tb;

                if (i < HistoryCanvas.Children.Count)
                {
                    tb = (TextBlock)HistoryCanvas.Children[i];
                }
                else
                {
                    tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
                    HistoryCanvas.Children.Add(tb);
                }

                tb.Text = item.Notation;
                tb.FontSize = item.IsCurrent ? 24 : 18;
                tb.FontWeight = item.IsCurrent ? FontWeights.Bold : FontWeights.Normal;

                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double width = tb.DesiredSize.Width;
                double x = centerX + (i - currentIndex) * spacing - width / 2.0;

                Canvas.SetLeft(tb, x);
                Canvas.SetTop(tb, 15);
            }
        }
    }
}