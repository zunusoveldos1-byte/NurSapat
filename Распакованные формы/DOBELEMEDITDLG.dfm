object DobElemEditDlg: TDobElemEditDlg
  Left = 252
  Top = 293
  BorderStyle = bsDialog
  Caption = 'Редактор'
  ClientHeight = 69
  ClientWidth = 293
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 8
    Top = 12
    Width = 26
    Height = 13
    Caption = 'Цвет'
  end
  object Label2: TLabel
    Left = 8
    Top = 36
    Width = 35
    Height = 13
    Caption = 'Размер'
  end
  object OKBtn: TButton
    Left = 208
    Top = 8
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 208
    Top = 36
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object ComboBox1: TComboBox
    Left = 56
    Top = 8
    Width = 145
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 2
  end
  object Edit1: TEdit
    Left = 56
    Top = 32
    Width = 61
    Height = 21
    TabOrder = 3
  end
end
