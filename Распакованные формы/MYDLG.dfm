object MyDlg: TMyDlg
  Left = 273
  Top = 189
  HorzScrollBar.Increment = 1
  HorzScrollBar.Visible = False
  VertScrollBar.Increment = 1
  VertScrollBar.Visible = False
  BorderIcons = []
  BorderStyle = bsNone
  Caption = 'Коробка'
  ClientHeight = 580
  ClientWidth = 547
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnClose = FormClose
  OnCloseQuery = FormCloseQuery
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Panel1: TPanel
    Left = 0
    Top = 0
    Width = 547
    Height = 580
    Align = alClient
    BevelWidth = 3
    TabOrder = 0
    object Label1: TLabel
      Left = 236
      Top = 10
      Width = 55
      Height = 13
      Caption = 'Внимание !'
    end
    object Label2: TLabel
      Left = 159
      Top = 25
      Width = 207
      Height = 13
      Caption = 'Ваша программа подлежит регистрации.'
    end
    object Label3: TLabel
      Left = 78
      Top = 41
      Width = 409
      Height = 13
      Caption = 
        'Для этого сформируйте и передайте серийный номер программы разра' +
        'ботчику.'
    end
    object Label4: TLabel
      Left = 83
      Top = 176
      Width = 339
      Height = 13
      Caption = 'тел. +38 044 585-4-585 режим работы пон-пят 9:00-18:00'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -11
      Font.Name = 'Tahoma'
      Font.Style = [fsBold]
      ParentFont = False
    end
    object Label7: TLabel
      Tag = -700
      Left = 188
      Top = 192
      Width = 89
      Height = 13
      Cursor = crHandPoint
      Caption = 'mailto: adgroup'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clActiveCaption
      Font.Height = -11
      Font.Name = 'Tahoma'
      Font.Style = [fsBold, fsUnderline]
      ParentFont = False
      OnClick = Label7Click
    end
    object SpeedButton1: TSpeedButton
      Left = 456
      Top = 12
      Width = 77
      Height = 29
      Caption = 'Закрыть'
      OnClick = SpeedButton1Click
    end
    object Label12: TLabel
      Left = 12
      Top = 57
      Width = 516
      Height = 52
      Alignment = taCenter
      Caption = 
        'Установите программу на компьютер на котором Вы хотите с ней раб' +
        'отать и выполните описанные ниже действия. Помните: в условия по' +
        'купки программы входит заранее оговоренное количество рабочих ме' +
        'ст, поэтому заранее продумайте те рабочие места, где должна рабо' +
        'тать программа и установку программы делайте именно на эти рабоч' +
        'ие места.'
      WordWrap = True
    end
    object Label5: TLabel
      Left = 16
      Top = 108
      Width = 517
      Height = 65
      Alignment = taCenter
      AutoSize = False
      Caption = 
        'Регистрация выдается в течение 4 рабочих дней. Звонок в компанию' +
        ' "Эйдигруп" с вопросом "почему так долго" только еще задержит эт' +
        'от процесс. Покупайте программу версии "Plus", в таком случае ре' +
        'гистрация либо интегрируется на инсталяционный компакт-диск и не' +
        ' понадобится вообще, либо понадобится один раз при самой первой ' +
        'инсталяции и будет действительна для любого компьютера. Эти прие' +
        'мущества доступны только в версии "Plus"!!!'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -11
      Font.Name = 'Tahoma'
      Font.Style = [fsUnderline]
      ParentFont = False
      WordWrap = True
    end
    object GroupBox1: TGroupBox
      Left = 12
      Top = 208
      Width = 525
      Height = 121
      Caption = 
        'Шаг 1. Реквизиты для регистрации программы (заполнить обязательн' +
        'о все поля)'
      TabOrder = 0
      object Label13: TLabel
        Left = 8
        Top = 96
        Width = 24
        Height = 13
        Caption = 'цель'
      end
      object LabeledEdit3: TLabeledEdit
        Left = 124
        Top = 68
        Width = 121
        Height = 21
        EditLabel.Width = 100
        EditLabel.Height = 13
        EditLabel.Caption = 'телефон (офисный)'
        LabelPosition = lpLeft
        TabOrder = 2
        OnChange = LabeledEdit1Change
      end
      object LabeledEdit2: TLabeledEdit
        Left = 124
        Top = 44
        Width = 121
        Height = 21
        EditLabel.Width = 30
        EditLabel.Height = 13
        EditLabel.Caption = 'город'
        LabelPosition = lpLeft
        TabOrder = 1
        OnChange = LabeledEdit1Change
      end
      object LabeledEdit1: TLabeledEdit
        Left = 124
        Top = 20
        Width = 121
        Height = 21
        EditLabel.Width = 114
        EditLabel.Height = 13
        EditLabel.Caption = 'название организации'
        LabelPosition = lpLeft
        TabOrder = 0
        OnChange = LabeledEdit1Change
      end
      object LabeledEdit4: TLabeledEdit
        Left = 396
        Top = 20
        Width = 121
        Height = 21
        EditLabel.Width = 121
        EditLabel.Height = 13
        EditLabel.Hint = 
          'Указываете название организации - поставщика Вам готовых констру' +
          'кций.'
        EditLabel.Caption = 'дилеры от организации'
        LabelPosition = lpLeft
        TabOrder = 3
        OnChange = LabeledEdit1Change
      end
      object ComboBox1: TComboBox
        Left = 36
        Top = 92
        Width = 481
        Height = 21
        DropDownCount = 30
        ItemHeight = 13
        TabOrder = 4
        OnChange = ComboBox1Change
        OnDrawItem = ComboBox1DrawItem
        OnMeasureItem = ComboBox1MeasureItem
      end
    end
    object GroupBox2: TGroupBox
      Left = 12
      Top = 332
      Width = 261
      Height = 237
      Caption = 'Шаг 2. Регистрация по электронной почте'
      TabOrder = 1
      object Label8: TLabel
        Left = 6
        Top = 17
        Width = 247
        Height = 52
        Alignment = taCenter
        AutoSize = False
        Caption = 
          'Для этого сначала нажмите кнопку "Серийный номер сохранить в фай' +
          'л", укажите имя файла и папку, куда Вы его хотите сохранить. Зат' +
          'ем этот файл отправьте по электронной почте. '
        WordWrap = True
      end
      object SpeedButton2: TSpeedButton
        Left = 8
        Top = 84
        Width = 245
        Height = 21
        Caption = 'Серийный номер "сохранить в файл"'
        OnClick = SpeedButton2Click
      end
      object Label9: TLabel
        Left = 6
        Top = 109
        Width = 247
        Height = 96
        Alignment = taCenter
        AutoSize = False
        Caption = 
          'По электронной почте Вам придет ответное письмо с прикрепленным ' +
          'регистрационным файлом. Этот файл нужно сохранить на компьютере ' +
          'в любом месте, а затем нажать кнопку "регистрационный номер откр' +
          'ыть из файла" и указать место где он лежит и название файла.'
        WordWrap = True
      end
      object SpeedButton3: TSpeedButton
        Left = 8
        Top = 208
        Width = 245
        Height = 21
        Caption = 'Регистрационный номер "открыть из файла"'
        OnClick = SpeedButton3Click
      end
      object Label6: TLabel
        Tag = -700
        Left = 12
        Top = 68
        Width = 89
        Height = 13
        Cursor = crHandPoint
        Caption = 'mailto: adgroup'
        Font.Charset = DEFAULT_CHARSET
        Font.Color = clActiveCaption
        Font.Height = -11
        Font.Name = 'Tahoma'
        Font.Style = [fsBold, fsUnderline]
        ParentFont = False
        OnClick = Label7Click
      end
    end
    object GroupBox3: TGroupBox
      Left = 276
      Top = 332
      Width = 261
      Height = 237
      Caption = 'Шаг 2. Регистрация по факсу (менее удобно)'
      TabOrder = 2
      object Label10: TLabel
        Left = 6
        Top = 17
        Width = 247
        Height = 68
        Alignment = taCenter
        AutoSize = False
        Caption = 
          'Для этого сначала нажмите кнопку "Серийный номер печать на принт' +
          'ер", распечатайте его. Затем этот лист отправте по факсу (номер ' +
          'указан вверху).'
        WordWrap = True
      end
      object SpeedButton4: TSpeedButton
        Left = 8
        Top = 84
        Width = 245
        Height = 21
        Caption = 'Серийный номер "печать на принтер"'
        OnClick = SpeedButton4Click
      end
      object SpeedButton5: TSpeedButton
        Left = 8
        Top = 208
        Width = 245
        Height = 21
        Caption = 'Ввести регистрационный номер'
        OnClick = SpeedButton5Click
      end
      object Label11: TLabel
        Left = 6
        Top = 109
        Width = 247
        Height = 96
        Alignment = taCenter
        AutoSize = False
        Caption = 
          'По факсу Вы получите ответ с регистрационным номером. Этот номер' +
          ' нужно ввести с клавиатуры с помощью кнопки "ввести регистрацион' +
          'ный номер".'
        WordWrap = True
      end
    end
  end
end
