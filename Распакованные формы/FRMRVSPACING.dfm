inherited frmRVSpacing: TfrmRVSpacing
  Left = 208
  Top = 265
  HelpContext = 91900
  Caption = 'Cell Spacing'
  ClientHeight = 206
  ClientWidth = 366
  PixelsPerInch = 96
  TextHeight = 13
  inherited gb: TGroupBox
    Left = 5
    Top = 3
    Width = 357
    Height = 159
    Caption = ' Spacing '
    inherited lblTop: TLabel
      Left = 10
      Top = 60
      Width = 50
      Caption = '&Horizontal:'
    end
    inherited lblBottom: TLabel
      Left = 10
      Top = 96
      Width = 38
      Caption = '&Vertical:'
    end
    inherited lblLeft: TLabel
      Left = 184
      Top = 60
      Width = 50
      Caption = 'H&orizontal:'
    end
    inherited lblRight: TLabel
      Left = 184
      Top = 96
      Width = 38
      Caption = 'V&ertical:'
    end
    object Label5: TLabel [4]
      Left = 3
      Top = 14
      Width = 168
      Height = 39
      Alignment = taCenter
      AutoSize = False
      Caption = 'Between cells'
      Layout = tlCenter
      WordWrap = True
    end
    object Label6: TLabel [5]
      Left = 177
      Top = 14
      Width = 175
      Height = 39
      Alignment = taCenter
      AutoSize = False
      Caption = 'From table border to cells'
      Layout = tlCenter
      WordWrap = True
    end
    object Bevel1: TBevel [6]
      Left = 173
      Top = 14
      Width = 10
      Height = 108
      Shape = bsLeftLine
    end
    inherited seTop: TRVSpinEdit
      Left = 111
      Top = 55
      MaxValue = 30.000000000000000000
      MinValue = -1.000000000000000000
    end
    inherited seBottom: TRVSpinEdit
      Left = 111
      Top = 91
      MaxValue = 30.000000000000000000
      MinValue = -1.000000000000000000
    end
    inherited seLeft: TRVSpinEdit
      Left = 289
      Top = 55
      MaxValue = 30.000000000000000000
      MinValue = -1.000000000000000000
    end
    inherited seRight: TRVSpinEdit
      Left = 289
      Top = 91
      MaxValue = 30.000000000000000000
      MinValue = -1.000000000000000000
    end
    inherited cbEqual: TCheckBox
      Left = 11
      Top = 129
    end
  end
  inherited btnOk: TButton
    Left = 154
    Top = 171
  end
  inherited btnCancel: TButton
    Left = 258
    Top = 171
  end
end
