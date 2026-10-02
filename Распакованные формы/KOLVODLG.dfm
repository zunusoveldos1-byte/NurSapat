object KolvoDlg: TKolvoDlg
  Left = 504
  Top = 250
  BorderStyle = bsNone
  Caption = 'Количество'
  ClientHeight = 77
  ClientWidth = 165
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clBlack
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object Bevel1: TBevel
    Left = 0
    Top = 0
    Width = 165
    Height = 77
    Align = alClient
    Style = bsRaised
  end
  object Bevel2: TBevel
    Left = 4
    Top = 4
    Width = 157
    Height = 69
  end
  object Label1: TLabel
    Left = 16
    Top = 16
    Width = 59
    Height = 13
    Caption = 'Количество'
    Transparent = True
  end
  object Edit1: TEdit
    Left = 84
    Top = 12
    Width = 57
    Height = 21
    TabOrder = 0
    Text = '1'
  end
  object UpDown1: TUpDown
    Left = 141
    Top = 12
    Width = 12
    Height = 21
    Associate = Edit1
    Min = 1
    Max = 32000
    Position = 1
    TabOrder = 1
  end
  object Button1: TButton
    Left = 12
    Top = 40
    Width = 69
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 2
  end
  object Button2: TButton
    Left = 84
    Top = 40
    Width = 69
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 3
  end
end
