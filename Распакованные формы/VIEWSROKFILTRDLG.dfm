object ViewSrokFiltrDlg: TViewSrokFiltrDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Настройка фильтра'
  ClientHeight = 572
  ClientWidth = 782
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 628
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 704
    Top = 4
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LabeledEdit1: TLabeledEdit
    Left = 84
    Top = 8
    Width = 537
    Height = 21
    EditLabel.Width = 73
    EditLabel.Height = 13
    EditLabel.Caption = 'Наименование'
    LabelPosition = lpLeft
    TabOrder = 2
  end
  object PageControl1: TPageControl
    Left = 4
    Top = 36
    Width = 777
    Height = 533
    ActivePage = TabSheet2
    TabOrder = 3
    object TabSheet1: TTabSheet
      Caption = 'Основные'
      object GroupBox1: TGroupBox
        Left = 4
        Top = 356
        Width = 209
        Height = 145
        Caption = 'Сигнализаторы и партии'
        TabOrder = 0
        object CheckBox1: TCheckBox
          Left = 8
          Top = 16
          Width = 73
          Height = 17
          Caption = 'включить'
          TabOrder = 0
          OnClick = CheckBox8Click
        end
        object CheckBox3: TCheckBox
          Left = 8
          Top = 36
          Width = 73
          Height = 17
          Caption = 'включить'
          TabOrder = 1
          OnClick = CheckBox8Click
        end
        object CheckBox5: TCheckBox
          Left = 8
          Top = 56
          Width = 73
          Height = 17
          Caption = 'включить'
          TabOrder = 2
          OnClick = CheckBox8Click
        end
        object CheckBox6: TCheckBox
          Left = 8
          Top = 76
          Width = 73
          Height = 17
          Caption = 'включить'
          TabOrder = 3
          OnClick = CheckBox8Click
        end
        object CheckBox7: TCheckBox
          Left = 8
          Top = 96
          Width = 73
          Height = 17
          Caption = 'включить'
          TabOrder = 4
          OnClick = CheckBox8Click
        end
        object CheckBox8: TCheckBox
          Left = 84
          Top = 16
          Width = 121
          Height = 17
          Caption = 'ламинация'
          TabOrder = 5
          OnClick = CheckBox8Click
        end
        object CheckBox9: TCheckBox
          Left = 84
          Top = 36
          Width = 121
          Height = 17
          Caption = 'арка'
          TabOrder = 6
          OnClick = CheckBox8Click
        end
        object CheckBox10: TCheckBox
          Left = 84
          Top = 56
          Width = 121
          Height = 17
          Caption = 'непрямоугольник'
          TabOrder = 7
          OnClick = CheckBox8Click
        end
        object CheckBox11: TCheckBox
          Left = 84
          Top = 76
          Width = 121
          Height = 17
          Caption = 'дверь'
          TabOrder = 8
          OnClick = CheckBox8Click
        end
        object CheckBox12: TCheckBox
          Left = 84
          Top = 96
          Width = 121
          Height = 17
          Caption = 'негабарит'
          TabOrder = 9
          OnClick = CheckBox8Click
        end
        object CheckBox13: TCheckBox
          Left = 84
          Top = 120
          Width = 121
          Height = 17
          Caption = 'партия назначена'
          TabOrder = 10
        end
        object CheckBox14: TCheckBox
          Left = 8
          Top = 120
          Width = 73
          Height = 17
          Caption = 'включить'
          TabOrder = 11
        end
      end
      object GroupBox6: TGroupBox
        Left = 220
        Top = 356
        Width = 541
        Height = 113
        Caption = 'Временной интервал'
        TabOrder = 1
        object Label3: TLabel
          Left = 8
          Top = 40
          Width = 96
          Height = 13
          Caption = 'фильтруемая дата'
        end
        object Label4: TLabel
          Left = 8
          Top = 64
          Width = 82
          Height = 13
          Caption = 'начало периода'
        end
        object Label5: TLabel
          Left = 8
          Top = 88
          Width = 100
          Height = 13
          Caption = 'окончание периода'
        end
        object ComboBox3: TComboBox
          Left = 116
          Top = 36
          Width = 417
          Height = 21
          Style = csDropDownList
          ItemHeight = 13
          TabOrder = 0
        end
        object ComboBox4: TComboBox
          Left = 116
          Top = 60
          Width = 137
          Height = 21
          Style = csDropDownList
          ItemHeight = 13
          TabOrder = 1
          OnChange = ComboBox4Change
        end
        object ComboBox5: TComboBox
          Left = 116
          Top = 84
          Width = 137
          Height = 21
          Style = csDropDownList
          ItemHeight = 13
          TabOrder = 2
        end
        object ComboBox6: TComboBox
          Left = 396
          Top = 60
          Width = 137
          Height = 21
          Style = csDropDownList
          ItemHeight = 13
          TabOrder = 3
        end
        object ComboBox7: TComboBox
          Left = 396
          Top = 84
          Width = 137
          Height = 21
          Style = csDropDownList
          ItemHeight = 13
          TabOrder = 4
        end
        object DateTimePicker3: TDateTimePicker
          Left = 256
          Top = 60
          Width = 85
          Height = 21
          Date = 39054.595968958330000000
          Time = 39054.595968958330000000
          TabOrder = 5
          OnChange = DateTimePicker1Change
        end
        object DateTimePicker4: TDateTimePicker
          Left = 256
          Top = 84
          Width = 85
          Height = 21
          Date = 39054.595968958330000000
          Time = 39054.595968958330000000
          TabOrder = 6
          OnChange = DateTimePicker1Change
        end
        object LabeledEdit3: TLabeledEdit
          Left = 356
          Top = 60
          Width = 37
          Height = 21
          EditLabel.Width = 8
          EditLabel.Height = 13
          EditLabel.Caption = '+'
          LabelPosition = lpLeft
          TabOrder = 7
        end
        object LabeledEdit4: TLabeledEdit
          Left = 356
          Top = 84
          Width = 37
          Height = 21
          EditLabel.Width = 8
          EditLabel.Height = 13
          EditLabel.Caption = '+'
          LabelPosition = lpLeft
          TabOrder = 8
        end
        object CheckBox4: TCheckBox
          Left = 8
          Top = 16
          Width = 97
          Height = 17
          Caption = 'включить'
          TabOrder = 9
        end
      end
      object GroupBox2: TGroupBox
        Left = 4
        Top = 0
        Width = 705
        Height = 353
        Caption = 'Этапы выполнения заказа'
        TabOrder = 2
        object LViewOperation: TADGListView
          Left = 4
          Top = 52
          Width = 201
          Height = 293
          Columns = <>
          ReadOnly = False
          TabOrder = 0
          OnSelectItem = LViewOperationSelectItem
        end
        object ToolBar1: TToolBar
          Left = 6
          Top = 15
          Width = 77
          Height = 36
          Align = alNone
          BorderWidth = 1
          ButtonHeight = 30
          ButtonWidth = 31
          Color = clBtnFace
          EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
          EdgeInner = esNone
          Images = SahredDMod.ButList24
          ParentColor = False
          TabOrder = 1
          object ToolButton1: TToolButton
            Left = 0
            Top = 0
            Action = OperationAdd
          end
          object ToolButton3: TToolButton
            Left = 31
            Top = 0
            Width = 8
            ImageIndex = 2
            Style = tbsSeparator
          end
          object ToolButton2: TToolButton
            Left = 39
            Top = 0
            Action = OperationDel
          end
        end
        object GroupBox4: TGroupBox
          Left = 212
          Top = 12
          Width = 485
          Height = 333
          Caption = 'Редактор действия'
          TabOrder = 2
          object ComboBox1: TComboBox
            Left = 8
            Top = 16
            Width = 469
            Height = 21
            Style = csDropDownList
            ItemHeight = 13
            TabOrder = 0
            OnChange = ComboBox1Change
          end
          object LView: TADGListView
            Left = 8
            Top = 40
            Width = 469
            Height = 285
            Color = clBtnFace
            Columns = <>
            ReadOnly = False
            TabOrder = 1
            OnCheckClick = LViewCheckClick
          end
        end
      end
    end
    object TabSheet2: TTabSheet
      Caption = 'Дополнительные'
      ImageIndex = 1
      object GroupBox3: TGroupBox
        Left = 0
        Top = 0
        Width = 493
        Height = 229
        Caption = 'Заказчики'
        TabOrder = 0
        object CheckBox2: TCheckBox
          Left = 8
          Top = 16
          Width = 77
          Height = 17
          Caption = 'включить'
          TabOrder = 0
        end
        object LView2: TADGListView
          Left = 8
          Top = 48
          Width = 477
          Height = 177
          Columns = <>
          ReadOnly = False
          TabOrder = 1
        end
        object ToolBar2: TToolBar
          Left = 86
          Top = 11
          Width = 76
          Height = 36
          Align = alNone
          BorderWidth = 1
          ButtonHeight = 30
          ButtonWidth = 31
          Color = clBtnFace
          EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
          EdgeInner = esNone
          Images = SahredDMod.ButList24
          ParentColor = False
          TabOrder = 2
          object ToolButton4: TToolButton
            Left = 0
            Top = 0
            Action = ZakazchikAdd
          end
          object ToolButton5: TToolButton
            Left = 31
            Top = 0
            Width = 8
            ImageIndex = 2
            Style = tbsSeparator
          end
          object ToolButton6: TToolButton
            Left = 39
            Top = 0
            Action = ZakazchikDel
          end
        end
      end
      object GroupBox5: TGroupBox
        Left = 0
        Top = 232
        Width = 493
        Height = 229
        Caption = 'Дилеры'
        TabOrder = 1
        object CheckBox15: TCheckBox
          Left = 8
          Top = 16
          Width = 77
          Height = 17
          Caption = 'включить'
          TabOrder = 0
        end
        object LView3: TADGListView
          Left = 8
          Top = 48
          Width = 477
          Height = 177
          Columns = <>
          ReadOnly = False
          TabOrder = 1
        end
        object ToolBar3: TToolBar
          Left = 86
          Top = 11
          Width = 76
          Height = 36
          Align = alNone
          BorderWidth = 1
          ButtonHeight = 30
          ButtonWidth = 31
          Color = clBtnFace
          EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
          EdgeInner = esNone
          Images = SahredDMod.ButList24
          ParentColor = False
          TabOrder = 2
          object ToolButton7: TToolButton
            Left = 0
            Top = 0
            Action = DealerAdd
          end
          object ToolButton8: TToolButton
            Left = 31
            Top = 0
            Width = 8
            ImageIndex = 2
            Style = tbsSeparator
          end
          object ToolButton9: TToolButton
            Left = 39
            Top = 0
            Action = DealerDel
          end
        end
      end
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 732
    Top = 64
    object OperationAdd: TAction
      Caption = 'Добавить действие'
      Hint = 'Добавить действие'
      ImageIndex = 0
      OnExecute = OperationAddExecute
    end
    object OperationDel: TAction
      Caption = 'Удалить действие'
      Hint = 'Удалить действие'
      ImageIndex = 5
      OnExecute = OperationDelExecute
    end
    object ZakazchikAdd: TAction
      Caption = 'Добавить заказчика в фильтр'
      Hint = 'Добавить заказчика в фильтр'
      ImageIndex = 0
      OnExecute = ZakazchikAddExecute
    end
    object ZakazchikDel: TAction
      Caption = 'Исключить заказчика из фильтра'
      Hint = 'Исключить заказчика из фильтра'
      ImageIndex = 5
      OnExecute = ZakazchikDelExecute
    end
    object DealerAdd: TAction
      Caption = 'Добавить дилера в фильтр'
      Hint = 'Добавить дилера в фильтр'
      ImageIndex = 0
      OnExecute = DealerAddExecute
    end
    object DealerDel: TAction
      Caption = 'Исключить дилера из фильтра'
      Hint = 'Исключить дилера из фильтра'
      ImageIndex = 5
      OnExecute = DealerDelExecute
    end
  end
end
