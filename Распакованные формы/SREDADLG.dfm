object SredaDlg: TSredaDlg
  Left = 353
  Top = 326
  BorderStyle = bsDialog
  Caption = 'Настройки среды'
  ClientHeight = 578
  ClientWidth = 658
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
  object OKBtn: TButton
    Left = 576
    Top = 548
    Width = 77
    Height = 25
    Caption = 'Закрыть'
    Default = True
    ModalResult = 1
    TabOrder = 0
    OnClick = OKBtnClick
  end
  object Edit12: TEdit
    Left = 4
    Top = 552
    Width = 45
    Height = 21
    Cursor = crArrow
    TabStop = False
    BorderStyle = bsNone
    Color = clBtnFace
    Ctl3D = True
    ParentCtl3D = False
    PasswordChar = ' '
    TabOrder = 1
    Visible = False
    OnChange = Edit12Change
  end
  object PageControl1: TPageControl
    Left = 0
    Top = 0
    Width = 657
    Height = 545
    ActivePage = TabSheet7
    TabOrder = 2
    object TabSheet1: TTabSheet
      Caption = 'Общие'
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object Label4: TLabel
        Left = 80
        Top = 0
        Width = 206
        Height = 13
        Caption = 'Реквизиты предприятия производителя'
        Transparent = True
      end
      object Label13: TLabel
        Left = 4
        Top = 20
        Width = 79
        Height = 13
        Caption = 'Производитель'
        Transparent = True
      end
      object Label22: TLabel
        Left = 52
        Top = 44
        Width = 31
        Height = 13
        Caption = 'Адрес'
        Transparent = True
      end
      object Label16: TLabel
        Left = 56
        Top = 68
        Width = 24
        Height = 13
        Caption = 'Р/СЧ'
        Transparent = True
      end
      object Label17: TLabel
        Left = 56
        Top = 92
        Width = 24
        Height = 13
        Caption = 'МФО'
        Transparent = True
      end
      object Label14: TLabel
        Left = 64
        Top = 116
        Width = 20
        Height = 13
        Caption = 'Код'
        Transparent = True
      end
      object Label15: TLabel
        Left = 60
        Top = 140
        Width = 24
        Height = 13
        Caption = 'Банк'
        Transparent = True
      end
      object Label2: TLabel
        Left = 24
        Top = 192
        Width = 62
        Height = 13
        Caption = 'Обновления'
        Transparent = True
        Visible = False
      end
      object Label1: TLabel
        Left = 12
        Top = 168
        Width = 76
        Height = 13
        Caption = 'Копия архивов'
        Transparent = True
      end
      object Label46: TLabel
        Left = 372
        Top = 44
        Width = 183
        Height = 13
        Caption = 'На листе заказа суммы указывать в'
        Transparent = True
      end
      object Label5: TLabel
        Left = 447
        Top = 20
        Width = 115
        Height = 13
        Caption = 'Национальная валюта'
        Transparent = True
      end
      object Label10: TLabel
        Left = 299
        Top = 92
        Width = 22
        Height = 13
        Caption = 'НДС'
        Transparent = True
      end
      object Label19: TLabel
        Left = 483
        Top = 192
        Width = 26
        Height = 13
        AutoSize = False
        Caption = 'Язык'
        Transparent = True
      end
      object Edit1: TEdit
        Left = 88
        Top = 16
        Width = 185
        Height = 21
        TabOrder = 0
        OnChange = Edit1Exit
      end
      object Edit2: TEdit
        Left = 88
        Top = 40
        Width = 185
        Height = 21
        TabOrder = 1
        OnChange = Edit1Exit
      end
      object Edit3: TEdit
        Left = 88
        Top = 64
        Width = 185
        Height = 21
        TabOrder = 2
        OnChange = Edit1Exit
      end
      object Edit4: TEdit
        Left = 88
        Top = 88
        Width = 185
        Height = 21
        TabOrder = 3
        OnChange = Edit1Exit
      end
      object Edit5: TEdit
        Left = 88
        Top = 112
        Width = 185
        Height = 21
        TabOrder = 4
        OnChange = Edit1Exit
      end
      object Edit6: TEdit
        Left = 88
        Top = 136
        Width = 185
        Height = 21
        TabOrder = 5
        OnChange = Edit1Exit
      end
      object BtnEdit1: TBtnEdit
        Left = 88
        Top = 164
        Width = 289
        Height = 21
        TabOrder = 6
        OnChange = Edit1Exit
        OnBtnClick = BtnEdit1BtnClick
        OnDblClick = BtnEdit1BtnClick
      end
      object BtnEdit3: TBtnEdit
        Left = 88
        Top = 188
        Width = 289
        Height = 21
        ReadOnly = True
        TabOrder = 7
        Visible = False
        OnBtnClick = BtnEdit1BtnClick
        OnDblClick = BtnEdit1BtnClick
      end
      object LEdit5: TLabeledEdit
        Left = 564
        Top = 116
        Width = 49
        Height = 21
        EditLabel.Width = 160
        EditLabel.Height = 13
        EditLabel.Caption = 'Установка импоста кратно, мм.'
        EditLabel.Transparent = True
        LabelPosition = lpLeft
        TabOrder = 8
        OnChange = Edit1Exit
      end
      object eNDS: TLabeledEdit
        Left = 564
        Top = 64
        Width = 49
        Height = 21
        EditLabel.Width = 44
        EditLabel.Height = 13
        EditLabel.Caption = 'НДС (%)'
        EditLabel.Transparent = True
        LabelPosition = lpLeft
        TabOrder = 9
        OnChange = Edit1Exit
      end
      object ValutaLocBox: TComboBox
        Left = 564
        Top = 40
        Width = 69
        Height = 22
        Style = csOwnerDrawFixed
        Ctl3D = False
        DropDownCount = 20
        ItemHeight = 16
        ParentCtl3D = False
        TabOrder = 10
        OnChange = ValutaLocBoxChange
      end
      object ValutaGenBox: TComboBox
        Left = 564
        Top = 16
        Width = 69
        Height = 22
        Style = csOwnerDrawFixed
        Ctl3D = False
        DropDownCount = 20
        ItemHeight = 16
        ParentCtl3D = False
        TabOrder = 11
        OnChange = ValutaLocBoxChange
      end
      object LabeledEdit10: TLabeledEdit
        Left = 564
        Top = 140
        Width = 49
        Height = 21
        EditLabel.Width = 91
        EditLabel.Height = 13
        EditLabel.Caption = 'Архив базы, дней'
        LabelPosition = lpLeft
        TabOrder = 12
        OnChange = Edit1Exit
      end
      object NDSBox: TComboBox
        Left = 328
        Top = 88
        Width = 305
        Height = 22
        Style = csOwnerDrawFixed
        Ctl3D = False
        ItemHeight = 16
        ParentCtl3D = False
        TabOrder = 13
        OnChange = ComboBox1Change
        Items.Strings = (
          'не использовать'
          'суммы с НДС, отображать и считать как % от суммы'
          'суммы без НДС, отображать и считать отдельно')
      end
      object CheckBox1: TCheckBox
        Left = 4
        Top = 476
        Width = 333
        Height = 17
        Caption = 'Автоматическая "упаковка" базы при выходе из программы'
        TabOrder = 14
        Visible = False
        OnClick = CheckBox1Click
      end
      object ComboBox3: TComboBox
        Left = 512
        Top = 188
        Width = 121
        Height = 22
        Style = csOwnerDrawFixed
        Ctl3D = False
        ItemHeight = 16
        ParentCtl3D = False
        TabOrder = 15
        OnChange = ComboBox1Change
      end
      object LabeledEdit30: TLabeledEdit
        Left = 564
        Top = 164
        Width = 49
        Height = 21
        EditLabel.Width = 144
        EditLabel.Height = 13
        EditLabel.Caption = 'Точность отображения сумм'
        EditLabel.Transparent = True
        LabelPosition = lpLeft
        TabOrder = 16
        OnChange = Edit1Exit
      end
      object GroupBox14: TGroupBox
        Left = 4
        Top = 220
        Width = 629
        Height = 89
        Caption = 'Оформление'
        TabOrder = 17
        object Shape1: TShape
          Left = 336
          Top = 60
          Width = 121
          Height = 21
        end
        object SpeedButton6: TSpeedButton
          Left = 460
          Top = 60
          Width = 97
          Height = 22
          Caption = 'Выбрать цвет'
          OnClick = SpeedButton6Click
        end
        object Label12: TLabel
          Left = 8
          Top = 64
          Width = 322
          Height = 13
          Caption = 'Цвет "выделенных" элементов в списке элементов на чертеже'
        end
        object CheckBox14: TCheckBox
          Left = 8
          Top = 16
          Width = 605
          Height = 17
          Caption = 
            'Сделать цвет фона "листа заказа" серым (игнорировать цветовые на' +
            'стройки Windows)'
          TabOrder = 0
          OnClick = CheckBox1Click
        end
        object CheckBox25: TCheckBox
          Left = 8
          Top = 36
          Width = 405
          Height = 17
          Caption = 'Установить размер шрифта для списков'
          TabOrder = 1
        end
        object LabeledEdit46: TLabeledEdit
          Left = 568
          Top = 36
          Width = 49
          Height = 21
          EditLabel.Width = 78
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер шрифта'
          LabelPosition = lpLeft
          TabOrder = 2
        end
      end
    end
    object TabSheet2: TTabSheet
      Caption = 'Раскрой'
      ImageIndex = 1
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object GroupBox5: TGroupBox
        Left = 4
        Top = 0
        Width = 641
        Height = 353
        Caption = 'Раскрой профиля'
        TabOrder = 0
        object Label9: TLabel
          Left = 8
          Top = 44
          Width = 153
          Height = 13
          Caption = 'Обработка обрезков профиля'
        end
        object Label8: TLabel
          Left = 8
          Top = 20
          Width = 134
          Height = 13
          Caption = 'Уровень раскроя профиля'
        end
        object Label26: TLabel
          Left = 536
          Top = 62
          Width = 99
          Height = 13
          Caption = 'Обозначение углов'
          Visible = False
        end
        object SpeedButton4: TSpeedButton
          Left = 404
          Top = 16
          Width = 233
          Height = 21
          Caption = 'Изменить нумерацию обрезков'
          OnClick = SpeedButton4Click
        end
        object CheckBox5: TCheckBox
          Left = 8
          Top = 88
          Width = 533
          Height = 17
          Caption = 
            'Делать параллельный раскрой профиля на две палки (для двухголово' +
            'чной пилы)'
          TabOrder = 0
          OnClick = CheckBox1Click
        end
        object CheckBox2: TCheckBox
          Left = 8
          Top = 68
          Width = 513
          Height = 17
          Caption = 
            'Не допускать попадание длины обрезков профиля в зону между "нели' +
            'квидом" и "мусором"'
          TabOrder = 1
          OnClick = CheckBox1Click
        end
        object ComboBox6: TComboBox
          Left = 564
          Top = 76
          Width = 73
          Height = 30
          Style = csOwnerDrawFixed
          ItemHeight = 24
          TabOrder = 2
          Visible = False
          OnChange = ComboBox1Change
          OnDrawItem = ComboBox6DrawItem
        end
        object ComboBox2: TComboBox
          Left = 164
          Top = 40
          Width = 473
          Height = 21
          Style = csDropDownList
          ItemHeight = 0
          TabOrder = 3
        end
        object ComboBox1: TComboBox
          Left = 164
          Top = 16
          Width = 173
          Height = 21
          Style = csDropDownList
          ItemHeight = 0
          TabOrder = 4
          OnChange = ComboBox1Change
        end
        object CheckBox15: TCheckBox
          Left = 8
          Top = 108
          Width = 617
          Height = 17
          Caption = 
            'При параллельном раскрое разбивать на две палки только левые и п' +
            'равые, верхние и нижние одинаковые детали'
          TabOrder = 5
          OnClick = CheckBox1Click
        end
        object CheckBox10: TCheckBox
          Left = 8
          Top = 128
          Width = 617
          Height = 17
          Caption = 'Армирование кроить дискретно 50 мм.'
          TabOrder = 6
          OnClick = CheckBox1Click
        end
        object GroupBox12: TGroupBox
          Left = 196
          Top = 208
          Width = 437
          Height = 141
          Caption = 'Количество ячеек в накопителе'
          TabOrder = 7
          object LabeledEdit31: TLabeledEdit
            Left = 104
            Top = 16
            Width = 37
            Height = 21
            EditLabel.Width = 24
            EditLabel.Height = 13
            EditLabel.Caption = 'рама'
            LabelPosition = lpLeft
            TabOrder = 0
          end
          object LabeledEdit32: TLabeledEdit
            Left = 104
            Top = 40
            Width = 37
            Height = 21
            EditLabel.Width = 41
            EditLabel.Height = 13
            EditLabel.Caption = 'створка'
            LabelPosition = lpLeft
            TabOrder = 1
          end
          object LabeledEdit33: TLabeledEdit
            Left = 104
            Top = 64
            Width = 37
            Height = 21
            EditLabel.Width = 35
            EditLabel.Height = 13
            EditLabel.Caption = 'импост'
            LabelPosition = lpLeft
            TabOrder = 2
          end
          object LabeledEdit34: TLabeledEdit
            Left = 104
            Top = 88
            Width = 37
            Height = 21
            EditLabel.Width = 85
            EditLabel.Height = 13
            EditLabel.Caption = 'москитная сетка'
            LabelPosition = lpLeft
            TabOrder = 3
          end
          object LabeledEdit45: TLabeledEdit
            Left = 104
            Top = 112
            Width = 37
            Height = 21
            EditLabel.Width = 72
            EditLabel.Height = 13
            EditLabel.Caption = 'дистанционер'
            LabelPosition = lpLeft
            TabOrder = 4
          end
          object LabeledEdit47: TLabeledEdit
            Left = 224
            Top = 16
            Width = 37
            Height = 21
            EditLabel.Width = 38
            EditLabel.Height = 13
            EditLabel.Caption = 'штапик'
            LabelPosition = lpLeft
            TabOrder = 5
          end
          object LabeledEdit48: TLabeledEdit
            Left = 224
            Top = 64
            Width = 37
            Height = 21
            EditLabel.Width = 56
            EditLabel.Height = 13
            EditLabel.Caption = 'фурнитура'
            LabelPosition = lpLeft
            TabOrder = 6
          end
          object LabeledEdit54: TLabeledEdit
            Left = 224
            Top = 88
            Width = 37
            Height = 21
            EditLabel.Width = 73
            EditLabel.Height = 13
            EditLabel.Caption = 'стеклопакеты'
            LabelPosition = lpLeft
            TabOrder = 7
          end
          object LabeledEdit55: TLabeledEdit
            Left = 224
            Top = 40
            Width = 37
            Height = 21
            EditLabel.Width = 74
            EditLabel.Height = 13
            EditLabel.Caption = 'подставочный'
            LabelPosition = lpLeft
            TabOrder = 8
          end
          object LabeledEdit60: TLabeledEdit
            Left = 392
            Top = 16
            Width = 37
            Height = 21
            EditLabel.Width = 111
            EditLabel.Height = 13
            EditLabel.Caption = 'готовые конструкции'
            LabelPosition = lpLeft
            TabOrder = 9
          end
          object LabeledEdit61: TLabeledEdit
            Left = 392
            Top = 40
            Width = 37
            Height = 21
            EditLabel.Width = 121
            EditLabel.Height = 13
            EditLabel.Hint = 'готовые дополнительные элементы'
            EditLabel.Caption = 'готовые доп. элементы'
            LabelPosition = lpLeft
            TabOrder = 10
          end
        end
        object GroupBox13: TGroupBox
          Left = 4
          Top = 208
          Width = 189
          Height = 141
          Caption = 'Сортировка профилей при печати'
          TabOrder = 8
          object LView2: TADGListView
            Left = 8
            Top = 16
            Width = 173
            Height = 117
            Columns = <>
            ReadOnly = False
            TabOrder = 0
            OnDblClick = LView2DblClick
          end
        end
        object CheckBox21: TCheckBox
          Left = 8
          Top = 148
          Width = 617
          Height = 17
          Caption = 
            'Детали створки складывать вместе с деталями рамы в одном ячеечно' +
            'м накопителе'
          TabOrder = 9
        end
        object CheckBox22: TCheckBox
          Left = 8
          Top = 168
          Width = 617
          Height = 17
          Caption = 
            'Выполнять сортировку таким образом, чтоб прутья с более длинными' +
            ' первыми деталями шли в начале раскроя'
          TabOrder = 10
        end
        object CheckBox30: TCheckBox
          Left = 8
          Top = 188
          Width = 617
          Height = 17
          Caption = 'Детали штапика на одну конструкцию складывать в одну ячейку'
          TabOrder = 11
        end
      end
      object GroupBox6: TGroupBox
        Left = 4
        Top = 352
        Width = 641
        Height = 97
        Caption = 'Раскрой стекла'
        TabOrder = 1
        object Label21: TLabel
          Left = 9
          Top = 20
          Width = 284
          Height = 13
          Caption = 'Неликвидный обрезок, если длина стороны меньше, мм'
          Transparent = True
        end
        object Label3: TLabel
          Left = 9
          Top = 44
          Width = 267
          Height = 13
          Caption = 'Неликвидный обрезок, если площадь меньше, м.кв.'
          Transparent = True
        end
        object Label23: TLabel
          Left = 8
          Top = 72
          Width = 144
          Height = 13
          Caption = 'Обработка обрезков стекла'
        end
        object Edit14: TEdit
          Left = 296
          Top = 40
          Width = 53
          Height = 21
          Ctl3D = True
          ParentCtl3D = False
          TabOrder = 0
          OnChange = Edit1Exit
        end
        object Edit11: TEdit
          Left = 296
          Top = 16
          Width = 53
          Height = 21
          Ctl3D = True
          ParentCtl3D = False
          TabOrder = 1
          OnChange = Edit1Exit
        end
        object ComboBox4: TComboBox
          Left = 156
          Top = 68
          Width = 481
          Height = 21
          Style = csDropDownList
          ItemHeight = 0
          TabOrder = 2
        end
      end
      object GroupBox7: TGroupBox
        Left = 4
        Top = 448
        Width = 377
        Height = 65
        Caption = 'Общие'
        TabOrder = 2
        object Label24: TLabel
          Left = 8
          Top = 20
          Width = 102
          Height = 13
          Caption = 'Кодировка деталей'
        end
        object ComboBox5: TComboBox
          Left = 116
          Top = 16
          Width = 257
          Height = 21
          Style = csDropDownList
          ItemHeight = 0
          TabOrder = 0
          OnChange = ComboBox1Change
        end
        object CheckBox6: TCheckBox
          Left = 8
          Top = 40
          Width = 353
          Height = 17
          Caption = 'Использовать только английские символы в кодировке деталей'
          TabOrder = 1
          OnClick = CheckBox1Click
        end
      end
      object GroupBox16: TGroupBox
        Left = 388
        Top = 448
        Width = 257
        Height = 65
        Caption = 'Негабаритные конструкции'
        TabOrder = 3
        object LabeledEdit49: TLabeledEdit
          Left = 112
          Top = 16
          Width = 33
          Height = 21
          EditLabel.Width = 102
          EditLabel.Height = 13
          EditLabel.Caption = 'ширина меньше, мм.'
          LabelPosition = lpLeft
          TabOrder = 0
        end
        object LabeledEdit50: TLabeledEdit
          Left = 112
          Top = 40
          Width = 33
          Height = 21
          EditLabel.Width = 101
          EditLabel.Height = 13
          EditLabel.Caption = 'высота меньше, мм.'
          LabelPosition = lpLeft
          TabOrder = 1
        end
        object LabeledEdit51: TLabeledEdit
          Left = 216
          Top = 40
          Width = 33
          Height = 21
          EditLabel.Width = 61
          EditLabel.Height = 13
          EditLabel.Caption = 'больше, мм.'
          LabelPosition = lpLeft
          TabOrder = 2
        end
        object LabeledEdit52: TLabeledEdit
          Left = 216
          Top = 16
          Width = 33
          Height = 21
          EditLabel.Width = 61
          EditLabel.Height = 13
          EditLabel.Caption = 'больше, мм.'
          LabelPosition = lpLeft
          TabOrder = 3
        end
      end
    end
    object TabSheet3: TTabSheet
      Caption = 'Корпоративный узел'
      ImageIndex = 2
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object GroupBox1: TGroupBox
        Left = 3
        Top = 3
        Width = 462
        Height = 166
        Caption = 'Отправка почты'
        TabOrder = 0
        object LabeledEdit1: TLabeledEdit
          Left = 124
          Top = 16
          Width = 237
          Height = 21
          EditLabel.Width = 94
          EditLabel.Height = 13
          EditLabel.Caption = 'Адрес получателя'
          LabelPosition = lpLeft
          TabOrder = 0
        end
        object LabeledEdit2: TLabeledEdit
          Left = 124
          Top = 64
          Width = 237
          Height = 21
          EditLabel.Width = 64
          EditLabel.Height = 13
          EditLabel.Caption = 'SMTP сервер'
          LabelPosition = lpLeft
          TabOrder = 1
        end
        object LabeledEdit5: TLabeledEdit
          Left = 396
          Top = 64
          Width = 37
          Height = 21
          EditLabel.Width = 24
          EditLabel.Height = 13
          EditLabel.Caption = 'порт'
          LabelPosition = lpLeft
          TabOrder = 2
        end
        object LabeledEdit13: TLabeledEdit
          Left = 124
          Top = 114
          Width = 109
          Height = 21
          EditLabel.Width = 72
          EditLabel.Height = 13
          EditLabel.Caption = 'Пользователь'
          LabelPosition = lpLeft
          TabOrder = 3
        end
        object LabeledEdit14: TLabeledEdit
          Left = 124
          Top = 138
          Width = 109
          Height = 21
          EditLabel.Width = 37
          EditLabel.Height = 13
          EditLabel.Caption = 'Пароль'
          LabelPosition = lpLeft
          PasswordChar = '*'
          TabOrder = 4
        end
        object CheckBox11: TCheckBox
          Left = 124
          Top = 91
          Width = 309
          Height = 17
          Caption = 'SMTP сервер требует аутентификацию'
          TabOrder = 5
          OnClick = CheckBox1Click
        end
        object LabeledEdit8: TLabeledEdit
          Left = 124
          Top = 40
          Width = 237
          Height = 21
          EditLabel.Width = 100
          EditLabel.Height = 13
          EditLabel.Caption = 'Адрес отправителя'
          LabelPosition = lpLeft
          TabOrder = 6
        end
      end
      object GroupBox2: TGroupBox
        Left = 3
        Top = 171
        Width = 462
        Height = 94
        Caption = 'Получение почты'
        TabOrder = 1
        object LabeledEdit6: TLabeledEdit
          Left = 124
          Top = 16
          Width = 237
          Height = 21
          EditLabel.Width = 64
          EditLabel.Height = 13
          EditLabel.Caption = 'POP3 сервер'
          LabelPosition = lpLeft
          TabOrder = 0
        end
        object LabeledEdit3: TLabeledEdit
          Left = 124
          Top = 40
          Width = 109
          Height = 21
          EditLabel.Width = 72
          EditLabel.Height = 13
          EditLabel.Caption = 'Пользователь'
          LabelPosition = lpLeft
          TabOrder = 1
        end
        object LabeledEdit4: TLabeledEdit
          Left = 124
          Top = 64
          Width = 109
          Height = 21
          EditLabel.Width = 37
          EditLabel.Height = 13
          EditLabel.Caption = 'Пароль'
          LabelPosition = lpLeft
          PasswordChar = '*'
          TabOrder = 2
        end
        object LabeledEdit7: TLabeledEdit
          Left = 396
          Top = 15
          Width = 37
          Height = 21
          EditLabel.Width = 24
          EditLabel.Height = 13
          EditLabel.Caption = 'порт'
          LabelPosition = lpLeft
          TabOrder = 3
        end
      end
    end
    object TabSheet5: TTabSheet
      Caption = 'Печать'
      ImageIndex = 4
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object GroupBox3: TGroupBox
        Left = 308
        Top = 268
        Width = 297
        Height = 85
        Caption = 'Параметры отображения шапки документов'
        TabOrder = 0
        object Label11: TLabel
          Left = 80
          Top = 64
          Width = 105
          Height = 13
          Caption = 'Шапка не загружена'
          Font.Charset = DEFAULT_CHARSET
          Font.Color = clBlack
          Font.Height = -11
          Font.Name = 'MS Sans Serif'
          Font.Style = []
          ParentFont = False
          ParentShowHint = False
          ShowHint = False
          Transparent = True
        end
        object Label6: TLabel
          Left = 16
          Top = 64
          Width = 58
          Height = 13
          Caption = 'Состояние:'
        end
        object LEdit2: TLabeledEdit
          Left = 80
          Top = 40
          Width = 49
          Height = 21
          EditLabel.Width = 60
          EditLabel.Height = 13
          EditLabel.Caption = 'Высота, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 0
          OnChange = Edit1Exit
        end
        object LEdit4: TLabeledEdit
          Left = 236
          Top = 40
          Width = 49
          Height = 21
          EditLabel.Width = 98
          EditLabel.Height = 13
          EditLabel.Caption = 'Отступ сверху, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 1
          OnChange = Edit1Exit
        end
        object LEdit3: TLabeledEdit
          Left = 236
          Top = 16
          Width = 49
          Height = 21
          EditLabel.Width = 97
          EditLabel.Height = 13
          EditLabel.Caption = 'Остступ слева, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 2
          OnChange = Edit1Exit
        end
        object LEdit1: TLabeledEdit
          Left = 80
          Top = 16
          Width = 49
          Height = 21
          EditLabel.Width = 63
          EditLabel.Height = 13
          EditLabel.Caption = 'Ширина, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 3
          OnChange = Edit1Exit
        end
      end
      object GroupBox4: TGroupBox
        Left = 4
        Top = 4
        Width = 297
        Height = 261
        Caption = 'Наклейки на стеклопакеты'
        TabOrder = 1
        object LabeledEdit15: TLabeledEdit
          Left = 236
          Top = 16
          Width = 49
          Height = 21
          EditLabel.Width = 165
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер наклейки по ширине, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 0
          OnChange = Edit1Exit
        end
        object LabeledEdit16: TLabeledEdit
          Left = 236
          Top = 40
          Width = 49
          Height = 21
          EditLabel.Width = 164
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер наклейки по высоте, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 1
          OnChange = Edit1Exit
        end
        object LabeledEdit17: TLabeledEdit
          Left = 236
          Top = 136
          Width = 49
          Height = 21
          EditLabel.Width = 98
          EditLabel.Height = 13
          EditLabel.Caption = 'Отступ сверху, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 2
          OnChange = Edit1Exit
        end
        object LabeledEdit18: TLabeledEdit
          Left = 236
          Top = 112
          Width = 49
          Height = 21
          EditLabel.Width = 97
          EditLabel.Height = 13
          EditLabel.Caption = 'Остступ слева, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 3
          OnChange = Edit1Exit
        end
        object LabeledEdit19: TLabeledEdit
          Left = 236
          Top = 64
          Width = 49
          Height = 21
          EditLabel.Width = 147
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер рамки по ширине, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 4
          OnChange = Edit1Exit
        end
        object LabeledEdit20: TLabeledEdit
          Left = 236
          Top = 88
          Width = 49
          Height = 21
          EditLabel.Width = 146
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер рамки по высоте, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 5
          OnChange = Edit1Exit
        end
        object LabeledEdit21: TLabeledEdit
          Left = 236
          Top = 160
          Width = 49
          Height = 21
          EditLabel.Width = 207
          EditLabel.Height = 13
          EditLabel.Caption = 'Зазор между наклейками по ширине, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 6
          OnChange = Edit1Exit
        end
        object LabeledEdit22: TLabeledEdit
          Left = 236
          Top = 184
          Width = 49
          Height = 21
          EditLabel.Width = 206
          EditLabel.Height = 13
          EditLabel.Caption = 'Зазор между наклейками по высоте, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 7
          OnChange = Edit1Exit
        end
        object LabeledEdit23: TLabeledEdit
          Left = 236
          Top = 208
          Width = 49
          Height = 21
          EditLabel.Width = 208
          EditLabel.Height = 13
          EditLabel.Caption = 'Количество наклеек на листе по ширине'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 8
          OnChange = Edit1Exit
        end
        object LabeledEdit24: TLabeledEdit
          Left = 236
          Top = 232
          Width = 49
          Height = 21
          EditLabel.Width = 207
          EditLabel.Height = 13
          EditLabel.Caption = 'Количество наклеек на листе по высоте'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 9
          OnChange = Edit1Exit
        end
      end
      object GroupBox8: TGroupBox
        Left = 308
        Top = 4
        Width = 297
        Height = 261
        Caption = 'Наклейки на профиль'
        TabOrder = 2
        object LabeledEdit35: TLabeledEdit
          Left = 236
          Top = 16
          Width = 49
          Height = 21
          EditLabel.Width = 165
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер наклейки по ширине, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 0
          OnChange = Edit1Exit
        end
        object LabeledEdit36: TLabeledEdit
          Left = 236
          Top = 40
          Width = 49
          Height = 21
          EditLabel.Width = 164
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер наклейки по высоте, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 1
          OnChange = Edit1Exit
        end
        object LabeledEdit37: TLabeledEdit
          Left = 236
          Top = 136
          Width = 49
          Height = 21
          EditLabel.Width = 98
          EditLabel.Height = 13
          EditLabel.Caption = 'Отступ сверху, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 2
          OnChange = Edit1Exit
        end
        object LabeledEdit38: TLabeledEdit
          Left = 236
          Top = 112
          Width = 49
          Height = 21
          EditLabel.Width = 97
          EditLabel.Height = 13
          EditLabel.Caption = 'Остступ слева, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 3
          OnChange = Edit1Exit
        end
        object LabeledEdit39: TLabeledEdit
          Left = 236
          Top = 64
          Width = 49
          Height = 21
          EditLabel.Width = 147
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер рамки по ширине, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 4
          OnChange = Edit1Exit
        end
        object LabeledEdit40: TLabeledEdit
          Left = 236
          Top = 88
          Width = 49
          Height = 21
          EditLabel.Width = 146
          EditLabel.Height = 13
          EditLabel.Caption = 'Размер рамки по высоте, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 5
          OnChange = Edit1Exit
        end
        object LabeledEdit41: TLabeledEdit
          Left = 236
          Top = 160
          Width = 49
          Height = 21
          EditLabel.Width = 207
          EditLabel.Height = 13
          EditLabel.Caption = 'Зазор между наклейками по ширине, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 6
          OnChange = Edit1Exit
        end
        object LabeledEdit42: TLabeledEdit
          Left = 236
          Top = 184
          Width = 49
          Height = 21
          EditLabel.Width = 206
          EditLabel.Height = 13
          EditLabel.Caption = 'Зазор между наклейками по высоте, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 7
          OnChange = Edit1Exit
        end
        object LabeledEdit43: TLabeledEdit
          Left = 236
          Top = 208
          Width = 49
          Height = 21
          EditLabel.Width = 208
          EditLabel.Height = 13
          EditLabel.Caption = 'Количество наклеек на листе по ширине'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 8
          OnChange = Edit1Exit
        end
        object LabeledEdit44: TLabeledEdit
          Left = 236
          Top = 232
          Width = 49
          Height = 21
          EditLabel.Width = 207
          EditLabel.Height = 13
          EditLabel.Caption = 'Количество наклеек на листе по высоте'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 9
          OnChange = Edit1Exit
        end
      end
    end
    object TabSheet6: TTabSheet
      Caption = 'Утилиты'
      ImageIndex = 5
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object Label7: TLabel
        Left = 8
        Top = 196
        Width = 356
        Height = 13
        Caption = 'Изменения в меню произойдут только после перезапуска программы.'
      end
      object ToolBar1: TToolBar
        Left = 8
        Top = 4
        Width = 109
        Height = 36
        Align = alNone
        BorderWidth = 1
        ButtonHeight = 30
        ButtonWidth = 31
        EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
        EdgeInner = esNone
        Images = SahredDMod.ButList24
        TabOrder = 0
        object ToolButton1: TToolButton
          Left = 0
          Top = 0
          Action = UserToolAdd
        end
        object ToolButton3: TToolButton
          Left = 31
          Top = 0
          Action = UserToolEdit
        end
        object ToolButton4: TToolButton
          Left = 62
          Top = 0
          Width = 8
          ImageIndex = 2
          Style = tbsSeparator
        end
        object ToolButton2: TToolButton
          Left = 70
          Top = 0
          Action = UserToolDel
        end
      end
      object LView: TADGListView
        Left = 8
        Top = 40
        Width = 633
        Height = 150
        Columns = <>
        ReadOnly = False
        TabOrder = 1
        OnDblClick = UserToolEditExecute
      end
    end
    object TabSheet7: TTabSheet
      Caption = 'Опции'
      ImageIndex = 6
      object GroupBox15: TGroupBox
        Left = 0
        Top = 0
        Width = 645
        Height = 317
        Caption = 'Общие'
        TabOrder = 0
        object Label20: TLabel
          Left = 24
          Top = 54
          Width = 357
          Height = 13
          Caption = 'При экспорте заказов в DBF дублирующиеся заказы сверять по полю'
        end
        object CheckBox27: TCheckBox
          Left = 8
          Top = 232
          Width = 641
          Height = 17
          Caption = 
            'При отображении мест установки соединителей для импоста точкой о' +
            'тсчета считать кромку рамы, а не кромку импоста'
          TabOrder = 0
        end
        object CheckBox26: TCheckBox
          Left = 8
          Top = 212
          Width = 621
          Height = 17
          Caption = 
            'Москитную сетку устанавливать только в створку (запретить устано' +
            'вку в габарит конструкции)'
          TabOrder = 1
        end
        object CheckBox24: TCheckBox
          Left = 8
          Top = 192
          Width = 621
          Height = 17
          Caption = 
            'По умолчанию автоматически обновлять заказы в соответствии с шаб' +
            'лоном'
          TabOrder = 2
        end
        object CheckBox23: TCheckBox
          Left = 8
          Top = 172
          Width = 621
          Height = 17
          Caption = 'Автоматически пересчитывать заказ при его сохранении или печати'
          TabOrder = 3
        end
        object CheckBox13: TCheckBox
          Left = 8
          Top = 152
          Width = 621
          Height = 17
          Caption = 
            'Напоминать, если долгое время не делалась процедура "сжатие базы' +
            '"'
          TabOrder = 4
          OnClick = CheckBox1Click
        end
        object CheckBox12: TCheckBox
          Left = 8
          Top = 132
          Width = 617
          Height = 17
          Caption = 
            'Формировать без цен заявки на материалы и фурнитуру по группе за' +
            'казов'
          TabOrder = 5
          OnClick = CheckBox1Click
        end
        object CheckBox9: TCheckBox
          Left = 8
          Top = 112
          Width = 181
          Height = 17
          Caption = 'Вести протокол базы данных'
          TabOrder = 6
          OnClick = CheckBox1Click
        end
        object CheckBox8: TCheckBox
          Left = 8
          Top = 72
          Width = 545
          Height = 17
          Caption = 
            'При экспорте заказов в DBF не допускать экспортирование заказов ' +
            'без номера заказа'
          TabOrder = 7
          OnClick = CheckBox1Click
        end
        object CheckBox4: TCheckBox
          Left = 8
          Top = 32
          Width = 545
          Height = 17
          Caption = 
            'При автоматическом списании материалов по заказу проверять на на' +
            'личие необходимого количества'
          TabOrder = 8
          OnClick = CheckBox1Click
        end
        object CheckBox3: TCheckBox
          Left = 8
          Top = 12
          Width = 389
          Height = 17
          Caption = 
            'По складу делать списания по партиям (первый пришел - первый уше' +
            'л)'
          TabOrder = 9
          OnClick = CheckBox1Click
        end
        object CheckBox28: TCheckBox
          Left = 8
          Top = 252
          Width = 637
          Height = 17
          Caption = 
            'Автоматически блокировать заказ при "экспорте списания во внешни' +
            'й склад"'
          TabOrder = 10
        end
        object CheckBox29: TCheckBox
          Left = 8
          Top = 292
          Width = 637
          Height = 17
          Caption = 'Не допускать дублирование номеров заказов'
          TabOrder = 11
          Visible = False
        end
        object CheckBox31: TCheckBox
          Left = 8
          Top = 92
          Width = 629
          Height = 17
          Caption = 
            'При экспорте заказов в DBF использовать алтернативное удаление з' +
            'аписей (затирать пустыми значениями)'
          TabOrder = 12
          OnClick = CheckBox1Click
        end
        object CheckBox32: TCheckBox
          Left = 8
          Top = 272
          Width = 637
          Height = 17
          Caption = 
            'Автоматически менять шаблон при смене дилера на листе заказа, на' +
            ' шаблон, применимый для данного дилера'
          TabOrder = 13
        end
        object ComboBox8: TComboBox
          Left = 388
          Top = 52
          Width = 245
          Height = 21
          Style = csDropDownList
          ItemHeight = 13
          TabOrder = 14
        end
      end
      object GroupBox11: TGroupBox
        Left = 0
        Top = 320
        Width = 645
        Height = 101
        Caption = 'Этапы выполнения заказа'
        TabOrder = 1
        object CheckBox17: TCheckBox
          Left = 8
          Top = 56
          Width = 617
          Height = 17
          Caption = 
            'По окончании этапа "списание материалов" автоматически списывать' +
            ' материлы со "встроенного" склада'
          TabOrder = 0
        end
        object CheckBox18: TCheckBox
          Left = 8
          Top = 76
          Width = 617
          Height = 17
          Caption = 
            'По окончании этапа "списание материалов" автоматически формирова' +
            'ть экспорт списания во "внешний" склад'
          TabOrder = 1
        end
        object CheckBox19: TCheckBox
          Left = 8
          Top = 16
          Width = 617
          Height = 17
          Caption = 
            'По окончании этапа "договор" автоматически устанавливать "оговор' +
            'енную сумму"'
          TabOrder = 2
        end
        object CheckBox20: TCheckBox
          Left = 8
          Top = 36
          Width = 613
          Height = 17
          Caption = 
            'По окончании этапа "договор" автоматически  блокировать изменени' +
            'я заказа'
          TabOrder = 3
        end
      end
    end
    object TabSheet4: TTabSheet
      Caption = 'Прочие'
      ImageIndex = 3
      object SpeedButton3: TSpeedButton
        Left = 304
        Top = 112
        Width = 265
        Height = 29
        Caption = 'Пересохранить все заказы в сжатом формате'
        OnClick = SpeedButton3Click
      end
      object Label18: TLabel
        Left = 267
        Top = 84
        Width = 162
        Height = 13
        Caption = 'В расчетах использовать склад'
        Transparent = True
      end
      object LabeledEdit9: TLabeledEdit
        Left = 152
        Top = 8
        Width = 61
        Height = 21
        EditLabel.Width = 147
        EditLabel.Height = 13
        EditLabel.Caption = 'Толщина стандартной линии'
        LabelPosition = lpLeft
        TabOrder = 0
        OnChange = Edit1Exit
      end
      object LabeledEdit11: TLabeledEdit
        Left = 576
        Top = 8
        Width = 61
        Height = 21
        EditLabel.Width = 232
        EditLabel.Height = 13
        EditLabel.Caption = 'От даты заказа до даты изготовления, дней'
        LabelPosition = lpLeft
        TabOrder = 1
        OnChange = Edit1Exit
      end
      object LabeledEdit12: TLabeledEdit
        Left = 576
        Top = 32
        Width = 61
        Height = 21
        EditLabel.Width = 206
        EditLabel.Height = 13
        EditLabel.Caption = 'От даты заказа до даты монтажа, дней'
        LabelPosition = lpLeft
        TabOrder = 2
        OnChange = Edit1Exit
      end
      object GroupBox9: TGroupBox
        Left = 304
        Top = 144
        Width = 265
        Height = 105
        Caption = 'Экспорт чертежа в Base4.DBF'
        TabOrder = 3
        object LabeledEdit25: TLabeledEdit
          Left = 100
          Top = 16
          Width = 45
          Height = 21
          EditLabel.Width = 77
          EditLabel.Height = 13
          EditLabel.Caption = 'Ширина, точек'
          LabelPosition = lpLeft
          TabOrder = 0
          OnChange = Edit1Exit
        end
        object LabeledEdit26: TLabeledEdit
          Left = 100
          Top = 40
          Width = 45
          Height = 21
          EditLabel.Width = 74
          EditLabel.Height = 13
          EditLabel.Caption = 'Высота, точек'
          LabelPosition = lpLeft
          TabOrder = 1
          OnChange = Edit1Exit
        end
        object LabeledEdit27: TLabeledEdit
          Left = 100
          Top = 64
          Width = 45
          Height = 21
          EditLabel.Width = 85
          EditLabel.Height = 13
          EditLabel.Caption = 'Точек на 1 дюйм'
          LabelPosition = lpLeft
          TabOrder = 2
          OnChange = Edit1Exit
        end
        object RadioGroup1: TRadioGroup
          Left = 156
          Top = 12
          Width = 97
          Height = 81
          Caption = 'Формат файла'
          Items.Strings = (
            'BMP'
            'JPG'
            'EMF')
          TabOrder = 3
          OnClick = RadioGroup1Click
        end
      end
      object GroupBox10: TGroupBox
        Left = 0
        Top = 108
        Width = 297
        Height = 189
        Caption = 'Нумерация заказов'
        TabOrder = 4
        object SpeedButton2: TSpeedButton
          Left = 8
          Top = 148
          Width = 281
          Height = 29
          Caption = 'Восстановить "исходную" нумерацию заказов'
          OnClick = SpeedButton2Click
        end
        object SpeedButton1: TSpeedButton
          Left = 8
          Top = 116
          Width = 281
          Height = 29
          Caption = 'Изменить нумерацию заказов'
          OnClick = SpeedButton1Click
        end
        object SpeedButton5: TSpeedButton
          Left = 8
          Top = 84
          Width = 281
          Height = 29
          Caption = 'Ввести пароль дилера'
          OnClick = SpeedButton5Click
        end
        object LabeledEdit28: TLabeledEdit
          Left = 240
          Top = 12
          Width = 49
          Height = 21
          EditLabel.Width = 150
          EditLabel.Height = 13
          EditLabel.Caption = 'Код подразделения (дилера)'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 0
        end
        object LabeledEdit29: TLabeledEdit
          Left = 240
          Top = 36
          Width = 49
          Height = 21
          EditLabel.Width = 227
          EditLabel.Height = 13
          EditLabel.Caption = 'Разделитель между кодом и номером заказа'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 1
        end
        object CheckBox16: TCheckBox
          Left = 28
          Top = 60
          Width = 261
          Height = 17
          Alignment = taLeftJustify
          Caption = 'Добавлять код подразделения в номер заказа'
          TabOrder = 2
        end
      end
      object LabeledEdit53: TLabeledEdit
        Left = 576
        Top = 56
        Width = 61
        Height = 21
        EditLabel.Width = 544
        EditLabel.Height = 13
        EditLabel.Caption = 
          'Количество дней, за которое предупреждать дилера об окончании де' +
          'йствия дилерской базы или пароля'
        LabelPosition = lpLeft
        TabOrder = 5
      end
      object ComboBox7: TComboBox
        Left = 436
        Top = 80
        Width = 201
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 6
      end
      object GroupBox17: TGroupBox
        Left = 0
        Top = 300
        Width = 297
        Height = 121
        Caption = 'Наименование информационных полей в заказчиках'
        TabOrder = 7
        object LabeledEdit56: TLabeledEdit
          Left = 56
          Top = 16
          Width = 233
          Height = 21
          EditLabel.Width = 34
          EditLabel.Height = 13
          EditLabel.Caption = 'Поле 1'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 0
        end
        object LabeledEdit57: TLabeledEdit
          Left = 56
          Top = 40
          Width = 233
          Height = 21
          EditLabel.Width = 34
          EditLabel.Height = 13
          EditLabel.Caption = 'Поле 2'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 1
        end
        object LabeledEdit58: TLabeledEdit
          Left = 56
          Top = 64
          Width = 233
          Height = 21
          EditLabel.Width = 34
          EditLabel.Height = 13
          EditLabel.Caption = 'Поле 3'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 2
        end
        object LabeledEdit59: TLabeledEdit
          Left = 56
          Top = 88
          Width = 233
          Height = 21
          EditLabel.Width = 34
          EditLabel.Height = 13
          EditLabel.Caption = 'Поле 4'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 3
        end
      end
    end
  end
  object Edit7: TEdit
    Left = 208
    Top = 552
    Width = 117
    Height = 21
    TabOrder = 3
    Visible = False
    OnChange = Edit1Exit
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 592
    object UserToolAdd: TAction
      Caption = 'Добавить'
      Hint = 'Добавить утилиту'
      ImageIndex = 0
      OnExecute = UserToolAddExecute
    end
    object UserToolEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать утилиту'
      ImageIndex = 7
      OnExecute = UserToolEditExecute
    end
    object UserToolDel: TAction
      Caption = 'Удалить'
      Hint = 'Удалить утилиту'
      ImageIndex = 5
      OnExecute = UserToolDelExecute
    end
  end
end
