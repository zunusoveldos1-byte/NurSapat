object KonstrNamesEnterDlg: TKonstrNamesEnterDlg
  Left = 232
  Top = 185
  BorderStyle = bsDialog
  Caption = 'Редактор наименования конструкции'
  ClientHeight = 242
  ClientWidth = 523
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 236
    Top = 60
    Width = 61
    Height = 13
    Caption = 'Примечание'
  end
  object Label2: TLabel
    Left = 24
    Top = 12
    Width = 73
    Height = 13
    Caption = 'Наименование'
  end
  object OKBtn: TButton
    Left = 440
    Top = 4
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 440
    Top = 32
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object BtnEdit1: TBtnEdit
    Left = 104
    Top = 8
    Width = 325
    Height = 21
    TabOrder = 2
    OnBtnClick = BtnEdit1BtnClick
  end
  object LabeledEdit1: TLabeledEdit
    Left = 104
    Top = 76
    Width = 325
    Height = 21
    EditLabel.Width = 64
    EditLabel.Height = 13
    EditLabel.Caption = 'для клиента'
    LabelPosition = lpLeft
    TabOrder = 3
  end
  object LabeledEdit2: TLabeledEdit
    Left = 104
    Top = 100
    Width = 325
    Height = 21
    EditLabel.Width = 93
    EditLabel.Height = 13
    EditLabel.Caption = 'для производства'
    LabelPosition = lpLeft
    TabOrder = 4
  end
  object GroupBox1: TGroupBox
    Left = 104
    Top = 128
    Width = 325
    Height = 109
    Caption = 'Параметры расчета прайс-листа'
    TabOrder = 5
    object Label16: TLabel
      Left = 89
      Top = 16
      Width = 42
      Height = 13
      Caption = 'минимум'
      Transparent = True
    end
    object Label19: TLabel
      Left = 142
      Top = 16
      Width = 47
      Height = 13
      Caption = 'максимум'
      Transparent = True
    end
    object Label20: TLabel
      Left = 206
      Top = 16
      Width = 19
      Height = 13
      Caption = 'шаг'
      Transparent = True
    end
    object Label21: TLabel
      Left = 6
      Top = 84
      Width = 80
      Height = 13
      Caption = 'Площадь, м.кв.'
      Transparent = True
    end
    object Label15: TLabel
      Left = 22
      Top = 60
      Width = 60
      Height = 13
      Caption = 'Высота, мм.'
      Transparent = True
    end
    object Label14: TLabel
      Left = 21
      Top = 36
      Width = 63
      Height = 13
      Caption = 'Ширина, мм.'
      Transparent = True
    end
    object Edit13: TEdit
      Left = 88
      Top = 32
      Width = 49
      Height = 21
      TabOrder = 0
    end
    object Edit14: TEdit
      Left = 140
      Top = 32
      Width = 49
      Height = 21
      TabOrder = 1
    end
    object Edit15: TEdit
      Left = 192
      Top = 32
      Width = 49
      Height = 21
      TabOrder = 2
    end
    object Edit18: TEdit
      Left = 192
      Top = 56
      Width = 49
      Height = 21
      TabOrder = 3
    end
    object Edit17: TEdit
      Left = 140
      Top = 56
      Width = 49
      Height = 21
      TabOrder = 4
    end
    object Edit16: TEdit
      Left = 88
      Top = 56
      Width = 49
      Height = 21
      TabOrder = 5
    end
    object Edit19: TEdit
      Left = 88
      Top = 80
      Width = 49
      Height = 21
      TabOrder = 6
    end
    object Edit20: TEdit
      Left = 140
      Top = 80
      Width = 49
      Height = 21
      TabOrder = 7
    end
    object Edit21: TEdit
      Left = 192
      Top = 80
      Width = 49
      Height = 21
      TabOrder = 8
    end
  end
  object LabeledEdit3: TLabeledEdit
    Left = 104
    Top = 32
    Width = 209
    Height = 21
    EditLabel.Width = 60
    EditLabel.Height = 13
    EditLabel.Caption = 'маркировка'
    LabelPosition = lpLeft
    TabOrder = 6
  end
end
