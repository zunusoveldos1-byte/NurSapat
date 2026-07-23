object ColorListDlg: TColorListDlg
  Left = 442
  Top = 255
  Caption = 'Справочник цветов'
  ClientHeight = 269
  ClientWidth = 219
  Color = clBtnFace
  Constraints.MinHeight = 100
  Constraints.MinWidth = 100
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnHide = FormHide
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Shape1: TShape
    Left = 1
    Top = 40
    Width = 33
    Height = 209
  end
  object ListBox1: TListBox
    Left = 36
    Top = 40
    Width = 181
    Height = 209
    Style = lbOwnerDrawFixed
    ItemHeight = 15
    MultiSelect = True
    PopupMenu = PopupMenu1
    Sorted = True
    TabOrder = 0
    OnClick = ListBox1Click
    OnDblClick = ColorEditExecute
    OnDrawItem = ListBox1DrawItem
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
      Action = ColorAdd
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = ColorEdit
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
      Action = ColorDelete
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
      Action = ColorClose
    end
  end
  object SBar: TStatusBar
    Left = 0
    Top = 250
    Width = 219
    Height = 19
    Panels = <
      item
        Alignment = taCenter
        Width = 70
      end
      item
        Width = 50
      end>
    ExplicitWidth = 222
  end
  object PopupMenu1: TPopupMenu
    Left = 152
    Top = 124
    object N1: TMenuItem
      Action = ColorAdd
    end
    object N3: TMenuItem
      Action = ColorEdit
    end
    object N4: TMenuItem
      Caption = '-'
    end
    object N2: TMenuItem
      Action = ColorDelete
    end
    object N5: TMenuItem
      Caption = 'Удалить все цвета'
      OnClick = BitBtn4Click
    end
  end
  object ColorDialog1: TColorDialog
    Left = 184
    Top = 124
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 120
    Top = 124
    object ColorAdd: TAction
      Caption = 'Добавить цвет'
      Hint = 'Добавить цвет'
      ImageIndex = 0
      OnExecute = ColorAddExecute
    end
    object ColorEdit: TAction
      Caption = 'Редактировать цвет'
      Hint = 'Редактировать цвет'
      ImageIndex = 7
      OnExecute = ColorEditExecute
    end
    object ColorDelete: TAction
      Caption = 'Удалить цвет'
      Hint = 'Удалить цвет'
      ImageIndex = 5
      OnExecute = ColorDeleteExecute
    end
    object ColorEditPal: TAction
      Caption = 'Назначить цвет'
      Hint = 'Назначить цвет'
      ImageIndex = 12
      OnExecute = ColorEditPalExecute
    end
    object ColorClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = ColorCloseExecute
    end
  end
end
