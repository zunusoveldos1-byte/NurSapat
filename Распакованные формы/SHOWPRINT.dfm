object ShowPrint: TShowPrint
  Left = 309
  Top = 233
  BorderIcons = [biSystemMenu, biMaximize]
  Caption = 'Печать'
  ClientHeight = 549
  ClientWidth = 791
  Color = clBtnFace
  Constraints.MinHeight = 300
  Constraints.MinWidth = 795
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnHide = FormHide
  OnMouseWheel = FormMouseWheel
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object ScrollBox1: TScrollBox
    Left = 4
    Top = 188
    Width = 781
    Height = 357
    HorzScrollBar.Tracking = True
    VertScrollBar.Tracking = True
    AutoScroll = False
    TabOrder = 0
    OnMouseWheel = ScrollBox1MouseWheel
    object PaintBox1: TPaintBox
      Left = 0
      Top = 0
      Width = 105
      Height = 105
      OnPaint = PaintBox1Paint
    end
  end
  object LView: TADGListView
    Left = 4
    Top = 4
    Width = 289
    Height = 181
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    PopupMenu = PopupMenu1
    TabOrder = 1
    OnSelectItem = LViewSelectItem
  end
  object ToolBar1: TToolBar
    Left = 392
    Top = 4
    Width = 293
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
      Action = PrnPrev
    end
    object ToolButton2: TToolButton
      Left = 31
      Top = 0
      Action = PrnNext
    end
    object ToolButton9: TToolButton
      Left = 62
      Top = 0
      Width = 8
      ImageIndex = 8
      Style = tbsSeparator
    end
    object ToolButton4: TToolButton
      Left = 70
      Top = 0
      Action = PrnSetup
    end
    object ToolButton5: TToolButton
      Left = 101
      Top = 0
      Action = PrnPrint
    end
    object ToolButton11: TToolButton
      Left = 132
      Top = 0
      Width = 8
      ImageIndex = 22
      Style = tbsSeparator
    end
    object ToolButton3: TToolButton
      Left = 140
      Top = 0
      Action = PrnSaveImage
    end
    object ToolButton10: TToolButton
      Left = 171
      Top = 0
      Width = 22
      ImageIndex = 9
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 193
      Top = 0
      Action = PrnExpWord
    end
    object ToolButton7: TToolButton
      Left = 224
      Top = 0
      Action = PrnExpExcel
    end
    object ToolButton8: TToolButton
      Left = 255
      Top = 0
      Action = PrnExpFile
    end
  end
  object Edit1: TEdit
    Left = 304
    Top = 8
    Width = 37
    Height = 27
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Tahoma'
    Font.Style = []
    ParentFont = False
    TabOrder = 3
    OnChange = Edit1Change
  end
  object Edit2: TEdit
    Left = 348
    Top = 8
    Width = 37
    Height = 27
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Tahoma'
    Font.Style = []
    ParentFont = False
    ReadOnly = True
    TabOrder = 4
  end
  object CheckBox1: TCheckBox
    Left = 692
    Top = 20
    Width = 57
    Height = 17
    Caption = 'поля'
    TabOrder = 5
    OnClick = CheckBox1Click
  end
  object CheckBox2: TCheckBox
    Left = 692
    Top = 4
    Width = 57
    Height = 17
    Caption = 'test'
    TabOrder = 6
    Visible = False
    OnClick = CheckBox1Click
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 304
    Top = 40
    object PrnPrev: TAction
      Caption = 'Предыдущая страница'
      Hint = 'Предыдущая страница'
      ImageIndex = 1
      OnExecute = PrnPrevExecute
    end
    object PrnNext: TAction
      Caption = 'Следующая страница'
      Hint = 'Следующая страница'
      ImageIndex = 2
      OnExecute = PrnNextExecute
    end
    object PrnExpFile: TAction
      Tag = 4
      Caption = 'Экспорт в файл'
      Hint = 'Экспорт в файл'
      ImageIndex = 21
      OnExecute = PrnExpFileExecute
    end
    object PrnExpWord: TAction
      Tag = 1
      Caption = 'Экспорт в MS Word'
      Hint = 'Экспорт в MS Word'
      ImageIndex = 18
      OnExecute = PrnExpWordExecute
    end
    object PrnExpExcel: TAction
      Tag = 2
      Caption = 'Экспорт в MS Excel'
      Hint = 'Экспорт в MS Excel'
      ImageIndex = 8
      OnExecute = PrnExpExcelExecute
    end
    object PrnSaveImage: TAction
      Caption = 'Сохранить как изображение'
      Hint = 'Сохранить как изображение'
      ImageIndex = 9
      OnExecute = PrnSaveImageExecute
    end
    object PrnSetup: TAction
      Caption = 'Параметры печати'
      Hint = 'Параметры печати'
      ImageIndex = 13
      OnExecute = PrnSetupExecute
    end
    object PrnPrint: TAction
      Caption = 'Печать на принтер'
      Hint = 'Печать на принтер'
      ImageIndex = 14
      OnExecute = PrnPrintExecute
    end
    object PrnFormEdit: TAction
      Caption = 'Редактировать печатную форму'
      Hint = 'Редактировать печатную форму'
      OnExecute = PrnFormEditExecute
    end
    object PrnFormAdd: TAction
      Caption = 'Добавить печатную форму'
      Hint = 'Добавить печатную форму'
      OnExecute = PrnFormAddExecute
    end
    object PrnFormAddLike: TAction
      Caption = 'Добавить печатную форму по образцу текущей'
      Hint = 'Добавить печатную форму по образцу текущей'
      OnExecute = PrnFormAddLikeExecute
      OnUpdate = PrnFormAddLikeUpdate
    end
    object PrnFormDel: TAction
      Caption = 'Удалить печатную форму'
      Hint = 'Удалить печатную форму'
      OnExecute = PrnFormDelExecute
    end
  end
  object PopupMenu1: TPopupMenu
    Left = 368
    Top = 40
    object N2: TMenuItem
      Action = PrnFormAdd
    end
    object N6: TMenuItem
      Action = PrnFormAddLike
    end
    object N5: TMenuItem
      Caption = '-'
    end
    object N1: TMenuItem
      Action = PrnFormEdit
    end
    object N7: TMenuItem
      Caption = '-'
    end
    object N4: TMenuItem
      Action = PrnFormDel
    end
  end
  object Timer1: TTimer
    Enabled = False
    Interval = 2000
    OnTimer = Timer1Timer
    Left = 424
    Top = 44
  end
end
