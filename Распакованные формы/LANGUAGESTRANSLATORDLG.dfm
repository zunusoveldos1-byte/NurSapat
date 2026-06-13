object LanguagesTranslatorDlg: TLanguagesTranslatorDlg
  Left = 227
  Top = 108
  Caption = 'Перевод интерфейса'
  ClientHeight = 519
  ClientWidth = 839
  Color = clBtnFace
  Constraints.MinHeight = 100
  Constraints.MinWidth = 855
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnHide = FormHide
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Button1: TButton
    Left = 756
    Top = 4
    Width = 75
    Height = 25
    Action = TranslationSave
    TabOrder = 0
  end
  object LView: TADGListView
    Left = 0
    Top = 35
    Width = 837
    Height = 458
    Columns = <>
    ReadOnly = False
    PopupMenu = PopupMenu1
    TabOrder = 1
    OnDblClick = TranslationEditExecute
  end
  object StatusBar1: TStatusBar
    Left = 0
    Top = 496
    Width = 839
    Height = 23
    Panels = <>
  end
  object ActionList1: TActionList
    Left = 612
    Top = 8
    object TranslationEdit: TAction
      Caption = 'Редактировать'
      OnExecute = TranslationEditExecute
    end
    object TranslationDel: TAction
      Caption = 'Удалить'
      ShortCut = 46
      OnExecute = TranslationDelExecute
    end
    object TranslationSave: TAction
      Caption = 'Сохранить'
      OnExecute = TranslationSaveExecute
    end
  end
  object PopupMenu1: TPopupMenu
    Left = 540
    Top = 12
    object N1: TMenuItem
      Action = TranslationEdit
    end
    object N2: TMenuItem
      Caption = '-'
    end
    object N3: TMenuItem
      Action = TranslationDel
    end
  end
end
