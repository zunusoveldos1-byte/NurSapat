object ZagotovDlg: TZagotovDlg
  Left = 93
  Top = 149
  BorderStyle = bsDialog
  Caption = 'Детали конструкции'
  ClientHeight = 396
  ClientWidth = 701
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clBlack
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = True
  Position = poScreenCenter
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label2: TLabel
    Left = 492
    Top = 0
    Width = 38
    Height = 13
    Caption = 'Детали'
    Transparent = True
  end
  object Label1: TLabel
    Left = 120
    Top = 0
    Width = 52
    Height = 13
    Caption = 'Элементы'
    Transparent = True
  end
  object LView1: TADGListView
    Left = 0
    Top = 16
    Width = 293
    Height = 377
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    TabOrder = 0
    OnClick = LView1Click
  end
  object LView2: TADGListView
    Left = 296
    Top = 16
    Width = 401
    Height = 377
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    TabOrder = 1
  end
  object CheckBox1: TCheckBox
    Left = 296
    Top = 0
    Width = 61
    Height = 17
    Caption = 'все'
    TabOrder = 2
    OnClick = CheckBox1Click
  end
end
