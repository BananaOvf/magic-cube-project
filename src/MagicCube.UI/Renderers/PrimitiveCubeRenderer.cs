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
using MagicCube.Core.Enums;
using MagicCube.Core.Enums.Pieces;
using MagicCube.Core.Models.Moves;
using MagicCube.Core.Models.Pieces;
using MagicCube.Services;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MagicCube.UI.Renderers
{
    public sealed class PrimitiveCubeRenderer : I3DRenderer
    {
        private const double CubieSize = 0.8;
        private const double HalfCubie = CubieSize / 2.0;
        private const double Step = 0.802;

        // ---------- Camera fields ----------
        private Viewport3D? _viewport;
        private ModelVisual3D? _rootVisual;
        private ModelVisual3D? _currentLayerVisual;

        private readonly Dictionary<string, CubieVisual> _cubies = new();

        // ---------- Camera ----------
        private double _cameraYaw, _cameraPitch, _cameraDistance;
        private readonly double _initialCameraDistance = Math.Sqrt(6 * 6 + 5 * 5 + 6 * 6);
        private readonly double _initialCameraYaw = Math.Atan2(6, 6);
        private readonly double _initialCameraPitch = Math.Atan2(5, Math.Sqrt(6 * 6 + 6 * 6));
        private PerspectiveCamera? _camera;

        public event UserRotatedFaceEventHandler? UserRotatedFace;

        // ---------- Static caches ----------
        private static readonly GeometryModel3D CoreCubeModel = CreateCoreCubeFrozen();
        private static readonly Dictionary<Face, Brush> FaceBrushes = CreateFaceBrushes();
        private static readonly Dictionary<Vector3D, GeometryModel3D> StickerMeshes = CreateStickerMeshes();

        private static Dictionary<Vector3D, GeometryModel3D> CreateStickerMeshes()
        {
            var dict = new Dictionary<Vector3D, GeometryModel3D>();
            Vector3D[] normals =
            {
                new(0, 0, 1),
                new(0, 0, -1),
                new(0, 1, 0),
                new(0, -1, 0),
                new(1, 0, 0),
                new(-1, 0, 0)
            };

            foreach (var n in normals)
            {
                var mesh = new MeshGeometry3D();
                BuildFaceQuad(mesh, n);
                mesh.Freeze();
                var model = new GeometryModel3D(mesh, null);
                model.Freeze();
                dict[n] = model;
            }
            return dict;
        }

        private static void BuildFaceQuad(MeshGeometry3D mesh, Vector3D normal)
        {
            const double inset = 0.08;
            double s = HalfCubie - inset;
            double d = HalfCubie + 0.002;

            Point3D p0, p1, p2, p3;
            if (normal.X > 0) { p0 = new(d, -s, -s); p1 = new(d, s, -s); p2 = new(d, s, s); p3 = new(d, -s, s); }
            else if (normal.X < 0) { p0 = new(-d, -s, s); p1 = new(-d, s, s); p2 = new(-d, s, -s); p3 = new(-d, -s, -s); }
            else if (normal.Y > 0) { p0 = new(-s, d, -s); p1 = new(-s, d, s); p2 = new(s, d, s); p3 = new(s, d, -s); }
            else if (normal.Y < 0) { p0 = new(-s, -d, s); p1 = new(-s, -d, -s); p2 = new(s, -d, -s); p3 = new(s, -d, s); }
            else if (normal.Z > 0) { p0 = new(-s, -s, d); p1 = new(s, -s, d); p2 = new(s, s, d); p3 = new(-s, s, d); }
            else { p0 = new(s, -s, -d); p1 = new(-s, -s, -d); p2 = new(-s, s, -d); p3 = new(s, s, -d); }

            int i = mesh.Positions.Count;
            mesh.Positions.Add(p0); mesh.Positions.Add(p1);
            mesh.Positions.Add(p2); mesh.Positions.Add(p3);
            mesh.Normals.Add(normal); mesh.Normals.Add(normal);
            mesh.Normals.Add(normal); mesh.Normals.Add(normal);
            mesh.TriangleIndices.Add(i); mesh.TriangleIndices.Add(i + 1); mesh.TriangleIndices.Add(i + 2);
            mesh.TriangleIndices.Add(i); mesh.TriangleIndices.Add(i + 2); mesh.TriangleIndices.Add(i + 3);
        }

        private static readonly string[] AllSlotKeys = Enum.GetNames<CenterId>()
            .Concat(Enum.GetNames<EdgeId>())
            .Concat(Enum.GetNames<CornerId>())
            .ToArray();

        private static readonly Dictionary<string, Vector3D> SlotPositions = new()
        {
            {"U",  new(0,Step,0)}, {"D",  new(0,-Step,0)},
            {"L",  new(-Step,0,0)}, {"R", new(Step,0,0)},
            {"F",  new(0,0,Step)}, {"B", new(0,0,-Step)},
            {"UR", new(Step,Step,0)}, {"UF", new(0,Step,Step)},
            {"UL", new(-Step,Step,0)}, {"UB", new(0,Step,-Step)},
            {"DR", new(Step,-Step,0)}, {"DF", new(0,-Step,Step)},
            {"DL", new(-Step,-Step,0)}, {"DB", new(0,-Step,-Step)},
            {"FR", new(Step,0,Step)}, {"FL", new(-Step,0,Step)},
            {"BL", new(-Step,0,-Step)}, {"BR", new(Step,0,-Step)},
            {"URF", new(Step,Step,Step)}, {"UFL", new(-Step,Step,Step)},
            {"ULB", new(-Step,Step,-Step)}, {"UBR", new(Step,Step,-Step)},
            {"DFR", new(Step,-Step,Step)}, {"DLF", new(-Step,-Step,Step)},
            {"DBL", new(-Step,-Step,-Step)}, {"DRB", new(Step,-Step,-Step)}
        };

        private static readonly Dictionary<string, TranslateTransform3D> SlotTransforms = CreateSlotTransforms();
        // ---------- Initialization ----------
        public void Initialize(object container)
        {
            _viewport = container as Viewport3D ?? throw new ArgumentException("Container must be Viewport3D");
            _viewport.Children.Clear();
            ConfigureCamera();
            ConfigureLights();
            CreateAllCubies();
        }

        private void ConfigureCamera()
        {
            _camera = new PerspectiveCamera
            {
                FieldOfView = 50,
                UpDirection = new Vector3D(0, 1, 0)
            };
            _cameraDistance = _initialCameraDistance;
            _cameraYaw = _initialCameraYaw;
            _cameraPitch = _initialCameraPitch;
            UpdateCameraPosition();
            _viewport!.Camera = _camera;
        }

        private void UpdateCameraPosition()
        {
            if (_camera == null) return;
            double pitch = Math.Clamp(_cameraPitch, -Math.PI / 2 + 0.01, Math.PI / 2 - 0.01);
            double x = _cameraDistance * Math.Cos(pitch) * Math.Sin(_cameraYaw);
            double y = _cameraDistance * Math.Sin(pitch);
            double z = _cameraDistance * Math.Cos(pitch) * Math.Cos(_cameraYaw);
            _camera.Position = new Point3D(x, y, z);
            _camera.LookDirection = new Vector3D(-x, -y, -z);
        }

        private void ConfigureLights()
        {
            var lights = new Model3DGroup();
            //lights.Children.Add(new AmbientLight(Color.FromRgb(90, 90, 90)));
            lights.Children.Add(new AmbientLight(Color.FromRgb(240, 240, 240)));
            //lights.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -2)));
            //lights.Children.Add(new DirectionalLight(Colors.White, new Vector3D(1, -0.5, -1)));
            _rootVisual = new ModelVisual3D { Content = lights };
            _viewport!.Children.Add(_rootVisual);
        }

        private void CreateAllCubies()
        {
            foreach (var key in AllSlotKeys)
            {
                var visual = new ModelVisual3D
                {
                    Content = new Model3DGroup(),
                    Transform = SlotTransforms[key]
                };
                _rootVisual!.Children.Add(visual);
                _cubies[key] = new CubieVisual(visual, SlotPositions[key]);
            }
        }

        public void UpdateStateImmediately(IReadOnlyCubeState state)
        {
            foreach (var slotKey in AllSlotKeys)
            {
                var cubie = _cubies[slotKey];
                cubie.Visual.Transform = SlotTransforms[slotKey];
                cubie.Visual.Content = BuildCubieContent(slotKey, state);
            }
        }

        // ---------- Static frozen resources ----------
        private static GeometryModel3D CreateCoreCubeFrozen()
        {
            var mesh = new MeshGeometry3D();
            AddCube(mesh, CubieSize);
            mesh.Freeze();
            var mat = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(25, 25, 25)));
            mat.Freeze();
            var model = new GeometryModel3D(mesh, mat);
            model.Freeze();
            return model;
        }

        private static Dictionary<Face, Brush> CreateFaceBrushes()
        {
            var dict = new Dictionary<Face, Brush>();
            foreach (Face face in Enum.GetValues<Face>())
            {
                Color color = face switch
                {
                    Face.U => Colors.White,
                    Face.D => Colors.Yellow,
                    Face.F => Colors.Green,
                    Face.B => Colors.Blue,
                    Face.R => Colors.Red,
                    Face.L => Colors.Orange,
                    _ => Colors.Gray
                };
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                dict[face] = brush;
            }
            return dict;
        }

        private static GeometryModel3D CreateStickerMeshFrozen()
        {
            var mesh = new MeshGeometry3D();
            const double inset = 0.08;
            double s = HalfCubie - inset;
            double d = HalfCubie + 0.002;
            Point3D p0 = new(-s, -s, d);
            Point3D p1 = new(s, -s, d);
            Point3D p2 = new(s, s, d);
            Point3D p3 = new(-s, s, d);
            mesh.Positions.Add(p0); mesh.Positions.Add(p1);
            mesh.Positions.Add(p2); mesh.Positions.Add(p3);
            mesh.Normals.Add(new Vector3D(0, 0, 1)); mesh.Normals.Add(new Vector3D(0, 0, 1));
            mesh.Normals.Add(new Vector3D(0, 0, 1)); mesh.Normals.Add(new Vector3D(0, 0, 1));
            int i0 = 0;
            mesh.TriangleIndices.Add(i0); mesh.TriangleIndices.Add(i0 + 1); mesh.TriangleIndices.Add(i0 + 2);
            mesh.TriangleIndices.Add(i0); mesh.TriangleIndices.Add(i0 + 2); mesh.TriangleIndices.Add(i0 + 3);
            mesh.Freeze();
            var model = new GeometryModel3D(mesh, null);
            model.Freeze();
            return model;
        }

        private static Dictionary<string, TranslateTransform3D> CreateSlotTransforms()
        {
            var dict = new Dictionary<string, TranslateTransform3D>();
            foreach (var kvp in SlotPositions)
            {
                var t = new TranslateTransform3D(kvp.Value.X, kvp.Value.Y, kvp.Value.Z);
                t.Freeze();
                dict[kvp.Key] = t;
            }
            return dict;
        }

        // ---------- Constructing the contents of a cube using caches ----------
        private Model3DGroup BuildCubieContent(string slotKey, IReadOnlyCubeState state)
        {
            var group = new Model3DGroup();
            group.Children.Add(CoreCubeModel);

            if (IsCenterSlot(slotKey))
            {
                var piece = state.GetCenterPiece(Enum.Parse<CenterId>(slotKey));
                group.Children.Add(MakeSticker(CenterToFace(piece.Id), CenterSlotNormals[slotKey]));
            }
            else if (IsEdgeSlot(slotKey))
            {
                var piece = state.GetEdgePiece(Enum.Parse<EdgeId>(slotKey));
                var faces = GetEdgeStickerOrder(piece);
                var slotNormals = EdgeSlotNormals[slotKey];
                for (int i = 0; i < 2; i++)
                    group.Children.Add(MakeSticker(faces[i], slotNormals[i]));
            }
            else if (IsCornerSlot(slotKey))
            {
                var piece = state.GetCornerPiece(Enum.Parse<CornerId>(slotKey));
                var faces = GetCornerStickerOrder(piece);
                var slotNormals = CornerSlotNormals[slotKey];
                for (int i = 0; i < 3; i++)
                    group.Children.Add(MakeSticker(faces[i], slotNormals[i]));
            }
            return group;
        }

        private GeometryModel3D MakeSticker(Face face, Vector3D normal)
        {
            var model = StickerMeshes[normal].Clone();
            model.Material = new DiffuseMaterial(FaceBrushes[face]);
            return model;
        }

        // ---------- Animation (group) ----------
        public async Task AnimateMoveAsync(
            Move move, IReadOnlyCubeState stateBeforeMove,
            Action<IMutableCubeState> onCompleted, TimeSpan? duration = null,
            CancellationToken token = default)
        {
            duration ??= TimeSpan.FromMilliseconds(250);
            if (stateBeforeMove is not IMutableCubeState mutable)
                throw new InvalidOperationException("Expected mutable state");

            var affectedKeys = GetSlotsAffectedByMove(move);
            if (affectedKeys.Count == 0)
            {
                onCompleted(mutable);
                return;
            }

            var axis = GetRotationAxis(move);
            var angle = GetRotationAngle(move);

            var layerVisual = new ModelVisual3D();
            var rotationTransform = new RotateTransform3D();
            layerVisual.Transform = rotationTransform;

            foreach (var key in affectedKeys)
            {
                var vis = _cubies[key].Visual;
                _rootVisual!.Children.Remove(vis);
                layerVisual.Children.Add(vis);
            }
            _rootVisual.Children.Add(layerVisual);
            _currentLayerVisual = layerVisual;

            var animationTimer = Stopwatch.StartNew();
            var tcs = new TaskCompletionSource();

            EventHandler? renderHandler = null;
            renderHandler = (_, _) =>
            {
                if (token.IsCancellationRequested)
                {
                    CompositionTarget.Rendering -= renderHandler;
                    CleanupGroup();
                    tcs.TrySetCanceled();
                    return;
                }

                var elapsed = animationTimer.Elapsed;
                double progress = Math.Min(elapsed.TotalMilliseconds / duration!.Value.TotalMilliseconds, 1.0);
                if (progress >= 1.0)
                {
                    CompositionTarget.Rendering -= renderHandler;
                    rotationTransform.Rotation = new AxisAngleRotation3D(axis, angle);
                    FinalizeAnimation();
                    onCompleted(mutable);
                    UpdateStateImmediately(mutable);
                    tcs.TrySetResult();
                    return;
                }

                double easedProgress = Ease(progress);
                double currentAngle = angle * easedProgress;
                rotationTransform.Rotation = new AxisAngleRotation3D(axis, currentAngle);
            };

            CompositionTarget.Rendering += renderHandler;
            await tcs.Task;
        }

        private static double Ease(double t) => t * t * (3.0 - 2.0 * t);

        private void CleanupGroup()
        {
            if (_currentLayerVisual == null || _rootVisual == null) return;
            for (int i = _currentLayerVisual.Children.Count - 1; i >= 0; i--)
            {
                var vis = (ModelVisual3D)_currentLayerVisual.Children[i];
                _currentLayerVisual.Children.RemoveAt(i);
                _rootVisual.Children.Add(vis);
            }
            _rootVisual.Children.Remove(_currentLayerVisual);
            _currentLayerVisual = null;
        }

        private void FinalizeAnimation() => CleanupGroup();

        // ---------- Helper methods ----------
        private static List<string> GetSlotsAffectedByMove(Move move) => move switch
        {
            FaceMove fm => GetFaceLayerKeys(fm.Face),
            SliceMove sm => GetSliceLayerKeys(sm.Slice),
            WideMove wm => GetFaceLayerKeys(wm.Face)
                .Concat(GetSliceLayerKeys(OppositeSlice(wm.Face)))
                .Distinct()
                .ToList(),
            RotationMove => AllSlotKeys.ToList(),
            _ => new List<string>()
        };

        private static Slice OppositeSlice(Face face) => face switch
        {
            Face.U => Slice.E,
            Face.D => Slice.E,
            Face.L => Slice.M,
            Face.R => Slice.M,
            Face.F => Slice.S,
            Face.B => Slice.S,
            _ => throw new ArgumentException()
        };

        private static List<string> GetFaceLayerKeys(Face face)
        {
            Func<Vector3D, bool> filter = face switch
            {
                Face.U => v => v.Y > 0,
                Face.D => v => v.Y < 0,
                Face.R => v => v.X > 0,
                Face.L => v => v.X < 0,
                Face.F => v => v.Z > 0,
                Face.B => v => v.Z < 0,
                _ => _ => true
            };
            return AllSlotKeys.Where(k => SlotPositions.TryGetValue(k, out var p) && filter(p)).ToList();
        }

        private static List<string> GetSliceLayerKeys(Slice slice)
        {
            Func<Vector3D, bool> filter = slice switch
            {
                Slice.M => v => Math.Abs(v.X) < 0.1,
                Slice.E => v => Math.Abs(v.Y) < 0.1,
                Slice.S => v => Math.Abs(v.Z) < 0.1,
                _ => _ => false
            };
            return AllSlotKeys.Where(k => SlotPositions.TryGetValue(k, out var p) && filter(p)).ToList();
        }

        private static Vector3D GetRotationAxis(Move move) => move switch
        {
            FaceMove fm => fm.Face switch
            {
                Face.U => new Vector3D(0, 1, 0),
                Face.D => new Vector3D(0, -1, 0),
                Face.R => new Vector3D(1, 0, 0),
                Face.L => new Vector3D(-1, 0, 0),
                Face.F => new Vector3D(0, 0, 1),
                Face.B => new Vector3D(0, 0, -1),
                _ => default
            },
            SliceMove sm => sm.Slice switch
            {
                Slice.M => new Vector3D(-1, 0, 0),
                Slice.E => new Vector3D(0, -1, 0),
                Slice.S => new Vector3D(0, 0, 1),
                _ => default
            },
            WideMove wm => GetRotationAxis(new FaceMove(wm.Face, wm.Direction)),
            RotationMove rm => rm.Axis switch
            {
                Axis.X => new Vector3D(1, 0, 0),
                Axis.Y => new Vector3D(0, 1, 0),
                Axis.Z => new Vector3D(0, 0, 1),
                _ => default
            },
            _ => default
        };

        private static double GetRotationAngle(Move move) => move.Direction switch
        {
            Direction.Clockwise => -90,
            Direction.CounterClockwise => 90,
            Direction.Double => -180,
            _ => -90
        };

        private static Face[] GetEdgeFaces(EdgeId id) => id switch
        {
            EdgeId.UR => new[] { Face.U, Face.R },
            EdgeId.UF => new[] { Face.U, Face.F },
            EdgeId.UL => new[] { Face.U, Face.L },
            EdgeId.UB => new[] { Face.U, Face.B },
            EdgeId.DR => new[] { Face.D, Face.R },
            EdgeId.DF => new[] { Face.D, Face.F },
            EdgeId.DL => new[] { Face.D, Face.L },
            EdgeId.DB => new[] { Face.D, Face.B },
            EdgeId.FR => new[] { Face.F, Face.R },
            EdgeId.FL => new[] { Face.F, Face.L },
            EdgeId.BL => new[] { Face.B, Face.L },
            EdgeId.BR => new[] { Face.B, Face.R },
            _ => Array.Empty<Face>()
        };

        private static Face[] GetCornerFaces(CornerId id) => id switch
        {
            CornerId.URF => new[] { Face.U, Face.R, Face.F },
            CornerId.UFL => new[] { Face.U, Face.F, Face.L },
            CornerId.ULB => new[] { Face.U, Face.L, Face.B },
            CornerId.UBR => new[] { Face.U, Face.B, Face.R },
            CornerId.DFR => new[] { Face.D, Face.F, Face.R },
            CornerId.DLF => new[] { Face.D, Face.L, Face.F },
            CornerId.DBL => new[] { Face.D, Face.B, Face.L },
            CornerId.DRB => new[] { Face.D, Face.R, Face.B },
            _ => Array.Empty<Face>()
        };

        private static Vector3D FaceNormal(Face face) => face switch
        {
            Face.U => new(0, 1, 0),
            Face.D => new(0, -1, 0),
            Face.F => new(0, 0, 1),
            Face.B => new(0, 0, -1),
            Face.R => new(1, 0, 0),
            Face.L => new(-1, 0, 0),
            _ => default
        };

        private static Face CenterToFace(CenterId id) => id switch
        {
            CenterId.U => Face.U,
            CenterId.D => Face.D,
            CenterId.L => Face.L,
            CenterId.R => Face.R,
            CenterId.F => Face.F,
            CenterId.B => Face.B,
            _ => Face.F
        };

        private static bool IsCenterSlot(string key) => Enum.TryParse<CenterId>(key, out _);
        private static bool IsEdgeSlot(string key) => Enum.TryParse<EdgeId>(key, out _);
        private static bool IsCornerSlot(string key) => Enum.TryParse<CornerId>(key, out _);

        private static Face[] GetEdgeStickerOrder(EdgePiece piece)
        {
            string id = piece.Id.ToString();
            Face f0 = Enum.Parse<Face>(id[0].ToString());
            Face f1 = Enum.Parse<Face>(id[1].ToString());
            return piece.Orientation == EdgeOrientation.Default ? new[] { f0, f1 } : new[] { f1, f0 };
        }

        private static Face[] GetCornerStickerOrder(CornerPiece piece)
        {
            Face[] baseFaces = GetCornerFaces(piece.Id);
            return piece.Orientation switch
            {
                CornerOrientation.Default => baseFaces,
                CornerOrientation.Clockwise => new[] { baseFaces[2], baseFaces[0], baseFaces[1] },
                CornerOrientation.CounterClockwise => new[] { baseFaces[1], baseFaces[2], baseFaces[0] },
                _ => baseFaces
            };
        }

        private static readonly Dictionary<string, Vector3D> CenterSlotNormals = new()
        {
            {"U", new(0,1,0)}, {"D", new(0,-1,0)}, {"L", new(-1,0,0)},
            {"R", new(1,0,0)}, {"F", new(0,0,1)}, {"B", new(0,0,-1)}
        };

        private static readonly Dictionary<string, List<Vector3D>> EdgeSlotNormals = new()
        {
            {"UR", new(){new(0,1,0), new(1,0,0)}},
            {"UF", new(){new(0,1,0), new(0,0,1)}},
            {"UL", new(){new(0,1,0), new(-1,0,0)}},
            {"UB", new(){new(0,1,0), new(0,0,-1)}},
            {"DR", new(){new(0,-1,0), new(1,0,0)}},
            {"DF", new(){new(0,-1,0), new(0,0,1)}},
            {"DL", new(){new(0,-1,0), new(-1,0,0)}},
            {"DB", new(){new(0,-1,0), new(0,0,-1)}},
            {"FR", new(){new(0,0,1), new(1,0,0)}},
            {"FL", new(){new(0,0,1), new(-1,0,0)}},
            {"BL", new(){new(0,0,-1), new(-1,0,0)}},
            {"BR", new(){new(0,0,-1), new(1,0,0)}}
        };

        private static readonly Dictionary<string, List<Vector3D>> CornerSlotNormals = new()
        {
            {"URF", new(){new(0,1,0), new(1,0,0), new(0,0,1)}},
            {"UFL", new(){new(0,1,0), new(0,0,1), new(-1,0,0)}},
            {"ULB", new(){new(0,1,0), new(-1,0,0), new(0,0,-1)}},
            {"UBR", new(){new(0,1,0), new(0,0,-1), new(1,0,0)}},
            {"DFR", new(){new(0,-1,0), new(0,0,1), new(1,0,0)}},
            {"DLF", new(){new(0,-1,0), new(-1,0,0), new(0,0,1)}},
            {"DBL", new(){new(0,-1,0), new(0,0,-1), new(-1,0,0)}},
            {"DRB", new(){new(0,-1,0), new(1,0,0), new(0,0,-1)}}
        };

        private static void AddCube(MeshGeometry3D mesh, double size)
        {
            double h = size / 2.0;
            Point3D[] pts = {
                new(-h,-h,-h), new(h,-h,-h), new(h,h,-h), new(-h,h,-h),
                new(-h,-h,h),  new(h,-h,h),  new(h,h,h),  new(-h,h,h)
            };
            int[] idx = {
                0,1,2, 0,2,3,  1,5,6, 1,6,2,  5,4,7, 5,7,6,
                4,0,3, 4,3,7,  3,2,6, 3,6,7,  4,5,1, 4,1,0
            };
            foreach (var p in pts) mesh.Positions.Add(p);
            foreach (var i in idx) mesh.TriangleIndices.Add(i);
        }

        // ---------- Camera control ----------
        public void RotateCamera(double deltaYaw, double deltaPitch)
        {
            _cameraYaw += deltaYaw;
            _cameraPitch += deltaPitch;
            UpdateCameraPosition();
        }

        public void ZoomCamera(double delta)
        {
            _cameraDistance = Math.Clamp(_cameraDistance - delta, 3.0, 20.0);
            UpdateCameraPosition();
        }

        public void ResetCamera()
        {
            _cameraYaw = _initialCameraYaw;
            _cameraPitch = _initialCameraPitch;
            _cameraDistance = _initialCameraDistance;
            UpdateCameraPosition();
        }

        private sealed class CubieVisual
        {
            public ModelVisual3D Visual { get; }
            public Vector3D Position { get; }
            public CubieVisual(ModelVisual3D visual, Vector3D position)
            {
                Visual = visual;
                Position = position;
            }
        }
    }
}