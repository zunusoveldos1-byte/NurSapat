object PostavschiksListDlg: TPostavschiksListDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Поставщики'
  ClientHeight = 438
  ClientWidth = 395
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
  object LView: TADGListView
    Left = 0
    Top = 40
    Width = 393
    Height = 397
    Columns = <>
    ReadOnly = False
    TabOrder = 0
    OnDblClick = PostavEditExecute
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 149
    Height = 37
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 30
    ButtonWidth = 31
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList24
    TabOrder = 1
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = PostavAdd
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = PostavEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton5: TToolButton
      Left = 62
      Top = 0
      Width = 8
      Caption = 'ToolButton5'
      ImageIndex = 23
      Style = tbsSeparator
    end
    object ToolButton2: TToolButton
      Left = 70
      Top = 0
      Action = PostavDel
    end
    object ToolButton4: TToolButton
      Left = 101
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 109
      Top = 0
      Action = PostavClose
      ParentShowHint = False
      ShowHint = True
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 320
    Top = 4
    object PostavAdd: TAction
      Caption = 'Добавить поставщика'
      Hint = 'Добавить поставщика'
      ImageIndex = 0
      OnExecute = PostavAddExecute
    end
    object PostavEdit: TAction
      Caption = 'Редактировать поставщика'
      Hint = 'Редактировать поставщика'
      ImageIndex = 7
      OnExecute = PostavEditExecute
    end
    object PostavDel: TAction
      Caption = 'Удалить поставщика'
      Hint = 'Удалить поставщика'
      ImageIndex = 5
      OnExecute = PostavDelExecute
    end
    object PostavClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = PostavCloseExecute
    end
  end
end
