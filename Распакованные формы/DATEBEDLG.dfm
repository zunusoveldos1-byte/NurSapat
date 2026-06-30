object DateBEDlg: TDateBEDlg
  Left = 469
  Top = 298
  BorderStyle = bsDialog
  Caption = 'Период'
  ClientHeight = 189
  ClientWidth = 316
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 4
    Top = 24
    Width = 36
    Height = 13
    Caption = 'начало'
  end
  object Label2: TLabel
    Left = 4
    Top = 48
    Width = 30
    Height = 13
    Caption = 'конец'
  end
  object Label4: TLabel
    Left = 4
    Top = 144
    Width = 305
    Height = 41
    AutoSize = False
    Caption = 
      'Например, если указать "начало периода" 01.01.2011, а "конец пер' +
      'иода" 31.01.2011, то будет просчитан период с 00:00 01.01.2011 п' +
      'о 23:59 31.01.2011'
    Color = clInfoBk
    ParentColor = False
    WordWrap = True
  end
  object Label3: TLabel
    Left = 68
    Top = 4
    Width = 68
    Height = 13
    Caption = 'Период №1'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold, fsUnderline]
    ParentFont = False
  end
  object Label5: TLabel
    Left = 4
    Top = 96
    Width = 36
    Height = 13
    Caption = 'начало'
  end
  object Label6: TLabel
    Left = 4
    Top = 120
    Width = 30
    Height = 13
    Caption = 'конец'
  end
  object Label7: TLabel
    Left = 68
    Top = 76
    Width = 68
    Height = 13
    Caption = 'Период №2'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold, fsUnderline]
    ParentFont = False
  end
  object OKBtn: TButton
    Left = 236
    Top = 4
    Width = 73
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 236
    Top = 32
    Width = 73
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object BegDate: TDateTimePicker
    Left = 44
    Top = 20
    Width = 125
    Height = 21
    Date = 40566.731344328700000000
    Time = 40566.731344328700000000
    TabOrder = 2
  end
  object EndDate: TDateTimePicker
    Left = 44
    Top = 44
    Width = 125
    Height = 21
    Date = 40566.731424421300000000
    Time = 40566.731424421300000000
    TabOrder = 3
    OnChange = EndDateChange
  end
  object BegDate2: TDateTimePicker
    Left = 44
    Top = 92
    Width = 125
    Height = 21
    Date = 40566.731344328700000000
    Time = 40566.731344328700000000
    TabOrder = 4
  end
  object EndDate2: TDateTimePicker
    Left = 44
    Top = 116
    Width = 125
    Height = 21
    Date = 40566.731424421300000000
    Time = 40566.731424421300000000
    TabOrder = 5
  end
end
