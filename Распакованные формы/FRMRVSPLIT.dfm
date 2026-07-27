inherited frmRVSplit: TfrmRVSplit
  Left = 183
  Top = 224
  HelpContext = 92000
  BorderStyle = bsDialog
  Caption = 'Split'
  ClientHeight = 245
  ClientWidth = 296
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object panUnmerge: TGroupBox
    Left = 12
    Top = 94
    Width = 273
    Height = 109
    TabOrder = 1
    Visible = False
    object cbUnmergeRows: TCheckBox
      Left = 13
      Top = 53
      Width = 206
      Height = 17
      Caption = 'Split to original &rows'
      Checked = True
      State = cbChecked
      TabOrder = 1
    end
    object cbUnmergeCols: TCheckBox
      Left = 13
      Top = 20
      Width = 206
      Height = 17
      Caption = 'Split to original columns'
      Checked = True
      State = cbChecked
      TabOrder = 0
    end
  end
  object panSplit: TGroupBox
    Left = 12
    Top = 94
    Width = 273
    Height = 109
    TabOrder = 2
    object Label1: TLabel
      Left = 12
      Top = 24
      Width = 94
      Height = 13
      Caption = 'Number of &columns:'
      FocusControl = seColumns
    end
    object Label2: TLabel
      Left = 12
      Top = 48
      Width = 77
      Height = 13
      Caption = 'Number of &rows:'
      FocusControl = seRows
    end
    object seColumns: TRVSpinEdit
      Left = 150
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
      Left = 150
      Top = 44
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 500.000000000000000000
      MinValue = 1.000000000000000000
      Value = 2.000000000000000000
      TabOrder = 1
    end
    object cbMerge: TCheckBox
      Left = 11
      Top = 74
      Width = 246
      Height = 17
      Caption = '&Merge before splitting'
      TabOrder = 2
    end
  end
  object btnOk: TButton
    Left = 83
    Top = 211
    Width = 100
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 3
  end
  object btnCancel: TButton
    Left = 187
    Top = 211
    Width = 100
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 4
  end
  object GroupBox1: TGroupBox
    Left = 12
    Top = 10
    Width = 273
    Height = 77
    Caption = 'Split to'
    TabOrder = 0
    object rbSplit: TRadioButton
      Left = 8
      Top = 24
      Width = 245
      Height = 17
      Caption = '&Specified number of rows and columns'
      Checked = True
      TabOrder = 0
      TabStop = True
      OnClick = rbClick
    end
    object rbUnmerge: TRadioButton
      Left = 8
      Top = 48
      Width = 245
      Height = 17
      Caption = '&Original cells'
      TabOrder = 1
      OnClick = rbClick
    end
  end
end
