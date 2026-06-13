object ChartDlg: TChartDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Графики'
  ClientHeight = 507
  ClientWidth = 686
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object Ch1: TChart
    Left = 0
    Top = 28
    Width = 685
    Height = 477
    Foot.Text.Strings = (
      '')
    Legend.CheckBoxesStyle = cbsRadio
    SubFoot.Text.Strings = (
      '')
    SubTitle.Text.Strings = (
      '')
    Title.Text.Strings = (
      'TChart')
    BottomAxis.AxisValuesFormat = '#'
    TabOrder = 0
    object Series1: TLineSeries
      Marks.Callout.Brush.Color = clBlack
      Marks.Visible = False
      ValueFormat = '#.###'
      Pointer.Dark3D = False
      Pointer.Draw3D = False
      Pointer.InflateMargins = True
      Pointer.Style = psRectangle
      Pointer.Visible = False
      XValues.Name = 'X'
      XValues.Order = loAscending
      YValues.Name = 'Y'
      YValues.Order = loNone
    end
    object Series2: TLineSeries
      Marks.Callout.Brush.Color = clBlack
      Marks.Visible = False
      DataSource = Series1
      Pointer.InflateMargins = True
      Pointer.Style = psRectangle
      Pointer.Visible = False
      XValues.Name = 'X'
      XValues.Order = loAscending
      YValues.Name = 'Y'
      YValues.Order = loNone
      object TeeFunction1: TAverageTeeFunction
        Period = 5.000000000000000000
        PeriodStyle = psRange
      end
    end
  end
end
