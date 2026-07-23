using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Assembly;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private ConstructionAssembly? _constructionAssembly;

    private readonly Dictionary<Guid, SubFrameGrid> _subFrameGrids = new();

    /// <summary>Корневой агрегат конструкции (source of truth для подрам).</summary>
    public ConstructionAssembly? ConstructionAssembly
    {
        get => _constructionAssembly;
        private set => SetProperty(ref _constructionAssembly, value);
    }

    public IReadOnlyDictionary<Guid, SubFrameGrid> SubFrameGrids => _subFrameGrids;

    public SubFrame? FindOwnerSubFrameAt(double worldX, double worldY) =>
        ConstructionAssembly?.FindOwnerAt(worldX, worldY);

    public SubFrame? FindSubFrameById(Guid? subFrameId)
    {
        if (subFrameId is null || ConstructionAssembly is null)
        {
            return null;
        }

        return ConstructionAssembly.SubFrames.FirstOrDefault(s => s.Id == subFrameId);
    }

    public GridLineCollection GridLinesForOwner(SubFrame owner)
    {
        return _subFrameGrids.TryGetValue(owner.Id, out var grid)
            ? grid.Lines
            : GridLineCollection.Empty;
    }

    private void InitializeNewConstructionAssembly(FrameNode newRoot)
    {
        FrameRoot = newRoot;
        ConstructionAssembly = null;
        _subFrameGrids.Clear();
        EnsureConstructionAssemblyFromFrameRoot();
    }

    private void AdoptConstructionAssembly(ConstructionAssembly assembly)
    {
        if (!assembly.ValidateOwnership())
        {
            throw new InvalidDataException(
                "ConstructionAssembly contains nodes without valid SubFrame ownership.");
        }

        ConstructionAssembly = assembly;
        FrameRoot = CreateFrameRootView(assembly);
        _subFrameGrids.Clear();
        foreach (var subFrame in assembly.SubFrames)
        {
            _subFrameGrids[subFrame.Id] = new SubFrameGrid(subFrame);
        }

        AssignSubFrameOwnershipToElements();
        RebuildAllOwnerGridLines();
        OnPropertyChanged(nameof(ConstructionAssembly));
        OnPropertyChanged(nameof(SubFrameGrids));
    }

    private static FrameNode CreateFrameRootView(ConstructionAssembly assembly)
    {
        var primary = assembly.SubFrames.OrderBy(owner => owner.OrderIndex).First();

        // Одна подрама без переходов: FrameRoot = SubFrame.Root (тот же объект),
        // чтобы TrySplit* на холсте попадал в дерево, сохраняемое в memento.
        if (assembly.SubFrames.Count == 1 && assembly.TransitionProfiles.Count == 0)
        {
            return primary.Root;
        }

        var global = assembly.GlobalFrame;
        var metadataRoot = primary.Root;
        var view = FrameNode.CreateRoot(
            global.X,
            global.Y,
            global.Width,
            global.Height,
            metadataRoot.IsFictitiousContainer);
        view.Shape = metadataRoot.Shape;
        view.TopProfile = metadataRoot.TopProfile;
        view.AttachedSill = metadataRoot.AttachedSill;
        return view;
    }

    public bool TryResolveOwnerContext(
        CadPoint worldPoint,
        out SubFrame owner,
        out CadPoint localPoint)
    {
        owner = FindOwnerSubFrameAt(worldPoint.X, worldPoint.Y)!;
        if (owner is null)
        {
            localPoint = default;
            return false;
        }

        localPoint = new CadPoint(
            owner.WorldToLocalX(worldPoint.X),
            owner.WorldToLocalY(worldPoint.Y));
        return true;
    }

    public bool TryFindOwnerByNodeId(
        Guid nodeId,
        out SubFrame owner,
        out FrameNode node)
    {
        if (ConstructionAssembly is { } assembly)
        {
            var ownerId = assembly.SubFrames
                .SelectMany(candidate => candidate.Root.EnumerateAllNodes())
                .FirstOrDefault(candidate => candidate.NodeId == nodeId)
                ?.OwnerSubFrameId;
            if (ownerId is Guid id
                && assembly.SubFrames.FirstOrDefault(candidate => candidate.Id == id) is { } candidate)
            {
                var found = candidate.Root.FindNodeById(nodeId);
                if (found is not null)
                {
                    owner = candidate;
                    node = found;
                    return true;
                }
            }
        }

        owner = null!;
        node = null!;
        return false;
    }

    private void AssignSubFrameOwnershipToElements()
    {
        if (ConstructionAssembly is not { } assembly)
        {
            return;
        }

        foreach (var element in CanvasElements)
        {
            if (element.Kind == GeometryElementKind.TransitionFrame)
            {
                element.SubFrameId = null;
                element.HostFrameNodeId = null;
                element.HostGridLineId = null;
                continue;
            }

            if (IsAssemblyFrameView(element, assembly))
            {
                element.SubFrameId = null;
                continue;
            }

            if (element.HostFrameNodeId is Guid nodeId
                && TryFindOwnerByNodeId(nodeId, out var nodeOwner, out _))
            {
                element.SubFrameId = nodeOwner.Id;
                continue;
            }

            if (element.HostSashId is Guid sashId
                && CanvasElements.FirstOrDefault(candidate => candidate.Id == sashId)
                    is { SubFrameId: Guid sashOwnerId })
            {
                element.SubFrameId = sashOwnerId;
                continue;
            }

            element.SubFrameId = assembly.FindOwnerAt(
                (double)element.X,
                (double)element.Y)?.Id;
        }
    }

    public FrameNode? FindLeafAtForOwner(double worldX, double worldY)
    {
        var owner = FindOwnerSubFrameAt(worldX, worldY);
        if (owner is not null)
        {
            return SubFrameCoordinateSystem.FindLeafAtWorld(owner, worldX, worldY);
        }

        return null;
    }

    public void RebuildOwnerGridLines(SubFrame owner)
    {
        if (_subFrameGrids.TryGetValue(owner.Id, out var grid))
        {
            grid.Rebuild();
        }
    }

    public void RebuildAllOwnerGridLines()
    {
        foreach (var grid in _subFrameGrids.Values)
        {
            grid.Rebuild();
        }
    }

    /// <summary>
    /// Абсолютные оси сетки по всем подрамам (совместимость HitTest / размеров).
    /// При отсутствии assembly — проекция из FrameRoot.
    /// </summary>
    public GridLineCollection GridLines
    {
        get
        {
            if (_subFrameGrids.Count > 0)
            {
                var lines = new List<GridLine>();
                foreach (var grid in _subFrameGrids.Values)
                {
                    lines.AddRange(grid.Lines.All);
                }

                return new GridLineCollection(lines);
            }

            return _legacyGridLines;
        }
    }

    private GridLineCollection _legacyGridLines = GridLineCollection.Empty;

    /// <summary>
    /// Пересобирает GridLines из подрам или FrameRoot (точка входа V13-совместимости).
    /// </summary>
    public void RebuildGridLines()
    {
        if (_subFrameGrids.Count > 0)
        {
            RebuildAllOwnerGridLines();
        }
        else
        {
            _legacyGridLines = FrameNodeGridLineProjector.Project(FrameRoot);
        }

        OnPropertyChanged(nameof(GridLines));
    }

    /// <summary>
    /// Гарантирует, что живое дерево <see cref="FrameRoot"/> отражено в
    /// <see cref="ConstructionAssembly"/> (один SubFrame, тот же экземпляр Root).
    /// Нужно для RecordPlacement / undo: иначе CaptureConstructionMemento падает
    /// или сохраняет пустое дерево.
    /// </summary>
    private void EnsureConstructionAssemblyFromFrameRoot()
    {
        if (FrameRoot is null)
        {
            return;
        }

        if (ConstructionAssembly is { SubFrames.Count: 1 } existing
            && ReferenceEquals(existing.SubFrames[0].Root, FrameRoot))
        {
            existing.GlobalFrame.X = FrameRoot.X;
            existing.GlobalFrame.Y = FrameRoot.Y;
            existing.GlobalFrame.Width = FrameRoot.Width;
            existing.GlobalFrame.Height = FrameRoot.Height;
            var sub = existing.SubFrames[0];
            // FrameRoot в мировых координатах: Origin=0, иначе SubFrameGrid дважды сдвинет оси.
            sub.OriginWorldX = 0;
            sub.OriginWorldY = 0;
            sub.OuterWidth = FrameRoot.X + FrameRoot.Width;
            sub.OuterHeight = FrameRoot.Y + FrameRoot.Height;
            RebuildOwnerGridLines(sub);
            return;
        }

        if (ConstructionAssembly is not null)
        {
            // Уже есть multi-subframe / загруженная сборка — не перетираем.
            return;
        }

        var root = FrameRoot;
        var subFrameId = root.OwnerSubFrameId ?? Guid.NewGuid();
        root.AssignOwnerSubFrame(subFrameId);

        // Root остаётся в мировых координатах редактора; Origin совпадает с Root.X/Y,
        // а SubFrameGrid.OffsetToWorld даёт нулевой сдвиг только если Origin=0.
        // Поэтому Origin = 0, а габарит берём из GlobalFrame / Root.
        var assembly = new ConstructionAssembly
        {
            GlobalFrame = GlobalFrame.FromBounds(root.X, root.Y, root.Width, root.Height),
            TransitionProfiles = [],
            SubFrames =
            [
                new SubFrame
                {
                    Id = subFrameId,
                    OriginWorldX = 0,
                    OriginWorldY = 0,
                    OuterWidth = root.X + root.Width,
                    OuterHeight = root.Y + root.Height,
                    OrderIndex = 0,
                    Root = root
                }
            ]
        };

        ConstructionAssembly = assembly;
        _subFrameGrids.Clear();
        _subFrameGrids[subFrameId] = new SubFrameGrid(assembly.SubFrames[0]);
        OnPropertyChanged(nameof(ConstructionAssembly));
        OnPropertyChanged(nameof(SubFrameGrids));
        OnPropertyChanged(nameof(GridLines));
    }

    public void ValidateConstructionOwnershipOrThrow()
    {
        // Редактор historically держит геометрию в FrameRoot; assembly может
        // появиться позже. Перед снимком истории поднимаем assembly из FrameRoot.
        if (ConstructionAssembly is null && FrameRoot is not null)
        {
            EnsureConstructionAssemblyFromFrameRoot();
        }

        if (ConstructionAssembly is not { } assembly)
        {
            if (CanvasElements.Count == 0 || FrameRoot is not null)
            {
                return;
            }

            throw new InvalidDataException(
                "Construction elements exist without ConstructionAssembly.");
        }

        if (!assembly.ValidateOwnership())
        {
            throw new InvalidDataException("ConstructionAssembly ownership invariant failed.");
        }

        foreach (var element in CanvasElements)
        {
            if (element.Kind == GeometryElementKind.TransitionFrame)
            {
                if (!assembly.TransitionProfiles.Any(profile => profile.Id == element.Id)
                    || element.SubFrameId is not null
                    || element.HostFrameNodeId is not null
                    || element.HostGridLineId is not null)
                {
                    throw new InvalidDataException(
                        $"TransitionProfile {element.Id} has impost/SubFrame ownership.");
                }

                continue;
            }

            if (IsAssemblyFrameView(element, assembly))
            {
                continue;
            }

            if (element.SubFrameId is not Guid ownerId
                || FindSubFrameById(ownerId) is not { } owner)
            {
                throw new InvalidDataException(
                    $"{element.Kind} {element.Id} has no valid SubFrame owner.");
            }

            if (element.HostFrameNodeId is Guid nodeId
                && owner.Root.FindNodeById(nodeId) is null)
            {
                throw new InvalidDataException(
                    $"{element.Kind} {element.Id} references a node outside its owner.");
            }

            if (element.HostGridLineId is Guid lineId
                && !_subFrameGrids[owner.Id].Lines.All.Any(line => line.Id.Value == lineId))
            {
                throw new InvalidDataException(
                    $"{element.Kind} {element.Id} references a grid line outside its owner.");
            }
        }
    }

    private static bool IsAssemblyFrameView(
        GeometryElement element,
        ConstructionAssembly assembly)
    {
        if (element.Kind is not (
                GeometryElementKind.FictitiousFrame
                or GeometryElementKind.DummyFrame
                or GeometryElementKind.RoundDummyFrame
                or GeometryElementKind.ArchDummyFrame
                or GeometryElementKind.TriangleDummyFrame))
        {
            return false;
        }

        return Math.Abs((double)element.WidthMm - assembly.GlobalFrame.Width)
                   <= CadMath.MmPrecision
               && Math.Abs((double)element.HeightMm - assembly.GlobalFrame.Height)
                   <= CadMath.MmPrecision;
    }
}
