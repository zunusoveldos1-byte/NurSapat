object LanguagesTranslatorEditDlg: TLanguagesTranslatorEditDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Перевод'
  ClientHeight = 60
  ClientWidth = 710
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 632
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 632
    Top = 31
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object TntEdit1: TEdit
    Left = 8
    Top = 8
    Width = 605
    Height = 21
    TabOrder = 2
  end
  object ComboBox1: TComboBox
    Left = 8
    Top = 32
    Width = 149
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 3
  end
end
