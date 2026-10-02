object ColorMatEditDlg: TColorMatEditDlg
  Left = 488
  Top = 281
  BorderStyle = bsDialog
  Caption = 'Редактор цвета материала'
  ClientHeight = 295
  ClientWidth = 323
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 40
    Top = 8
    Width = 43
    Height = 13
    Caption = 'Артикул'
    Transparent = True
  end
  object Label2: TLabel
    Left = 56
    Top = 32
    Width = 26
    Height = 13
    Caption = 'Цвет'
    Transparent = True
  end
  object Label3: TLabel
    Left = 55
    Top = 56
    Width = 26
    Height = 13
    Caption = 'Цена'
    Transparent = True
  end
  object Label4: TLabel
    Left = 4
    Top = 80
    Width = 74
    Height = 13
    Caption = 'Артикул связи'
    Transparent = True
  end
  object Bevel1: TBevel
    Left = 4
    Top = 128
    Width = 209
    Height = 161
    Shape = bsFrame
  end
  object Label12: TLabel
    Left = 63
    Top = 216
    Width = 88
    Height = 13
    Caption = 'Длина прута, мм.'
    Transparent = True
  end
  object Label7: TLabel
    Left = 77
    Top = 240
    Width = 73
    Height = 13
    Caption = 'Неликвид, мм.'
    Transparent = True
  end
  object Label18: TLabel
    Left = 95
    Top = 264
    Width = 54
    Height = 13
    Caption = 'Мусор, мм.'
    Transparent = True
  end
  object OKBtn: TButton
    Left = 240
    Top = 4
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 240
    Top = 32
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object Edit1: TEdit
    Left = 84
    Top = 4
    Width = 145
    Height = 21
    TabOrder = 2
    OnChange = Edit1Change
  end
  object ColorBox: TComboBox
    Left = 84
    Top = 28
    Width = 145
    Height = 21
    Style = csDropDownList
    DropDownCount = 15
    ItemHeight = 13
    TabOrder = 3
    OnChange = ColorBoxChange
  end
  object Edit2: TEdit
    Left = 84
    Top = 52
    Width = 73
    Height = 21
    TabOrder = 4
    OnChange = Edit2Change
  end
  object Edit3: TEdit
    Left = 84
    Top = 76
    Width = 145
    Height = 21
    TabOrder = 5
    OnChange = Edit1Change
  end
  object cbKroitDlinu: TCheckBox
    Left = 8
    Top = 132
    Width = 201
    Height = 17
    Caption = 'Кроить длину нестандартно'
    TabOrder = 6
    OnClick = cbKroitDlinuClick
  end
  object Edit7: TEdit
    Left = 152
    Top = 260
    Width = 53
    Height = 21
    TabOrder = 7
    OnChange = Edit2Change
  end
  object Edit4: TEdit
    Left = 152
    Top = 236
    Width = 53
    Height = 21
    TabOrder = 8
    OnChange = Edit2Change
  end
  object Edit12: TEdit
    Left = 152
    Top = 212
    Width = 53
    Height = 21
    TabOrder = 9
    OnChange = Edit2Change
  end
  object cbKroitParal: TCheckBox
    Left = 8
    Top = 148
    Width = 199
    Height = 17
    Caption = 'параллельный раскрой на 2 прута'
    TabOrder = 10
    OnClick = cbKroitDlinuClick
  end
  object CheckBox6: TCheckBox
    Left = 8
    Top = 164
    Width = 199
    Height = 17
    Caption = '"складировать обрезки"'
    TabOrder = 11
    OnClick = cbKroitDlinuClick
  end
  object CheckBox9: TCheckBox
    Left = 8
    Top = 180
    Width = 197
    Height = 29
    Caption = 'разрешать принцип расчета стоимости с учетом всех остатков'
    TabOrder = 12
    WordWrap = True
    OnClick = cbKroitDlinuClick
  end
  object LabeledEdit1: TLabeledEdit
    Left = 184
    Top = 100
    Width = 45
    Height = 21
    EditLabel.Width = 163
    EditLabel.Height = 13
    EditLabel.Caption = 'Код цулаг для сварочника KMW'
    LabelPosition = lpLeft
    TabOrder = 13
    OnChange = Edit1Change
  end
end
