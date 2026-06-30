object AboutDlg: TAboutDlg
  Left = 211
  Top = 258
  BorderIcons = [biSystemMenu]
  BorderStyle = bsSingle
  Caption = 'О программе'
  ClientHeight = 517
  ClientWidth = 583
  Color = clGray
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = False
  Position = poScreenCenter
  OnCreate = FormCreate
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Image1: TImage
    Left = 0
    Top = 0
    Width = 583
    Height = 251
    Stretch = True
  end
  object Label1: TLabel
    Tag = -700
    Left = 28
    Top = 84
    Width = 267
    Height = 42
    Caption = 'Application.Title'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -37
    Font.Name = 'Times New Roman'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label3: TLabel
    Tag = -700
    Left = 28
    Top = 128
    Width = 263
    Height = 16
    Caption = 'Copyright '#169' 1999-2011 Курбацкий Алексей'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label5: TLabel
    Tag = -700
    Left = 76
    Top = 14
    Width = 49
    Height = 23
    Caption = '5.123'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -21
    Font.Name = 'Times New Roman'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label6: TLabel
    Tag = -700
    Left = 360
    Top = 292
    Width = 48
    Height = 16
    Caption = 'e-mail:'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label8: TLabel
    Tag = -700
    Left = 188
    Top = 220
    Width = 30
    Height = 13
    Caption = '(none)'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWhite
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label9: TLabel
    Tag = -700
    Left = 410
    Top = 292
    Width = 153
    Height = 16
    Cursor = crHandPoint
    Caption = 'raskon@adgroup.com.ua'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsUnderline]
    ParentFont = False
    Transparent = True
    OnClick = Label9Click
  end
  object Label10: TLabel
    Left = 28
    Top = 220
    Width = 104
    Height = 13
    Caption = 'Физическая память'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label11: TLabel
    Left = 28
    Top = 232
    Width = 153
    Height = 13
    Caption = 'Загрузка памяти компьютера'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label12: TLabel
    Tag = -700
    Left = 188
    Top = 232
    Width = 30
    Height = 13
    Caption = '(none)'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label14: TLabel
    Tag = -700
    Left = 412
    Top = 256
    Width = 128
    Height = 16
    Cursor = crHandPoint
    Caption = 'www.adgroup.com.ua'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsUnderline]
    ParentFont = False
    Transparent = True
    OnClick = Label14Click
  end
  object Label2: TLabel
    Tag = -700
    Left = 220
    Top = 404
    Width = 22
    Height = 16
    Caption = 'tel:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label4: TLabel
    Tag = -700
    Left = 244
    Top = 404
    Width = 105
    Height = 16
    Caption = '+38 044 585-4-585'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label7: TLabel
    Left = 28
    Top = 256
    Width = 75
    Height = 16
    Caption = 'Контакты:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold, fsUnderline]
    ParentFont = False
    Transparent = True
  end
  object Label15: TLabel
    Left = 28
    Top = 292
    Width = 103
    Height = 16
    Caption = 'Разработчик:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label16: TLabel
    Tag = -700
    Left = 220
    Top = 292
    Width = 129
    Height = 16
    Caption = 'Курбацкий Алексей'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label17: TLabel
    Tag = -700
    Left = 220
    Top = 312
    Width = 123
    Height = 16
    Caption = 'Кривошеев Сергей'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label20: TLabel
    Tag = -700
    Left = 220
    Top = 420
    Width = 22
    Height = 16
    Caption = 'tel:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label21: TLabel
    Tag = -700
    Left = 244
    Top = 420
    Width = 105
    Height = 16
    Caption = '+38 067 440-53-40'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label22: TLabel
    Left = 28
    Top = 312
    Width = 154
    Height = 16
    Caption = 'Консультационная и'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label18: TLabel
    Left = 28
    Top = 328
    Width = 183
    Height = 16
    Caption = 'техническая поддержка:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label19: TLabel
    Left = 28
    Top = 348
    Width = 110
    Height = 16
    Caption = 'Отдел продаж:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label23: TLabel
    Tag = -700
    Left = 220
    Top = 348
    Width = 134
    Height = 16
    Caption = 'Боховко Константин'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label26: TLabel
    Tag = -700
    Left = 220
    Top = 436
    Width = 22
    Height = 16
    Caption = 'tel:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label27: TLabel
    Tag = -700
    Left = 244
    Top = 436
    Width = 105
    Height = 16
    Caption = '+38 050 440-30-30'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label28: TLabel
    Left = 28
    Top = 488
    Width = 426
    Height = 16
    Caption = 'Режим работы с 9:00 до 18:00 с понедельника по пятницу.'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold, fsUnderline]
    ParentFont = False
    Transparent = True
  end
  object Label13: TLabel
    Tag = -700
    Left = 28
    Top = 144
    Width = 319
    Height = 16
    Caption = 'Авторское свидетельство № 19134 от 10.01.2007г.'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label29: TLabel
    Tag = -700
    Left = 410
    Top = 312
    Width = 157
    Height = 16
    Cursor = crHandPoint
    Caption = 'support@adgroup.com.ua'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsUnderline]
    ParentFont = False
    Transparent = True
    OnClick = Label9Click
  end
  object Label30: TLabel
    Tag = -700
    Left = 360
    Top = 312
    Width = 48
    Height = 16
    Caption = 'e-mail:'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label32: TLabel
    Tag = -700
    Left = 410
    Top = 348
    Width = 144
    Height = 16
    Cursor = crHandPoint
    Caption = 'order@adgroup.com.ua'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsUnderline]
    ParentFont = False
    Transparent = True
    OnClick = Label9Click
  end
  object Label33: TLabel
    Tag = -700
    Left = 360
    Top = 348
    Width = 48
    Height = 16
    Caption = 'e-mail:'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label24: TLabel
    Tag = -700
    Left = 220
    Top = 328
    Width = 122
    Height = 16
    Caption = 'Ковальчук Андрей'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label34: TLabel
    Left = 348
    Top = 220
    Width = 30
    Height = 13
    Caption = '(none)'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label25: TLabel
    Left = 348
    Top = 232
    Width = 30
    Height = 13
    Caption = '(none)'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label35: TLabel
    Left = 28
    Top = 384
    Width = 488
    Height = 16
    Caption = 'Телефонные номера единые для всех представителей компании:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label36: TLabel
    Tag = -700
    Left = 372
    Top = 256
    Width = 35
    Height = 16
    Caption = 'www:'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label37: TLabel
    Tag = -700
    Left = 244
    Top = 452
    Width = 82
    Height = 16
    Caption = 'ADGroupKiev'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label38: TLabel
    Tag = -700
    Left = 196
    Top = 452
    Width = 47
    Height = 16
    Caption = 'skype:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label39: TLabel
    Tag = -700
    Left = 244
    Top = 468
    Width = 130
    Height = 16
    Caption = 'sip@adgroup.com.ua'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    ParentFont = False
    Transparent = True
  end
  object Label40: TLabel
    Tag = -700
    Left = 172
    Top = 468
    Width = 72
    Height = 16
    Caption = 'direct SIP:'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label41: TLabel
    Tag = -700
    Left = 356
    Top = 404
    Width = 135
    Height = 16
    Caption = '(многоканальный)'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label42: TLabel
    Tag = -700
    Left = 356
    Top = 436
    Width = 43
    Height = 16
    Caption = '(MTS)'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label43: TLabel
    Tag = -700
    Left = 356
    Top = 420
    Width = 67
    Height = 16
    Caption = '(Kievstar)'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label44: TLabel
    Tag = -700
    Left = 356
    Top = 452
    Width = 193
    Height = 16
    Caption = '(только звонки, без чата)'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label45: TLabel
    Tag = -700
    Left = 412
    Top = 272
    Width = 165
    Height = 16
    Cursor = crHandPoint
    Caption = 'www.adgroup.com.ua/forum'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clAqua
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsUnderline]
    ParentFont = False
    Transparent = True
    OnClick = Label14Click
  end
  object Label46: TLabel
    Tag = -700
    Left = 364
    Top = 272
    Width = 43
    Height = 16
    Caption = 'forum:'
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWhite
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object Label31: TLabel
    Tag = -700
    Left = 28
    Top = 34
    Width = 5
    Height = 23
    Color = clBlack
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWhite
    Font.Height = -21
    Font.Name = 'Times New Roman'
    Font.Style = []
    ParentColor = False
    ParentFont = False
    Transparent = True
    Visible = False
  end
  object Timer1: TTimer
    Enabled = False
    Interval = 300
    OnTimer = Timer1Timer
    Left = 548
    Top = 472
  end
end
