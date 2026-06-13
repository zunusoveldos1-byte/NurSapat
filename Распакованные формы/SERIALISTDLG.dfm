object SeriaListDlg: TSeriaListDlg
  Left = 322
  Top = 179
  Caption = 'Серии'
  ClientHeight = 368
  ClientWidth = 316
  Color = clBtnFace
  Constraints.MinHeight = 100
  Constraints.MinWidth = 200
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnHide = FormHide
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object ToolBar1: TToolBar
    Left = 4
    Top = 0
    Width = 146
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
      Action = SeriaAdd
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = SeriaEdit
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
      Action = SeriaDelete
    end
    object ToolButton5: TToolButton
      Left = 101
      Top = 0
      Width = 8
      ImageIndex = 2
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 109
      Top = 0
      Action = Close
    end
  end
  object LView: TADGListView
    Left = 4
    Top = 36
    Width = 309
    Height = 329
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    TabOrder = 1
    OnDblClick = SeriaEditExecute
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 284
    object SeriaAdd: TAction
      Caption = 'Добавить серию'
      Hint = 'Добавить серию'
      ImageIndex = 0
      OnExecute = SeriaAddExecute
    end
    object SeriaEdit: TAction
      Caption = 'Редактировать серию'
      Hint = 'Редактировать серию'
      ImageIndex = 7
      OnExecute = SeriaEditExecute
    end
    object SeriaDelete: TAction
      Caption = 'Удалить серию'
      Hint = 'Удалить серию'
      ImageIndex = 5
      OnExecute = SeriaDeleteExecute
    end
    object Close: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = CloseExecute
    end
  end
end
