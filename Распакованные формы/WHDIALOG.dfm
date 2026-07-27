object WHDialog: TWHDialog
  Left = 492
  Top = 444
  BorderStyle = bsNone
  Caption = 'Габариты'
  ClientHeight = 101
  ClientWidth = 165
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Bevel1: TBevel
    Left = 0
    Top = 0
    Width = 165
    Height = 101
    Align = alClient
    Style = bsRaised
  end
  object Bevel2: TBevel
    Left = 4
    Top = 4
    Width = 157
    Height = 93
  end
  object Label1: TLabel
    Left = 12
    Top = 16
    Width = 67
    Height = 13
    Caption = 'Ширина, мм. '
    Transparent = True
  end
  object Label2: TLabel
    Left = 12
    Top = 40
    Width = 63
    Height = 13
    Caption = 'Высота, мм.'
    Transparent = True
  end
  object EWidthdlg: TEdit
    Left = 88
    Top = 12
    Width = 61
    Height = 21
    TabOrder = 0
    Text = '1000'
    OnChange = EWidthdlgExit
  end
  object EHeightdlg: TEdit
    Left = 88
    Top = 36
    Width = 61
    Height = 21
    TabOrder = 1
    Text = '1000'
    OnChange = EHeightdlgExit
  end
  object Button1: TButton
    Left = 12
    Top = 64
    Width = 69
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 2
  end
  object Button2: TButton
    Left = 84
    Top = 64
    Width = 69
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 3
  end
end
