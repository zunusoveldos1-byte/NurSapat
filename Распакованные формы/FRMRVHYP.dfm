inherited frmRVHyp: TfrmRVHyp
  Left = 187
  Top = 184
  HelpContext = 90400
  BorderStyle = bsDialog
  Caption = 'Insert Hyperlink'
  ClientHeight = 175
  ClientWidth = 356
  OldCreateOrder = True
  Position = poScreenCenter
  OnActivate = FormActivate
  PixelsPerInch = 96
  TextHeight = 13
  object gb: TGroupBox
    Left = 6
    Top = 4
    Width = 343
    Height = 127
    Caption = 'Hyperlink'
    TabOrder = 0
    object lblText: TLabel
      Left = 15
      Top = 33
      Width = 24
      Height = 13
      Caption = 'T&ext:'
      FocusControl = txtText
    end
    object lblTarget: TLabel
      Left = 15
      Top = 63
      Width = 34
      Height = 13
      Caption = '&Target:'
      FocusControl = txtTarget
    end
    object txtText: TEdit
      Left = 80
      Top = 29
      Width = 250
      Height = 21
      TabOrder = 0
      OnChange = txtTargetChange
    end
    object txtTarget: TEdit
      Left = 80
      Top = 59
      Width = 250
      Height = 21
      TabOrder = 1
      OnChange = txtTargetChange
    end
    object btnHypProp: TButton
      Left = 230
      Top = 91
      Width = 100
      Height = 25
      Caption = '&Attributes...'
      TabOrder = 2
      OnClick = btnHypPropClick
    end
  end
  object btnOk: TButton
    Left = 145
    Top = 140
    Width = 100
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 1
  end
  object btnCancel: TButton
    Left = 249
    Top = 140
    Width = 100
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 2
  end
end
