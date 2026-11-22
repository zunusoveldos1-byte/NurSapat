object SkladOstsProfListDlg: TSkladOstsProfListDlg
  Left = 262
  Top = 142
  BorderStyle = bsDialog
  Caption = 'Склад: обрезки профиля (просмотр текущего состояния)'
  ClientHeight = 536
  ClientWidth = 765
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
    Left = 560
    Top = 12
    Width = 32
    Height = 13
    Caption = 'Склад'
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 77
    Height = 37
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 30
    ButtonWidth = 31
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList24
    TabOrder = 0
    object ToolButton4: TToolButton
      Left = 0
      Top = 0
      Action = SkladOstProfPrint
    end
    object ToolButton6: TToolButton
      Left = 31
      Top = 0
      Width = 8
      ImageIndex = 13
      Style = tbsSeparator
    end
    object ToolButton8: TToolButton
      Left = 39
      Top = 0
      Action = SkladOstProfClose
    end
  end
  object LView: TADGListView
    Left = 0
    Top = 40
    Width = 765
    Height = 493
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    TabOrder = 1
  end
  object ComboBox1: TComboBox
    Left = 596
    Top = 8
    Width = 165
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 2
    OnChange = ComboBox1Change
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 444
    object SkladOstProfClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = SkladOstProfCloseExecute
    end
    object SkladOstProfPrint: TAction
      Caption = 'Печать'
      Hint = 'Печать'
      ImageIndex = 14
      OnExecute = SkladOstProfPrintExecute
    end
  end
end
