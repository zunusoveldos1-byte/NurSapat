object ZakazListDlg: TZakazListDlg
  Left = 241
  Top = 185
  BorderIcons = [biSystemMenu, biMaximize]
  Caption = 'Заказы'
  ClientHeight = 515
  ClientWidth = 771
  Color = clBtnFace
  Constraints.MinHeight = 300
  Constraints.MinWidth = 750
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
  object PageControl1: TPageControl
    Left = 0
    Top = 0
    Width = 771
    Height = 515
    ActivePage = TabSheet2
    Align = alClient
    TabOrder = 0
    OnChange = PageControl1Change
    object TabSheet1: TTabSheet
      Caption = 'Заказы'
      OnResize = TabSheet1Resize
      object Label1: TLabel
        Left = 572
        Top = 8
        Width = 38
        Height = 13
        Caption = 'фильтр'
        FocusControl = ComboBox1
      end
      object ComboBox1: TComboBox
        Left = 616
        Top = 4
        Width = 141
        Height = 21
        Style = csDropDownList
        DropDownCount = 15
        ItemHeight = 0
        TabOrder = 0
        OnChange = ComboBox1Change
      end
      object ToolBar1: TToolBar
        Left = 0
        Top = 0
        Width = 465
        Height = 31
        Align = alNone
        BorderWidth = 1
        ButtonHeight = 25
        ButtonWidth = 25
        EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
        EdgeInner = esNone
        Images = SahredDMod.ButList
        TabOrder = 1
        object ToolButton3: TToolButton
          Left = 0
          Top = 0
          Action = ZakazesOpen
        end
        object ToolButton7: TToolButton
          Left = 25
          Top = 0
          Action = ZakazesRefresh
        end
        object ToolButton6: TToolButton
          Left = 50
          Top = 0
          Action = ZakazesSendEMail
        end
        object ToolButton14: TToolButton
          Left = 75
          Top = 0
          Action = ZakazesSaveSel2File
        end
        object ToolButton10: TToolButton
          Left = 100
          Top = 0
          Width = 8
          ImageIndex = 7
          Style = tbsSeparator
        end
        object ToolButton8: TToolButton
          Left = 108
          Top = 0
          Action = ZakazesRaskr
        end
        object ToolButton9: TToolButton
          Left = 133
          Top = 0
          Action = ZakazesSteklopak
        end
        object ToolButton11: TToolButton
          Left = 158
          Top = 0
          Action = ZakazesMatZajav
        end
        object ToolButton12: TToolButton
          Left = 183
          Top = 0
          Action = ZakazesFurnZajav
        end
        object ToolButton13: TToolButton
          Left = 208
          Top = 0
          Action = ZakazesOtchet
        end
        object ToolButton22: TToolButton
          Left = 233
          Top = 0
          Action = ZakazesOtchetOplata
        end
        object ToolButton16: TToolButton
          Left = 258
          Top = 0
          Action = ZakazesExpAny
        end
        object ToolButton23: TToolButton
          Left = 283
          Top = 0
          Action = ZakazesSelect
        end
        object ToolButton4: TToolButton
          Left = 308
          Top = 0
          Width = 8
          ImageIndex = 7
          Style = tbsSeparator
        end
        object ToolButton2: TToolButton
          Left = 316
          Top = 0
          Action = ZakazesDelete
        end
        object ToolButton1: TToolButton
          Left = 341
          Top = 0
          Width = 8
          ImageIndex = 2
          Style = tbsSeparator
        end
        object ToolButton15: TToolButton
          Left = 349
          Top = 0
          Action = ZakazesFiltrAdd
        end
        object ToolButton17: TToolButton
          Left = 374
          Top = 0
          Action = ZakazesFiltrEdit
        end
        object ToolButton18: TToolButton
          Left = 399
          Top = 0
          Action = ZakazesFiltrDel
        end
        object ToolButton19: TToolButton
          Left = 424
          Top = 0
          Width = 8
          Caption = 'ToolButton19'
          ImageIndex = 10
          Style = tbsSeparator
        end
        object ToolButton5: TToolButton
          Left = 432
          Top = 0
          Action = ZakazesClose
        end
      end
      object LView: TADGListView
        Left = 0
        Top = 32
        Width = 761
        Height = 433
        Columns = <>
        FullDrag = True
        LargeImages = Form1.EtapImageList
        ReadOnly = False
        PopupMenu = PopupMenu1
        TabOrder = 2
        OnDblClick = ZakazesOpenExecute
        OnInfoTip = LViewInfoTip
        OnSelectItem = LViewSelectItem
      end
      object StatusBar1: TStatusBar
        Left = 0
        Top = 468
        Width = 763
        Height = 19
        Panels = <>
      end
    end
    object TabSheet2: TTabSheet
      Caption = 'Конструкции'
      ImageIndex = 1
      OnResize = TabSheet2Resize
      object Label2: TLabel
        Left = 572
        Top = 8
        Width = 38
        Height = 13
        Caption = 'фильтр'
        FocusControl = ComboBox2
      end
      object LView2: TADGListView
        Left = 0
        Top = 32
        Width = 761
        Height = 433
        Columns = <>
        LargeImages = Form1.EtapImageList
        ReadOnly = False
        PopupMenu = PopupMenu2
        TabOrder = 0
        OnInfoTip = LViewInfoTip
        OnSelectItem = LViewSelectItem
      end
      object ComboBox2: TComboBox
        Left = 616
        Top = 4
        Width = 141
        Height = 21
        Style = csDropDownList
        DropDownCount = 15
        ItemHeight = 13
        TabOrder = 1
        OnChange = ComboBox1Change
      end
      object StatusBar2: TStatusBar
        Left = 0
        Top = 468
        Width = 763
        Height = 19
        Panels = <>
      end
    end
    object TabSheet3: TTabSheet
      Caption = 'Партии'
      ImageIndex = 2
      OnResize = TabSheet3Resize
      object Label3: TLabel
        Left = 572
        Top = 8
        Width = 38
        Height = 13
        Caption = 'фильтр'
        FocusControl = ComboBox3
      end
      object LView3: TADGListView
        Left = 0
        Top = 32
        Width = 761
        Height = 237
        Columns = <>
        LargeImages = Form1.EtapImageList
        ReadOnly = False
        PopupMenu = PopupMenu3
        TabOrder = 0
        OnInfoTip = LViewInfoTip
        OnSelectItem = LViewSelectItem
      end
      object StatusBar3: TStatusBar
        Left = 0
        Top = 272
        Width = 763
        Height = 19
        Align = alNone
        Panels = <>
      end
      object ComboBox3: TComboBox
        Left = 616
        Top = 4
        Width = 141
        Height = 21
        Style = csDropDownList
        DropDownCount = 15
        ItemHeight = 13
        TabOrder = 2
        OnChange = ComboBox1Change
      end
      object ToolBar2: TToolBar
        Left = 0
        Top = 0
        Width = 89
        Height = 31
        Align = alNone
        BorderWidth = 1
        ButtonHeight = 25
        ButtonWidth = 25
        EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
        EdgeInner = esNone
        Images = SahredDMod.ButList
        TabOrder = 3
        object ToolButton25: TToolButton
          Left = 0
          Top = 0
          Action = PartRaskr
        end
        object ToolButton21: TToolButton
          Left = 25
          Top = 0
          Action = PartSteklopak
        end
        object ToolButton20: TToolButton
          Left = 50
          Top = 0
          Width = 8
          Caption = 'ToolButton20'
          ImageIndex = 7
          Style = tbsSeparator
        end
        object ToolButton38: TToolButton
          Left = 58
          Top = 0
          Action = ZakazesClose
        end
      end
      object LView4: TADGListView
        Left = 0
        Top = 292
        Width = 761
        Height = 193
        Columns = <>
        LargeImages = Form1.EtapImageList
        ReadOnly = False
        TabOrder = 4
      end
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList
    Left = 664
    Top = 76
    object ZakazesClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 6
      OnExecute = ZakazesCloseExecute
    end
    object ZakazesOpen: TAction
      Caption = 'Открыть'
      Hint = 'Открыть заказ'
      ImageIndex = 15
      OnExecute = ZakazesOpenExecute
    end
    object ZakazesDelete: TAction
      Caption = 'Удалить'
      Hint = 'Удалить заказ'
      ImageIndex = 1
      OnExecute = ZakazesDeleteExecute
    end
    object ZakazesSendEMail: TAction
      Caption = 'Отправить по почте'
      Hint = 'Отправить заказ по почте'
      ImageIndex = 21
      OnExecute = ZakazesSendEMailExecute
    end
    object ZakazesRefresh: TAction
      Caption = 'Обновить справочник'
      Hint = 'Обновить справочник'
      ImageIndex = 25
      OnExecute = ZakazesRefreshExecute
    end
    object ZakazesRaskr: TAction
      Caption = 'Раскрой материалов по выбранным заказам'
      Hint = 'Раскрой материалов по выбранным заказам'
      ImageIndex = 26
      OnExecute = ZakazesRaskrExecute
    end
    object ZakazesSteklopak: TAction
      Caption = 
        'Заявка на стеклопакеты, москитные сетки, подоконники, отливы, фу' +
        'рнитуру по выбранным заказам'
      Hint = 
        'Заявка на стеклопакеты, москитные сетки, подоконники, отливы, фу' +
        'рнитуру по выбранным заказам'
      ImageIndex = 27
      OnExecute = ZakazesSteklopakExecute
    end
    object ZakazesMatZajav: TAction
      Caption = 'Складская заявка на материалы по выбранным заказам'
      Hint = 'Складская заявка на материалы по выбранным заказам'
      ImageIndex = 18
      OnExecute = ZakazesMatZajavExecute
      OnUpdate = ZakazesMatZajavUpdate
    end
    object ZakazesFurnZajav: TAction
      Caption = 'Складская заявка на фурнитуру по выбранным заказам'
      Hint = 'Складская заявка на фурнитуру по выбранным заказам'
      ImageIndex = 30
      OnExecute = ZakazesFurnZajavExecute
      OnUpdate = ZakazesFurnZajavUpdate
    end
    object ZakazesOtchet: TAction
      Caption = 'Отчет по заказам'
      Hint = 'Отчет по заказам'
      ImageIndex = 22
      OnExecute = ZakazesOtchetExecute
    end
    object ZakazesOtchetOplata: TAction
      Caption = 'Отчет по оплатам по выбранным заказам'
      Hint = 'Отчет по оплатам по выбранным заказам'
      ImageIndex = 22
      OnExecute = ZakazesOtchetOplataExecute
    end
    object ZakazesSaveSel2File: TAction
      Hint = 'Сохранить выбранные заказы в отдельные файлы'
      ImageIndex = 17
      OnExecute = ZakazesSaveSel2FileExecute
    end
    object ZakazesChart: TAction
      ShortCut = 24699
      OnExecute = ZakazesChartExecute
    end
    object ZakazesExpDBF: TAction
      Caption = 'Экспорт списания во внешний склад (DBF)'
      Hint = 'Экспорт списания во внешний склад (DBF)'
      ImageIndex = 36
      OnExecute = ZakazesExpDBFExecute
    end
    object ZakazesExpAny: TAction
      Caption = 'Экспорт данных'
      Hint = 'Экспорт данных'
      ImageIndex = 23
      OnExecute = ZakazesExpAnyExecute
    end
    object ZakazesFiltrAdd: TAction
      Caption = 'Добавить фильтр'
      Hint = 'Добавить фильтр'
      ImageIndex = 0
      OnExecute = ZakazesFiltrAddExecute
    end
    object ZakazesFiltrEdit: TAction
      Caption = 'Редактировать фильтр'
      Hint = 'Редактировать фильтр'
      ImageIndex = 2
      OnExecute = ZakazesFiltrEditExecute
    end
    object ZakazesFiltrDel: TAction
      Caption = 'Удалить фильтр'
      Hint = 'Удалить фильтр'
      ImageIndex = 1
      OnExecute = ZakazesFiltrDelExecute
    end
    object KonListPartCreate: TAction
      Caption = 'Создать партию'
      OnExecute = KonListPartCreateExecute
    end
    object KonListPartChange: TAction
      Caption = 'Изменить партию'
      OnExecute = KonListPartChangeExecute
    end
    object PartRaskr: TAction
      Caption = 'Раскрой материалов по выбранной партии'
      ImageIndex = 26
      OnExecute = PartRaskrExecute
    end
    object PartSteklopak: TAction
      Caption = 
        'Заявка на стеклопакеты, москитные сетки, подоконники, отливы, фу' +
        'рнитуру по выбранным партиям'
      Hint = 
        'Заявка на стеклопакеты, москитные сетки, подоконники, отливы, фу' +
        'рнитуру по выбранным партиям'
      ImageIndex = 27
      OnExecute = PartSteklopakExecute
    end
    object ZakazesSelect: TAction
      Caption = 'Выделение группы заказов'
      Hint = 'Выделение группы заказов'
      ImageIndex = 31
      OnExecute = ZakazesSelectExecute
    end
  end
  object PopupMenu1: TPopupMenu
    OnPopup = PopupMenu1Popup
    Left = 720
    Top = 76
    object N1: TMenuItem
      Caption = 'Цветовая группа'
      object N2: TMenuItem
        Caption = 'Нет'
        OnClick = N8Click
      end
      object N3: TMenuItem
        Tag = 1
        Caption = 'Красный'
        OnClick = N8Click
      end
      object N4: TMenuItem
        Tag = 2
        Caption = 'Синий'
        OnClick = N8Click
      end
      object N5: TMenuItem
        Tag = 3
        Caption = 'Зеленый'
        OnClick = N8Click
      end
      object N6: TMenuItem
        Tag = 4
        Caption = 'Желтый'
        OnClick = N8Click
      end
      object N7: TMenuItem
        Tag = 5
        Caption = 'Серый'
        OnClick = N8Click
      end
      object N8: TMenuItem
        Tag = 6
        Caption = 'Черный'
        OnClick = N8Click
      end
    end
    object N9: TMenuItem
      Caption = 'Внешний вид таблицы'
      object N10: TMenuItem
        Caption = 'Сохранить'
        OnClick = N10Click
      end
      object N11: TMenuItem
        Caption = 'Вернуть к первоначальному виду'
        OnClick = N11Click
      end
    end
    object N12: TMenuItem
      Caption = '-'
    end
    object N13: TMenuItem
      Caption = 'Установить состояние: "нет"'
      OnClick = N13Click
    end
    object N14: TMenuItem
      Caption = 'Установить состояние: "нужен"'
      OnClick = N14Click
    end
    object N15: TMenuItem
      Caption = 'Установить состояние: "в работе"'
      OnClick = N15Click
    end
    object N16: TMenuItem
      Caption = 'Установить состояние: "выполнен"'
      OnClick = N16Click
    end
  end
  object PopupMenu2: TPopupMenu
    OnPopup = PopupMenu2Popup
    Left = 720
    Top = 124
    object N17: TMenuItem
      Caption = 'Установить состояние: "нет"'
      OnClick = N17Click
    end
    object N18: TMenuItem
      Caption = 'Установить состояние: "нужен"'
      OnClick = N18Click
    end
    object N19: TMenuItem
      Caption = 'Установить состояние: "в работе"'
      OnClick = N19Click
    end
    object N20: TMenuItem
      Caption = 'Установить состояние: "выполнен"'
      OnClick = N20Click
    end
    object N21: TMenuItem
      Action = KonListPartCreate
    end
    object N22: TMenuItem
      Action = KonListPartChange
    end
  end
  object Timer1: TTimer
    Enabled = False
    Interval = 100
    OnTimer = Timer1Timer
    Left = 668
    Top = 128
  end
  object PopupMenu3: TPopupMenu
    OnPopup = PopupMenu3Popup
    Left = 720
    Top = 172
    object MenuItem1: TMenuItem
      Caption = 'Установить состояние: "нет"'
      OnClick = MenuItem1Click
    end
    object MenuItem2: TMenuItem
      Caption = 'Установить состояние: "нужен"'
      OnClick = MenuItem2Click
    end
    object MenuItem3: TMenuItem
      Caption = 'Установить состояние: "в работе"'
      OnClick = MenuItem3Click
    end
    object MenuItem4: TMenuItem
      Caption = 'Установить состояние: "выполнен"'
      OnClick = MenuItem4Click
    end
  end
end
