object PrintParDlg: TPrintParDlg
  Left = 278
  Top = 200
  BorderStyle = bsDialog
  Caption = 'Параметры печати'
  ClientHeight = 594
  ClientWidth = 609
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object PageControl1: TPageControl
    Left = 4
    Top = 4
    Width = 601
    Height = 553
    ActivePage = TabSheet1
    TabOrder = 0
    object TabSheet1: TTabSheet
      Caption = 'Общие'
      object Label5: TLabel
        Left = 8
        Top = 12
        Width = 36
        Height = 13
        Caption = 'Шрифт'
      end
      object Label9: TLabel
        Left = 8
        Top = 36
        Width = 39
        Height = 13
        Caption = 'Валюта'
      end
      object Label13: TLabel
        Left = 8
        Top = 60
        Width = 36
        Height = 13
        Caption = 'Заявки'
      end
      object Label22: TLabel
        Left = 8
        Top = 84
        Width = 26
        Height = 13
        Caption = 'Язык'
      end
      object BtnEdit1: TBtnEdit
        Left = 52
        Top = 8
        Width = 121
        Height = 21
        ReadOnly = True
        TabOrder = 0
        OnBtnClick = SpeedButton1Click
      end
      object ValutBoxM: TComboBox
        Left = 52
        Top = 32
        Width = 121
        Height = 21
        Style = csDropDownList
        DropDownCount = 20
        ItemHeight = 13
        TabOrder = 1
      end
      object ShapModeBox: TComboBox
        Left = 52
        Top = 56
        Width = 121
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        ItemIndex = 0
        TabOrder = 2
        Text = 'Фирм. бланк'
        Items.Strings = (
          'Фирм. бланк'
          'Инфо заказ')
      end
      object ComboBox1: TComboBox
        Left = 52
        Top = 80
        Width = 121
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 3
      end
      object CheckBox28: TCheckBox
        Left = 8
        Top = 112
        Width = 365
        Height = 17
        Caption = 'Использовать альтернативный метод печати (не рекомендуется)'
        TabOrder = 4
      end
      object GroupBox1: TGroupBox
        Left = 376
        Top = 0
        Width = 213
        Height = 161
        Hint = 
          'Количество знаков после запятой при отображении определенных зна' +
          'чений'
        Caption = 'Точность отображения'
        TabOrder = 5
        object LabeledEdit1: TLabeledEdit
          Left = 160
          Top = 36
          Width = 41
          Height = 21
          EditLabel.Width = 102
          EditLabel.Height = 13
          EditLabel.Caption = 'Углов реза деталей'
          LabelPosition = lpLeft
          TabOrder = 0
        end
        object LabeledEdit6: TLabeledEdit
          Left = 160
          Top = 60
          Width = 41
          Height = 21
          EditLabel.Width = 25
          EditLabel.Height = 13
          EditLabel.Caption = 'Сумм'
          LabelPosition = lpLeft
          TabOrder = 1
        end
        object LabeledEdit7: TLabeledEdit
          Left = 160
          Top = 108
          Width = 41
          Height = 21
          EditLabel.Width = 53
          EditLabel.Height = 13
          EditLabel.Caption = 'Площадей'
          LabelPosition = lpLeft
          TabOrder = 2
        end
        object LabeledEdit8: TLabeledEdit
          Left = 160
          Top = 132
          Width = 41
          Height = 21
          EditLabel.Width = 145
          EditLabel.Height = 13
          EditLabel.Caption = 'Количества в спецификации'
          LabelPosition = lpLeft
          TabOrder = 3
        end
        object LabeledEdit9: TLabeledEdit
          Left = 160
          Top = 12
          Width = 41
          Height = 21
          EditLabel.Width = 93
          EditLabel.Height = 13
          EditLabel.Caption = 'Размеров деталей'
          LabelPosition = lpLeft
          TabOrder = 4
        end
        object LabeledEdit10: TLabeledEdit
          Left = 160
          Top = 84
          Width = 41
          Height = 21
          EditLabel.Width = 99
          EditLabel.Height = 13
          EditLabel.Caption = 'Сумм в прайс-листе'
          LabelPosition = lpLeft
          TabOrder = 5
        end
      end
      object GroupBox2: TGroupBox
        Left = 188
        Top = 164
        Width = 401
        Height = 77
        Caption = 
          'Печать наклеек при постановке конструкций на склад готовой проду' +
          'кции'
        TabOrder = 6
        object Label6: TLabel
          Left = 12
          Top = 48
          Width = 42
          Height = 13
          Caption = 'принтер'
        end
        object Label8: TLabel
          Left = 12
          Top = 24
          Width = 83
          Height = 13
          Caption = 'печатная форма'
        end
        object ComboBox2: TComboBox
          Left = 108
          Top = 20
          Width = 281
          Height = 21
          Style = csDropDownList
          DropDownCount = 40
          ItemHeight = 13
          TabOrder = 0
        end
        object ComboBox3: TComboBox
          Left = 108
          Top = 44
          Width = 281
          Height = 21
          DropDownCount = 40
          ItemHeight = 13
          TabOrder = 1
        end
      end
      object GroupBox3: TGroupBox
        Left = 188
        Top = 244
        Width = 401
        Height = 77
        Caption = 'Печать документа при отгрузке заказа со склада готовой продукции'
        TabOrder = 7
        object Label14: TLabel
          Left = 12
          Top = 48
          Width = 42
          Height = 13
          Caption = 'принтер'
        end
        object Label15: TLabel
          Left = 12
          Top = 24
          Width = 83
          Height = 13
          Caption = 'печатная форма'
        end
        object ComboBox4: TComboBox
          Left = 108
          Top = 20
          Width = 281
          Height = 21
          Style = csDropDownList
          DropDownCount = 40
          ItemHeight = 13
          TabOrder = 0
        end
        object ComboBox5: TComboBox
          Left = 108
          Top = 44
          Width = 281
          Height = 21
          DropDownCount = 40
          ItemHeight = 13
          TabOrder = 1
        end
      end
    end
    object TabSheet2: TTabSheet
      Caption = 'Счет-фактура'
      ImageIndex = 1
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object Label18: TLabel
        Left = 352
        Top = 4
        Width = 165
        Height = 13
        Caption = 'Примечание для счета-фактуры'
        Transparent = True
      end
      object Label3: TLabel
        Left = 272
        Top = 172
        Width = 13
        Height = 13
        Caption = '№'
        Transparent = True
      end
      object Label11: TLabel
        Left = 440
        Top = 172
        Width = 26
        Height = 13
        Caption = 'Дата'
        Transparent = True
      end
      object Memo1: TMemo
        Left = 288
        Top = 20
        Width = 301
        Height = 141
        ScrollBars = ssBoth
        TabOrder = 0
      end
      object Edit3: TEdit
        Left = 288
        Top = 168
        Width = 53
        Height = 21
        TabOrder = 1
      end
      object Edit9: TEdit
        Left = 472
        Top = 168
        Width = 117
        Height = 21
        TabOrder = 2
      end
      object LabeledEdit3: TLabeledEdit
        Left = 288
        Top = 216
        Width = 301
        Height = 21
        EditLabel.Width = 136
        EditLabel.Height = 13
        EditLabel.Caption = 'Система налогообложения'
        LabelPosition = lpLeft
        TabOrder = 3
      end
      object LabeledEdit4: TLabeledEdit
        Left = 288
        Top = 240
        Width = 301
        Height = 21
        EditLabel.Width = 108
        EditLabel.Height = 13
        EditLabel.Caption = 'Срок действия счета'
        LabelPosition = lpLeft
        TabOrder = 4
      end
      object CheckBox7: TCheckBox
        Left = 288
        Top = 192
        Width = 301
        Height = 17
        Caption = 'номер и дата такие же, как номер и дата заказа'
        TabOrder = 5
      end
    end
    object TabSheet3: TTabSheet
      Caption = 'Коммерческое предложение'
      ImageIndex = 2
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object Label2: TLabel
        Left = 370
        Top = 8
        Width = 79
        Height = 13
        Caption = 'Кем утвержден'
        Transparent = True
      end
      object Label7: TLabel
        Left = 344
        Top = 56
        Width = 101
        Height = 13
        Caption = 'Оформитель заказа'
        Transparent = True
        Visible = False
      end
      object Label10: TLabel
        Left = 9
        Top = 80
        Width = 70
        Height = 13
        Caption = 'Доп. затраты'
        Transparent = True
      end
      object Label1: TLabel
        Left = 24
        Top = 8
        Width = 56
        Height = 13
        Caption = 'Фурнитура'
      end
      object Label4: TLabel
        Left = 8
        Top = 32
        Width = 74
        Height = 13
        Caption = 'Гар. обслужив'
      end
      object Label12: TLabel
        Left = 20
        Top = 56
        Width = 63
        Height = 13
        Caption = 'Сертификат'
      end
      object Edit1: TEdit
        Left = 452
        Top = 4
        Width = 133
        Height = 21
        TabOrder = 0
      end
      object Edit2: TEdit
        Left = 452
        Top = 28
        Width = 133
        Height = 21
        TabOrder = 1
      end
      object ChBox1: TCheckBox
        Left = 84
        Top = 120
        Width = 213
        Height = 17
        Caption = 'Достака транспортом производителя'
        Checked = True
        State = cbChecked
        TabOrder = 2
      end
      object Edit5: TEdit
        Left = 452
        Top = 52
        Width = 133
        Height = 21
        TabOrder = 3
        Visible = False
      end
      object Edit8: TEdit
        Left = 84
        Top = 76
        Width = 501
        Height = 21
        TabOrder = 4
      end
      object Edit4: TEdit
        Left = 84
        Top = 4
        Width = 149
        Height = 21
        TabOrder = 5
      end
      object Edit7: TEdit
        Left = 84
        Top = 28
        Width = 149
        Height = 21
        TabOrder = 6
      end
      object Edit11: TEdit
        Left = 84
        Top = 52
        Width = 149
        Height = 21
        TabOrder = 7
      end
      object CheckBox3: TCheckBox
        Left = 84
        Top = 100
        Width = 273
        Height = 17
        Caption = 'Печать монтажных и дополнительных элементов'
        Checked = True
        State = cbChecked
        TabOrder = 8
      end
      object Edit22: TEdit
        Left = 304
        Top = 116
        Width = 281
        Height = 21
        TabOrder = 9
      end
      object LabeledEdit2: TLabeledEdit
        Left = 544
        Top = 140
        Width = 41
        Height = 21
        EditLabel.Width = 79
        EditLabel.Height = 13
        EditLabel.Caption = 'Приложение №'
        LabelPosition = lpLeft
        TabOrder = 10
      end
      object CheckBox2: TCheckBox
        Left = 84
        Top = 140
        Width = 289
        Height = 17
        Caption = 'Печать реквизит заказчика и производителя'
        TabOrder = 11
      end
      object LabeledEdit5: TLabeledEdit
        Left = 176
        Top = 168
        Width = 409
        Height = 21
        EditLabel.Width = 165
        EditLabel.Height = 13
        EditLabel.Caption = 'Прмечание на смете материалов'
        LabelPosition = lpLeft
        TabOrder = 12
      end
    end
    object TabSheet4: TTabSheet
      Caption = 'Опции'
      ImageIndex = 3
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object CheckBox1: TCheckBox
        Left = 8
        Top = 24
        Width = 189
        Height = 17
        Caption = 'Печать Прибыли в калькуляции'
        TabOrder = 1
      end
      object ChBox2: TCheckBox
        Left = 8
        Top = 44
        Width = 105
        Height = 17
        Caption = 'Цветной чертеж'
        TabOrder = 2
      end
      object CheckBox4: TCheckBox
        Left = 8
        Top = 64
        Width = 349
        Height = 17
        Caption = 'Сортировка материалов в спецификации по артикулу'
        TabOrder = 3
      end
      object CheckBox6: TCheckBox
        Left = 8
        Top = 84
        Width = 249
        Height = 17
        Caption = 'Печать армирования на чертеже детальном'
        TabOrder = 4
      end
      object CheckBox5: TCheckBox
        Left = 8
        Top = 104
        Width = 177
        Height = 17
        Caption = 'Прайс-лист стоимость за м.кв.'
        TabOrder = 5
      end
      object CheckBox8: TCheckBox
        Left = 8
        Top = 124
        Width = 249
        Height = 17
        Caption = 'Подписывать наименования с/п на чертеже'
        TabOrder = 6
      end
      object CheckBox9: TCheckBox
        Left = 8
        Top = 4
        Width = 161
        Height = 17
        Caption = 'Печать цен и сумм в смете'
        TabOrder = 0
      end
      object CheckBox10: TCheckBox
        Left = 8
        Top = 144
        Width = 297
        Height = 17
        Caption = 'Печать даты оформл. заказа на сборочном чертеже'
        TabOrder = 7
      end
      object CheckBox11: TCheckBox
        Left = 8
        Top = 164
        Width = 297
        Height = 17
        Caption = 'Печать армирования на сборочном чертеже'
        TabOrder = 8
      end
      object CheckBox13: TCheckBox
        Left = 8
        Top = 184
        Width = 365
        Height = 17
        Caption = 'Печать заказчика, адреса, телефона на внутренней документации'
        TabOrder = 9
      end
      object CheckBox14: TCheckBox
        Left = 8
        Top = 204
        Width = 297
        Height = 17
        Caption = 'Печать "таблицы трудозатрат" на сборочном чертеже'
        TabOrder = 10
      end
      object CheckBox15: TCheckBox
        Left = 8
        Top = 224
        Width = 297
        Height = 17
        Caption = 'Скрывать в отчете по складу нулевые позиции'
        TabOrder = 11
      end
      object CheckBox16: TCheckBox
        Left = 8
        Top = 244
        Width = 333
        Height = 17
        Caption = 'На коммерческом предложении печать стоимости за 1 м.кв.'
        TabOrder = 12
      end
      object CheckBox17: TCheckBox
        Left = 8
        Top = 264
        Width = 333
        Height = 17
        Caption = 'Печать даты изготовления заказа'
        TabOrder = 13
      end
      object CheckBox18: TCheckBox
        Left = 8
        Top = 284
        Width = 333
        Height = 17
        Caption = 'Печать даты монтажа заказа'
        TabOrder = 14
      end
      object CheckBox12: TCheckBox
        Left = 8
        Top = 304
        Width = 333
        Height = 17
        Caption = 'Печать на отправном листе стеклопакетов'
        TabOrder = 15
      end
      object CheckBox19: TCheckBox
        Left = 8
        Top = 364
        Width = 201
        Height = 17
        Caption = 'Печать размерных линий'
        TabOrder = 18
      end
      object CheckBox20: TCheckBox
        Left = 8
        Top = 324
        Width = 261
        Height = 17
        Caption = 'Печать на отправном листе москитных сеток'
        TabOrder = 16
      end
      object CheckBox21: TCheckBox
        Left = 8
        Top = 344
        Width = 329
        Height = 17
        Caption = 'Печать на отправном листе наименований конструкций'
        TabOrder = 17
      end
      object CheckBox22: TCheckBox
        Left = 8
        Top = 384
        Width = 201
        Height = 17
        Caption = 'Печатать цвет уплотнителя'
        TabOrder = 19
      end
      object CheckBox23: TCheckBox
        Left = 8
        Top = 404
        Width = 553
        Height = 17
        Caption = 
          'На коммерческом предложении печать стоимостей конструкций с учет' +
          'ом дополнительных материалов'
        TabOrder = 20
      end
      object CheckBox24: TCheckBox
        Left = 8
        Top = 424
        Width = 577
        Height = 17
        Caption = 
          'На раскрое профиля маркировать "последние детали" внутри каждой ' +
          'конструкции'
        TabOrder = 21
      end
      object CheckBox25: TCheckBox
        Left = 8
        Top = 444
        Width = 577
        Height = 17
        Caption = 'На заявке на стеклопакеты подробной сортировать стеклопакеты'
        TabOrder = 22
      end
      object CheckBox26: TCheckBox
        Left = 8
        Top = 464
        Width = 577
        Height = 17
        Caption = 'На заявке на стеклопакеты компактной сортировать стеклопакеты'
        TabOrder = 23
      end
      object CheckBox27: TCheckBox
        Left = 8
        Top = 484
        Width = 397
        Height = 17
        Caption = 'Печать номера страницы, даты, времени'
        TabOrder = 24
      end
      object CheckBox29: TCheckBox
        Left = 8
        Top = 504
        Width = 397
        Height = 17
        Caption = 'Печать размеров створки на сборочных чертежах'
        TabOrder = 25
      end
    end
  end
  object Button1: TButton
    Left = 448
    Top = 563
    Width = 77
    Height = 25
    Caption = 'ОК'
    Default = True
    ModalResult = 1
    TabOrder = 1
    OnClick = OKBtnClick
  end
  object Button2: TButton
    Left = 528
    Top = 563
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 2
  end
end
