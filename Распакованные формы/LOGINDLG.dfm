object LoginDlg: TLoginDlg
  Left = 394
  Top = 303
  BorderIcons = []
  BorderStyle = bsDialog
  ClientHeight = 119
  ClientWidth = 244
  Color = clBtnFace
  Font.Charset = RUSSIAN_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'Tahoma'
  Font.Style = []
  OldCreateOrder = False
  Position = poScreenCenter
  OnShow = Dialog1Show
  PixelsPerInch = 96
  TextHeight = 13
  object Bevel1: TBevel
    Left = 4
    Top = 4
    Width = 237
    Height = 85
    Shape = bsFrame
    Style = bsRaised
  end
  object Label1: TLabel
    Left = 24
    Top = 40
    Width = 82
    Height = 13
    Caption = 'Введите пароль'
    Transparent = True
  end
  object Label2: TLabel
    Left = 12
    Top = 64
    Width = 97
    Height = 13
    Caption = 'Подтвердите ввод'
    Transparent = True
  end
  object Label3: TLabel
    Left = 32
    Top = 16
    Width = 72
    Height = 13
    Caption = 'Пользователь'
    Transparent = True
  end
  object Edit1: TEdit
    Left = 112
    Top = 36
    Width = 121
    Height = 21
    PasswordChar = '*'
    TabOrder = 0
  end
  object Edit2: TEdit
    Left = 112
    Top = 60
    Width = 121
    Height = 21
    PasswordChar = '*'
    TabOrder = 1
  end
  object Button1: TButton
    Left = 84
    Top = 92
    Width = 77
    Height = 25
    Caption = 'ОК'
    Default = True
    TabOrder = 2
    OnClick = Button1Click
  end
  object Edit3: TEdit
    Left = 112
    Top = 12
    Width = 121
    Height = 21
    TabOrder = 3
  end
end
