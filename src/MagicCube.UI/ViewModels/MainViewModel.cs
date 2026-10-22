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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagicCube.Core.Abstractions;
using MagicCube.Core.Models;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Utils;
using MagicCube.Services;
using MagicCube.UI.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MagicCube.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject, ITimerKeyHandler
    {
        private readonly IReadOnlyCubeState _cubeState;
        private readonly IMutableCubeState _mutableCubeState;
        private readonly ICubeStateProvider _stateProvider;
        private readonly ITimerService _timerService;
        private readonly I3DRenderer _renderer;
        private readonly ISolvesRepository _solvesRepository;
        private readonly ISolvesExporter _solvesExporter;
        private readonly ISettingsService _settingsService;
        private readonly IUiSettingsService _uiSettingsService;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localization;

        private bool _isAnimating;
        private int _solveStartHistoryIndex = -1;
        private Stopwatch _holdStopwatch = new Stopwatch();
        private bool _spacePressed;
        private static readonly TimeSpan HoldDuration = TimeSpan.FromSeconds(1);

        private CancellationTokenSource? _holdCts;
        private CancellationTokenSource? _solveCts;
        private CancellationTokenSource? _queueCts;
        private CancellationTokenSource? _animationCts;

        private SemaphoreSlim _executionSemaphore = new(1, 1);

        [ObservableProperty] private List<ICubeSolver> _solvers;

        [ObservableProperty] private double _holdProgress;
        [ObservableProperty] private bool _isHoldIndicatorVisible;
        [ObservableProperty] private bool _isCompressedView = true;
        [ObservableProperty] private bool _showSplashScreenSetting;
        [ObservableProperty] private bool _isApplyingSolve;
        [ObservableProperty] private double _holdIndicatorOpacity;
        [ObservableProperty] private double _animationTimeScale = 1.0;
        [ObservableProperty] private TimeSpan _elapsedTime;
        [ObservableProperty] private string _currentMoveDisplay;
        [ObservableProperty] private string _scrambleText = string.Empty;

        [ObservableProperty] private bool _isEnglishSelected;
        [ObservableProperty] private bool _isRussianSelected;

        public ObservableCollection<HistoryItem> MoveHistory { get; } = new();
        public ObservableCollection<SolveRecord> Solves { get; } = new();

        public MainViewModel(
            IReadOnlyCubeState cubeState,
            IMutableCubeState mutableCubeState,
            ICubeStateProvider stateProvider,
            ITimerService timerService,
            I3DRenderer renderer,
            ISolvesRepository solvesRepository,
            ISolvesExporter solvesExporter,
            ISettingsService settingsService,
            IUiSettingsService uiSettingsService,
            IDialogService dialogService,
            IEnumerable<ICubeSolver> solvers,
            ILocalizationService localization)
        {
            _cubeState = cubeState;
            _mutableCubeState = mutableCubeState;
            _stateProvider = stateProvider;
            _timerService = timerService;
            _renderer = renderer;
            _solvesRepository = solvesRepository;
            _settingsService = settingsService;
            _uiSettingsService = uiSettingsService;
            _dialogService = dialogService;
            _solvesExporter = solvesExporter;
            _solvers = solvers.ToList();
            _localization = localization;

            IsCompressedView = _uiSettingsService.IsCompressedView;
            ShowSplashScreenSetting = _uiSettingsService.ShowSplashScreen;
            AnimationTimeScale = _uiSettingsService.AnimationTimeScale;

            _localization.SetLanguage(_uiSettingsService.Language);
            IsEnglishSelected = (_uiSettingsService.Language == "en");
            IsRussianSelected = (_uiSettingsService.Language == "ru");

            CurrentMoveDisplay = _localization.GetString("Status_Ready");

            _queueCts = new CancellationTokenSource();

            _timerService.Tick += OnTimerTick;

            _ = LoadSolvesAsync();
            RefreshHistory();
        }

        partial void OnIsHoldIndicatorVisibleChanged(bool value)
        {
            if (_timerService.IsRunning) return;
            HoldIndicatorOpacity = value ? 1.0 : 0.0;
        }

        partial void OnShowSplashScreenSettingChanged(bool value)
        {
            _uiSettingsService.ShowSplashScreen = value;
        }

        partial void OnAnimationTimeScaleChanged(double value)
        {
            _uiSettingsService.AnimationTimeScale = value;
        }

        partial void OnIsCompressedViewChanged(bool value)
        {
            _uiSettingsService.IsCompressedView = value;
            RefreshHistory();
        }

        private TimeSpan GetAnimationDuration() =>
            TimeSpan.FromSeconds(0.5 / AnimationTimeScale);

        private void CancelSolve()
        {
            _solveCts?.Cancel();
            _solveCts = null;
        }

        private void CancelQueueAndAnimation()
        {
            _queueCts?.Cancel();
            _animationCts?.Cancel();
        }

        private void CancelAndResetQueue()
        {
            CancelSolve();
            CancelQueueAndAnimation();
            IsApplyingSolve = false;
            _queueCts?.Dispose();
            _queueCts = new CancellationTokenSource();
        }

        private void OnTimerTick(TimeSpan elapsed)
        {
            ElapsedTime = elapsed;
        }

        private async Task LoadSolvesAsync()
        {
            var items = await _solvesRepository.GetAllAsync();
            Solves.Clear();
            foreach (var item in items.OrderByDescending(s => s.Timestamp))
                Solves.Add(item);
        }

        private void RefreshHistory()
        {
            MoveHistory.Clear();

            if (IsCompressedView)
            {
                var compressed = HistoryCompressor.Compress(_cubeState.MoveHistory, out var indexMap);
                int curIdx = _cubeState.CurrentMoveIndex;
                int currentCompressedIdx = -1;
                if (curIdx >= 0)
                {
                    for (int i = curIdx; i >= 0; i--)
                        if (indexMap.TryGetValue(i, out int mapped))
                        {
                            currentCompressedIdx = mapped;
                            break;
                        }
                }
                for (int i = 0; i < compressed.Count; i++)
                    MoveHistory.Add(new HistoryItem(compressed[i].ToNotationString(), i == currentCompressedIdx));

                CurrentMoveDisplay = currentCompressedIdx >= 0 && currentCompressedIdx < compressed.Count
                    ? compressed[currentCompressedIdx].ToNotationString()
                    : _localization.GetString("Status_NoMoves");
            }
            else
            {
                int curIdx = _cubeState.CurrentMoveIndex;
                for (int i = 0; i < _cubeState.MoveHistory.Count; i++)
                    MoveHistory.Add(new HistoryItem(_cubeState.MoveHistory[i].ToNotationString(), i == curIdx));

                CurrentMoveDisplay = curIdx >= 0 && curIdx < _cubeState.MoveHistory.Count
                    ? _cubeState.MoveHistory[curIdx].ToNotationString()
                    : _localization.GetString("Status_NoMoves");
            }

            OnPropertyChanged(nameof(CurrentMoveDisplay));
        }

        private Move? GetCurrentMove()
        {
            int cur = _cubeState.CurrentMoveIndex;
            return cur >= 0 && cur < _cubeState.MoveHistory.Count ? _cubeState.MoveHistory[cur] : null;
        }
        private Move? GetNextMove()
        {
            int cur = _cubeState.CurrentMoveIndex;
            return cur + 1 < _cubeState.MoveHistory.Count ? _cubeState.MoveHistory[cur + 1] : null;
        }

        [RelayCommand]
        private async Task DeleteSolve(Guid id)
        {
            bool? confirmed = _dialogService.ShowConfirmation(
                _localization.GetString("Dialog_DeleteSolve_Message"),
                _localization.GetString("Dialog_DeleteSolve_Title"));

            if (confirmed != true)
                return;

            await _solvesRepository.DeleteAsync(id);
            await LoadSolvesAsync();
        }

        [RelayCommand]
        private async Task ResetCube()
        {
            CancelSolve();
            CancelQueueAndAnimation();
            IsApplyingSolve = false;

            _timerService.Reset();
            IsHoldIndicatorVisible = false;
            HoldProgress = 0;
            ScrambleText = string.Empty;
            _solveStartHistoryIndex = -1;

            using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try { await _executionSemaphore.WaitAsync(waitCts.Token); }
            catch (OperationCanceledException) { return; }

            try
            {
                _mutableCubeState.Reset();
                RefreshHistory();
                _stateProvider.NotifyStateChanged();
                _renderer.UpdateStateImmediately(_cubeState);
            }
            finally
            {
                _queueCts?.Dispose();
                _queueCts = new CancellationTokenSource();
                _executionSemaphore.Release();
            }
        }

        [RelayCommand]
        private void StopSolving()
        {
            CancelSolve();
            CancelQueueAndAnimation();
            IsApplyingSolve = false;
            _queueCts?.Dispose();
            _queueCts = new CancellationTokenSource();
        }

        [RelayCommand]
        private void ResetCamera()
        {
            _renderer.ResetCamera();
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task Undo()
        {
            if (IsApplyingSolve)
                CancelAndResetQueue();

            CancellationToken queueToken;
            try { queueToken = _queueCts!.Token; }
            catch (ObjectDisposedException) { return; }

            try { await _executionSemaphore.WaitAsync(queueToken); }
            catch (OperationCanceledException) { return; }

            var lastMove = GetCurrentMove();
            if (lastMove == null)
            {
                _executionSemaphore.Release();
                return;
            }

            _isAnimating = true;
            _animationCts = new CancellationTokenSource();
            CancellationToken animToken = _animationCts.Token;

            try
            {
                var inverse = lastMove.Inverse();
                await _renderer.AnimateMoveAsync(
                    inverse,
                    _cubeState,
                    mutable =>
                    {
                        mutable.Undo();
                        RefreshHistory();
                        _stateProvider.NotifyStateChanged();
                    },
                    duration: GetAnimationDuration(),
                    token: animToken);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isAnimating = false;
                _animationCts?.Dispose();
                _animationCts = null;
                _executionSemaphore.Release();
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task Redo()
        {
            if (IsApplyingSolve)
                CancelAndResetQueue();

            CancellationToken queueToken;
            try { queueToken = _queueCts!.Token; }
            catch (ObjectDisposedException) { return; }

            try { await _executionSemaphore.WaitAsync(queueToken); }
            catch (OperationCanceledException) { return; }

            var nextMove = GetNextMove();
            if (nextMove == null)
            {
                _executionSemaphore.Release();
                return;
            }

            _isAnimating = true;
            _animationCts = new CancellationTokenSource();
            CancellationToken animToken = _animationCts.Token;

            try
            {
                await _renderer.AnimateMoveAsync(
                    nextMove,
                    _cubeState,
                    mutable =>
                    {
                        mutable.Redo();
                        RefreshHistory();
                        _stateProvider.NotifyStateChanged();
                    },
                    duration: GetAnimationDuration(),
                    token: animToken);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isAnimating = false;
                _animationCts?.Dispose();
                _animationCts = null;
                _executionSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task ExecuteMove(Move move)
        {
            if (!IsApplyingSolve) CancelSolve();

            CancellationToken queueToken;
            try { queueToken = _queueCts!.Token; }
            catch (ObjectDisposedException) { return; }

            try { await _executionSemaphore.WaitAsync(queueToken); }
            catch (OperationCanceledException) { return; }

            _isAnimating = true;
            _animationCts = new CancellationTokenSource();
            CancellationToken animToken = _animationCts.Token;

            try
            {
                await _renderer.AnimateMoveAsync(
                    move,
                    _cubeState,
                    mutable =>
                    {
                        mutable.ApplyMove(move);

                        RefreshHistory();
                        _stateProvider.NotifyStateChanged();
                    },
                    duration: GetAnimationDuration(),
                    token: animToken
                );
            }
            catch (OperationCanceledException)
            {
                _renderer.UpdateStateImmediately(_cubeState);
            }
            finally
            {
                _isAnimating = false;
                _animationCts?.Dispose();
                _animationCts = null;
                _executionSemaphore.Release();
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task ExecuteNotation(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation)) return;

            if (IsApplyingSolve)
                CancelAndResetQueue();

            try
            {
                var move = MoveParser.ParseOne(notation);
                await ExecuteMove(move);
            }
            catch (FormatException) { }
        }

        [RelayCommand]
        private async Task Scramble()
        {
            CancelSolve();
            CancelQueueAndAnimation();
            IsApplyingSolve = false;

            using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try { await _executionSemaphore.WaitAsync(waitCts.Token); }
            catch (OperationCanceledException) { return; }

            try
            {
                _mutableCubeState.Reset();
                var moves = ScrambleGenerator.Generate(20);
                ScrambleText = MoveParser.SerializeMany(moves);
                foreach (var m in moves) _mutableCubeState.ApplyMove(m);
                RefreshHistory();
                _stateProvider.NotifyStateChanged();
                _renderer.UpdateStateImmediately(_cubeState);
            }
            finally
            {
                _queueCts?.Dispose();
                _queueCts = new CancellationTokenSource();
                _executionSemaphore.Release();
            }
        }


        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task SolveWithSolver(ICubeSolver solver)
        {
            if (IsApplyingSolve) return;

            CancelQueueAndAnimation();

            using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try { await _executionSemaphore.WaitAsync(waitCts.Token); }
            catch (OperationCanceledException) { return; }

            bool semaphoreReleased = false;
            try
            {
                _queueCts?.Dispose();
                _queueCts = new CancellationTokenSource();
                _solveCts = new CancellationTokenSource();
                CancellationToken localToken = _solveCts.Token;
                IsApplyingSolve = true;

                _executionSemaphore.Release();
                semaphoreReleased = true;

                var solution = await Task.Run(
                    async () => await solver.SolveAsync(_cubeState, localToken),
                    localToken);

                if (solution != null)
                {
                    foreach (var move in solution)
                    {
                        localToken.ThrowIfCancellationRequested();
                        await ExecuteMove(move);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsApplyingSolve = false;
                _solveCts?.Dispose();
                _solveCts = null;
                _queueCts?.Dispose();
                _queueCts = new CancellationTokenSource();

                if (!semaphoreReleased && _executionSemaphore.CurrentCount == 0)
                    _executionSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task ExportSolves()
        {
            string? filePath = _dialogService.ShowSaveFileDialog(
                "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                ".xlsx");

            if (filePath != null)
                await _solvesExporter.ExportAsync(Solves, filePath);
        }

        void ITimerKeyHandler.OnSpaceKeyDown()
        {
            if (_isAnimating) return;
            if (_spacePressed) return;

            _spacePressed = true;
            _holdStopwatch.Restart();
            _holdCts = new CancellationTokenSource();
            IsHoldIndicatorVisible = true;
            HoldProgress = 0;

            _ = UpdateHoldProgressAsync(_holdCts.Token);
        }

        private async Task UpdateHoldProgressAsync(CancellationToken token)
        {
            try
            {
                while (_holdStopwatch.Elapsed < HoldDuration)
                {
                    token.ThrowIfCancellationRequested();
                    double progress = _holdStopwatch.Elapsed.TotalMilliseconds / HoldDuration.TotalMilliseconds * 100.0;
                    HoldProgress = Math.Min(progress, 100.0);
                    await Task.Delay(30, token);
                }
                HoldProgress = 100;
            }
            catch (OperationCanceledException)
            {
                // do nothing — the hold is interrupted
            }
        }

        void ITimerKeyHandler.OnSpaceKeyUp()
        {
            if (!_spacePressed) return;

            _spacePressed = false;
            _holdCts?.Cancel();
            IsHoldIndicatorVisible = false;

            double holdSec = _holdStopwatch.Elapsed.TotalSeconds;

            if (_timerService.IsRunning)
            {
                _timerService.Stop();

                var solutionMoves = _cubeState.MoveHistory.Skip(_solveStartHistoryIndex).ToList();
                var scrambleMoves = _cubeState.MoveHistory.Take(_solveStartHistoryIndex).ToList();
                var solve = new SolveRecord(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    ElapsedTime,
                    MoveParser.SerializeMany(scrambleMoves),
                    MoveParser.SerializeMany(solutionMoves)
                );
                _solveStartHistoryIndex = -1;
                _ = _solvesRepository.AddAsync(solve);
                Solves.Insert(0, solve);
            }
            else
            {
                if (holdSec >= HoldDuration.TotalSeconds)
                {
                    _timerService.Reset();
                    _timerService.Start();
                    _solveStartHistoryIndex = _cubeState.MoveHistory.Count;
                }
            }

            HoldProgress = 0;
        }

        [RelayCommand]
        private void ShowAbout()
        {
            string title = _localization.GetString("Menu_About");
            string text = _localization.GetLongText("About");

            _dialogService.ShowMessage(text, title);
        }

        [RelayCommand]
        private void ShowHelp()
        {
            string title = _localization.GetString("Menu_Help");
            string text = _localization.GetLongText("Help");

            _dialogService.ShowMessage(text, title);
        }

        [RelayCommand]
        private void SetLanguage(string lang)
        {
            _localization.SetLanguage(lang);

            IsEnglishSelected = (lang == "en");
            IsRussianSelected = (lang == "ru");

            _uiSettingsService.Language = lang;
            _uiSettingsService.Save();
        }

        public record HistoryItem(string Notation, bool IsCurrent);
    }
}