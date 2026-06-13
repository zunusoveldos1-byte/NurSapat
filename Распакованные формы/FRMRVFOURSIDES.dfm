inherited frmRVFourSides: TfrmRVFourSides
  Left = 109
  Top = 157
  HelpContext = 90300
  BorderStyle = bsDialog
  Caption = 'Padding'
  ClientHeight = 163
  ClientWidth = 345
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object gb: TGroupBox
    Left = 8
    Top = 4
    Width = 329
    Height = 117
    Caption = 'Padding'
    TabOrder = 0
    object lblTop: TLabel
      Left = 13
      Top = 28
      Width = 22
      Height = 13
      Caption = '&Top:'
      FocusControl = seTop
    end
    object lblBottom: TLabel
      Left = 13
      Top = 62
      Width = 33
      Height = 13
      Caption = '&Bottom'
      FocusControl = seBottom
    end
    object lblLeft: TLabel
      Left = 188
      Top = 28
      Width = 21
      Height = 13
      Caption = '&Left:'
      FocusControl = seLeft
    end
    object lblRight: TLabel
      Left = 188
      Top = 62
      Width = 25
      Height = 13
      Caption = '&Right'
      FocusControl = seRight
    end
    object seTop: TRVSpinEdit
      Left = 83
      Top = 23
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 100.000000000000000000
      MinValue = -50.000000000000000000
      TabOrder = 0
      OnChange = seTopChange
    end
    object seBottom: TRVSpinEdit
      Left = 83
      Top = 57
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 100.000000000000000000
      MinValue = -50.000000000000000000
      TabOrder = 1
      OnChange = seTopChange
    end
    object seLeft: TRVSpinEdit
      Left = 258
      Top = 23
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 100.000000000000000000
      MinValue = -50.000000000000000000
      TabOrder = 2
      OnChange = seTopChange
    end
    object seRight: TRVSpinEdit
      Left = 258
      Top = 57
      Width = 57
      Height = 22
      Increment = 1.000000000000000000
      MaxValue = 100.000000000000000000
      MinValue = -50.000000000000000000
      TabOrder = 3
      OnChange = seTopChange
    end
    object cbEqual: TCheckBox
      Left = 12
      Top = 90
      Width = 235
      Height = 17
      Caption = '&Equal values'
      TabOrder = 4
      OnClick = cbEqualClick
    end
  end
  object btnOk: TButton
    Left = 132
    Top = 130
    Width = 100
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 1
  end
  object btnCancel: TButton
    Left = 236
    Top = 130
    Width = 100
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 2
  end
end
