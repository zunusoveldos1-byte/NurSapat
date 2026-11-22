object SkladDocListDlg: TSkladDocListDlg
  Left = 254
  Top = 153
  BorderIcons = [biSystemMenu, biMaximize]
  Caption = 'Склад: перечень документов'
  ClientHeight = 457
  ClientWidth = 699
  Color = clBtnFace
  Constraints.MinHeight = 300
  Constraints.MinWidth = 500
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
  object Label1: TLabel
    Left = 496
    Top = 12
    Width = 32
    Height = 13
    Caption = 'Склад'
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 489
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
      Action = SkladDocAdd
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = SkladDocEdit
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
      Action = SkladDocDelete
    end
    object ToolButton8: TToolButton
      Left = 101
      Top = 0
      Width = 8
      ImageIndex = 19
      Style = tbsSeparator
    end
    object ToolButton10: TToolButton
      Left = 109
      Top = 0
      Action = ExpFile
    end
    object ToolButton18: TToolButton
      Left = 140
      Top = 0
      Action = SkladExpDBFAll
    end
    object ToolButton9: TToolButton
      Left = 171
      Top = 0
      Action = CalcOstatok
    end
    object ToolButton15: TToolButton
      Left = 202
      Top = 0
      Action = CalcPlan
    end
    object ToolButton16: TToolButton
      Left = 233
      Top = 0
      Action = CalcInventar
    end
    object ToolButton13: TToolButton
      Left = 264
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object ToolButton11: TToolButton
      Left = 272
      Top = 0
      Action = SkladMinOst
    end
    object ToolButton12: TToolButton
      Left = 303
      Top = 0
      Action = SkladOtchetDeficit
    end
    object ToolButton14: TToolButton
      Left = 334
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object ToolButton17: TToolButton
      Left = 342
      Top = 0
      Action = SkladNewInvent
    end
    object ToolButton5: TToolButton
      Left = 373
      Top = 0
      Action = SkladPrintDoc
    end
    object ToolButton6: TToolButton
      Left = 404
      Top = 0
      Width = 8
      ImageIndex = 2
      Style = tbsSeparator
    end
    object ToolButton20: TToolButton
      Left = 412
      Top = 0
      Action = SkladNamesView
    end
    object ToolButton19: TToolButton
      Left = 443
      Top = 0
      Width = 8
      Caption = 'ToolButton19'
      ImageIndex = 23
      Style = tbsSeparator
    end
    object ToolButton7: TToolButton
      Left = 451
      Top = 0
      Action = CloseAction
    end
  end
  object LView: TADGListView
    Left = 0
    Top = 36
    Width = 697
    Height = 421
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    TabOrder = 1
    OnDblClick = SkladDocEditExecute
  end
  object ComboBox1: TComboBox
    Left = 532
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
    Left = 644
    object SkladDocAdd: TAction
      Caption = 'Добавить'
      Hint = 'Добавить документ'
      ImageIndex = 0
      OnExecute = SkladDocAddExecute
    end
    object SkladDocEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать документ'
      ImageIndex = 7
      OnExecute = SkladDocEditExecute
    end
    object SkladDocDelete: TAction
      Caption = 'Удалить'
      Hint = 'Удалить документ'
      ImageIndex = 5
      OnExecute = SkladDocDeleteExecute
    end
    object CalcOstatok: TAction
      Caption = 'Отчет'
      Hint = 'Отчет по складу'
      ImageIndex = 34
      OnExecute = CalcOstatokExecute
    end
    object CalcPlan: TAction
      Caption = 'План'
      Hint = 'Отчет по складу (планирование)'
      ImageIndex = 34
      OnExecute = CalcPlanExecute
    end
    object CloseAction: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = CloseActionExecute
    end
    object SkladPrintDoc: TAction
      Caption = 'Печать'
      Hint = 'Печать документа'
      ImageIndex = 14
      OnExecute = SkladPrintDocExecute
    end
    object ExpFile: TAction
      Caption = 'Экспорт в файл'
      Hint = 'Экспорт документа в файл'
      ImageIndex = 28
      OnExecute = ExpFileExecute
    end
    object SkladMinOst: TAction
      Caption = 'Неснижаемый остаток'
      Hint = 'Неснижаемый остаток'
      ImageIndex = 35
      OnExecute = SkladMinOstExecute
    end
    object SkladOtchetDeficit: TAction
      Caption = 'Ведомость дефицита'
      Hint = 'Ведомость дефицита'
      ImageIndex = 33
      OnExecute = SkladOtchetDeficitExecute
    end
    object CalcInventar: TAction
      Caption = 'Отчет по инвентаризации'
      Hint = 'Отчет по инвентаризации'
      ImageIndex = 34
      OnExecute = CalcInventarExecute
    end
    object SkladNewInvent: TAction
      Caption = 'Создать шаблон документа для инвентаризации'
      Hint = 'Создать шаблон документа для инвентаризации'
      ImageIndex = 32
      OnExecute = SkladNewInventExecute
    end
    object SkladExpDBFAll: TAction
      Caption = 'Экспортировать выбранные документы в DBF'
      Hint = 'Экспортировать выбранные документы в DBF'
      ImageIndex = 26
      OnExecute = SkladExpDBFAllExecute
    end
    object SkladNamesView: TAction
      Caption = 'Справочник складов'
      Hint = 'Справочник складов'
      ImageIndex = 36
      OnExecute = SkladNamesViewExecute
    end
  end
end
