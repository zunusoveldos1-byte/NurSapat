object SkladDocEditDlg: TSkladDocEditDlg
  Left = 192
  Top = 255
  Caption = 'Редактор материалов в документе'
  ClientHeight = 529
  ClientWidth = 808
  Color = clBtnFace
  Constraints.MinHeight = 300
  Constraints.MinWidth = 820
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 32
    Top = 8
    Width = 31
    Height = 13
    Caption = 'Номер'
    Transparent = True
  end
  object Label2: TLabel
    Left = 40
    Top = 32
    Width = 26
    Height = 13
    Caption = 'Дата'
    Transparent = True
  end
  object Label3: TLabel
    Left = 47
    Top = 56
    Width = 18
    Height = 13
    Caption = 'Тип'
    Transparent = True
  end
  object Label4: TLabel
    Left = 3
    Top = 80
    Width = 61
    Height = 13
    Caption = 'Примечание'
    Transparent = True
  end
  object Label5: TLabel
    Left = 319
    Top = 56
    Width = 32
    Height = 13
    Caption = 'Склад'
    Transparent = True
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 120
    Width = 115
    Height = 31
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 25
    ButtonWidth = 25
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList
    TabOrder = 0
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = SkladKolAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton5: TToolButton
      Left = 25
      Top = 0
      Action = SkladKolAddAll
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 50
      Top = 0
      Action = SkladKolEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton4: TToolButton
      Left = 75
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object ToolButton2: TToolButton
      Left = 83
      Top = 0
      Action = SkladKolDelete
      ParentShowHint = False
      ShowHint = True
    end
  end
  object Edit1: TEdit
    Left = 72
    Top = 4
    Width = 133
    Height = 21
    TabOrder = 1
    OnChange = Edit1Change
  end
  object TypBox: TComboBox
    Left = 72
    Top = 52
    Width = 233
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 2
    OnChange = TypBoxChange
  end
  object Edit2: TEdit
    Left = 72
    Top = 76
    Width = 489
    Height = 21
    TabOrder = 3
    OnChange = Edit1Change
  end
  object Edit3: TEdit
    Left = 72
    Top = 28
    Width = 133
    Height = 21
    TabOrder = 4
  end
  object CheckBox1: TCheckBox
    Left = 260
    Top = 128
    Width = 297
    Height = 17
    Caption = 'Использовать свои курсы валют для этого документа'
    TabOrder = 5
    OnClick = CheckBox1Click
  end
  object VEditor: TValueListEditor
    Left = 564
    Top = 4
    Width = 153
    Height = 145
    DefaultRowHeight = 16
    Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goColSizing, goEditing, goThumbTracking]
    Strings.Strings = (
      '=')
    TabOrder = 6
    TitleCaptions.Strings = (
      'Валюта'
      'Курс')
    OnSetEditText = VEditorSetEditText
    ColWidths = (
      86
      61)
  end
  object StatusBar1: TStatusBar
    Left = 0
    Top = 510
    Width = 808
    Height = 19
    Panels = <
      item
        Alignment = taCenter
        Width = 60
      end
      item
        Width = 500
      end
      item
        Width = 50
      end>
  end
  object OKBtn: TButton
    Left = 724
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 8
  end
  object CancelBtn: TButton
    Left = 724
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 9
  end
  object LView: TADGListView
    Left = 0
    Top = 152
    Width = 805
    Height = 357
    Columns = <>
    ReadOnly = False
    TabOrder = 10
    OnClick = LViewClick
    OnDblClick = LViewDblClick
  end
  object ComboBox1: TComboBox
    Left = 360
    Top = 52
    Width = 201
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 11
    OnChange = TypBoxChange
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList
    Left = 680
    Top = 500
    object SkladKolAdd: TAction
      Caption = 'Добавить'
      Hint = 'Добавить материал'
      ImageIndex = 0
      OnExecute = SkladKolAddExecute
    end
    object SkladKolEdit: TAction
      Caption = 'Редактировать'
      Hint = 'Редактировать материал'
      ImageIndex = 2
      OnExecute = SkladKolEditExecute
    end
    object SkladKolDelete: TAction
      Caption = 'Удалить'
      Hint = 'Удалить материал'
      ImageIndex = 1
      OnExecute = SkladKolDeleteExecute
    end
    object SkladKolAddAll: TAction
      Hint = 'Добавить группу материалов'
      ImageIndex = 33
      OnExecute = SkladKolAddAllExecute
    end
  end
end
