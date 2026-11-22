object ElemChangeListDlg: TElemChangeListDlg
  Left = 227
  Top = 108
  Caption = 'Настройка замены элементов'
  ClientHeight = 571
  ClientWidth = 931
  Color = clBtnFace
  Constraints.MinHeight = 50
  Constraints.MinWidth = 400
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label2: TLabel
    Left = 608
    Top = 12
    Width = 31
    Height = 13
    Caption = 'Серия'
  end
  object LView: TADGListView
    Left = 2
    Top = 36
    Width = 925
    Height = 533
    Columns = <>
    ReadOnly = False
    TabOrder = 0
    OnDblClick = ElemChangeEditExecute
  end
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
    TabOrder = 1
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = ElemChangeAdd
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = ElemChangeEdit
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
      Action = ElemChangeDel
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
      Action = ElemChangeClose
    end
  end
  object ComboBox2: TComboBox
    Left = 644
    Top = 8
    Width = 281
    Height = 21
    Style = csOwnerDrawFixed
    DropDownCount = 30
    ItemHeight = 15
    TabOrder = 2
    OnChange = ComboBox2Change
    OnDrawItem = ComboBox2DrawItem
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 236
    Top = 8
    object ElemChangeAdd: TAction
      Caption = 'Добавить замену элементов'
      Hint = 'Добавить замену элементов'
      ImageIndex = 0
      Visible = False
    end
    object ElemChangeEdit: TAction
      Caption = 'Редактировать замену элементов'
      Hint = 'Редактировать замену элементов'
      ImageIndex = 7
      OnExecute = ElemChangeEditExecute
    end
    object ElemChangeDel: TAction
      Caption = 'Удалить замену элементов'
      Hint = 'Удалить замену элементов'
      ImageIndex = 5
      Visible = False
    end
    object ElemChangeClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = ElemChangeCloseExecute
    end
  end
end
