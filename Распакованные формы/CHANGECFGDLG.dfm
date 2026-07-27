object ChangeCfgDlg: TChangeCfgDlg
  Left = 386
  Top = 276
  BorderStyle = bsDialog
  Caption = 'Выбор конфигурации'
  ClientHeight = 67
  ClientWidth = 345
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
    Left = 260
    Top = 8
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
    OnClick = OKBtnClick
  end
  object CancelBtn: TButton
    Left = 260
    Top = 36
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object ComboBox1: TComboBox
    Left = 12
    Top = 8
    Width = 225
    Height = 21
    Style = csDropDownList
    DropDownCount = 20
    ItemHeight = 0
    TabOrder = 2
  end
end
