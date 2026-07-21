using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models.Grid;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Корневой агрегат конструкции: глобальный контейнер, переходные профили и независимые подрамы.
/// </summary>
public sealed class ConstructionAssembly
{
    public required GlobalFrame GlobalFrame { get; init; }

    public IReadOnlyList<TransitionProfileModel> TransitionProfiles { get; init; } = [];

    public IReadOnlyList<SubFrame> SubFrames { get; init; } = [];

    /// <summary>
    /// OverallWidth = Σ(SubFrame.OuterWidth) + Σ(TransitionProfile.Thickness).
    /// </summary>
    public double ComputeOverallWidth()
    {
        var subTotal = SubFrames.Sum(s => s.OuterWidth);
        var transitionTotal = TransitionProfiles.Sum(t => t.ThicknessMm);
        return CadMath.RoundMm(subTotal + transitionTotal);
    }

    public double ComputeOverallHeight()
    {
        if (SubFrames.Count == 0)
        {
            return GlobalFrame.Height;
        }

        return CadMath.RoundMm(SubFrames.Max(s => s.OuterHeight));
    }

    public bool ValidateOverallWidth(double expectedWidth, double toleranceMm = CadMath.MmPrecision) =>
        Math.Abs(ComputeOverallWidth() - expectedWidth) <= toleranceMm;

    public SubFrame? FindOwnerByNode(FrameNode node)
    {
        if (node.OwnerSubFrameId is not Guid ownerId)
        {
            return null;
        }

        return SubFrames.FirstOrDefault(candidate => candidate.Id == ownerId);
    }

    public bool ValidateOwnership()
    {
        foreach (var subFrame in SubFrames)
        {
            if (subFrame.Root.OwnerSubFrameId != subFrame.Id
                || subFrame.Root.EnumerateAllNodes().Any(node =>
                    node.OwnerSubFrameId != subFrame.Id
                    || node.SeparatorKind == SeparatorKind.Transition))
            {
                return false;
            }
        }

        return TransitionProfiles.All(profile =>
            profile.LeftSubFrameId is Guid left
            && profile.RightSubFrameId is Guid right
            && left != right
            && SubFrames.Any(subFrame => subFrame.Id == left)
            && SubFrames.Any(subFrame => subFrame.Id == right));
    }

    public bool TryResizeSubFrameWidth(Guid subFrameId, double newOuterWidth)
    {
        var ordered = SubFrames.OrderBy(owner => owner.OrderIndex).ToList();
        var index = ordered.FindIndex(owner => owner.Id == subFrameId);
        if (index < 0 || newOuterWidth <= CadMath.MmPrecision)
        {
            return false;
        }

        var target = ordered[index];
        var oldRight = target.OuterRightWorld;
        var delta = CadMath.RoundMm(newOuterWidth - target.OuterWidth);
        if (Math.Abs(delta) <= CadMath.MmPrecision)
        {
            return true;
        }

        if (!target.Root.TryResizeOuter(newOuterWidth, target.OuterHeight))
        {
            return false;
        }

        target.OuterWidth = CadMath.RoundMm(newOuterWidth);
        for (var i = index + 1; i < ordered.Count; i++)
        {
            ordered[i].OriginWorldX = CadMath.RoundMm(ordered[i].OriginWorldX + delta);
        }

        foreach (var profile in TransitionProfiles.Where(profile =>
                     profile.Orientation == GridLineOrientation.Vertical
                     && profile.AxisCoordinate >= oldRight - CadMath.MmPrecision))
        {
            profile.AxisCoordinate = CadMath.RoundMm(profile.AxisCoordinate + delta);
        }

        GlobalFrame.Width = CadMath.RoundMm(GlobalFrame.Width + delta);
        return true;
    }

    public bool TryResizeBounds(
        double x,
        double y,
        double width,
        double height)
    {
        if (width <= CadMath.Epsilon
            || height <= CadMath.Epsilon
            || GlobalFrame.Width <= CadMath.Epsilon
            || GlobalFrame.Height <= CadMath.Epsilon)
        {
            return false;
        }

        var oldX = GlobalFrame.X;
        var oldY = GlobalFrame.Y;
        var scaleX = width / GlobalFrame.Width;
        var scaleY = height / GlobalFrame.Height;

        foreach (var subFrame in SubFrames)
        {
            var resizedWidth = CadMath.RoundMm(subFrame.OuterWidth * scaleX);
            var resizedHeight = CadMath.RoundMm(subFrame.OuterHeight * scaleY);
            if (!subFrame.Root.TryResizeOuter(resizedWidth, resizedHeight, frameProfileInset: 0))
            {
                return false;
            }

            subFrame.OriginWorldX = CadMath.RoundMm(
                x + (subFrame.OriginWorldX - oldX) * scaleX);
            subFrame.OriginWorldY = CadMath.RoundMm(
                y + (subFrame.OriginWorldY - oldY) * scaleY);
            subFrame.OuterWidth = resizedWidth;
            subFrame.OuterHeight = resizedHeight;
        }

        foreach (var transition in TransitionProfiles)
        {
            transition.AxisCoordinate = transition.Orientation == GridLineOrientation.Vertical
                ? CadMath.RoundMm(x + (transition.AxisCoordinate - oldX) * scaleX)
                : CadMath.RoundMm(y + (transition.AxisCoordinate - oldY) * scaleY);
        }

        GlobalFrame.X = x;
        GlobalFrame.Y = y;
        GlobalFrame.Width = width;
        GlobalFrame.Height = height;
        return true;
    }

    public SubFrame? FindOwnerAt(double worldX, double worldY)
    {
        if (IsInsideTransitionBody(worldX, worldY))
        {
            return null;
        }

        var ordered = SubFrames.OrderBy(subFrame => subFrame.OrderIndex).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            var subFrame = ordered[index];
            var isLast = index == ordered.Count - 1;
            var insideX = worldX >= subFrame.OriginWorldX - CadMath.MmPrecision
                && (isLast
                    ? worldX <= subFrame.OuterRightWorld + CadMath.MmPrecision
                    : worldX < subFrame.OuterRightWorld - CadMath.MmPrecision);
            var insideY = worldY >= subFrame.OriginWorldY - CadMath.MmPrecision
                && worldY <= subFrame.OuterTopWorld + CadMath.MmPrecision;
            if (insideX && insideY)
            {
                return subFrame;
            }
        }

        return null;
    }

    private bool IsInsideTransitionBody(double worldX, double worldY)
    {
        foreach (var profile in TransitionProfiles)
        {
            if (profile.Orientation == GridLineOrientation.Vertical)
            {
                if (worldX >= profile.InnerLeftFace - CadMath.MmPrecision
                    && worldX <= profile.InnerRightFace + CadMath.MmPrecision
                    && worldY >= GlobalFrame.Y - CadMath.MmPrecision
                    && worldY <= GlobalFrame.Top + CadMath.MmPrecision)
                {
                    return true;
                }
            }
            else if (worldY >= profile.InnerLeftFace - CadMath.MmPrecision
                     && worldY <= profile.InnerRightFace + CadMath.MmPrecision
                     && worldX >= GlobalFrame.X - CadMath.MmPrecision
                     && worldX <= GlobalFrame.Right + CadMath.MmPrecision)
            {
                return true;
            }
        }

        return false;
    }

    public TransitionProfileModel? FindTransitionAt(double worldX, double worldY, double toleranceMm = 2.0)
    {
        foreach (var profile in TransitionProfiles)
        {
            if (profile.Orientation == GridLineOrientation.Vertical)
            {
                if (Math.Abs(worldX - profile.AxisCoordinate) <= toleranceMm
                    && worldY >= GlobalFrame.Y - toleranceMm
                    && worldY <= GlobalFrame.Top + toleranceMm)
                {
                    return profile;
                }
            }
            else
            {
                if (Math.Abs(worldY - profile.AxisCoordinate) <= toleranceMm
                    && worldX >= GlobalFrame.X - toleranceMm
                    && worldX <= GlobalFrame.Right + toleranceMm)
                {
                    return profile;
                }
            }
        }

        return null;
    }

    public TransitionProfileModel? FindAdjacentTransition(SubFrame subFrame, bool leftSide)
    {
        foreach (var profile in TransitionProfiles)
        {
            if (profile.Orientation != GridLineOrientation.Vertical)
            {
                continue;
            }

            if (leftSide && profile.RightSubFrameId == subFrame.Id)
            {
                return profile;
            }

            if (!leftSide && profile.LeftSubFrameId == subFrame.Id)
            {
                return profile;
            }
        }

        return null;
    }
}
