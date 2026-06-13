inherited frmRVFillColor: TfrmRVFillColor
  Left = 260
  Top = 231
  HelpContext = 90100
  BorderStyle = bsDialog
  Caption = 'Fill Color'
  ClientHeight = 239
  ClientWidth = 323
  OldCreateOrder = True
  Position = poScreenCenter
  OnActivate = FormActivate
  PixelsPerInch = 96
  TextHeight = 13
  object gb: TGroupBox
    Left = 10
    Top = 6
    Width = 303
    Height = 193
    Caption = 'Fill Color'
    TabOrder = 0
    object lblColor: TLabel
      Left = 174
      Top = 22
      Width = 9
      Height = 13
      Caption = '---'
    end
    object lbl: TLabel
      Left = 27
      Top = 142
      Width = 41
      Height = 13
      Caption = '&Apply to:'
      FocusControl = cmb
    end
    object rvcg: TRVColorGrid
      Left = 20
      Top = 22
      Width = 144
      Height = 114
      FirstCaption = 'Transparent'
      Indeterminate = True
      OnColorChange = rvcgColorChange
      TabOrder = 0
      TabStop = True
      BorderStyle = bsNone
    end
    object btnColor: TButton
      Left = 177
      Top = 72
      Width = 100
      Height = 25
      Caption = '&More Colors...'
      TabOrder = 1
      OnClick = btnColorClick
    end
    object cmb: TComboBox
      Left = 25
      Top = 162
      Width = 145
      Height = 21
      Style = csDropDownList
      ItemHeight = 13
      TabOrder = 3
      OnClick = cmbClick
    end
    object btnPadding: TButton
      Left = 177
      Top = 102
      Width = 100
      Height = 25
      Caption = '&Padding...'
      TabOrder = 2
      OnClick = btnPaddingClick
    end
  end
  object btnOk: TButton
    Left = 113
    Top = 206
    Width = 100
    Height = 25
    Caption = 'OK'
    Default = True
    TabOrder = 1
    OnClick = btnOkClick
  end
  object btnCancel: TButton
    Left = 216
    Top = 206
    Width = 100
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 2
  end
  object ColorDialog1: TColorDialog
    Ctl3D = True
    Left = 220
    Top = 156
  end
end
