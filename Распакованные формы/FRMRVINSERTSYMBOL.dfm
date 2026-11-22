inherited frmRVInsertSymbol: TfrmRVInsertSymbol
  Left = 239
  Top = 266
  HelpContext = 90500
  ActiveControl = dg
  Caption = 'Insert Symbol'
  ClientHeight = 355
  ClientWidth = 653
  Constraints.MinHeight = 300
  Constraints.MinWidth = 630
  OldCreateOrder = True
  Position = poScreenCenter
  OnResize = FormResize
  PixelsPerInch = 96
  TextHeight = 13
  object lblCharset: TLabel
    Left = 14
    Top = 44
    Width = 68
    Height = 13
    Caption = '&Character Set:'
    FocusControl = cmbCharset
  end
  object lblFont: TLabel
    Left = 14
    Top = 14
    Width = 24
    Height = 13
    Caption = '&Font:'
    FocusControl = cmbFont
  end
  object Label1: TLabel
    Left = 16
    Top = 328
    Width = 32
    Height = 13
    Anchors = [akLeft, akBottom]
    Caption = 'Label1'
  end
  object lblBlock: TLabel
    Left = 354
    Top = 44
    Width = 30
    Height = 13
    Anchors = [akTop, akRight]
    Caption = 'Block:'
    FocusControl = cmbBlock
  end
  object btnOk: TButton
    Left = 441
    Top = 322
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Caption = 'Insert'
    Default = True
    ModalResult = 1
    TabOrder = 4
  end
  object btnCancel: TButton
    Left = 544
    Top = 322
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 5
  end
  object cmbFont: TRVFontComboBox
    Left = 120
    Top = 10
    Width = 221
    Height = 21
    Style = csDropDownList
    Anchors = [akLeft, akTop, akRight]
    DropDownCount = 20
    ItemHeight = 13
    TabOrder = 0
    OnClick = cmbFontClick
  end
  object cmbCharset: TRVFontCharsetComboBox
    Left = 120
    Top = 40
    Width = 221
    Height = 21
    Style = csDropDownList
    Anchors = [akLeft, akTop, akRight]
    DropDownCount = 20
    ItemHeight = 13
    TabOrder = 1
    OnClick = cmbCharsetClick
    AddDefaultCharset = True
    DefaultCharsetCaption = 'Unicode'
  end
  object dg: TRVGrid
    Left = 11
    Top = 74
    Width = 635
    Height = 237
    Anchors = [akLeft, akTop, akRight, akBottom]
    TabOrder = 3
    TabStop = True
    OnDblClick = dgDblClick
    OnEnter = dgEnter
    BoundsType = bbtByGridBounds
    RowCount = 8
    RowsVisible = 8
    ColCount = 28
    GridLineWidth = 1
    DefaultColWidth = 32
    DefaultRowHeight = 20
    OnDrawCell = dgDrawCell
    OnSelectCell = dgSelectCell
    OnTopLeftChanged = dgTopLeftChanged
  end
  object cmbBlock: TComboBox
    Left = 401
    Top = 40
    Width = 238
    Height = 21
    Style = csDropDownList
    Anchors = [akTop, akRight]
    DropDownCount = 20
    ItemHeight = 13
    TabOrder = 2
    OnClick = cmbBlockClick
  end
end
