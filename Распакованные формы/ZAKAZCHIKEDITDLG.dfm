object ZakazchikEditDlg: TZakazchikEditDlg
  Left = 286
  Top = 269
  BorderStyle = bsDialog
  Caption = 'Редактор заказчика'
  ClientHeight = 545
  ClientWidth = 563
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 36
    Top = 8
    Width = 47
    Height = 13
    Caption = 'Заказчик'
    Transparent = True
  end
  object Label2: TLabel
    Left = 40
    Top = 56
    Width = 44
    Height = 13
    Caption = 'Телефон'
    Transparent = True
  end
  object Label3: TLabel
    Left = 280
    Top = 56
    Width = 51
    Height = 13
    Caption = 'Код ЗКПО'
    Transparent = True
  end
  object Label4: TLabel
    Left = 308
    Top = 80
    Width = 24
    Height = 13
    Caption = 'Банк'
    Transparent = True
  end
  object Label5: TLabel
    Left = 316
    Top = 104
    Width = 15
    Height = 13
    Caption = 'р/с'
    Transparent = True
  end
  object Label6: TLabel
    Left = 304
    Top = 128
    Width = 24
    Height = 13
    Caption = 'МФО'
    Transparent = True
  end
  object Label7: TLabel
    Left = 32
    Top = 80
    Width = 53
    Height = 13
    Caption = 'Юр. адрес'
    Transparent = True
  end
  object Label8: TLabel
    Left = 24
    Top = 152
    Width = 61
    Height = 13
    Caption = 'Примечание'
    Transparent = True
  end
  object Label9: TLabel
    Left = 312
    Top = 8
    Width = 18
    Height = 13
    Caption = 'Тип'
    Transparent = True
  end
  object Label10: TLabel
    Left = 52
    Top = 32
    Width = 31
    Height = 13
    Caption = 'Город'
    Transparent = True
  end
  object SpeedButton2: TSpeedButton
    Left = 280
    Top = 172
    Width = 23
    Height = 22
    Hint = 'выбрать цвет поля'
    Caption = '...'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'Tahoma'
    Font.Style = [fsBold]
    ParentFont = False
    OnClick = SpeedButton2Click
  end
  object SpeedButton3: TSpeedButton
    Left = 304
    Top = 172
    Width = 23
    Height = 22
    Hint = 'отказаться от цвета'
    Caption = 'X'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'Tahoma'
    Font.Style = [fsBold]
    ParentFont = False
    OnClick = SpeedButton2Click
  end
  object SpeedButton5: TSpeedButton
    Left = 304
    Top = 196
    Width = 23
    Height = 22
    Hint = 'отказаться от цвета'
    Caption = 'X'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'Tahoma'
    Font.Style = [fsBold]
    ParentFont = False
    OnClick = SpeedButton2Click
  end
  object SpeedButton4: TSpeedButton
    Left = 280
    Top = 196
    Width = 23
    Height = 22
    Hint = 'выбрать цвет поля'
    Caption = '...'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'Tahoma'
    Font.Style = [fsBold]
    ParentFont = False
    OnClick = SpeedButton2Click
  end
  object SpeedButton7: TSpeedButton
    Left = 304
    Top = 220
    Width = 23
    Height = 22
    Hint = 'отказаться от цвета'
    Caption = 'X'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'Tahoma'
    Font.Style = [fsBold]
    ParentFont = False
    OnClick = SpeedButton2Click
  end
  object SpeedButton6: TSpeedButton
    Left = 280
    Top = 220
    Width = 23
    Height = 22
    Hint = 'выбрать цвет поля'
    Caption = '...'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'Tahoma'
    Font.Style = [fsBold]
    ParentFont = False
    OnClick = SpeedButton2Click
  end
  object SpeedButton9: TSpeedButton
    Left = 304
    Top = 244
    Width = 23
    Height = 22
    Hint = 'отказаться от цвета'
    Caption = 'X'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'Tahoma'
    Font.Style = [fsBold]
    ParentFont = False
    OnClick = SpeedButton2Click
  end
  object SpeedButton8: TSpeedButton
    Left = 280
    Top = 244
    Width = 23
    Height = 22
    Hint = 'выбрать цвет поля'
    Caption = '...'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'Tahoma'
    Font.Style = [fsBold]
    ParentFont = False
    OnClick = SpeedButton2Click
  end
  object OKBtn: TButton
    Left = 484
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 484
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object Edit1: TEdit
    Left = 88
    Top = 4
    Width = 189
    Height = 21
    TabOrder = 2
  end
  object Edit2: TEdit
    Left = 88
    Top = 52
    Width = 125
    Height = 21
    TabOrder = 3
  end
  object Edit3: TEdit
    Left = 336
    Top = 52
    Width = 124
    Height = 21
    TabOrder = 4
  end
  object Edit4: TEdit
    Left = 336
    Top = 76
    Width = 124
    Height = 21
    TabOrder = 5
  end
  object Edit5: TEdit
    Left = 336
    Top = 100
    Width = 124
    Height = 21
    TabOrder = 6
  end
  object Edit6: TEdit
    Left = 336
    Top = 124
    Width = 124
    Height = 21
    TabOrder = 7
  end
  object Edit7: TEdit
    Left = 88
    Top = 76
    Width = 189
    Height = 21
    TabOrder = 8
  end
  object Edit8: TEdit
    Left = 88
    Top = 148
    Width = 469
    Height = 21
    TabOrder = 9
  end
  object ComboBox1: TComboBox
    Left = 336
    Top = 4
    Width = 125
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 10
    OnChange = ComboBox1Change
  end
  object Edit9: TEdit
    Left = 88
    Top = 28
    Width = 125
    Height = 21
    TabOrder = 11
  end
  object LabeledEdit1: TLabeledEdit
    Left = 88
    Top = 100
    Width = 189
    Height = 21
    EditLabel.Width = 28
    EditLabel.Height = 13
    EditLabel.Caption = 'e-mail'
    LabelPosition = lpLeft
    TabOrder = 12
  end
  object LabeledEdit2: TLabeledEdit
    Left = 88
    Top = 124
    Width = 189
    Height = 21
    EditLabel.Width = 30
    EditLabel.Height = 13
    EditLabel.Caption = 'WWW'
    LabelPosition = lpLeft
    TabOrder = 13
  end
  object GroupBox1: TGroupBox
    Left = 4
    Top = 268
    Width = 337
    Height = 273
    Caption = 'Дилер'
    TabOrder = 14
    object SpeedButton1: TSpeedButton
      Left = 104
      Top = 16
      Width = 101
      Height = 22
      Caption = 'Сформировать'
      OnClick = SpeedButton1Click
    end
    object LabeledEdit3: TLabeledEdit
      Left = 32
      Top = 16
      Width = 69
      Height = 21
      EditLabel.Width = 20
      EditLabel.Height = 13
      EditLabel.Caption = 'Код'
      LabelPosition = lpLeft
      TabOrder = 0
      OnChange = DateTimePicker1Change
    end
    object LView: TADGListView
      Left = 8
      Top = 100
      Width = 321
      Height = 137
      Columns = <>
      ReadOnly = False
      TabOrder = 1
      OnCheckClick = LViewCheckClick
    end
    object LabeledEdit4: TLabeledEdit
      Left = 48
      Top = 244
      Width = 281
      Height = 21
      EditLabel.Width = 37
      EditLabel.Height = 13
      EditLabel.Caption = 'Пароль'
      LabelPosition = lpLeft
      ReadOnly = True
      TabOrder = 2
    end
    object CheckBox1: TCheckBox
      Left = 8
      Top = 80
      Width = 313
      Height = 17
      Caption = 'Ограничить выбор шаблонов заказа'
      TabOrder = 3
      OnClick = CheckBox2Click
    end
    object CheckBox2: TCheckBox
      Left = 8
      Top = 40
      Width = 181
      Height = 17
      Caption = 'Ограничить срок действия'
      TabOrder = 4
      OnClick = CheckBox2Click
    end
    object DateTimePicker1: TDateTimePicker
      Left = 236
      Top = 40
      Width = 93
      Height = 21
      Date = 40283.565787557870000000
      Time = 40283.565787557870000000
      TabOrder = 5
      OnChange = DateTimePicker1Change
    end
    object CheckBox3: TCheckBox
      Left = 8
      Top = 60
      Width = 225
      Height = 17
      Hint = 
        'действует только в безлимитной дилерской программе и при включен' +
        'ной защите от копирования '
      Caption = 'Привязать к компьютеру'
      TabOrder = 6
      OnClick = CheckBox2Click
    end
    object Edit10: TEdit
      Left = 236
      Top = 60
      Width = 93
      Height = 21
      TabOrder = 7
      OnChange = DateTimePicker1Change
    end
  end
  object CheckBox4: TCheckBox
    Left = 336
    Top = 32
    Width = 97
    Height = 17
    Caption = 'Заблокировать'
    TabOrder = 15
  end
  object LabeledEdit5: TLabeledEdit
    Left = 88
    Top = 172
    Width = 189
    Height = 21
    EditLabel.Width = 72
    EditLabel.Height = 13
    EditLabel.Caption = 'Информация 1'
    LabelPosition = lpLeft
    TabOrder = 16
  end
  object LabeledEdit6: TLabeledEdit
    Left = 88
    Top = 196
    Width = 189
    Height = 21
    EditLabel.Width = 72
    EditLabel.Height = 13
    EditLabel.Caption = 'Информация 2'
    LabelPosition = lpLeft
    TabOrder = 17
  end
  object LabeledEdit7: TLabeledEdit
    Left = 88
    Top = 220
    Width = 189
    Height = 21
    EditLabel.Width = 72
    EditLabel.Height = 13
    EditLabel.Caption = 'Информация 3'
    LabelPosition = lpLeft
    TabOrder = 18
  end
  object LabeledEdit8: TLabeledEdit
    Left = 88
    Top = 244
    Width = 189
    Height = 21
    EditLabel.Width = 72
    EditLabel.Height = 13
    EditLabel.Caption = 'Информация 4'
    LabelPosition = lpLeft
    TabOrder = 19
  end
end
