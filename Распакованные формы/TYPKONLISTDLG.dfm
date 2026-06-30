object TypKonListDlg: TTypKonListDlg
  Left = 285
  Top = 268
  Hint = 'Типовые конструкции'
  BorderStyle = bsDialog
  Caption = 'Типовые конструкции'
  ClientHeight = 524
  ClientWidth = 781
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 412
    Top = 16
    Width = 36
    Height = 13
    Caption = 'Группа'
    Transparent = True
  end
  object LBox: TListBox
    Left = 0
    Top = 36
    Width = 781
    Height = 485
    Style = lbOwnerDrawFixed
    Columns = 4
    ExtendedSelect = False
    ItemHeight = 160
    TabOrder = 0
    OnDblClick = TypKonCloseExecute
    OnDrawItem = LBoxDrawItem
    OnMeasureItem = LBoxMeasureItem
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 257
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
      Action = TypKonAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 25
      Top = 0
      Action = TypKonEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton4: TToolButton
      Left = 50
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object ToolButton2: TToolButton
      Left = 58
      Top = 0
      Action = TypKonDelete
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton11: TToolButton
      Left = 83
      Top = 0
      Action = TypKonDeleteAll
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton5: TToolButton
      Left = 108
      Top = 0
      Width = 22
      ImageIndex = 4
      Style = tbsSeparator
    end
    object ToolButton7: TToolButton
      Left = 130
      Top = 0
      Action = TypKonGroupAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton8: TToolButton
      Left = 155
      Top = 0
      Action = TypKonGroupEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton9: TToolButton
      Left = 180
      Top = 0
      Action = TypKonGroupDelete
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton10: TToolButton
      Left = 205
      Top = 0
      Width = 21
      ImageIndex = 19
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 226
      Top = 0
      Action = TypKonClose
      ParentShowHint = False
      ShowHint = True
    end
  end
  object GroupBox: TComboBox
    Left = 452
    Top = 12
    Width = 329
    Height = 21
    Style = csDropDownList
    DropDownCount = 20
    ItemHeight = 13
    TabOrder = 2
    OnChange = GroupBoxChange
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList
    Left = 272
    Top = 4
    object TypKonAdd: TAction
      Caption = 'Добавить в "Типовые конструкции"'
      Hint = 'Добавить в "Типовые конструкции"'
      ImageIndex = 0
      OnExecute = TypKonAddExecute
    end
    object TypKonClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 6
      OnExecute = TypKonCloseExecute
    end
    object TypKonEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать'
      ImageIndex = 2
      OnExecute = TypKonEditExecute
    end
    object TypKonDelete: TAction
      Caption = 'Удалить'
      Hint = 'Удалить'
      ImageIndex = 1
      OnExecute = TypKonDeleteExecute
    end
    object TypKonDeleteAll: TAction
      Caption = 'Удалить все типовые кострукции'
      Hint = 'Удалить все типовые кострукции'
      ImageIndex = 24
      OnExecute = TypKonDeleteAllExecute
    end
    object TypKonGroupAdd: TAction
      Caption = 'Добавить группу'
      Hint = 'Добавить группу'
      ImageIndex = 0
      OnExecute = TypKonGroupAddExecute
    end
    object TypKonGroupEdit: TAction
      Caption = 'Редактировать группу'
      Hint = 'Редактировать группу'
      ImageIndex = 2
      OnExecute = TypKonGroupEditExecute
    end
    object TypKonGroupDelete: TAction
      Caption = 'Удалить группу'
      Hint = 'Удалить группу'
      ImageIndex = 1
      OnExecute = TypKonGroupDeleteExecute
    end
  end
end
