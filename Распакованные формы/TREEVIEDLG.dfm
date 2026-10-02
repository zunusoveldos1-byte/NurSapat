object TreeVieDlg: TTreeVieDlg
  Left = 106
  Top = 186
  Caption = 'System'
  ClientHeight = 273
  ClientWidth = 551
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = True
  OnActivate = FormActivate
  PixelsPerInch = 96
  TextHeight = 13
  object BitBtn1: TBitBtn
    Left = 52
    Top = 248
    Width = 75
    Height = 25
    DoubleBuffered = True
    Kind = bkOK
    ParentDoubleBuffered = False
    TabOrder = 0
  end
  object ListBox1: TListBox
    Left = 4
    Top = 4
    Width = 197
    Height = 157
    ItemHeight = 13
    TabOrder = 1
    OnClick = ListBox1Click
    OnDblClick = ListBox1DblClick
  end
  object ListBox2: TListBox
    Left = 204
    Top = 4
    Width = 353
    Height = 269
    ItemHeight = 13
    TabOrder = 2
  end
end
