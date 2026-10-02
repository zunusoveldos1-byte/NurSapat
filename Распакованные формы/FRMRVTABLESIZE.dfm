object frmRVTableSize: TfrmRVTableSize
  Left = 109
  Top = 173
  AutoSize = True
  BorderStyle = bsNone
  ClientHeight = 145
  ClientWidth = 150
  Color = clWindow
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = False
  Scaled = False
  OnActivate = FormActivate
  OnClose = FormClose
  OnCreate = FormCreate
  OnDeactivate = FormDeactivate
  PixelsPerInch = 96
  TextHeight = 13
  object grid: TRVGrid
    Left = 0
    Top = 0
    Width = 150
    Height = 120
    ParentShowHint = False
    ShowHint = True
    TabOrder = 0
    TabStop = True
    Color = clMenu
    OnKeyDown = gridKeyDown
    OnMouseMove = gridMouseMove
    OnMouseUp = gridMouseUp
    BorderStyle = bsNone
    RowCount = 4
    RowsVisible = 4
    ColCount = 5
    GridLineWidth = 0
    DefaultColWidth = 30
    DefaultRowHeight = 30
    OnDrawCell = gridDrawCell
    OnSelectCell = gridSelectCell
  end
  object Panel1: TPanel
    Left = 0
    Top = 120
    Width = 150
    Height = 25
    BevelOuter = bvNone
    Caption = '1 x 1'
    Color = clInfoBk
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clInfoText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    TabOrder = 1
    OnClick = Panel1Click
    OnMouseMove = Panel1MouseMove
    object Shape1: TShape
      Left = 0
      Top = 0
      Width = 150
      Height = 25
      Align = alClient
      Brush.Style = bsClear
      Pen.Color = clBtnShadow
      OnMouseDown = Shape1MouseDown
      OnMouseMove = Panel1MouseMove
    end
  end
end
