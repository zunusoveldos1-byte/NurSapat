object SeriaEditDlg: TSeriaEditDlg
  Left = 410
  Top = 440
  BorderStyle = bsDialog
  Caption = 'Редактор серии'
  ClientHeight = 83
  ClientWidth = 384
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
    Left = 56
    Top = 12
    Width = 31
    Height = 13
    Caption = 'Серия'
    Transparent = True
  end
  object Label2: TLabel
    Left = 8
    Top = 36
    Width = 79
    Height = 13
    Caption = 'Производитель'
    Transparent = True
  end
  object Label3: TLabel
    Left = 44
    Top = 60
    Width = 42
    Height = 13
    Caption = 'Система'
    Transparent = True
  end
  object OKBtn: TButton
    Left = 300
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 300
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object Edit1: TEdit
    Left = 92
    Top = 8
    Width = 201
    Height = 21
    TabOrder = 2
    OnChange = Edit1Change
  end
  object ComboBox1: TComboBox
    Left = 92
    Top = 32
    Width = 201
    Height = 21
    Style = csDropDownList
    DropDownCount = 20
    ItemHeight = 0
    TabOrder = 3
    OnChange = ComboBox1Change
  end
  object ComboBox2: TComboBox
    Left = 92
    Top = 56
    Width = 201
    Height = 21
    Style = csDropDownList
    DropDownCount = 20
    ItemHeight = 0
    TabOrder = 4
    OnChange = ComboBox1Change
  end
end
