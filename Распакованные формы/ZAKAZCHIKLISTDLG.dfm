object ZakazchikListDlg: TZakazchikListDlg
  Left = 180
  Top = 166
  Hint = 'Заказчики'
  BorderIcons = [biSystemMenu, biMaximize, biHelp]
  Caption = 'Заказчики'
  ClientHeight = 380
  ClientWidth = 751
  Color = clBtnFace
  Constraints.MinHeight = 250
  Constraints.MinWidth = 400
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
      Action = ZakazchikAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = ZakazchikEdit
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
      Action = ZakazchikDelete
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
      Action = ZakazchikClose
      ParentShowHint = False
      ShowHint = True
    end
  end
  object LView: TADGListView
    Left = 0
    Top = 36
    Width = 749
    Height = 345
    Columns = <>
    ReadOnly = False
    PopupMenu = PopupMenu1
    TabOrder = 1
    OnDblClick = ZakazchikEditExecute
  end
  object PopupMenu1: TPopupMenu
    Left = 384
    Top = 4
    object N1: TMenuItem
      Action = ZakazchikAdd
    end
    object N2: TMenuItem
      Action = ZakazchikEdit
    end
    object N3: TMenuItem
      Caption = '-'
    end
    object N4: TMenuItem
      Action = ZakazchikDelete
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 416
    Top = 4
    object ZakazchikAdd: TAction
      Caption = 'Добавить'
      Hint = 'Добавить заказчика'
      ImageIndex = 0
      OnExecute = ZakazchikAddExecute
    end
    object ZakazchikEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать заказчика'
      ImageIndex = 7
      OnExecute = ZakazchikEditExecute
    end
    object ZakazchikDelete: TAction
      Caption = 'Удалить'
      Hint = 'Удалить заказчика'
      ImageIndex = 5
      OnExecute = ZakazchikDeleteExecute
    end
    object ZakazchikClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = ZakazchikCloseExecute
    end
  end
end
