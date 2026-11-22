object ZakazProplataEditDlg: TZakazProplataEditDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Редактор оплаты'
  ClientHeight = 151
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
    Left = 12
    Top = 56
    Width = 73
    Height = 13
    Caption = 'Форма оплаты'
  end
  object SpeedButton1: TSpeedButton
    Left = 204
    Top = 4
    Width = 23
    Height = 22
    Caption = '...'
    OnClick = SpeedButton1Click
  end
  object OKBtn: TButton
    Left = 304
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 304
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LabeledEdit1: TLabeledEdit
    Left = 88
    Top = 4
    Width = 113
    Height = 21
    EditLabel.Width = 26
    EditLabel.Height = 13
    EditLabel.Caption = 'Дата'
    LabelPosition = lpLeft
    TabOrder = 2
  end
  object LabeledEdit2: TLabeledEdit
    Left = 88
    Top = 28
    Width = 73
    Height = 21
    EditLabel.Width = 31
    EditLabel.Height = 13
    EditLabel.Caption = 'Сумма'
    LabelPosition = lpLeft
    TabOrder = 3
  end
  object ComboBox1: TComboBox
    Left = 164
    Top = 28
    Width = 65
    Height = 22
    Style = csOwnerDrawFixed
    DropDownCount = 16
    ItemHeight = 16
    TabOrder = 4
  end
  object ComboBox2: TComboBox
    Left = 88
    Top = 52
    Width = 65
    Height = 22
    Style = csOwnerDrawFixed
    ItemHeight = 16
    TabOrder = 5
  end
  object LabeledEdit3: TLabeledEdit
    Left = 88
    Top = 76
    Width = 141
    Height = 21
    EditLabel.Width = 64
    EditLabel.Height = 13
    EditLabel.Caption = 'Плательщик'
    LabelPosition = lpLeft
    TabOrder = 6
  end
  object LabeledEdit4: TLabeledEdit
    Left = 88
    Top = 100
    Width = 141
    Height = 21
    EditLabel.Width = 61
    EditLabel.Height = 13
    EditLabel.Caption = 'Получатель'
    LabelPosition = lpLeft
    TabOrder = 7
  end
  object LabeledEdit5: TLabeledEdit
    Left = 88
    Top = 124
    Width = 289
    Height = 21
    EditLabel.Width = 61
    EditLabel.Height = 13
    EditLabel.Caption = 'Примечание'
    LabelPosition = lpLeft
    TabOrder = 8
  end
end
