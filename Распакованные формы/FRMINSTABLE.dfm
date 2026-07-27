inherited frmInsTable: TfrmInsTable
  Left = 210
  Top = 119
  HelpContext = 90600
  BorderStyle = bsDialog
  Caption = 'Insert Table'
  ClientHeight = 247
  ClientWidth = 345
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object gbSize: TGroupBox
    Left = 7
    Top = 4
    Width = 330
    Height = 77
    Caption = 'Table size'
    TabOrder = 0
    object lblnCols: TLabel
      Left = 12
      Top = 24
      Width = 94
      Height = 13
      Caption = 'Number of &columns:'
      FocusControl = seColumns
    end
    object lblnRows: TLabel
      Left = 12
      Top = 48
      Width = 77
      Height = 13
      Caption = 'Number of &rows:'
      FocusControl = seRows
    end
    object seColumns: TRVSpinEdit
      Left = 161
      Top = 20
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 100.000000000000000000
      MinValue = 1.000000000000000000
      Value = 2.000000000000000000
      TabOrder = 0
    end
    object seRows: TRVSpinEdit
      Left = 161
      Top = 44
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 500.000000000000000000
      MinValue = 1.000000000000000000
      Value = 2.000000000000000000
      TabOrder = 1
    end
  end
  object cbRemember: TCheckBox
    Left = 8
    Top = 188
    Width = 322
    Height = 17
    Caption = 'Remember &dimensions for new tables'
    TabOrder = 2
  end
  object btnOk: TButton
    Left = 132
    Top = 215
    Width = 100
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 3
  end
  object btnCancel: TButton
    Left = 236
    Top = 215
    Width = 100
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 4
  end
  object gbLayout: TGroupBox
    Left = 7
    Top = 84
    Width = 330
    Height = 101
    Caption = 'Table layout'
    TabOrder = 1
    object rbFitContents: TRadioButton
      Left = 12
      Top = 24
      Width = 249
      Height = 17
      Caption = '&Autosize'
      Checked = True
      TabOrder = 0
      TabStop = True
      OnClick = rbFitManualClick
    end
    object rbFitWindow: TRadioButton
      Left = 12
      Top = 48
      Width = 247
      Height = 17
      Caption = 'Size table to fit &window'
      TabOrder = 1
      OnClick = rbFitManualClick
    end
    object rbFitManual: TRadioButton
      Left = 12
      Top = 72
      Width = 147
      Height = 17
      Caption = 'Size table &manually'
      TabOrder = 2
      OnClick = rbFitManualClick
    end
    object seWidth: TRVSpinEdit
      Left = 180
      Top = 68
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 1000.000000000000000000
      MinValue = 1.000000000000000000
      Value = 100.000000000000000000
      TabOrder = 3
    end
    object cmbWidthType: TComboBox
      Left = 240
      Top = 67
      Width = 80
      Height = 21
      Style = csDropDownList
      ItemHeight = 13
      Items.Strings = (
        'Percent'
        'Pixels')
      TabOrder = 4
      OnClick = cmbWidthTypeClick
    end
  end
end
