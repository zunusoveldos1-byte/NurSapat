object DefaultListDlg: TDefaultListDlg
  Left = 382
  Top = 192
  BorderStyle = bsDialog
  Caption = 'Шаблоны заказов'
  ClientHeight = 351
  ClientWidth = 380
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 147
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
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = DefaultAdd
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = DefaultEdit
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
      Action = DefaultDelete
    end
    object ToolButton5: TToolButton
      Left = 101
      Top = 0
      Width = 8
      ImageIndex = 4
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 109
      Top = 0
      Action = Close
    end
  end
  object LView: TListView
    Left = 0
    Top = 40
    Width = 377
    Height = 309
    Columns = <>
    TabOrder = 1
    OnDblClick = DefaultEditExecute
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 348
    Top = 4
    object DefaultAdd: TAction
      Caption = 'Добавить'
      Hint = 'Добавить шаблон'
      ImageIndex = 0
      OnExecute = DefaultAddExecute
    end
    object DefaultEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать шаблон'
      ImageIndex = 7
      OnExecute = DefaultEditExecute
    end
    object DefaultDelete: TAction
      Caption = 'Удалить'
      Hint = 'Удалить шаблон'
      ImageIndex = 5
      OnExecute = DefaultDeleteExecute
    end
    object Close: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = CloseExecute
    end
  end
end
