object MaterialsListDlg: TMaterialsListDlg
  Left = 255
  Top = 171
  BorderIcons = [biSystemMenu, biMaximize]
  Caption = 'Справочник материалов'
  ClientHeight = 523
  ClientWidth = 939
  Color = clBtnFace
  Constraints.MinHeight = 300
  Constraints.MinWidth = 650
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
  object Label2: TLabel
    Left = 708
    Top = 12
    Width = 78
    Height = 13
    Caption = 'производитель'
    Transparent = True
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 247
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
      Action = MaterialAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = MaterialEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton7: TToolButton
      Left = 62
      Top = 0
      Width = 8
      ImageIndex = 5
      Style = tbsSeparator
    end
    object ToolButton2: TToolButton
      Left = 70
      Top = 0
      Action = MaterialDelete
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
    object ToolButton8: TToolButton
      Left = 109
      Top = 0
      Action = MaterialExpE
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton9: TToolButton
      Left = 140
      Top = 0
      Action = MaterialExpF
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton10: TToolButton
      Left = 171
      Top = 0
      Action = MaterialImpExcel
    end
    object ToolButton4: TToolButton
      Left = 202
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 210
      Top = 0
      Action = Close
      ParentShowHint = False
      ShowHint = True
    end
  end
  object SBar: TStatusBar
    Left = 0
    Top = 502
    Width = 939
    Height = 21
    Panels = <
      item
        Alignment = taCenter
        Text = '0'
        Width = 100
      end
      item
        Width = 0
      end>
  end
  object ProizvodCBox: TComboBox
    Left = 792
    Top = 8
    Width = 145
    Height = 21
    Style = csDropDownList
    DropDownCount = 30
    ItemHeight = 13
    TabOrder = 2
    OnChange = ProizvodCBoxChange
  end
  object LView: TADGListView
    Left = 0
    Top = 36
    Width = 937
    Height = 465
    Columns = <>
    ReadOnly = False
    PopupMenu = PopupMenu1
    TabOrder = 3
    OnClick = LViewClick
    OnDblClick = MaterialEditExecute
  end
  object PopupMenu1: TPopupMenu
    Left = 640
    Top = 56
    object N1: TMenuItem
      Action = MaterialAdd
    end
    object N3: TMenuItem
      Action = MaterialEdit
    end
    object N6: TMenuItem
      Caption = '-'
    end
    object N2: TMenuItem
      Action = MaterialDelete
    end
    object N4: TMenuItem
      Caption = '-'
    end
    object N9: TMenuItem
      Action = MaterialDeleteReplace
    end
    object N5: TMenuItem
      Action = DelAllMatAct
    end
    object N8: TMenuItem
      Action = ClearAllPriseAct
    end
    object N7: TMenuItem
      Caption = 'Изменить цену на материалы'
      OnClick = N7Click
    end
    object N10: TMenuItem
      Action = Material20Pup
    end
    object N11: TMenuItem
      Action = Material20PDown
    end
    object N12: TMenuItem
      Action = MaterialSetDopDefault
      Caption = 'Установить "печать на ком. предл." по умолчанию'
    end
    object N13: TMenuItem
      Action = MaterialImportFromFile
    end
    object N14: TMenuItem
      Action = MaterialChangeProizvod
    end
    object N15: TMenuItem
      Action = MaterialChangeOthod
    end
    object N34: TMenuItem
      Action = MaterialChangeLongP
    end
    object N16: TMenuItem
      Caption = 'Установить опции'
      object N18: TMenuItem
        Caption = 'печать на "сборочном чертеже"'
        OnClick = MaterialPrintOptionExecute
      end
      object N19: TMenuItem
        Caption = 'группировать позиции'
        OnClick = MaterialPrintOptionExecute
      end
      object N20: TMenuItem
        Caption = '-'
      end
      object N21: TMenuItem
        Caption = 'печать на "коммерческом предложении"'
        OnClick = MaterialPrintOptionExecute
      end
      object N22: TMenuItem
        Caption = 'группировать позиции'
        OnClick = MaterialPrintOptionExecute
      end
      object N23: TMenuItem
        Caption = '-'
      end
      object N24: TMenuItem
        Caption = 'печать на "отправном листе"'
        OnClick = MaterialPrintOptionExecute
      end
      object N25: TMenuItem
        Caption = 'группировать позиции'
        OnClick = MaterialPrintOptionExecute
      end
      object N38: TMenuItem
        Caption = '-'
      end
      object N39: TMenuItem
        Caption = 'использовать в доп. мат. в полной программе'
        OnClick = MaterialPrintOptionExecute
      end
      object N40: TMenuItem
        Caption = 'использовать в доп. мат. в дилерской программе'
        OnClick = MaterialPrintOptionExecute
      end
    end
    object N17: TMenuItem
      Caption = 'Снять опции'
      object N26: TMenuItem
        Caption = 'печать на "сборочном чертеже"'
        OnClick = MaterialPrintOptionExecute
      end
      object N27: TMenuItem
        Caption = 'группировать позиции'
        OnClick = MaterialPrintOptionExecute
      end
      object N28: TMenuItem
        Caption = '-'
      end
      object N29: TMenuItem
        Caption = 'печать на "коммерческом предложении"'
        OnClick = MaterialPrintOptionExecute
      end
      object N30: TMenuItem
        Caption = 'группировать позиции'
        OnClick = MaterialPrintOptionExecute
      end
      object N31: TMenuItem
        Caption = '-'
      end
      object N32: TMenuItem
        Caption = 'печать на "отправном листе"'
        OnClick = MaterialPrintOptionExecute
      end
      object N33: TMenuItem
        Caption = 'группировать позиции'
        OnClick = MaterialPrintOptionExecute
      end
      object N37: TMenuItem
        Caption = '-'
      end
      object N41: TMenuItem
        Caption = 'использовать в доп. мат. в полной программе'
        OnClick = MaterialPrintOptionExecute
      end
      object N42: TMenuItem
        Caption = 'использовать в доп. мат. в дилерской программе'
        OnClick = MaterialPrintOptionExecute
      end
    end
    object N35: TMenuItem
      Action = MaterialPostavAdd
    end
    object N36: TMenuItem
      Action = MaterialPostavDel
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 672
    Top = 56
    object DelAllMatAct: TAction
      Caption = 'Удалить все материалы'
      Hint = 'Удалить все материалы'
      OnExecute = DelAllMatActExecute
    end
    object ClearAllPriseAct: TAction
      Caption = 'Очистить все цены'
      Hint = 'Очистить все цены'
      OnExecute = ClearAllPriseActExecute
    end
    object MaterialAdd: TAction
      Caption = 'Добавить'
      Hint = 'Добавить материал'
      ImageIndex = 0
      OnExecute = MaterialAddExecute
    end
    object MaterialEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать материал'
      ImageIndex = 7
      ShortCut = 13
      OnExecute = MaterialEditExecute
    end
    object MaterialDelete: TAction
      Caption = 'Удалить'
      Hint = 'Удалить материал'
      ImageIndex = 5
      OnExecute = MaterialDeleteExecute
    end
    object MaterialDeleteReplace: TAction
      Caption = 'Удалить с заменой'
      Hint = 'Удалить материал с заменой на другой'
      OnExecute = MaterialDeleteReplaceExecute
    end
    object Close: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = CloseExecute
    end
    object MaterialChangeView: TAction
      Caption = 'Смена вида'
      Hint = 'Смена вида'
      OnExecute = MaterialChangeViewExecute
    end
    object MaterialExpE: TAction
      Caption = 'Экспорт в MS Excel'
      Hint = 'Экспорт в MS Excel'
      ImageIndex = 27
      OnExecute = MaterialExpEExecute
    end
    object MaterialExpF: TAction
      Caption = 'Экспорт в файл'
      Hint = 'Экспорт в файл'
      ImageIndex = 28
      OnExecute = MaterialExpFExecute
    end
    object MaterialImpExcel: TAction
      Caption = 'Импорт цен из файла MS Excel'
      Hint = 'Импорт цен из файла MS Excel'
      ImageIndex = 30
      OnExecute = MaterialImpExcelExecute
    end
    object Material20Pup: TAction
      Caption = 'Повысить цены на %НДС'
      OnExecute = Material20PupExecute
    end
    object Material20PDown: TAction
      Caption = 'Понизить цены на %НДС'
      OnExecute = Material20PDownExecute
    end
    object MaterialSetDopDefault: TAction
      Caption = 'Установить "печать в доп. эл-ах" по умолчанию'
      OnExecute = MaterialSetDopDefaultExecute
    end
    object MaterialImportFromFile: TAction
      Caption = 'Импортировать из файла'
      Hint = 'Импортировать из файла'
      Visible = False
      OnExecute = MaterialImportFromFileExecute
    end
    object MaterialChangeValuteToEuro: TAction
      ShortCut = 49221
      OnExecute = MaterialChangeValuteToEuroExecute
    end
    object MaterialChangeProizvod: TAction
      Caption = 'Изменить производителя'
      Hint = 'Изменить производителя'
      OnExecute = MaterialChangeProizvodExecute
    end
    object MaterialChangeOthod: TAction
      Caption = 'Изменить % отхода'
      OnExecute = MaterialChangeOthodExecute
    end
    object MaterialPrintOption: TAction
      OnExecute = MaterialPrintOptionExecute
    end
    object MaterialDeleteUnused: TAction
      ShortCut = 49237
      OnExecute = MaterialDeleteUnusedExecute
    end
    object MaterialChangeLongP: TAction
      Caption = 'Изменить "длину прута"'
      OnExecute = MaterialChangeLongPExecute
    end
    object MaterialPostavAdd: TAction
      Caption = 'Добавить поставщика к материалам'
      Hint = 'Добавить поставщика к материалам'
      OnExecute = MaterialPostavAddExecute
    end
    object MaterialPostavDel: TAction
      Caption = 'Удалить поставщика из материалов'
      Hint = 'Удалить поставщика из материалов'
      OnExecute = MaterialPostavDelExecute
    end
  end
end
