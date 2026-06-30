object KonstrNamesEditDlg: TKonstrNamesEditDlg
  Left = 232
  Top = 165
  BorderStyle = bsDialog
  Caption = 'Редактор наименования конструкции'
  ClientHeight = 106
  ClientWidth = 453
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 44
    Top = 60
    Width = 41
    Height = 13
    Caption = 'ед. изм.'
    Transparent = True
  end
  object OKBtn: TButton
    Left = 368
    Top = 4
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
    OnClick = OKBtnClick
  end
  object CancelBtn: TButton
    Left = 368
    Top = 32
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LabeledEdit1: TLabeledEdit
    Left = 88
    Top = 32
    Width = 249
    Height = 21
    EditLabel.Width = 73
    EditLabel.Height = 13
    EditLabel.Caption = 'Наименование'
    EditLabel.Transparent = True
    LabelPosition = lpLeft
    TabOrder = 2
  end
  object LabeledEdit2: TLabeledEdit
    Left = 88
    Top = 8
    Width = 249
    Height = 21
    EditLabel.Width = 20
    EditLabel.Height = 13
    EditLabel.Caption = 'Код'
    EditLabel.Transparent = True
    LabelPosition = lpLeft
    TabOrder = 3
  end
  object ComboBox1: TComboBox
    Left = 88
    Top = 56
    Width = 93
    Height = 21
    Style = csDropDownList
    ItemHeight = 0
    TabOrder = 4
  end
  object LabeledEdit3: TLabeledEdit
    Left = 88
    Top = 80
    Width = 249
    Height = 21
    EditLabel.Width = 51
    EditLabel.Height = 13
    EditLabel.Caption = 'Код связи'
    EditLabel.Transparent = True
    LabelPosition = lpLeft
    TabOrder = 5
  end
end
