// Copyright (C) 2026 Magic Cube Project
// SPDX-License-Identifier: GPL-3.0-or-later

using MagicCube.Core.Abstractions;
using MagicCube.Core.Models.Moves;

namespace MagicCube.Services
{
    public delegate void UserRotatedFaceEventHandler(Move move);

    public interface I3DRenderer
    {
        event UserRotatedFaceEventHandler? UserRotatedFace;

        void Initialize(object container);
        void UpdateStateImmediately(IReadOnlyCubeState state);
        Task AnimateMoveAsync(Move move, IReadOnlyCubeState stateBeforeMove, Action<IMutableCubeState> onCompleted,
                              TimeSpan? duration = null, CancellationToken token = default);

        void RotateCamera(double deltaYaw, double deltaPitch);
        void ZoomCamera(double delta);
        void ResetCamera();
    }
}
