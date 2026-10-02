object RolLamelDlg: TRolLamelDlg
  Left = 177
  Top = 175
  BorderStyle = bsDialog
  Caption = 'Ламели'
  ClientHeight = 127
  ClientWidth = 617
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label15: TLabel
    Left = 292
    Top = 24
    Width = 85
    Height = 13
    Caption = 'Ламели в коробе'
    Transparent = True
  end
  object Label16: TLabel
    Left = 392
    Top = 4
    Width = 34
    Height = 13
    Caption = 'кол-во'
    Transparent = True
  end
  object Label17: TLabel
    Left = 472
    Top = 4
    Width = 11
    Height = 13
    Caption = '%'
    Transparent = True
  end
  object Label5: TLabel
    Left = 26
    Top = 96
    Width = 108
    Height = 13
    Caption = 'Специальные ламели'
    Transparent = True
  end
  object Label4: TLabel
    Left = 16
    Top = 72
    Width = 116
    Height = 13
    Caption = 'Ламели с перфорацией'
    Transparent = True
  end
  object Label3: TLabel
    Left = 10
    Top = 48
    Width = 122
    Height = 13
    Caption = 'Ламели без перфорации'
    Transparent = True
  end
  object OKBtn: TButton
    Left = 536
    Top = 4
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 536
    Top = 32
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object Edit6: TEdit
    Left = 388
    Top = 20
    Width = 45
    Height = 21
    TabOrder = 2
    Text = '0'
  end
  object UpDown1: TUpDown
    Left = 433
    Top = 20
    Width = 15
    Height = 21
    Associate = Edit6
    TabOrder = 3
    OnChangingEx = UpDown1ChangingEx
  end
  object Edit3: TEdit
    Left = 388
    Top = 44
    Width = 45
    Height = 21
    ReadOnly = True
    TabOrder = 4
    Text = '0'
  end
  object Edit7: TEdit
    Left = 452
    Top = 44
    Width = 45
    Height = 21
    ReadOnly = True
    TabOrder = 5
    Text = '0'
  end
  object Edit8: TEdit
    Left = 452
    Top = 68
    Width = 45
    Height = 21
    ReadOnly = True
    TabOrder = 6
    Text = '0'
  end
  object Edit4: TEdit
    Left = 388
    Top = 68
    Width = 45
    Height = 21
    TabOrder = 7
    Text = '0'
  end
  object UpDown2: TUpDown
    Left = 433
    Top = 68
    Width = 15
    Height = 21
    Associate = Edit4
    TabOrder = 8
    OnChangingEx = UpDown1ChangingEx
  end
  object Edit5: TEdit
    Left = 388
    Top = 92
    Width = 45
    Height = 21
    TabOrder = 9
    Text = '0'
  end
  object UpDown3: TUpDown
    Left = 433
    Top = 92
    Width = 15
    Height = 21
    Associate = Edit5
    TabOrder = 10
    OnChangingEx = UpDown1ChangingEx
  end
  object Edit9: TEdit
    Left = 452
    Top = 92
    Width = 45
    Height = 21
    ReadOnly = True
    TabOrder = 11
    Text = '0'
  end
  object ComboBox4: TComboBox
    Left = 144
    Top = 44
    Width = 237
    Height = 21
    Style = csDropDownList
    ItemHeight = 0
    TabOrder = 12
    OnChange = ComboBox1Change
  end
  object ComboBox5: TComboBox
    Left = 144
    Top = 68
    Width = 237
    Height = 21
    Style = csDropDownList
    ItemHeight = 0
    TabOrder = 13
    OnChange = ComboBox1Change
  end
  object ComboBox6: TComboBox
    Left = 144
    Top = 92
    Width = 237
    Height = 21
    Style = csDropDownList
    ItemHeight = 0
    TabOrder = 14
    OnChange = ComboBox1Change
  end
end
