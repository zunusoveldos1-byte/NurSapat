object ZakParDlg: TZakParDlg
  Left = 286
  Top = 185
  BorderStyle = bsDialog
  Caption = 'Параметры заказа'
  ClientHeight = 526
  ClientWidth = 700
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
  object OKBtn: TButton
    Left = 540
    Top = 492
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object GroupBox1: TGroupBox
    Left = 368
    Top = 292
    Width = 261
    Height = 193
    Caption = ' Рентабельность (%) '
    TabOrder = 1
    object Label33: TLabel
      Tag = -700
      Left = 13
      Top = 40
      Width = 8
      Height = 13
      AutoSize = False
      Caption = '1'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clBlack
      Font.Height = -11
      Font.Name = 'MS Sans Serif'
      Font.Style = [fsBold]
      ParentFont = False
      Transparent = False
    end
    object Label34: TLabel
      Tag = -700
      Left = 13
      Top = 64
      Width = 8
      Height = 13
      AutoSize = False
      Caption = '2'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clBlack
      Font.Height = -11
      Font.Name = 'MS Sans Serif'
      Font.Style = [fsBold]
      ParentFont = False
      Transparent = False
    end
    object Label36: TLabel
      Tag = -700
      Left = 13
      Top = 112
      Width = 8
      Height = 13
      AutoSize = False
      Caption = '4'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clBlack
      Font.Height = -11
      Font.Name = 'MS Sans Serif'
      Font.Style = [fsBold]
      ParentFont = False
      Transparent = False
    end
    object Label35: TLabel
      Tag = -700
      Left = 13
      Top = 88
      Width = 8
      Height = 13
      AutoSize = False
      Caption = '3'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clBlack
      Font.Height = -11
      Font.Name = 'MS Sans Serif'
      Font.Style = [fsBold]
      ParentFont = False
      Transparent = False
    end
    object Label6: TLabel
      Tag = -700
      Left = 13
      Top = 136
      Width = 8
      Height = 13
      AutoSize = False
      Caption = '5'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clBlack
      Font.Height = -11
      Font.Name = 'MS Sans Serif'
      Font.Style = [fsBold]
      ParentFont = False
      Transparent = False
    end
    object Label7: TLabel
      Tag = -700
      Left = 13
      Top = 160
      Width = 8
      Height = 13
      AutoSize = False
      Caption = '6'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clBlack
      Font.Height = -11
      Font.Name = 'MS Sans Serif'
      Font.Style = [fsBold]
      ParentFont = False
      Transparent = False
    end
    object Label4: TLabel
      Left = 8
      Top = 17
      Width = 102
      Height = 13
      Caption = 'для "полной" прогр.'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -11
      Font.Name = 'Tahoma'
      Font.Style = [fsUnderline]
      ParentFont = False
    end
    object Label5: TLabel
      Left = 128
      Top = 17
      Width = 120
      Height = 13
      Caption = 'для "дилерской" прогр.'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -11
      Font.Name = 'Tahoma'
      Font.Style = [fsUnderline]
      ParentFont = False
    end
    object Bevel1: TBevel
      Left = 108
      Top = 16
      Width = 9
      Height = 165
      Shape = bsRightLine
    end
    object ZkPEdit3: TEdit
      Left = 31
      Top = 36
      Width = 46
      Height = 21
      AutoSize = False
      TabOrder = 0
      OnChange = ZkPEdit2Exit
    end
    object ZkPEdit4: TEdit
      Left = 31
      Top = 60
      Width = 46
      Height = 21
      AutoSize = False
      TabOrder = 1
      OnChange = ZkPEdit2Exit
    end
    object ZkPEdit5: TEdit
      Left = 31
      Top = 84
      Width = 46
      Height = 21
      AutoSize = False
      TabOrder = 2
      OnChange = ZkPEdit2Exit
    end
    object ZkPEdit6: TEdit
      Left = 31
      Top = 108
      Width = 46
      Height = 21
      AutoSize = False
      TabOrder = 3
      OnChange = ZkPEdit2Exit
    end
    object ZkpEdit7: TEdit
      Left = 31
      Top = 132
      Width = 46
      Height = 21
      AutoSize = False
      TabOrder = 4
      OnChange = ZkPEdit2Exit
    end
    object ZkpEdit8: TEdit
      Left = 31
      Top = 156
      Width = 46
      Height = 21
      AutoSize = False
      TabOrder = 5
      OnChange = ZkPEdit2Exit
    end
    object LEdit1: TLabeledEdit
      Left = 168
      Top = 36
      Width = 45
      Height = 21
      EditLabel.Width = 41
      EditLabel.Height = 13
      EditLabel.Caption = 'розница'
      LabelPosition = lpLeft
      TabOrder = 6
      OnChange = ZkPEdit2Exit
    end
    object LEdit2: TLabeledEdit
      Left = 168
      Top = 60
      Width = 45
      Height = 21
      EditLabel.Width = 41
      EditLabel.Height = 13
      EditLabel.Caption = 'закупка'
      LabelPosition = lpLeft
      TabOrder = 7
      OnChange = ZkPEdit2Exit
    end
    object CheckBox1: TCheckBox
      Left = 129
      Top = 91
      Width = 129
      Height = 90
      Caption = 
        '"закупку" ("закупочную цену") считать не как рентабельность, а к' +
        'ак скидку от розничной стоимости конструкции'
      TabOrder = 8
      WordWrap = True
      OnClick = CheckBox1Click
    end
  end
  object GroupBox4: TGroupBox
    Left = 4
    Top = 0
    Width = 693
    Height = 289
    Caption = 'Параметры расчета стоимости заказа'
    TabOrder = 2
    object Label18: TLabel
      Left = 72
      Top = 16
      Width = 265
      Height = 13
      Caption = 'Принцип расчета стекла (плоскостных материалов)'
    end
    object Label19: TLabel
      Left = 64
      Top = 56
      Width = 277
      Height = 13
      Caption = 'Принцип расчета профиля (длиномерных материалов)'
    end
    object Label22: TLabel
      Left = 460
      Top = 16
      Width = 177
      Height = 13
      Caption = 'Спец. условия для расчета стекла'
    end
    object Label23: TLabel
      Left = 140
      Top = 96
      Width = 125
      Height = 13
      Caption = 'Принцип расчета затрат'
    end
    object Label24: TLabel
      Tag = -700
      Left = 424
      Top = 192
      Width = 6
      Height = 13
      Caption = '1'
    end
    object Label25: TLabel
      Tag = -700
      Left = 424
      Top = 216
      Width = 6
      Height = 13
      Caption = '2'
    end
    object Label26: TLabel
      Tag = -700
      Left = 424
      Top = 240
      Width = 6
      Height = 13
      Caption = '3'
    end
    object Label28: TLabel
      Tag = -700
      Left = 424
      Top = 264
      Width = 6
      Height = 13
      Caption = '4'
    end
    object Label29: TLabel
      Tag = -700
      Left = 488
      Top = 192
      Width = 6
      Height = 13
      Caption = '5'
    end
    object Label32: TLabel
      Tag = -700
      Left = 488
      Top = 216
      Width = 6
      Height = 13
      Caption = '6'
    end
    object Label37: TLabel
      Tag = -700
      Left = 488
      Top = 240
      Width = 6
      Height = 13
      Caption = '7'
    end
    object Label38: TLabel
      Tag = -700
      Left = 488
      Top = 264
      Width = 6
      Height = 13
      Caption = '8'
    end
    object Label39: TLabel
      Tag = -700
      Left = 552
      Top = 192
      Width = 6
      Height = 13
      Caption = '9'
    end
    object Label40: TLabel
      Tag = -700
      Left = 548
      Top = 216
      Width = 12
      Height = 13
      Caption = '10'
    end
    object Label41: TLabel
      Tag = -700
      Left = 548
      Top = 240
      Width = 12
      Height = 13
      Caption = '11'
    end
    object Label42: TLabel
      Tag = -700
      Left = 548
      Top = 264
      Width = 12
      Height = 13
      Caption = '12'
    end
    object Label43: TLabel
      Tag = -700
      Left = 612
      Top = 192
      Width = 12
      Height = 13
      Caption = '13'
    end
    object Label44: TLabel
      Tag = -700
      Left = 612
      Top = 216
      Width = 12
      Height = 13
      Caption = '14'
    end
    object Label45: TLabel
      Tag = -700
      Left = 612
      Top = 240
      Width = 12
      Height = 13
      Caption = '15'
    end
    object Label1: TLabel
      Left = 132
      Top = 136
      Width = 141
      Height = 13
      Caption = 'Принцип формирования цен'
    end
    object Label2: TLabel
      Left = 108
      Top = 176
      Width = 198
      Height = 13
      Caption = 'В расчетах использовать уплотнитель'
    end
    object Label3: TLabel
      Left = 488
      Top = 172
      Width = 109
      Height = 13
      Caption = 'Коефициенты затрат'
    end
    object SteklCb: TComboBox
      Left = 8
      Top = 32
      Width = 405
      Height = 21
      Style = csDropDownList
      ItemHeight = 13
      TabOrder = 0
      OnChange = ZkPCheckBox9Click
    end
    object ProfilCb: TComboBox
      Left = 8
      Top = 72
      Width = 405
      Height = 21
      Style = csDropDownList
      ItemHeight = 13
      TabOrder = 1
      OnChange = ZkPCheckBox9Click
    end
    object ZatratCb: TComboBox
      Left = 8
      Top = 112
      Width = 405
      Height = 21
      Style = csDropDownList
      ItemHeight = 13
      TabOrder = 2
      OnChange = ZkPCheckBox9Click
    end
    object ZE0: TEdit
      Left = 436
      Top = 188
      Width = 37
      Height = 21
      TabOrder = 3
      OnChange = ZE0Exit
    end
    object ZE1: TEdit
      Tag = 1
      Left = 436
      Top = 212
      Width = 37
      Height = 21
      TabOrder = 4
      OnChange = ZE0Exit
    end
    object ZE2: TEdit
      Tag = 2
      Left = 436
      Top = 236
      Width = 37
      Height = 21
      TabOrder = 5
      OnChange = ZE0Exit
    end
    object ZE3: TEdit
      Tag = 3
      Left = 436
      Top = 260
      Width = 37
      Height = 21
      TabOrder = 6
      OnChange = ZE0Exit
    end
    object ZE4: TEdit
      Tag = 4
      Left = 500
      Top = 188
      Width = 37
      Height = 21
      TabOrder = 7
      OnChange = ZE0Exit
    end
    object ZE5: TEdit
      Tag = 5
      Left = 500
      Top = 212
      Width = 37
      Height = 21
      TabOrder = 8
      OnChange = ZE0Exit
    end
    object ZE6: TEdit
      Tag = 6
      Left = 500
      Top = 236
      Width = 37
      Height = 21
      TabOrder = 9
      OnChange = ZE0Exit
    end
    object ZE7: TEdit
      Tag = 7
      Left = 500
      Top = 260
      Width = 37
      Height = 21
      TabOrder = 10
      OnChange = ZE0Exit
    end
    object ZE8: TEdit
      Tag = 8
      Left = 564
      Top = 188
      Width = 37
      Height = 21
      TabOrder = 11
      OnChange = ZE0Exit
    end
    object ZE9: TEdit
      Tag = 9
      Left = 564
      Top = 212
      Width = 37
      Height = 21
      TabOrder = 12
      OnChange = ZE0Exit
    end
    object ZE10: TEdit
      Tag = 10
      Left = 564
      Top = 236
      Width = 37
      Height = 21
      TabOrder = 13
      OnChange = ZE0Exit
    end
    object ZE11: TEdit
      Tag = 11
      Left = 564
      Top = 260
      Width = 37
      Height = 21
      TabOrder = 14
      OnChange = ZE0Exit
    end
    object ZE12: TEdit
      Tag = 12
      Left = 628
      Top = 188
      Width = 37
      Height = 21
      TabOrder = 15
      OnChange = ZE0Exit
    end
    object ZE13: TEdit
      Tag = 13
      Left = 628
      Top = 212
      Width = 37
      Height = 21
      TabOrder = 16
      OnChange = ZE0Exit
    end
    object ZE14: TEdit
      Tag = 14
      Left = 628
      Top = 236
      Width = 37
      Height = 21
      TabOrder = 17
      OnChange = ZE0Exit
    end
    object CenaCb: TComboBox
      Left = 8
      Top = 152
      Width = 405
      Height = 21
      Style = csDropDownList
      ItemHeight = 13
      TabOrder = 18
      OnChange = ZkPCheckBox9Click
    end
    object UplTypCb: TComboBox
      Left = 8
      Top = 192
      Width = 405
      Height = 21
      Style = csDropDownList
      ItemHeight = 13
      TabOrder = 19
      OnChange = ZkPCheckBox9Click
    end
    object SGrid1: TStringGrid
      Left = 420
      Top = 32
      Width = 265
      Height = 133
      ColCount = 4
      DefaultColWidth = 50
      DefaultRowHeight = 16
      FixedCols = 0
      RowCount = 3
      FixedRows = 0
      Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goColSizing, goEditing]
      TabOrder = 20
      OnSelectCell = SGrid1SelectCell
      OnSetEditText = SGrid1SetEditText
    end
  end
  object GroupBox5: TGroupBox
    Left = 192
    Top = 292
    Width = 173
    Height = 121
    Caption = 'w - Давление ветра [H/мм.кв.]'
    TabOrder = 3
    object Label9: TLabel
      Left = 24
      Top = 20
      Width = 52
      Height = 13
      Caption = 'А ( 0 - 8 м)'
    end
    object Label10: TLabel
      Left = 21
      Top = 44
      Width = 54
      Height = 13
      Caption = 'В ( 8 -20 м)'
    end
    object Label11: TLabel
      Left = 9
      Top = 68
      Width = 67
      Height = 13
      Caption = 'С (20 - 100 м)'
    end
    object Label12: TLabel
      Left = 13
      Top = 96
      Width = 64
      Height = 13
      Caption = 'f - прогиб  L/'
    end
    object Edit8: TEdit
      Left = 80
      Top = 16
      Width = 57
      Height = 21
      TabOrder = 0
      OnChange = ZkPEdit2Exit
    end
    object Edit9: TEdit
      Left = 80
      Top = 40
      Width = 57
      Height = 21
      TabOrder = 1
      OnChange = ZkPEdit2Exit
    end
    object Edit10: TEdit
      Left = 80
      Top = 64
      Width = 57
      Height = 21
      TabOrder = 2
      OnChange = ZkPEdit2Exit
    end
    object Edit11: TEdit
      Left = 80
      Top = 92
      Width = 57
      Height = 21
      TabOrder = 3
      OnChange = ZkPEdit2Exit
    end
  end
  object GroupBox6: TGroupBox
    Left = 4
    Top = 292
    Width = 185
    Height = 193
    Caption = 'Скидки на цены на материалы %'
    TabOrder = 4
    object Edit12: TLabeledEdit
      Left = 104
      Top = 16
      Width = 37
      Height = 21
      EditLabel.Width = 98
      EditLabel.Height = 13
      EditLabel.Caption = 'профиль холодный'
      LabelPosition = lpLeft
      TabOrder = 0
      OnChange = ZkPEdit2Exit
    end
    object Edit14: TLabeledEdit
      Left = 104
      Top = 40
      Width = 37
      Height = 21
      EditLabel.Width = 85
      EditLabel.Height = 13
      EditLabel.Caption = 'профиль теплый'
      LabelPosition = lpLeft
      TabOrder = 1
      OnChange = ZkPEdit2Exit
    end
    object Edit13: TLabeledEdit
      Left = 104
      Top = 88
      Width = 37
      Height = 21
      EditLabel.Width = 84
      EditLabel.Height = 13
      EditLabel.Caption = 'комплектующие'
      LabelPosition = lpLeft
      TabOrder = 2
      OnChange = ZkPEdit2Exit
    end
    object Edit15: TLabeledEdit
      Left = 104
      Top = 112
      Width = 37
      Height = 21
      EditLabel.Width = 66
      EditLabel.Height = 13
      EditLabel.Caption = 'уплотнители'
      LabelPosition = lpLeft
      TabOrder = 3
      OnChange = ZkPEdit2Exit
    end
    object Edit16: TLabeledEdit
      Left = 104
      Top = 136
      Width = 37
      Height = 21
      EditLabel.Width = 59
      EditLabel.Height = 13
      EditLabel.Caption = 'остекление'
      LabelPosition = lpLeft
      TabOrder = 4
      OnChange = ZkPEdit2Exit
    end
    object Edit17: TLabeledEdit
      Left = 104
      Top = 160
      Width = 37
      Height = 21
      EditLabel.Width = 56
      EditLabel.Height = 13
      EditLabel.Caption = 'фурнитура'
      LabelPosition = lpLeft
      TabOrder = 5
      OnChange = ZkPEdit2Exit
    end
    object Edit18: TLabeledEdit
      Left = 104
      Top = 64
      Width = 37
      Height = 21
      EditLabel.Width = 66
      EditLabel.Height = 13
      EditLabel.Caption = 'армирование'
      LabelPosition = lpLeft
      TabOrder = 6
      OnChange = ZkPEdit2Exit
    end
  end
  object Button1: TButton
    Left = 620
    Top = 492
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 5
  end
end
