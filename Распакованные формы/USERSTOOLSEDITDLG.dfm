object UsersToolsEditDlg: TUsersToolsEditDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Редактор утилиты'
  ClientHeight = 67
  ClientWidth = 580
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = False
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object SpeedButton1: TSpeedButton
    Left = 432
    Top = 32
    Width = 23
    Height = 22
    Caption = '...'
    OnClick = SpeedButton1Click
  end
  object OKBtn: TButton
    Left = 500
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 500
    Top = 32
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
    Width = 209
    Height = 21
    EditLabel.Width = 73
    EditLabel.Height = 13
    EditLabel.Caption = 'Наименование'
    LabelPosition = lpLeft
    TabOrder = 2
  end
  object LabeledEdit2: TLabeledEdit
    Left = 84
    Top = 32
    Width = 345
    Height = 21
    EditLabel.Width = 26
    EditLabel.Height = 13
    EditLabel.Caption = 'Файл'
    LabelPosition = lpLeft
    TabOrder = 3
  end
end
