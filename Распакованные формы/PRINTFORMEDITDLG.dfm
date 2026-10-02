object PrintFormEditDlg: TPrintFormEditDlg
  Left = 227
  Top = 108
  BorderIcons = [biSystemMenu, biMaximize]
  Caption = 'Редактор печатной формы'
  ClientHeight = 546
  ClientWidth = 787
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 628
    Top = 520
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 708
    Top = 520
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 2
  end
  object PageControl1: TPageControl
    Left = 0
    Top = 0
    Width = 787
    Height = 517
    ActivePage = TabSheet2
    TabOrder = 1
    object TabSheet1: TTabSheet
      Caption = 'Общие'
      object LabeledEdit1: TLabeledEdit
        Left = 104
        Top = 8
        Width = 209
        Height = 21
        EditLabel.Width = 73
        EditLabel.Height = 13
        EditLabel.Caption = 'Наименование'
        LabelPosition = lpLeft
        TabOrder = 0
      end
      object LabeledEdit2: TLabeledEdit
        Left = 104
        Top = 84
        Width = 41
        Height = 21
        EditLabel.Width = 31
        EditLabel.Height = 13
        EditLabel.Caption = 'Строк'
        LabelPosition = lpLeft
        TabOrder = 1
      end
      object LabeledEdit3: TLabeledEdit
        Left = 104
        Top = 108
        Width = 41
        Height = 21
        EditLabel.Width = 49
        EditLabel.Height = 13
        EditLabel.Caption = 'Столбцов'
        LabelPosition = lpLeft
        TabOrder = 2
      end
      object LabeledEdit4: TLabeledEdit
        Left = 272
        Top = 36
        Width = 41
        Height = 21
        EditLabel.Width = 81
        EditLabel.Height = 13
        EditLabel.Caption = 'Левое поле, мм.'
        LabelPosition = lpLeft
        TabOrder = 3
      end
      object LabeledEdit5: TLabeledEdit
        Left = 272
        Top = 60
        Width = 41
        Height = 21
        EditLabel.Width = 87
        EditLabel.Height = 13
        EditLabel.Caption = 'Правое поле, мм.'
        LabelPosition = lpLeft
        TabOrder = 4
      end
      object LabeledEdit6: TLabeledEdit
        Left = 272
        Top = 84
        Width = 41
        Height = 21
        EditLabel.Width = 92
        EditLabel.Height = 13
        EditLabel.Caption = 'Верхнее поле, мм.'
        LabelPosition = lpLeft
        TabOrder = 5
      end
      object LabeledEdit7: TLabeledEdit
        Left = 272
        Top = 108
        Width = 41
        Height = 21
        EditLabel.Width = 89
        EditLabel.Height = 13
        EditLabel.Caption = 'Нижнее поле, мм.'
        LabelPosition = lpLeft
        TabOrder = 6
      end
      object LabeledEdit8: TLabeledEdit
        Left = 104
        Top = 36
        Width = 41
        Height = 21
        EditLabel.Width = 95
        EditLabel.Height = 13
        EditLabel.Caption = 'Ширина листа, мм.'
        LabelPosition = lpLeft
        TabOrder = 7
      end
      object LabeledEdit9: TLabeledEdit
        Left = 104
        Top = 60
        Width = 41
        Height = 21
        EditLabel.Width = 92
        EditLabel.Height = 13
        EditLabel.Caption = 'Высота листа, мм.'
        LabelPosition = lpLeft
        TabOrder = 8
      end
      object CheckBox1: TCheckBox
        Left = 4
        Top = 140
        Width = 461
        Height = 17
        Caption = 'Альбомная ориентация страницы (иначе портретная)'
        TabOrder = 9
      end
      object CheckBox2: TCheckBox
        Left = 4
        Top = 156
        Width = 461
        Height = 17
        Caption = 
          'Направление построения документа слева направо (иначе традиционн' +
          'о сверху вниз)'
        TabOrder = 10
      end
      object ToolBar5: TToolBar
        Left = 448
        Top = 0
        Width = 69
        Height = 36
        Align = alNone
        BorderWidth = 1
        ButtonHeight = 30
        ButtonWidth = 31
        EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
        EdgeInner = esNone
        Images = SahredDMod.ButList24
        TabOrder = 11
        object ToolButton20: TToolButton
          Left = 0
          Top = 0
          Action = PrnFormSaveToFile
        end
        object ToolButton21: TToolButton
          Left = 31
          Top = 0
          Action = PrnFormLoadFromFile
        end
      end
      object LabeledEdit36: TLabeledEdit
        Left = 368
        Top = 8
        Width = 73
        Height = 21
        EditLabel.Width = 38
        EditLabel.Height = 13
        EditLabel.Caption = 'фильтр'
        LabelPosition = lpLeft
        TabOrder = 12
      end
      object CheckBox3: TCheckBox
        Left = 4
        Top = 172
        Width = 461
        Height = 17
        Caption = 'Разрешать печатать нумерацию страниц'
        TabOrder = 13
      end
    end
    object TabSheet2: TTabSheet
      Caption = 'Редактор'
      ImageIndex = 1
      OnResize = TabSheet2Resize
      object LView: TADGListView
        Left = 0
        Top = 36
        Width = 313
        Height = 385
        Columns = <>
        HideSelection = False
        MultiSelect = True
        ReadOnly = True
        PopupMenu = PopupMenu1
        TabOrder = 0
        OnSelectItem = LViewSelectItem
      end
      object ScrollBox1: TScrollBox
        Left = 316
        Top = 36
        Width = 461
        Height = 449
        HorzScrollBar.Tracking = True
        VertScrollBar.Tracking = True
        TabOrder = 1
        object PaintBox1: TPaintBox
          Left = 0
          Top = 0
          Width = 105
          Height = 105
          OnMouseDown = PaintBox1MouseDown
          OnMouseMove = PaintBox1MouseMove
          OnPaint = PaintBox1Paint
        end
      end
      object ToolBar3: TToolBar
        Left = 0
        Top = 0
        Width = 217
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
        object ToolButton9: TToolButton
          Left = 0
          Top = 0
          Action = PrintObjectAdd
          ParentShowHint = False
          ShowHint = True
        end
        object ToolButton17: TToolButton
          Left = 31
          Top = 0
          Width = 8
          ImageIndex = 6
          Style = tbsSeparator
        end
        object ToolButton15: TToolButton
          Left = 39
          Top = 0
          Action = PrintObjectCopy
        end
        object ToolButton16: TToolButton
          Left = 70
          Top = 0
          Action = PrintObjectPaste
        end
        object ToolButton11: TToolButton
          Left = 101
          Top = 0
          Width = 8
          ImageIndex = 2
          Style = tbsSeparator
        end
        object ToolButton13: TToolButton
          Left = 109
          Top = 0
          Action = PrintObjectUp
        end
        object ToolButton14: TToolButton
          Left = 140
          Top = 0
          Action = PrintObjectDown
        end
        object ToolButton10: TToolButton
          Left = 171
          Top = 0
          Width = 8
          ImageIndex = 6
          Style = tbsSeparator
        end
        object ToolButton12: TToolButton
          Left = 179
          Top = 0
          Action = PrintObjectDel
          ParentShowHint = False
          ShowHint = True
        end
      end
      object ToolBar4: TToolBar
        Left = 676
        Top = 0
        Width = 99
        Height = 36
        Align = alNone
        BorderWidth = 1
        ButtonHeight = 30
        ButtonWidth = 31
        EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
        EdgeInner = esNone
        Images = SahredDMod.ButList24
        TabOrder = 3
        object ToolButton18: TToolButton
          Left = 0
          Top = 0
          Action = PagePrev
        end
        object ToolButton1: TToolButton
          Left = 31
          Top = 0
          Action = PageRefresh
        end
        object ToolButton19: TToolButton
          Left = 62
          Top = 0
          Action = PageNext
        end
      end
      object Panel1: TPanel
        Left = 0
        Top = 424
        Width = 313
        Height = 61
        BevelOuter = bvLowered
        TabOrder = 4
      end
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 36
    Top = 520
    object PrintObjectAdd: TAction
      Caption = 'Добавить элемент'
      Hint = 'Добавить элемент'
      ImageIndex = 0
      OnExecute = PrintObjectAddExecute
    end
    object PrintObjectDel: TAction
      Caption = 'Удалить элемент'
      Hint = 'Удалить элемент'
      ImageIndex = 5
      OnExecute = PrintObjectDelExecute
    end
    object PrintObjectUp: TAction
      Caption = 'Поднять элемент вверх'
      Hint = 'Поднять элемент вверх'
      ImageIndex = 17
      OnExecute = PrintObjectUpExecute
    end
    object PrintObjectDown: TAction
      Caption = 'Опустить элемент вниз'
      Hint = 'Опустить элемент вниз'
      ImageIndex = 6
      OnExecute = PrintObjectUpExecute
    end
    object PrintObjectCopy: TAction
      Caption = 'Копировать элемент'
      Hint = 'Копировать элемент'
      ImageIndex = 24
      OnExecute = PrintObjectCopyExecute
      OnUpdate = PrintObjectCopyUpdate
    end
    object PrintObjectPaste: TAction
      Caption = 'Вставить элемент'
      Hint = 'Вставить элемент'
      ImageIndex = 25
      OnExecute = PrintObjectPasteExecute
      OnUpdate = PrintObjectPasteUpdate
    end
    object PageNext: TAction
      Caption = 'Следующая страница'
      Hint = 'Следующая страница'
      ImageIndex = 2
      OnExecute = PageNextExecute
      OnUpdate = PageNextUpdate
    end
    object PagePrev: TAction
      Caption = 'Предыдущая страница'
      Hint = 'Предыдущая страница'
      ImageIndex = 1
      OnExecute = PagePrevExecute
      OnUpdate = PagePrevUpdate
    end
    object PageRefresh: TAction
      Caption = 'Обновить страницу'
      Hint = 'Обновить страницу'
      ImageIndex = 15
      OnExecute = PageRefreshExecute
      OnUpdate = PageRefreshUpdate
    end
    object PrnFormSaveToFile: TAction
      Caption = 'Сохранить в файл печатную форму'
      Hint = 'Сохранить в файл печатную форму'
      ImageIndex = 16
      OnExecute = PrnFormSaveToFileExecute
    end
    object PrnFormLoadFromFile: TAction
      Caption = 'Открыть из файла печатную форму'
      Hint = 'Открыть из файла печатную форму'
      ImageIndex = 11
      OnExecute = PrnFormLoadFromFileExecute
      OnUpdate = PrnFormLoadFromFileUpdate
    end
  end
  object PopupMenu1: TPopupMenu
    Left = 4
    Top = 520
    object N3: TMenuItem
      Action = PrintObjectAdd
    end
    object N4: TMenuItem
      Caption = '-'
    end
    object N1: TMenuItem
      Action = PrintObjectCopy
    end
    object N2: TMenuItem
      Action = PrintObjectPaste
    end
    object N5: TMenuItem
      Caption = '-'
    end
    object N6: TMenuItem
      Action = PrintObjectDel
    end
  end
end
