object CursesListDlg: TCursesListDlg
  Left = 295
  Top = 136
  BorderStyle = bsDialog
  Caption = 'Курсы валют'
  ClientHeight = 375
  ClientWidth = 650
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
  object Label1: TLabel
    Tag = -700
    Left = 152
    Top = 0
    Width = 17
    Height = 13
    Caption = '....'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
    Transparent = True
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 147
    Height = 36
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 30
    ButtonWidth = 31
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList24
    TabOrder = 0
    Transparent = True
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = CursesAdd
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = CursesEdit
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
      Action = CursesDelete
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
      Action = Close
    end
  end
  object LView: TADGListView
    Left = 0
    Top = 36
    Width = 649
    Height = 341
    Columns = <>
    ReadOnly = False
    TabOrder = 1
    OnDblClick = LViewDblClick
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 392
    object CursesAdd: TAction
      Caption = 'Добавить'
      Hint = 'Добавить курсы'
      ImageIndex = 0
      OnExecute = CursesAddExecute
    end
    object CursesEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать'
      ImageIndex = 7
      OnExecute = CursesEditExecute
    end
    object CursesDelete: TAction
      Caption = 'Удалить'
      Hint = 'Удалить'
      ImageIndex = 5
      OnExecute = CursesDeleteExecute
    end
    object Close: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = CloseExecute
    end
  end
end
