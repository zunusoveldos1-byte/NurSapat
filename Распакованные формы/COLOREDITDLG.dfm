object ColorEditDlg: TColorEditDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Редактор цвета'
  ClientHeight = 99
  ClientWidth = 409
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Shape1: TShape
    Left = 84
    Top = 32
    Width = 121
    Height = 21
  end
  object Label1: TLabel
    Left = 56
    Top = 36
    Width = 26
    Height = 13
    Caption = 'Цвет'
  end
  object SpeedButton1: TSpeedButton
    Left = 208
    Top = 32
    Width = 97
    Height = 22
    Caption = 'Выбрать цвет'
    OnClick = SpeedButton1Click
  end
  object OKBtn: TButton
    Left = 332
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
    OnClick = OKBtnClick
  end
  object CancelBtn: TButton
    Left = 332
    Top = 30
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object LabeledEdit1: TLabeledEdit
    Left = 84
    Top = 8
    Width = 221
    Height = 21
    EditLabel.Width = 73
    EditLabel.Height = 13
    EditLabel.Caption = 'Наименование'
    LabelPosition = lpLeft
    TabOrder = 2
  end
  object LabeledEdit2: TLabeledEdit
    Left = 208
    Top = 56
    Width = 97
    Height = 21
    EditLabel.Width = 175
    EditLabel.Height = 13
    EditLabel.Caption = 'Код ламинации для оборудования'
    LabelPosition = lpLeft
    TabOrder = 3
  end
  object CheckBox1: TCheckBox
    Left = 4
    Top = 80
    Width = 297
    Height = 17
    Caption = 'цвет для ламинации'
    TabOrder = 4
  end
end
