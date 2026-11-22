object TypKonEditDlg: TTypKonEditDlg
  Left = 344
  Top = 175
  BorderStyle = bsDialog
  Caption = 'Редактор типовой конструкции'
  ClientHeight = 64
  ClientWidth = 450
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 8
    Top = 12
    Width = 73
    Height = 13
    Caption = 'Наименование'
    Transparent = True
  end
  object Label2: TLabel
    Left = 48
    Top = 36
    Width = 36
    Height = 13
    Caption = 'Группа'
    Transparent = True
  end
  object OKBtn: TButton
    Left = 368
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 368
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object Edit1: TEdit
    Left = 88
    Top = 8
    Width = 265
    Height = 21
    TabOrder = 2
    OnChange = Edit1Change
  end
  object GroupBox: TComboBox
    Left = 88
    Top = 32
    Width = 265
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 3
    OnChange = GroupBoxChange
  end
end
