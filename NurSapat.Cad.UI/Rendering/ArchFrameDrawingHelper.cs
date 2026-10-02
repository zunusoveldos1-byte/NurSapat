using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Разбор габаритов арочной коробки: прямоугольная часть + дуга сверху (R = W/2 по умолчанию).
/// </summary>
public static class ArchFrameDrawingHelper
{
  public static double DefaultArcHeightForWidth(double width) =>
      width / 2.0;

  public static ProfileSegment CreateArcTopProfile(
      double originX,
      double originY,
      double width,
      double boxHeight,
      double arcHeight)
  {
      var segment = ProfileSegment.CreateTopRail(originX, originY, width, boxHeight);
      segment.SegmentType = SegmentType.Arc;
      segment.ArcHeight = Math.Clamp(
          arcHeight,
          ProfileSegment.MinSafeArcHeightMm,
          segment.MaximumArcHeight);
      return segment;
  }

  public static bool TryResolveLayout(
      CadEditorViewModel editor,
      GeometryElement element,
      out double originX,
      out double originY,
      out double width,
      out double boxHeight,
      out ProfileSegment topProfile)
  {
      topProfile = null!;
      width = (double)element.WidthMm;
      boxHeight = (double)element.HeightMm;
      originX = 0;
      originY = 0;
      if (width <= CadMath.Epsilon || boxHeight <= CadMath.Epsilon)
      {
          return false;
      }

      originX = (double)element.X - width / 2.0;
      originY = (double)element.Y - boxHeight / 2.0;

      if (TryUseFrameRootTopProfile(editor, width, boxHeight, out topProfile))
      {
          return true;
      }

      var arcHeight = DefaultArcHeightForWidth(width);
      topProfile = CreateArcTopProfile(originX, originY, width, boxHeight, arcHeight);
      return true;
  }

  public static double GetTotalHeight(double boxHeight, ProfileSegment topProfile) =>
      topProfile.SegmentType == SegmentType.Arc
          ? boxHeight + topProfile.ArcHeight
          : boxHeight;

  public static double GetOuterArcRadius(ProfileSegment topProfile) =>
      ProfileSegment.ComputeRadiusFromSagitta(topProfile.ChordWidth, topProfile.ArcHeight);

  public static void EnsureFrameRootArcProfile(FrameNode frameRoot, double width)
  {
      frameRoot.EnsureTopProfile();
      var profile = frameRoot.TopProfile;
      if (profile is null)
      {
          return;
      }

      profile.SyncTopRailGeometry(frameRoot.X, frameRoot.Y, frameRoot.Width, frameRoot.Height);
      if (profile.SegmentType != SegmentType.Arc)
      {
          profile.ConvertToArc(DefaultArcHeightForWidth(width));
          return;
      }

      profile.Radius = profile.ClampRadius(DefaultArcHeightForWidth(width));
  }

  public static void SyncFrameRootArcWidth(FrameNode frameRoot, double width)
  {
      frameRoot.EnsureTopProfile();
      var profile = frameRoot.TopProfile;
      if (profile is null)
      {
          return;
      }

      profile.SyncTopRailGeometry(frameRoot.X, frameRoot.Y, frameRoot.Width, frameRoot.Height);
      if (profile.SegmentType == SegmentType.Arc)
      {
          profile.Radius = profile.ClampRadius(DefaultArcHeightForWidth(width));
      }
  }

  private static bool TryUseFrameRootTopProfile(
      CadEditorViewModel editor,
      double width,
      double boxHeight,
      out ProfileSegment topProfile)
  {
      topProfile = null!;
      var root = editor.FrameRoot;
      if (root?.TopProfile is not ProfileSegment profile ||
          profile.SegmentType != SegmentType.Arc)
      {
          return false;
      }

      if (Math.Abs(root.Width - width) > CadMath.MmPrecision ||
          Math.Abs(root.Height - boxHeight) > CadMath.MmPrecision)
      {
          return false;
      }

      topProfile = profile;
      return true;
  }
}
