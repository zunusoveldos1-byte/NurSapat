object PostavschiksEditDlg: TPostavschiksEditDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Редактор поставщика'
  ClientHeight = 108
  ClientWidth = 695
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 612
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 612
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LabeledEdit1: TLabeledEdit
    Left = 156
    Top = 4
    Width = 445
    Height = 21
    EditLabel.Width = 73
    EditLabel.Height = 13
    EditLabel.Caption = 'Наименование'
    LabelPosition = lpLeft
    TabOrder = 2
  end
  object CheckBox1: TCheckBox
    Left = 156
    Top = 32
    Width = 445
    Height = 17
    Caption = 
      'Формировать файл в формате "CML" системы электронной торговли ко' +
      'мпании "ТБМ"'
    TabOrder = 3
  end
  object LabeledEdit2: TLabeledEdit
    Left = 156
    Top = 56
    Width = 149
    Height = 21
    EditLabel.Width = 150
    EditLabel.Height = 13
    EditLabel.Caption = 'Идентификатор контрагента'
    LabelPosition = lpLeft
    TabOrder = 4
  end
  object LabeledEdit3: TLabeledEdit
    Left = 156
    Top = 80
    Width = 149
    Height = 21
    EditLabel.Width = 87
    EditLabel.Height = 13
    EditLabel.Caption = 'Секретный ключ'
    LabelPosition = lpLeft
    TabOrder = 5
  end
end
