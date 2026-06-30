object MatEditNewDlg: TMatEditNewDlg
  Left = 246
  Top = 157
  BorderStyle = bsDialog
  Caption = 'Редактор материала'
  ClientHeight = 656
  ClientWidth = 701
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 532
    Top = 628
    Width = 81
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 616
    Top = 628
    Width = 81
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object PageControl1: TPageControl
    Left = 0
    Top = 0
    Width = 697
    Height = 625
    ActivePage = TabSheet1
    TabOrder = 2
    object TabSheet1: TTabSheet
      Caption = 'Общие'
      ExplicitHeight = 581
      object GroupBox1: TGroupBox
        Left = 476
        Top = 0
        Width = 209
        Height = 405
        Caption = 'Длиномерные материалы'
        TabOrder = 0
        object Label6: TLabel
          Left = 24
          Top = 172
          Width = 147
          Height = 13
          Caption = 'Площадь покрытия, м.кв./м.'
          Font.Charset = DEFAULT_CHARSET
          Font.Color = clWindowText
          Font.Height = -11
          Font.Name = 'Tahoma'
          Font.Style = [fsUnderline]
          ParentFont = False
        end
        object Label4: TLabel
          Left = 16
          Top = 316
          Width = 25
          Height = 13
          Caption = 'Пила'
          Transparent = True
        end
        object cbKroitParal: TCheckBox
          Left = 8
          Top = 32
          Width = 199
          Height = 17
          Caption = 'параллельный раскрой на 2 прута'
          TabOrder = 0
          OnClick = cbTermoClick
        end
        object cbKroitDlinu: TCheckBox
          Left = 8
          Top = 16
          Width = 89
          Height = 17
          Caption = 'Кроить длину'
          TabOrder = 1
          OnClick = cbTermoClick
        end
        object Edit12: TLabeledEdit
          Left = 136
          Top = 96
          Width = 53
          Height = 21
          EditLabel.Width = 88
          EditLabel.Height = 13
          EditLabel.Caption = 'Длина прута, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 2
          OnChange = Edit3Change
        end
        object Edit4: TLabeledEdit
          Left = 136
          Top = 120
          Width = 53
          Height = 21
          EditLabel.Width = 73
          EditLabel.Height = 13
          EditLabel.Caption = 'Неликвид, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 3
          OnChange = Edit3Change
        end
        object Edit7: TLabeledEdit
          Left = 136
          Top = 144
          Width = 53
          Height = 21
          EditLabel.Width = 54
          EditLabel.Height = 13
          EditLabel.Caption = 'Мусор, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 4
          OnChange = Edit3Change
        end
        object LabeledEdit3: TLabeledEdit
          Left = 136
          Top = 188
          Width = 53
          Height = 21
          EditLabel.Width = 54
          EditLabel.Height = 13
          EditLabel.Caption = 'внутреняя'
          LabelPosition = lpLeft
          TabOrder = 5
          OnChange = Edit3Change
        end
        object LabeledEdit4: TLabeledEdit
          Left = 136
          Top = 212
          Width = 53
          Height = 21
          EditLabel.Width = 50
          EditLabel.Height = 13
          EditLabel.Caption = 'наружная'
          LabelPosition = lpLeft
          TabOrder = 6
          OnChange = Edit3Change
        end
        object LabeledEdit5: TLabeledEdit
          Left = 136
          Top = 240
          Width = 53
          Height = 21
          EditLabel.Width = 107
          EditLabel.Height = 13
          EditLabel.Caption = 'Высота профиля, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 7
          OnChange = Edit3Change
        end
        object cbTermo: TCheckBox
          Left = 8
          Top = 336
          Width = 145
          Height = 17
          Caption = 'теплый профиль (термо)'
          TabOrder = 8
          OnClick = cbTermoClick
        end
        object CheckBox1: TCheckBox
          Left = 8
          Top = 352
          Width = 197
          Height = 17
          Caption = 'экспортировать на пилу'
          TabOrder = 9
          OnClick = cbTermoClick
        end
        object CheckBox6: TCheckBox
          Left = 8
          Top = 48
          Width = 199
          Height = 17
          Caption = '"складировать обрезки"'
          TabOrder = 10
          OnClick = cbTermoClick
        end
        object CheckBox7: TCheckBox
          Left = 8
          Top = 368
          Width = 197
          Height = 17
          Caption = 'экспортировать на сварочник'
          TabOrder = 11
          OnClick = cbTermoClick
        end
        object ComboBox1: TComboBox
          Left = 48
          Top = 312
          Width = 141
          Height = 21
          Style = csDropDownList
          DropDownCount = 15
          ItemHeight = 13
          TabOrder = 12
          OnChange = CBox1Change
        end
        object LabeledEdit6: TLabeledEdit
          Left = 136
          Top = 264
          Width = 53
          Height = 21
          EditLabel.Width = 120
          EditLabel.Height = 13
          EditLabel.Caption = 'Группа для сварочника'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 13
          OnChange = LEdit1Change
        end
        object CheckBox8: TCheckBox
          Left = 8
          Top = 384
          Width = 197
          Height = 17
          Caption = 'печать наклейки на профиль'
          TabOrder = 14
          OnClick = cbTermoClick
        end
        object CheckBox9: TCheckBox
          Left = 8
          Top = 64
          Width = 197
          Height = 29
          Caption = 'разрешать принцип расчета стоимости с учетом всех остатков'
          TabOrder = 15
          WordWrap = True
          OnClick = cbTermoClick
        end
        object LabeledEdit9: TLabeledEdit
          Left = 136
          Top = 288
          Width = 53
          Height = 21
          EditLabel.Width = 69
          EditLabel.Height = 13
          EditLabel.Caption = 'Этап раскроя'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 16
          OnChange = Edit3Change
        end
      end
      object GroupBox2: TGroupBox
        Left = 476
        Top = 408
        Width = 209
        Height = 169
        Caption = 'Плоскостные материалы'
        TabOrder = 1
        object Edit8: TLabeledEdit
          Left = 136
          Top = 68
          Width = 53
          Height = 21
          EditLabel.Width = 63
          EditLabel.Height = 13
          EditLabel.Caption = 'Ширина, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 0
          OnChange = Edit3Change
        end
        object Edit9: TLabeledEdit
          Left = 136
          Top = 92
          Width = 53
          Height = 21
          EditLabel.Width = 60
          EditLabel.Height = 13
          EditLabel.Caption = 'Высота, мм.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 1
          OnChange = Edit3Change
        end
        object cbKroitPlosk: TCheckBox
          Left = 8
          Top = 16
          Width = 113
          Height = 17
          Caption = 'Кроить плоскость'
          TabOrder = 2
          OnClick = cbTermoClick
        end
        object LabeledEdit1: TLabeledEdit
          Left = 152
          Top = 120
          Width = 53
          Height = 21
          EditLabel.Width = 136
          EditLabel.Height = 13
          EditLabel.Hint = 'Минимальная расчетная площадь, м.кв.'
          EditLabel.Caption = 'Мин. расч. площадь, м.кв.'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 3
          OnChange = Edit3Change
        end
        object Edit14: TLabeledEdit
          Left = 152
          Top = 144
          Width = 53
          Height = 21
          EditLabel.Width = 129
          EditLabel.Height = 13
          EditLabel.Caption = 'Затемнение стекла (0..1)'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          TabOrder = 4
          OnChange = Edit3Change
        end
        object CheckBox11: TCheckBox
          Left = 8
          Top = 32
          Width = 199
          Height = 17
          Caption = 'вращать заготовки'
          TabOrder = 5
          OnClick = cbTermoClick
        end
        object CheckBox12: TCheckBox
          Left = 8
          Top = 48
          Width = 199
          Height = 17
          Caption = '"складировать обрезки"'
          TabOrder = 6
          OnClick = cbTermoClick
        end
      end
      object GroupBox3: TGroupBox
        Left = 4
        Top = 252
        Width = 465
        Height = 121
        Caption = 'Опции печати'
        TabOrder = 2
        object Shape1: TShape
          Left = 244
          Top = 92
          Width = 121
          Height = 21
        end
        object SpeedButton1: TSpeedButton
          Left = 368
          Top = 92
          Width = 89
          Height = 22
          Caption = 'Выбрать цвет'
          OnClick = SpeedButton1Click
        end
        object cbBool0: TCheckBox
          Left = 8
          Top = 16
          Width = 229
          Height = 17
          Caption = 'печать на "сборочном чертеже"'
          TabOrder = 0
          OnClick = cbTermoClick
        end
        object cbBool2: TCheckBox
          Left = 244
          Top = 16
          Width = 137
          Height = 17
          Caption = 'группировать позиции'
          TabOrder = 1
          OnClick = cbTermoClick
        end
        object cbBool1: TCheckBox
          Left = 8
          Top = 32
          Width = 229
          Height = 17
          Caption = 'печать на "коммерческом предложении"'
          TabOrder = 2
          OnClick = cbTermoClick
        end
        object cbBool5: TCheckBox
          Left = 244
          Top = 32
          Width = 137
          Height = 17
          Caption = 'группировать позиции'
          TabOrder = 3
          OnClick = cbTermoClick
        end
        object cbBool3: TCheckBox
          Left = 8
          Top = 48
          Width = 229
          Height = 17
          Caption = 'печать на "отправном листе"'
          TabOrder = 4
          OnClick = cbTermoClick
        end
        object cbBool4: TCheckBox
          Left = 244
          Top = 48
          Width = 137
          Height = 17
          Caption = 'группировать позиции'
          TabOrder = 5
          OnClick = cbTermoClick
        end
        object CheckBox2: TCheckBox
          Left = 8
          Top = 64
          Width = 229
          Height = 17
          Caption = 'печать в "подоконниках"'
          TabOrder = 6
          OnClick = cbTermoClick
        end
        object CheckBox3: TCheckBox
          Left = 8
          Top = 80
          Width = 229
          Height = 17
          Caption = 'печать в "отливах"'
          TabOrder = 7
          OnClick = cbTermoClick
        end
        object CheckBox10: TCheckBox
          Left = 8
          Top = 96
          Width = 229
          Height = 17
          Caption = 'выделить цветом'
          TabOrder = 8
          OnClick = cbTermoClick
        end
      end
      object GroupBox4: TGroupBox
        Left = 4
        Top = 376
        Width = 465
        Height = 217
        Caption = 'Цвета материала и цены'
        TabOrder = 3
        object Label1: TLabel
          Left = 338
          Top = 32
          Width = 39
          Height = 13
          Caption = 'Валюта'
          Transparent = True
        end
        object SG2: TStringGrid
          Left = 4
          Top = 52
          Width = 453
          Height = 141
          ColCount = 4
          DefaultRowHeight = 16
          FixedCols = 0
          Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goRowSelect, goThumbTracking]
          TabOrder = 0
          OnDblClick = MatColorEditExecute
          ColWidths = (
            108
            191
            61
            64)
        end
        object CBox6: TComboBox
          Left = 384
          Top = 28
          Width = 73
          Height = 21
          Style = csDropDownList
          DropDownCount = 15
          ItemHeight = 13
          TabOrder = 1
          OnChange = CBox1Change
        end
        object ToolBar1: TToolBar
          Left = 6
          Top = 15
          Width = 107
          Height = 36
          Align = alNone
          BorderWidth = 1
          ButtonHeight = 30
          ButtonWidth = 31
          EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
          EdgeInner = esNone
          Images = SahredDMod.ButList24
          TabOrder = 2
          object ToolButton1: TToolButton
            Left = 0
            Top = 0
            Action = MatColorAdd
          end
          object ToolButton3: TToolButton
            Left = 31
            Top = 0
            Action = MatColorEdit
          end
          object ToolButton4: TToolButton
            Left = 62
            Top = 0
            Width = 8
            ImageIndex = 7
            Style = tbsSeparator
          end
          object ToolButton2: TToolButton
            Left = 70
            Top = 0
            Action = MatColorDelete
          end
        end
        object cbPriseTable: TCheckBox
          Left = 4
          Top = 196
          Width = 429
          Height = 17
          Hint = 
            'Как правило данная опция применяется только для настройки цен дл' +
            'я готовых комплектов фурнитуры'
          Caption = 'цена по таблице (смотрите закладку "дополнительно")'
          TabOrder = 3
          OnClick = cbTermoClick
        end
      end
      object GroupBox5: TGroupBox
        Left = 4
        Top = 0
        Width = 465
        Height = 249
        Caption = 'Общие'
        TabOrder = 4
        object Label10: TLabel
          Left = 46
          Top = 116
          Width = 36
          Height = 13
          Caption = 'Группа'
          Transparent = True
        end
        object Label21: TLabel
          Left = 47
          Top = 92
          Width = 36
          Height = 13
          Caption = 'Раздел'
          Transparent = True
        end
        object Label2: TLabel
          Left = 8
          Top = 20
          Width = 73
          Height = 13
          Caption = 'Наименование'
          Transparent = True
        end
        object Label13: TLabel
          Left = 258
          Top = 80
          Width = 61
          Height = 13
          Caption = 'Осн.ед.изм.'
          Transparent = True
        end
        object Label14: TLabel
          Left = 257
          Top = 104
          Width = 62
          Height = 13
          Caption = 'Доп.ед.изм.'
          Transparent = True
        end
        object Label3: TLabel
          Left = 268
          Top = 128
          Width = 125
          Height = 13
          Caption = 'Коеф.перевода (уд.вес)'
          Transparent = True
        end
        object Label15: TLabel
          Left = 402
          Top = 64
          Width = 47
          Height = 13
          Caption = 'Расценка'
          Transparent = True
        end
        object Label11: TLabel
          Left = 5
          Top = 68
          Width = 79
          Height = 13
          Caption = 'Производитель'
          Transparent = True
        end
        object Edit2: TEdit
          Left = 88
          Top = 16
          Width = 361
          Height = 21
          Ctl3D = True
          ParentCtl3D = False
          TabOrder = 0
          OnChange = LEdit1Change
        end
        object LEdit1: TLabeledEdit
          Left = 88
          Top = 40
          Width = 361
          Height = 21
          EditLabel.Width = 61
          EditLabel.Height = 13
          EditLabel.Caption = 'Примечание'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          LabelSpacing = 5
          TabOrder = 1
          OnChange = LEdit1Change
        end
        object CBox1: TComboBox
          Left = 88
          Top = 88
          Width = 161
          Height = 21
          Style = csDropDownList
          DropDownCount = 20
          ItemHeight = 13
          TabOrder = 2
          OnChange = CBox1Change
          Items.Strings = (
            'сандартный'
            'остекление'
            'фурнитура')
        end
        object CBox8: TComboBox
          Left = 88
          Top = 112
          Width = 161
          Height = 21
          Style = csDropDownList
          ItemHeight = 13
          TabOrder = 3
          OnChange = CBox1Change
          Items.Strings = (
            'Нет'
            'Профиль'
            'Армирование'
            'Штапик'
            'Дистанционер')
        end
        object CBox9: TComboBox
          Left = 324
          Top = 76
          Width = 73
          Height = 21
          Style = csDropDownList
          DropDownCount = 15
          ItemHeight = 13
          TabOrder = 4
          OnChange = CBox1Change
        end
        object Edit15: TEdit
          Left = 324
          Top = 100
          Width = 53
          Height = 21
          ReadOnly = True
          TabOrder = 5
        end
        object Edit3: TEdit
          Left = 396
          Top = 124
          Width = 53
          Height = 21
          Ctl3D = True
          ParentCtl3D = False
          TabOrder = 6
          OnChange = Edit3Change
        end
        object LabeledEdit2: TLabeledEdit
          Left = 396
          Top = 148
          Width = 53
          Height = 21
          EditLabel.Width = 51
          EditLabel.Height = 13
          EditLabel.Caption = 'Отход, %'
          LabelPosition = lpLeft
          TabOrder = 7
          OnChange = Edit3Change
        end
        object RascBar: TTrackBar
          Left = 400
          Top = 76
          Width = 45
          Height = 45
          Hint = 
            'Цена на материал будет указыватся за единицу товара осн. либо до' +
            'п. "единицы измерения"'
          Max = 1
          Orientation = trVertical
          PageSize = 1
          TabOrder = 8
          ThumbLength = 22
          TickMarks = tmTopLeft
          OnChange = RascBarChange
        end
        object CBox7: TComboBox
          Left = 88
          Top = 64
          Width = 161
          Height = 21
          Style = csDropDownList
          DropDownCount = 20
          ItemHeight = 13
          TabOrder = 9
          OnChange = CBox1Change
        end
        object CheckBox4: TCheckBox
          Left = 8
          Top = 176
          Width = 169
          Height = 17
          Caption = 'делать списание по складу'
          TabOrder = 10
          OnClick = cbTermoClick
        end
        object CheckBox5: TCheckBox
          Left = 8
          Top = 160
          Width = 169
          Height = 17
          Caption = 'экспортировать в Base9.DBF'
          TabOrder = 11
          OnClick = cbTermoClick
        end
        object LabeledEdit7: TLabeledEdit
          Left = 88
          Top = 136
          Width = 161
          Height = 21
          Hint = 
            'Используется для заданя группы материала при экспорте данных во ' +
            'внешнюю бухгалтерскую программу'
          EditLabel.Width = 79
          EditLabel.Height = 13
          EditLabel.Hint = 
            'Используется для задани группы материала при экспорте данных во ' +
            'внешнюю бухгалтерсую программу'
          EditLabel.Caption = 'Группа экспорт'
          EditLabel.Transparent = True
          LabelPosition = lpLeft
          LabelSpacing = 5
          TabOrder = 12
          OnChange = LEdit1Change
        end
        object LabeledEdit8: TLabeledEdit
          Left = 396
          Top = 172
          Width = 53
          Height = 21
          Alignment = taRightJustify
          EditLabel.Width = 138
          EditLabel.Height = 13
          EditLabel.Caption = 'Время выполнения, ч:мм:сс'
          LabelPosition = lpLeft
          TabOrder = 13
          OnChange = LEdit1Change
        end
        object CheckBox13: TCheckBox
          Left = 8
          Top = 208
          Width = 381
          Height = 17
          Caption = 'использовать в дополнительных материалах в полной программе'
          TabOrder = 14
          OnClick = cbTermoClick
        end
        object CheckBox14: TCheckBox
          Left = 8
          Top = 224
          Width = 381
          Height = 17
          Caption = 'использовать в дополнительных материалах в дилерской программе'
          TabOrder = 15
          OnClick = cbTermoClick
        end
        object CheckBox16: TCheckBox
          Left = 8
          Top = 192
          Width = 381
          Height = 17
          Caption = 'учитывать на складе готовой продукции'
          TabOrder = 16
          OnClick = cbTermoClick
        end
      end
    end
    object TabSheet2: TTabSheet
      Caption = 'Дополнительно'
      ImageIndex = 1
      ExplicitHeight = 581
      object GroupBox6: TGroupBox
        Left = 4
        Top = 0
        Width = 681
        Height = 337
        Caption = 'Цена по таблице'
        TabOrder = 0
        object Label17: TLabel
          Left = 20
          Top = 20
          Width = 29
          Height = 13
          Caption = 'строк'
          Transparent = True
        end
        object Label16: TLabel
          Left = 68
          Top = 20
          Width = 47
          Height = 13
          Caption = 'столбцов'
          Transparent = True
        end
        object Edit6: TEdit
          Left = 64
          Top = 36
          Width = 53
          Height = 21
          TabOrder = 0
          OnExit = Edit6Exit
        end
        object Edit5: TEdit
          Left = 8
          Top = 36
          Width = 53
          Height = 21
          TabOrder = 1
          OnExit = Edit6Exit
        end
        object SG1: TStringGrid
          Left = 8
          Top = 64
          Width = 665
          Height = 261
          ColCount = 11
          DefaultColWidth = 50
          DefaultRowHeight = 16
          FixedCols = 0
          RowCount = 11
          FixedRows = 0
          Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goEditing, goThumbTracking]
          TabOrder = 2
          OnSetEditText = SG1SetEditText
        end
      end
      object GroupBox7: TGroupBox
        Left = 4
        Top = 340
        Width = 369
        Height = 173
        Caption = 'Поставщики'
        TabOrder = 1
        object ToolBar4: TToolBar
          Left = 8
          Top = 16
          Width = 141
          Height = 36
          Align = alNone
          BorderWidth = 1
          ButtonHeight = 30
          ButtonWidth = 31
          EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
          EdgeInner = esNone
          Images = SahredDMod.ButList24
          TabOrder = 0
          Transparent = True
          object ToolButton8: TToolButton
            Left = 0
            Top = 0
            Action = PostavAdd
          end
          object ToolButton9: TToolButton
            Left = 31
            Top = 0
            Action = PostavDel
          end
          object ToolButton5: TToolButton
            Left = 62
            Top = 0
            Width = 8
            Caption = 'ToolButton5'
            ImageIndex = 0
            Style = tbsSeparator
          end
          object ToolButton10: TToolButton
            Left = 70
            Top = 0
            Action = PostavUp
          end
          object ToolButton11: TToolButton
            Left = 101
            Top = 0
            Action = PostavDown
          end
        end
        object LView: TADGListView
          Left = 8
          Top = 56
          Width = 353
          Height = 109
          Columns = <>
          ReadOnly = False
          TabOrder = 1
        end
      end
      object LabeledEdit10: TLabeledEdit
        Left = 204
        Top = 520
        Width = 57
        Height = 21
        EditLabel.Width = 192
        EditLabel.Height = 13
        EditLabel.Hint = 'Применяется для экспорта стеклопакетов в OptiWay'
        EditLabel.Caption = 'Обозначение для экспорта в OptiWay'
        LabelPosition = lpLeft
        TabOrder = 2
        OnChange = LEdit1Change
      end
      object CheckBox15: TCheckBox
        Left = 4
        Top = 544
        Width = 353
        Height = 17
        Hint = 'Применяется для экспорта стеклопакетов в OptiWay'
        Caption = 'Данное стекло содержит энергосберегающее напыление'
        TabOrder = 3
        OnClick = cbTermoClick
      end
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 640
    object MatColorAdd: TAction
      Caption = 'Добавить цвет'
      Hint = 'Добавить цвет'
      ImageIndex = 0
      OnExecute = MatColorAddExecute
    end
    object MatColorEdit: TAction
      Caption = 'Редактировать данные цвета'
      Hint = 'Редактировать данные цвета'
      ImageIndex = 7
      OnExecute = MatColorEditExecute
    end
    object MatColorDelete: TAction
      Caption = 'Удалить цвет'
      Hint = 'Удалить цвет'
      ImageIndex = 5
      OnExecute = MatColorDeleteExecute
    end
    object PostavAdd: TAction
      Caption = 'Добавить поставщика'
      Hint = 'Добавить поставщика'
      ImageIndex = 0
      OnExecute = PostavAddExecute
    end
    object PostavDel: TAction
      Caption = 'Удалить поставщика'
      Hint = 'Удалить поставщика'
      ImageIndex = 5
      OnExecute = PostavDelExecute
    end
    object PostavUp: TAction
      Caption = 'Поднять вверх'
      Hint = 'Поднять вверх'
      ImageIndex = 17
      OnExecute = PostavUpExecute
    end
    object PostavDown: TAction
      Caption = 'Опустить вниз'
      Hint = 'Опустить вниз'
      ImageIndex = 6
      OnExecute = PostavDownExecute
    end
  end
end
