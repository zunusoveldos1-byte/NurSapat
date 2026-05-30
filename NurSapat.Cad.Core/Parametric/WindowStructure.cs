using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Parametric;

public sealed class WindowStructure
{
    public WindowOptions Options { get; } = new();

    public WindowFrame OuterFrame { get; private set; }

    public WindowSection RootSection { get; private set; }

    public CadSelectionKind SelectedKind { get; private set; } = CadSelectionKind.Frame;

    public Guid? SelectedSectionId { get; private set; }

    public Guid? SelectedImpostOwnerId { get; private set; }

    public WindowSection? SelectedSection => LeafSections.FirstOrDefault(s => s.Id == SelectedSectionId);

    public WindowSection? SelectedImpostOwner => Sections.FirstOrDefault(s => s.Id == SelectedImpostOwnerId);

    public IEnumerable<WindowSection> Sections => RootSection.Flatten();

    public IEnumerable<WindowSection> LeafSections => Sections.Where(s => s.IsLeaf);

    public IEnumerable<CadLine> SnapLines
    {
        get
        {
            yield return new CadLine(new CadPoint(0, 0), new CadPoint(Options.Width, 0));
            yield return new CadLine(new CadPoint(Options.Width, 0), new CadPoint(Options.Width, Options.Height));
            yield return new CadLine(new CadPoint(Options.Width, Options.Height), new CadPoint(0, Options.Height));
            yield return new CadLine(new CadPoint(0, Options.Height), new CadPoint(0, 0));

            foreach (var section in Sections)
            {
                if (section.SplitImpost != null)
                {
                    yield return section.SplitImpost.Axis;
                }
            }
        }
    }

    public WindowStructure()
    {
        OuterFrame = new WindowFrame(Options.Width, Options.Height, Options.FrameProfileWidth, Options.ProfileKind);
        RootSection = new WindowSection(OuterFrame.InnerBounds);
    }

    public void InitializeFrame(double width, double height, bool hiddenProfile = false)
    {
        Options.Width = width;
        Options.Height = height;
        Options.ProfileKind = hiddenProfile ? ProfileKind.Hidden : ProfileKind.Pvc;
        OuterFrame = new WindowFrame(width, height, Options.FrameProfileWidth, Options.ProfileKind);
        RootSection = new WindowSection(OuterFrame.InnerBounds);
        SelectFrame();
    }

    public WindowSection? HitTestLeaf(CadPoint point) => RootSection.HitTest(point);

    public void SelectFrame()
    {
        SelectedKind = CadSelectionKind.Frame;
        SelectedSectionId = null;
        SelectedImpostOwnerId = null;
    }

    public void SelectSection(Guid? sectionId)
    {
        SelectedSectionId = sectionId;
        SelectedImpostOwnerId = null;
        var section = SelectedSection;
        SelectedKind = section?.Fill.Kind switch
        {
            SectionFillKind.Sash => CadSelectionKind.Sash,
            SectionFillKind.Glazing or SectionFillKind.Corrugation => CadSelectionKind.Glazing,
            _ => CadSelectionKind.Section
        };
    }

    public void SelectImpost(Guid ownerSectionId)
    {
        SelectedImpostOwnerId = ownerSectionId;
        SelectedSectionId = null;
        SelectedKind = CadSelectionKind.Impost;
    }

    public bool HitTestImpost(CadPoint point, double tolerance, out WindowSection? owner)
    {
        foreach (var section in Sections)
        {
            if (section.SplitImpost != null && DistanceToSegment(point, section.SplitImpost.Axis) <= tolerance)
            {
                owner = section;
                return true;
            }
        }

        owner = null;
        return false;
    }

    public void SplitSelected(SectionSplitOrientation orientation, CadPoint? point = null)
    {
        var section = SelectedSection ?? RootSection;
        if (!section.IsLeaf)
        {
            return;
        }

        if (orientation == SectionSplitOrientation.Vertical)
        {
            section.SplitVertical(point?.X ?? section.Bounds.X + section.Bounds.Width / 2, Options.ImpostProfileWidth);
        }
        else if (orientation == SectionSplitOrientation.Horizontal)
        {
            section.SplitHorizontal(point?.Y ?? section.Bounds.Y - section.Bounds.Height / 2, Options.ImpostProfileWidth);
        }
        else
        {
            section.SplitAngled(45, Options.ImpostProfileWidth);
        }

        SelectImpost(section.Id);
    }

    public void MoveSelectedImpost(double coordinate)
    {
        SelectedImpostOwner?.MoveSplit(coordinate);
    }

    public void ApplyFillToSelected(SectionFillKind fillKind)
    {
        var section = SelectedSection;
        if (section?.IsLeaf == true)
        {
            section.ApplyFill(fillKind);
            SelectSection(section.Id);
        }
    }

    public void AddHardwareToSelected(SashHardwareModifier modifier)
    {
        var section = SelectedSection;
        if (section?.Fill.HasSash == true && !section.Fill.HardwareModifiers.Contains(modifier))
        {
            section.Fill.HardwareModifiers.Add(modifier);
        }
    }

    public void SetHardwareModifier(SashHardwareModifier modifier, bool enabled)
    {
        var section = SelectedSection;
        if (section?.Fill.HasSash != true)
        {
            return;
        }

        if (enabled && !section.Fill.HardwareModifiers.Contains(modifier))
        {
            section.Fill.HardwareModifiers.Add(modifier);
        }
        else if (!enabled)
        {
            section.Fill.HardwareModifiers.Remove(modifier);
        }
    }

    public void Recalculate()
    {
        OuterFrame.Resize(Options.Width, Options.Height);
        OuterFrame.ProfileWidth = Options.FrameProfileWidth;
        OuterFrame.ProfileKind = Options.ProfileKind;
        RootSection.Recalculate(OuterFrame.InnerBounds);
    }

    private static double DistanceToSegment(CadPoint p, CadLine line)
    {
        var segment = line.End - line.Start;
        var lengthSquared = segment.X * segment.X + segment.Y * segment.Y;
        if (lengthSquared <= CadMath.Epsilon)
        {
            return p.DistanceTo(line.Start);
        }

        var t = Math.Clamp(((p.X - line.Start.X) * segment.X + (p.Y - line.Start.Y) * segment.Y) / lengthSquared, 0, 1);
        var projection = new CadPoint(line.Start.X + t * segment.X, line.Start.Y + t * segment.Y);
        return p.DistanceTo(projection);
    }
}
