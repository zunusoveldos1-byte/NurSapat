object SetUpPriseDlg: TSetUpPriseDlg
  Left = 423
  Top = 308
  BorderStyle = bsDialog
  Caption = 'Цена на материал'
  ClientHeight = 308
  ClientWidth = 486
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
  object Button1: TButton
    Left = 402
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object Button2: TButton
    Left = 402
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object TermoEnableCb: TCheckBox
    Left = 8
    Top = 8
    Width = 321
    Height = 17
    Caption = 'выбирать только профиля и штапики определенного типа'
    TabOrder = 2
    OnClick = TermoEnableCbClick
  end
  object TermoCb: TCheckBox
    Left = 342
    Top = 8
    Width = 53
    Height = 17
    Caption = 'термо'
    TabOrder = 3
    OnClick = TermoEnableCbClick
  end
  object LView: TADGListView
    Left = 8
    Top = 104
    Width = 250
    Height = 197
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    TabOrder = 4
    OnDblClick = PriseEditExecute
  end
  object ToolBar1: TToolBar
    Left = 8
    Top = 72
    Width = 89
    Height = 31
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 25
    ButtonWidth = 25
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList
    TabOrder = 5
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = PriseAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 25
      Top = 0
      Action = PriseEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton7: TToolButton
      Left = 50
      Top = 0
      Width = 8
      ImageIndex = 5
      Style = tbsSeparator
    end
    object ToolButton2: TToolButton
      Left = 58
      Top = 0
      Action = PriseDel
      ParentShowHint = False
      ShowHint = True
    end
  end
  object CheckBox1: TCheckBox
    Left = 8
    Top = 28
    Width = 321
    Height = 17
    Caption = 'выбирать только профиля и штапики с расценкой за 1 кг.'
    TabOrder = 6
    OnClick = TermoEnableCbClick
  end
  object CheckBox2: TCheckBox
    Left = 8
    Top = 48
    Width = 221
    Height = 17
    Caption = 'цену изменять на указанный %'
    TabOrder = 7
    OnClick = TermoEnableCbClick
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList
    Left = 368
    Top = 276
    object PriseAdd: TAction
      Hint = 'Добавить цвет'
      ImageIndex = 0
      OnExecute = PriseAddExecute
    end
    object PriseEdit: TAction
      Hint = 'Редактировать цену'
      ImageIndex = 2
      OnExecute = PriseEditExecute
    end
    object PriseDel: TAction
      Hint = 'Удалить цвет'
      ImageIndex = 1
      OnExecute = PriseDelExecute
    end
  end
end
