object PersonEditDlg: TPersonEditDlg
  Left = 358
  Top = 222
  BorderStyle = bsDialog
  Caption = 'Редактор пользователя'
  ClientHeight = 711
  ClientWidth = 698
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnClose = FormClose
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 536
    Top = 680
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
    OnClick = OKBtnClick
  end
  object CancelBtn: TButton
    Left = 616
    Top = 680
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object PageControl1: TPageControl
    Left = 0
    Top = 0
    Width = 697
    Height = 673
    ActivePage = TabSheet2
    TabOrder = 2
    object TabSheet1: TTabSheet
      Caption = 'Общие'
      object Label1: TLabel
        Left = 28
        Top = 12
        Width = 72
        Height = 13
        Caption = 'Пользователь'
        Transparent = True
      end
      object Label2: TLabel
        Left = 65
        Top = 44
        Width = 36
        Height = 13
        Caption = 'пароль'
        Transparent = True
      end
      object Label3: TLabel
        Left = 16
        Top = 60
        Width = 85
        Height = 29
        Alignment = taRightJustify
        AutoSize = False
        Caption = 'подтверждение пароля'
        Transparent = True
        WordWrap = True
      end
      object Label4: TLabel
        Left = 8
        Top = 152
        Width = 100
        Height = 13
        Caption = 'Доступы (права)'
        Font.Charset = DEFAULT_CHARSET
        Font.Color = clWindowText
        Font.Height = -11
        Font.Name = 'MS Sans Serif'
        Font.Style = [fsBold, fsUnderline]
        ParentFont = False
        Transparent = True
      end
      object Bevel1: TBevel
        Left = 4
        Top = 148
        Width = 677
        Height = 5
        Shape = bsTopLine
      end
      object Label5: TLabel
        Left = 84
        Top = 124
        Width = 18
        Height = 13
        Caption = 'тип'
      end
      object Bevel2: TBevel
        Left = 336
        Top = 152
        Width = 5
        Height = 489
        Shape = bsLeftLine
      end
      object Edit1: TEdit
        Left = 108
        Top = 8
        Width = 121
        Height = 21
        TabOrder = 0
        OnChange = Edit1Change
      end
      object Edit2: TEdit
        Left = 108
        Top = 40
        Width = 121
        Height = 21
        PasswordChar = '*'
        TabOrder = 1
      end
      object Edit3: TEdit
        Left = 108
        Top = 64
        Width = 121
        Height = 21
        PasswordChar = '*'
        TabOrder = 2
      end
      object CheckBox1: TCheckBox
        Left = 8
        Top = 168
        Width = 313
        Height = 17
        Caption = 'администраторские права (доб/изм/удл пользователей)'
        TabOrder = 3
      end
      object CheckBox2: TCheckBox
        Left = 8
        Top = 192
        Width = 317
        Height = 17
        Caption = 'справочник материалов'
        TabOrder = 4
      end
      object CheckBox3: TCheckBox
        Left = 8
        Top = 212
        Width = 321
        Height = 17
        Caption = 'настройка элементов и справочник серий'
        TabOrder = 5
      end
      object CheckBox4: TCheckBox
        Left = 8
        Top = 232
        Width = 125
        Height = 17
        Caption = 'экономика в целом'
        TabOrder = 6
      end
      object CheckBox5: TCheckBox
        Left = 8
        Top = 252
        Width = 241
        Height = 17
        Caption = 'изменение коэфициентов на листе заказа'
        TabOrder = 7
      end
      object CheckBox6: TCheckBox
        Left = 8
        Top = 352
        Width = 161
        Height = 17
        Caption = 'печать цен на материалы'
        TabOrder = 8
      end
      object CheckBox7: TCheckBox
        Left = 8
        Top = 372
        Width = 161
        Height = 17
        Caption = 'удаление заказов'
        TabOrder = 9
      end
      object LabeledEdit1: TLabeledEdit
        Left = 292
        Top = 8
        Width = 33
        Height = 21
        EditLabel.Width = 53
        EditLabel.Height = 13
        EditLabel.Hint = 
          'позволяет разграничить доступ к заказам и заказчикам среди неско' +
          'льких ползователей программы (чтоб пользователи просматривали за' +
          'казы только в пределах свой "группы" )'
        EditLabel.Caption = '№ группы'
        LabelPosition = lpLeft
        TabOrder = 10
        OnChange = Edit1Change
      end
      object CheckBox8: TCheckBox
        Left = 8
        Top = 556
        Width = 321
        Height = 17
        Caption = 'доступ к информации только пользователей своей группы'
        TabOrder = 11
      end
      object CheckBox9: TCheckBox
        Left = 8
        Top = 392
        Width = 161
        Height = 17
        Caption = 'склад'
        TabOrder = 12
      end
      object CheckBox10: TCheckBox
        Left = 8
        Top = 432
        Width = 253
        Height = 17
        Caption = 'склад: редактирование накладной на отпуск'
        TabOrder = 13
      end
      object CheckBox11: TCheckBox
        Left = 8
        Top = 272
        Width = 273
        Height = 17
        Caption = 'изменение коэфициента затрат на листе заказа'
        TabOrder = 14
      end
      object CheckBox12: TCheckBox
        Left = 8
        Top = 292
        Width = 317
        Height = 17
        Caption = 'изменение коэфициентов доп. расходов на листе заказа'
        TabOrder = 15
      end
      object CheckBox13: TCheckBox
        Left = 8
        Top = 412
        Width = 145
        Height = 17
        Caption = 'склад: редактирование'
        TabOrder = 16
      end
      object LabeledEdit2: TLabeledEdit
        Left = 108
        Top = 96
        Width = 121
        Height = 21
        EditLabel.Width = 23
        EditLabel.Height = 13
        EditLabel.Hint = 
          'можно указать ФИО пользователя и тогда на распечатках будет отоб' +
          'ражаться "кто оформил данный заказ"'
        EditLabel.Caption = 'ФИО'
        LabelPosition = lpLeft
        TabOrder = 17
        OnChange = Edit1Change
      end
      object CheckBox15: TCheckBox
        Left = 8
        Top = 452
        Width = 325
        Height = 17
        Caption = 'ставить предыдущее состояние этапов выполнения заказа'
        TabOrder = 18
      end
      object CheckBox16: TCheckBox
        Left = 8
        Top = 576
        Width = 321
        Height = 17
        Caption = 'при открытии заказа менять группу у заказа на свою'
        TabOrder = 19
      end
      object CheckBox17: TCheckBox
        Left = 8
        Top = 332
        Width = 317
        Height = 17
        Caption = 'изменение скидки №2 на листе заказа'
        TabOrder = 20
      end
      object CheckBox18: TCheckBox
        Left = 8
        Top = 312
        Width = 273
        Height = 17
        Caption = 'изменение скидки №1 на листе заказа'
        TabOrder = 21
      end
      object CheckBox19: TCheckBox
        Left = 8
        Top = 472
        Width = 325
        Height = 17
        Caption = 'отменять блокировку изменений заказа'
        TabOrder = 22
      end
      object CheckBox20: TCheckBox
        Left = 8
        Top = 596
        Width = 321
        Height = 17
        Caption = 'запретить экспорт заблокированного заказа'
        TabOrder = 23
      end
      object CheckBox21: TCheckBox
        Left = 8
        Top = 512
        Width = 325
        Height = 17
        Caption = 'курсы валют: редактирование'
        TabOrder = 24
      end
      object CheckBox22: TCheckBox
        Left = 8
        Top = 492
        Width = 325
        Height = 17
        Caption = 'курсы валют: просмотр'
        TabOrder = 25
      end
      object CheckBox23: TCheckBox
        Left = 8
        Top = 532
        Width = 325
        Height = 17
        Caption = 'отображать сумму, проплату, долг в справочнике заказов'
        TabOrder = 26
      end
      object ComboBox1: TComboBox
        Left = 108
        Top = 120
        Width = 217
        Height = 21
        Style = csDropDownList
        ItemHeight = 13
        TabOrder = 27
      end
      object CheckBox24: TCheckBox
        Left = 8
        Top = 616
        Width = 321
        Height = 25
        Caption = 
          'не допускать изменения этапа "коммерческое предложение" при забл' +
          'окированном дилере'
        TabOrder = 28
        WordWrap = True
      end
      object CheckBox25: TCheckBox
        Left = 344
        Top = 152
        Width = 325
        Height = 17
        Caption = 'оплаты: подробный просмотр'
        TabOrder = 29
      end
      object CheckBox26: TCheckBox
        Left = 344
        Top = 172
        Width = 317
        Height = 17
        Caption = 'оплаты: добавить'
        TabOrder = 30
      end
      object CheckBox27: TCheckBox
        Left = 344
        Top = 192
        Width = 317
        Height = 17
        Caption = 'оплаты: редактировать'
        TabOrder = 31
      end
      object CheckBox28: TCheckBox
        Left = 344
        Top = 212
        Width = 317
        Height = 17
        Caption = 'оплаты: удалить'
        TabOrder = 32
      end
      object CheckBox29: TCheckBox
        Left = 344
        Top = 232
        Width = 317
        Height = 17
        Caption = 'оплаты: отчет'
        TabOrder = 33
      end
      object CheckBox30: TCheckBox
        Left = 344
        Top = 620
        Width = 337
        Height = 17
        Caption = 'разрешить менять номер у "заблокированного заказа"'
        TabOrder = 34
      end
    end
    object TabSheet2: TTabSheet
      Caption = 'Печать'
      ImageIndex = 1
      object CheckBox14: TCheckBox
        Left = 4
        Top = 4
        Width = 329
        Height = 17
        Caption = 'показывать только выбранные печатные формы'
        TabOrder = 0
      end
      object LView: TADGListView
        Left = 4
        Top = 60
        Width = 681
        Height = 577
        Columns = <>
        HideSelection = False
        MultiSelect = True
        ReadOnly = True
        TabOrder = 1
      end
      object ToolBar1: TToolBar
        Left = 4
        Top = 28
        Width = 114
        Height = 31
        Align = alNone
        BorderWidth = 1
        ButtonHeight = 25
        ButtonWidth = 25
        EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
        EdgeInner = esNone
        Images = SahredDMod.ButList
        TabOrder = 2
        Transparent = True
        object ToolButton1: TToolButton
          Left = 0
          Top = 0
          Action = PrnAdd
        end
        object ToolButton3: TToolButton
          Left = 25
          Top = 0
          Action = PrnDelete
        end
        object ToolButton2: TToolButton
          Left = 50
          Top = 0
          Width = 8
          ImageIndex = 0
          Style = tbsSeparator
        end
        object ToolButton4: TToolButton
          Left = 58
          Top = 0
          Action = PrnMoveUp
        end
        object ToolButton5: TToolButton
          Left = 83
          Top = 0
          Action = PrnMoveDown
        end
      end
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList
    Left = 168
    object PrnAdd: TAction
      Caption = 'Добавить форму'
      Hint = 'Добавить форму'
      ImageIndex = 0
      OnExecute = PrnAddExecute
    end
    object PrnDelete: TAction
      Caption = 'Удалить форму'
      Hint = 'Удалить форму'
      ImageIndex = 1
      OnExecute = PrnDeleteExecute
    end
    object PrnMoveUp: TAction
      Caption = 'Поднять вверх'
      Hint = 'Поднять вверх'
      ImageIndex = 34
      OnExecute = PrnMoveUpExecute
    end
    object PrnMoveDown: TAction
      Caption = 'Опустить вниз'
      Hint = 'Опустить вниз'
      ImageIndex = 35
      OnExecute = PrnMoveDownExecute
    end
  end
end
