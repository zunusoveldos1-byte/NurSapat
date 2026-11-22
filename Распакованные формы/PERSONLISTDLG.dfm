object PersonListDlg: TPersonListDlg
  Left = 789
  Top = 208
  Hint = 'Пользователи'
  Caption = 'Пользователи'
  ClientHeight = 324
  ClientWidth = 239
  Color = clBtnFace
  Constraints.MinHeight = 100
  Constraints.MinWidth = 150
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnHide = FormHide
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object ToolBar1: TToolBar
    Left = 0
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
      Action = PersonAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = PersonEdit
      ParentShowHint = False
      ShowHint = True
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
      Action = PersonDelete
      ParentShowHint = False
      ShowHint = True
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
      Action = PersonClose
      ParentShowHint = False
      ShowHint = True
    end
  end
  object LView: TADGListView
    Left = 0
    Top = 36
    Width = 237
    Height = 285
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    TabOrder = 1
    OnDblClick = PersonEditExecute
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 184
    Top = 4
    object PersonList: TAction
      Caption = 'Пользователи'
      Hint = 'Пользователи'
      OnExecute = PersonListExecute
    end
    object PersonAdd: TAction
      Caption = 'Добавить пользователя'
      Hint = 'Добавить пользователя'
      ImageIndex = 0
      OnExecute = PersonAddExecute
    end
    object PersonDelete: TAction
      Caption = 'Удалить пользователя'
      Hint = 'Удалить пользователя'
      ImageIndex = 5
      OnExecute = PersonDeleteExecute
    end
    object PersonEdit: TAction
      Caption = 'Редактировать пользователя'
      Hint = 'Редактировать пользователя'
      ImageIndex = 7
      OnExecute = PersonEditExecute
    end
    object PersonClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = PersonCloseExecute
    end
  end
end
