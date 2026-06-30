object ColInfoEditDlg: TColInfoEditDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Редактор колонки'
  ClientHeight = 416
  ClientWidth = 501
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
    Left = 420
    Top = 8
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 420
    Top = 36
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LabeledEdit7: TLabeledEdit
    Left = 200
    Top = 80
    Width = 129
    Height = 21
    EditLabel.Width = 113
    EditLabel.Height = 13
    EditLabel.Caption = 'колонка (переменная)'
    LabelPosition = lpLeft
    TabOrder = 2
  end
  object LabeledEdit8: TLabeledEdit
    Left = 284
    Top = 56
    Width = 45
    Height = 21
    EditLabel.Width = 61
    EditLabel.Height = 13
    EditLabel.Caption = 'ширина, мм.'
    LabelPosition = lpLeft
    TabOrder = 3
  end
  object LabeledEdit9: TLabeledEdit
    Left = 200
    Top = 104
    Width = 129
    Height = 21
    EditLabel.Width = 112
    EditLabel.Height = 13
    EditLabel.Caption = 'условие отображения'
    LabelPosition = lpLeft
    TabOrder = 4
  end
  object GroupBox1: TGroupBox
    Left = 4
    Top = 132
    Width = 161
    Height = 121
    Caption = 'Выравнивание шапки'
    TabOrder = 5
    object CheckBox23: TCheckBox
      Left = 8
      Top = 16
      Width = 145
      Height = 17
      Caption = 'по горизонтали лево'
      TabOrder = 0
      OnClick = CheckBox23Click
    end
    object CheckBox24: TCheckBox
      Left = 8
      Top = 32
      Width = 145
      Height = 17
      Caption = 'по горизонтали центр'
      TabOrder = 1
      OnClick = CheckBox23Click
    end
    object CheckBox25: TCheckBox
      Left = 8
      Top = 48
      Width = 145
      Height = 17
      Caption = 'по горизонтали право'
      TabOrder = 2
      OnClick = CheckBox23Click
    end
    object CheckBox26: TCheckBox
      Left = 8
      Top = 64
      Width = 145
      Height = 17
      Caption = 'по вертикали по центру'
      TabOrder = 3
      OnClick = CheckBox23Click
    end
    object CheckBox27: TCheckBox
      Left = 8
      Top = 80
      Width = 145
      Height = 17
      Caption = 'не обрезать надпись'
      TabOrder = 4
      OnClick = CheckBox23Click
    end
    object CheckBox28: TCheckBox
      Left = 8
      Top = 96
      Width = 145
      Height = 17
      Caption = 'авторазмер по высоте'
      TabOrder = 5
      OnClick = CheckBox23Click
    end
  end
  object GroupBox2: TGroupBox
    Left = 168
    Top = 132
    Width = 161
    Height = 121
    Caption = 'Выравнивание ячеек'
    TabOrder = 6
    object CheckBox1: TCheckBox
      Left = 8
      Top = 16
      Width = 145
      Height = 17
      Caption = 'по горизонтали лево'
      TabOrder = 0
      OnClick = CheckBox1Click
    end
    object CheckBox2: TCheckBox
      Left = 8
      Top = 32
      Width = 145
      Height = 17
      Caption = 'по горизонтали центр'
      TabOrder = 1
      OnClick = CheckBox1Click
    end
    object CheckBox3: TCheckBox
      Left = 8
      Top = 48
      Width = 145
      Height = 17
      Caption = 'по горизонтали право'
      TabOrder = 2
      OnClick = CheckBox1Click
    end
    object CheckBox4: TCheckBox
      Left = 8
      Top = 64
      Width = 145
      Height = 17
      Caption = 'по вертикали по центру'
      TabOrder = 3
      OnClick = CheckBox1Click
    end
    object CheckBox5: TCheckBox
      Left = 8
      Top = 80
      Width = 145
      Height = 17
      Caption = 'не обрезать надпись'
      TabOrder = 4
      OnClick = CheckBox1Click
    end
    object CheckBox6: TCheckBox
      Left = 8
      Top = 96
      Width = 145
      Height = 17
      Caption = 'авторазмер по высоте'
      TabOrder = 5
      OnClick = CheckBox1Click
    end
  end
  object GroupBox3: TGroupBox
    Left = 4
    Top = 256
    Width = 121
    Height = 157
    Caption = 'Ячейка шапки'
    TabOrder = 7
    object CheckBox31: TCheckBox
      Left = 8
      Top = 16
      Width = 89
      Height = 17
      Caption = 'жирный'
      TabOrder = 0
      OnClick = CheckBox31Click
    end
    object CheckBox30: TCheckBox
      Left = 8
      Top = 48
      Width = 89
      Height = 17
      Caption = 'наклонный'
      TabOrder = 1
      OnClick = CheckBox31Click
    end
    object CheckBox29: TCheckBox
      Left = 8
      Top = 32
      Width = 89
      Height = 17
      Caption = 'подчерк'
      TabOrder = 2
      OnClick = CheckBox31Click
    end
    object LabeledEdit10: TLabeledEdit
      Left = 8
      Top = 104
      Width = 33
      Height = 21
      EditLabel.Width = 24
      EditLabel.Height = 13
      EditLabel.Caption = 'лево'
      TabOrder = 3
    end
    object LabeledEdit11: TLabeledEdit
      Left = 44
      Top = 80
      Width = 33
      Height = 21
      EditLabel.Width = 24
      EditLabel.Height = 13
      EditLabel.Caption = 'верх'
      TabOrder = 4
    end
    object LabeledEdit12: TLabeledEdit
      Left = 80
      Top = 104
      Width = 33
      Height = 21
      EditLabel.Width = 30
      EditLabel.Height = 13
      EditLabel.Caption = 'право'
      TabOrder = 5
    end
    object LabeledEdit13: TLabeledEdit
      Left = 44
      Top = 128
      Width = 33
      Height = 21
      EditLabel.Width = 17
      EditLabel.Height = 13
      EditLabel.Caption = 'низ'
      TabOrder = 6
    end
  end
  object GroupBox4: TGroupBox
    Left = 128
    Top = 256
    Width = 121
    Height = 157
    Caption = 'Ячейка обычная'
    TabOrder = 8
    object CheckBox7: TCheckBox
      Left = 8
      Top = 16
      Width = 89
      Height = 17
      Caption = 'жирный'
      TabOrder = 0
      OnClick = CheckBox31Click
    end
    object CheckBox8: TCheckBox
      Left = 8
      Top = 48
      Width = 89
      Height = 17
      Caption = 'наклонный'
      TabOrder = 1
      OnClick = CheckBox31Click
    end
    object CheckBox9: TCheckBox
      Left = 8
      Top = 32
      Width = 89
      Height = 17
      Caption = 'подчерк'
      TabOrder = 2
      OnClick = CheckBox31Click
    end
    object LabeledEdit14: TLabeledEdit
      Left = 8
      Top = 104
      Width = 33
      Height = 21
      EditLabel.Width = 24
      EditLabel.Height = 13
      EditLabel.Caption = 'лево'
      TabOrder = 3
    end
    object LabeledEdit15: TLabeledEdit
      Left = 44
      Top = 80
      Width = 33
      Height = 21
      EditLabel.Width = 24
      EditLabel.Height = 13
      EditLabel.Caption = 'верх'
      TabOrder = 4
    end
    object LabeledEdit16: TLabeledEdit
      Left = 80
      Top = 104
      Width = 33
      Height = 21
      EditLabel.Width = 30
      EditLabel.Height = 13
      EditLabel.Caption = 'право'
      TabOrder = 5
    end
    object LabeledEdit17: TLabeledEdit
      Left = 44
      Top = 128
      Width = 33
      Height = 21
      EditLabel.Width = 17
      EditLabel.Height = 13
      EditLabel.Caption = 'низ'
      TabOrder = 6
    end
  end
  object GroupBox5: TGroupBox
    Left = 252
    Top = 256
    Width = 121
    Height = 157
    Caption = 'Ячейка суб-итоговая'
    TabOrder = 9
    object CheckBox10: TCheckBox
      Left = 8
      Top = 16
      Width = 89
      Height = 17
      Caption = 'жирный'
      TabOrder = 0
      OnClick = CheckBox31Click
    end
    object CheckBox11: TCheckBox
      Left = 8
      Top = 48
      Width = 89
      Height = 17
      Caption = 'наклонный'
      TabOrder = 1
      OnClick = CheckBox31Click
    end
    object CheckBox12: TCheckBox
      Left = 8
      Top = 32
      Width = 89
      Height = 17
      Caption = 'подчерк'
      TabOrder = 2
      OnClick = CheckBox31Click
    end
    object LabeledEdit18: TLabeledEdit
      Left = 8
      Top = 104
      Width = 33
      Height = 21
      EditLabel.Width = 24
      EditLabel.Height = 13
      EditLabel.Caption = 'лево'
      TabOrder = 3
    end
    object LabeledEdit19: TLabeledEdit
      Left = 44
      Top = 80
      Width = 33
      Height = 21
      EditLabel.Width = 24
      EditLabel.Height = 13
      EditLabel.Caption = 'верх'
      TabOrder = 4
    end
    object LabeledEdit20: TLabeledEdit
      Left = 80
      Top = 104
      Width = 33
      Height = 21
      EditLabel.Width = 30
      EditLabel.Height = 13
      EditLabel.Caption = 'право'
      TabOrder = 5
    end
    object LabeledEdit21: TLabeledEdit
      Left = 44
      Top = 128
      Width = 33
      Height = 21
      EditLabel.Width = 17
      EditLabel.Height = 13
      EditLabel.Caption = 'низ'
      TabOrder = 6
    end
  end
  object GroupBox6: TGroupBox
    Left = 376
    Top = 256
    Width = 121
    Height = 157
    Caption = 'Ячейка итоговая'
    TabOrder = 10
    object CheckBox13: TCheckBox
      Left = 8
      Top = 16
      Width = 89
      Height = 17
      Caption = 'жирный'
      TabOrder = 0
      OnClick = CheckBox31Click
    end
    object CheckBox14: TCheckBox
      Left = 8
      Top = 48
      Width = 89
      Height = 17
      Caption = 'наклонный'
      TabOrder = 1
      OnClick = CheckBox31Click
    end
    object CheckBox15: TCheckBox
      Left = 8
      Top = 32
      Width = 89
      Height = 17
      Caption = 'подчерк'
      TabOrder = 2
      OnClick = CheckBox31Click
    end
    object LabeledEdit22: TLabeledEdit
      Left = 8
      Top = 104
      Width = 33
      Height = 21
      EditLabel.Width = 24
      EditLabel.Height = 13
      EditLabel.Caption = 'лево'
      TabOrder = 3
    end
    object LabeledEdit23: TLabeledEdit
      Left = 44
      Top = 80
      Width = 33
      Height = 21
      EditLabel.Width = 24
      EditLabel.Height = 13
      EditLabel.Caption = 'верх'
      TabOrder = 4
    end
    object LabeledEdit24: TLabeledEdit
      Left = 80
      Top = 104
      Width = 33
      Height = 21
      EditLabel.Width = 30
      EditLabel.Height = 13
      EditLabel.Caption = 'право'
      TabOrder = 5
    end
    object LabeledEdit25: TLabeledEdit
      Left = 44
      Top = 128
      Width = 33
      Height = 21
      EditLabel.Width = 17
      EditLabel.Height = 13
      EditLabel.Caption = 'низ'
      TabOrder = 6
    end
  end
  object TntEdit2: TLabeledEdit
    Left = 156
    Top = 32
    Width = 173
    Height = 21
    EditLabel.Width = 99
    EditLabel.Height = 13
    EditLabel.Caption = 'строка заголовка 2'
    LabelPosition = lpLeft
    TabOrder = 11
  end
  object TntEdit1: TLabeledEdit
    Left = 156
    Top = 8
    Width = 173
    Height = 21
    EditLabel.Width = 99
    EditLabel.Height = 13
    EditLabel.Caption = 'строка заголовка 1'
    LabelPosition = lpLeft
    TabOrder = 12
  end
end
