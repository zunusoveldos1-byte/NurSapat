object DefaultEditDlg: TDefaultEditDlg
  Left = 333
  Top = 335
  BorderStyle = bsDialog
  Caption = 'Шаблон заказа'
  ClientHeight = 340
  ClientWidth = 461
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 36
    Top = 92
    Width = 44
    Height = 13
    Caption = 'Затраты'
    Transparent = True
  end
  object Label2: TLabel
    Left = 36
    Top = 188
    Width = 52
    Height = 13
    Caption = 'Демонтаж'
    Transparent = True
  end
  object Label3: TLabel
    Left = 36
    Top = 212
    Width = 40
    Height = 13
    Caption = 'Монтаж'
    Transparent = True
  end
  object Label4: TLabel
    Left = 8
    Top = 12
    Width = 73
    Height = 13
    Caption = 'Наименование'
    Transparent = True
  end
  object SpeedButton1: TSpeedButton
    Left = 276
    Top = 116
    Width = 181
    Height = 25
    Caption = 'Шаблонные параметры заказа'
    OnClick = SpeedButton1Click
  end
  object Label5: TLabel
    Left = 96
    Top = 300
    Width = 39
    Height = 13
    Caption = 'Валюта'
    Transparent = True
  end
  object Label6: TLabel
    Left = 36
    Top = 64
    Width = 83
    Height = 13
    Caption = 'Давление ветра'
    Transparent = True
  end
  object Label7: TLabel
    Left = 36
    Top = 116
    Width = 89
    Height = 13
    Caption = 'Доп. расходы, %'
    Transparent = True
  end
  object Label8: TLabel
    Left = 36
    Top = 140
    Width = 107
    Height = 13
    Caption = 'Доп. расходы, фикс.'
    Transparent = True
  end
  object Label9: TLabel
    Left = 8
    Top = 36
    Width = 45
    Height = 13
    Caption = 'Профиль'
    Transparent = False
  end
  object Label10: TLabel
    Left = 36
    Top = 268
    Width = 91
    Height = 13
    Caption = 'Монтаж доп. мат.'
    Transparent = True
  end
  object OKBtn: TButton
    Left = 380
    Top = 4
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object Edit2: TEdit
    Left = 144
    Top = 184
    Width = 53
    Height = 21
    TabOrder = 3
  end
  object Edit3: TEdit
    Left = 144
    Top = 208
    Width = 53
    Height = 21
    TabOrder = 4
  end
  object Edit1: TEdit
    Left = 144
    Top = 88
    Width = 53
    Height = 21
    TabOrder = 1
  end
  object ComboBox1: TComboBox
    Left = 200
    Top = 88
    Width = 65
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 2
  end
  object ComboBox2: TComboBox
    Left = 200
    Top = 184
    Width = 65
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 5
  end
  object ComboBox3: TComboBox
    Left = 200
    Top = 208
    Width = 65
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 6
  end
  object ComboBox4: TComboBox
    Left = 88
    Top = 88
    Width = 53
    Height = 21
    Style = csDropDownList
    DropDownCount = 20
    ItemHeight = 13
    TabOrder = 7
  end
  object Edit4: TEdit
    Left = 88
    Top = 8
    Width = 237
    Height = 21
    TabOrder = 8
  end
  object ComboBox5: TComboBox
    Left = 144
    Top = 296
    Width = 73
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 9
  end
  object ComboBox6: TComboBox
    Left = 144
    Top = 60
    Width = 53
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 10
  end
  object Edit5: TEdit
    Left = 144
    Top = 112
    Width = 53
    Height = 21
    TabOrder = 11
  end
  object Edit6: TEdit
    Left = 144
    Top = 136
    Width = 53
    Height = 21
    TabOrder = 12
  end
  object ComboBox7: TComboBox
    Left = 88
    Top = 32
    Width = 177
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 13
  end
  object Edit7: TEdit
    Left = 144
    Top = 264
    Width = 53
    Height = 21
    TabOrder = 14
  end
  object ComboBox8: TComboBox
    Left = 200
    Top = 264
    Width = 65
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 15
  end
  object Edit8: TEdit
    Left = 144
    Top = 160
    Width = 53
    Height = 21
    TabOrder = 16
  end
  object CheckBox1: TCheckBox
    Left = 20
    Top = 162
    Width = 97
    Height = 17
    Caption = 'Скидка №1, %'
    TabOrder = 17
    OnClick = CheckBox1Click
  end
  object Edit9: TEdit
    Left = 144
    Top = 232
    Width = 53
    Height = 21
    TabOrder = 18
  end
  object CheckBox2: TCheckBox
    Left = 20
    Top = 234
    Width = 97
    Height = 17
    Caption = 'Скидка №2, %'
    TabOrder = 19
    OnClick = CheckBox1Click
  end
  object CheckBox3: TCheckBox
    Left = 20
    Top = 318
    Width = 433
    Height = 17
    Caption = 
      'в дилерской программе шаблон будет виден только после ввода паро' +
      'ля дилера'
    TabOrder = 20
    OnClick = CheckBox1Click
  end
end
