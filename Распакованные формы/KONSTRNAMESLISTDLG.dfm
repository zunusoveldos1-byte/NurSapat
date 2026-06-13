object KonstrNamesListDlg: TKonstrNamesListDlg
  Left = 278
  Top = 167
  BorderStyle = bsDialog
  Caption = 'Наименования конструкций'
  ClientHeight = 290
  ClientWidth = 783
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object LView: TListView
    Left = 180
    Top = 32
    Width = 601
    Height = 257
    Columns = <>
    RowSelect = True
    TabOrder = 0
    ViewStyle = vsReport
    OnColumnClick = LViewColumnClick
    OnCompare = LViewCompare
    OnDblClick = KonstrNameEditExecute
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 125
    Height = 31
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 25
    ButtonWidth = 25
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList
    TabOrder = 1
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = KonstrNameAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 25
      Top = 0
      Action = KonstrNameEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton7: TToolButton
      Left = 50
      Top = 0
      Width = 8
      ImageIndex = 5
      Style = tbsSeparator
    end
    object ToolButton2: TToolButton
      Left = 58
      Top = 0
      Action = KonstrNameDel
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton5: TToolButton
      Left = 83
      Top = 0
      Width = 8
      ImageIndex = 4
      Style = tbsSeparator
    end
    object ToolButton8: TToolButton
      Left = 91
      Top = 0
      Action = CLose
      ParentShowHint = False
      ShowHint = True
    end
  end
  object ListBox1: TListBox
    Left = 0
    Top = 32
    Width = 177
    Height = 257
    Style = lbOwnerDrawFixed
    ItemHeight = 160
    TabOrder = 2
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList
    Left = 524
    object KonstrNameAdd: TAction
      Caption = 'Добавить'
      Hint = 'Добавить наименование конмтрукции'
      ImageIndex = 0
      OnExecute = KonstrNameAddExecute
    end
    object KonstrNameEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать наименование конструкции'
      ImageIndex = 2
      OnExecute = KonstrNameEditExecute
    end
    object KonstrNameDel: TAction
      Caption = 'Удалить'
      Hint = 'Удалить наименование конструкции'
      ImageIndex = 1
      OnExecute = KonstrNameDelExecute
    end
    object CLose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 6
      OnExecute = CLoseExecute
    end
  end
end
