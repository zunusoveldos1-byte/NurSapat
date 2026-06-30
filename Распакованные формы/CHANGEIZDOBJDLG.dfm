object ChangeIzdObjDlg: TChangeIzdObjDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Выбор элемента'
  ClientHeight = 63
  ClientWidth = 556
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label7: TLabel
    Left = 120
    Top = 0
    Width = 43
    Height = 13
    Caption = 'Элемент'
    Transparent = True
  end
  object Label8: TLabel
    Left = 352
    Top = 0
    Width = 26
    Height = 13
    Caption = 'Цвет'
    Transparent = True
  end
  object OKBtn: TButton
    Left = 476
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 476
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object ComboBox1: TComboBox
    Left = 4
    Top = 16
    Width = 273
    Height = 21
    Style = csDropDownList
    DropDownCount = 30
    ItemHeight = 13
    TabOrder = 2
    OnChange = ComboBox1Change
  end
  object ComboBox2: TComboBox
    Left = 284
    Top = 16
    Width = 161
    Height = 21
    Style = csDropDownList
    DropDownCount = 30
    ItemHeight = 13
    TabOrder = 3
    OnChange = ComboBox1Change
  end
end
