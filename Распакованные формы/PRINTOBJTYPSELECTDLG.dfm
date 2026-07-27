object PrintObjTypSelectDlg: TPrintObjTypSelectDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Выберите тип элемента'
  ClientHeight = 67
  ClientWidth = 384
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 300
    Top = 8
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
    OnClick = OKBtnClick
  end
  object CancelBtn: TButton
    Left = 300
    Top = 36
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object ComboBox1: TComboBox
    Left = 8
    Top = 8
    Width = 277
    Height = 21
    Style = csDropDownList
    DropDownCount = 20
    ItemHeight = 13
    TabOrder = 2
  end
end
