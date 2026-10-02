object SkladNameListDlg: TSkladNameListDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Справочник складов'
  ClientHeight = 314
  ClientWidth = 226
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnHide = FormHide
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
      Action = SkladNameAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = SkladNameEdit
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
      Action = SkladNameDel
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
      Action = SkladNameClose
      ParentShowHint = False
      ShowHint = True
    end
  end
  object LView: TADGListView
    Left = 0
    Top = 36
    Width = 225
    Height = 277
    Columns = <>
    ReadOnly = False
    TabOrder = 1
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 172
    Top = 8
    object SkladNameAdd: TAction
      Caption = 'Добавить склад'
      Hint = 'Добавить склад'
      ImageIndex = 0
      OnExecute = SkladNameAddExecute
    end
    object SkladNameEdit: TAction
      Caption = 'Редактировать склад'
      Hint = 'Редактировать склад'
      ImageIndex = 7
      OnExecute = SkladNameEditExecute
    end
    object SkladNameDel: TAction
      Caption = 'Удалить склад'
      Hint = 'Удалить склад'
      ImageIndex = 5
      OnExecute = SkladNameDelExecute
    end
    object SkladNameClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = SkladNameCloseExecute
    end
  end
end
