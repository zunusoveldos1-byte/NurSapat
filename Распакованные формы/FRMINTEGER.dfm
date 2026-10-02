inherited frmInteger: TfrmInteger
  Left = 276
  Top = 376
  HelpContext = 90700
  BorderStyle = bsDialog
  Caption = 'frmInteger'
  ClientHeight = 102
  ClientWidth = 325
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object GroupBox1: TGroupBox
    Left = 4
    Top = 4
    Width = 316
    Height = 59
    TabOrder = 0
    object lbl: TLabel
      Left = 12
      Top = 24
      Width = 10
      Height = 13
      Caption = 'lbl'
      FocusControl = se
    end
    object se: TRVSpinEdit
      Left = 233
      Top = 24
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 100.000000000000000000
      MinValue = 1.000000000000000000
      Value = 1.000000000000000000
      TabOrder = 0
    end
  end
  object btnOk: TButton
    Left = 113
    Top = 71
    Width = 100
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 1
  end
  object btnCancel: TButton
    Left = 219
    Top = 71
    Width = 100
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 2
  end
end
