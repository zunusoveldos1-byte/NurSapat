object LineArEditDlg: TLineArEditDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Редактор линий'
  ClientHeight = 179
  ClientWidth = 384
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 304
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 304
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object ADGListView1: TADGListView
    Left = 0
    Top = 44
    Width = 273
    Height = 133
    Columns = <>
    ReadOnly = False
    TabOrder = 2
    OnDblClick = DrawLineEditExecute
  end
  object ToolBar1: TToolBar
    Left = 4
    Top = 4
    Width = 109
    Height = 36
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 30
    ButtonWidth = 31
    Color = clBtnFace
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList24
    ParentColor = False
    TabOrder = 3
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = DrawLineAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton4: TToolButton
      Left = 31
      Top = 0
      Action = DrawLineEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 62
      Top = 0
      Width = 8
      ImageIndex = 2
      Style = tbsSeparator
    end
    object ToolButton2: TToolButton
      Left = 70
      Top = 0
      Action = DrawLineDel
      ParentShowHint = False
      ShowHint = True
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 344
    Top = 68
    object DrawLineAdd: TAction
      Caption = 'Добавить линию'
      Hint = 'Добавить линию'
      ImageIndex = 0
      OnExecute = DrawLineAddExecute
    end
    object DrawLineEdit: TAction
      Caption = 'Редактировать линию'
      Hint = 'Редактировать линию'
      ImageIndex = 7
      OnExecute = DrawLineEditExecute
    end
    object DrawLineDel: TAction
      Caption = 'Удалить линию'
      Hint = 'Удалить линию'
      ImageIndex = 5
      OnExecute = DrawLineDelExecute
    end
  end
end
