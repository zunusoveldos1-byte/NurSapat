object DobElemDlg: TDobElemDlg
  Left = 385
  Top = 341
  Caption = 'Установочные (монтажные) и дополнительные элементы'
  ClientHeight = 411
  ClientWidth = 585
  Color = clBtnFace
  Constraints.MinHeight = 150
  Constraints.MinWidth = 300
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
    Left = 0
    Top = 0
    Width = 169
    Height = 37
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
      Action = DobElemAdd
    end
    object ToolButton4: TToolButton
      Left = 31
      Top = 0
      Action = DobElemMatAdd
    end
    object ToolButton3: TToolButton
      Left = 62
      Top = 0
      Action = DobElemEdit
    end
    object ToolButton2: TToolButton
      Left = 93
      Top = 0
      Action = DobElemDel
    end
    object ToolButton5: TToolButton
      Left = 124
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 132
      Top = 0
      Action = DobElemClose
    end
  end
  object LView: TListView
    Left = 2
    Top = 40
    Width = 581
    Height = 369
    Columns = <>
    PopupMenu = PopupMenu1
    TabOrder = 1
    OnDblClick = LViewDblClick
    OnDeletion = LViewDeletion
  end
  object PopupMenu1: TPopupMenu
    MenuAnimation = [maLeftToRight, maTopToBottom]
    Left = 260
    object N1: TMenuItem
      Action = DobElemAdd
    end
    object N5: TMenuItem
      Action = DobElemMatAdd
    end
    object N4: TMenuItem
      Action = DobElemEdit
    end
    object N3: TMenuItem
      Caption = '-'
    end
    object N2: TMenuItem
      Action = DobElemDel
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 444
    Top = 4
    object DobElemAdd: TAction
      Caption = 'Добавить элемент'
      Hint = 'Добавить элемент'
      ImageIndex = 0
      ShortCut = 45
      OnExecute = DobElemAddExecute
      OnUpdate = DobElemAddUpdate
    end
    object DobElemMatAdd: TAction
      Caption = 'Добавить материал из справочника материалов'
      Hint = 'Добавить материал из справочника материалов'
      ImageIndex = 0
      OnExecute = DobElemMatAddExecute
    end
    object DobElemEdit: TAction
      Caption = 'Редактировать элемент'
      Hint = 'Редактировать элемент'
      ImageIndex = 7
      OnExecute = DobElemEditExecute
      OnUpdate = DobElemAddUpdate
    end
    object DobElemDel: TAction
      Caption = 'Удалить элемент'
      Hint = 'Удалить элемент'
      ImageIndex = 5
      ShortCut = 46
      OnExecute = DobElemDelExecute
      OnUpdate = DobElemAddUpdate
    end
    object DobElemClose: TAction
      ImageIndex = 22
      OnExecute = DobElemCloseExecute
    end
  end
end
