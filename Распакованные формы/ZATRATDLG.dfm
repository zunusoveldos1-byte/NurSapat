object ZatratDlg: TZatratDlg
  Left = 146
  Top = 150
  BorderStyle = bsDialog
  Caption = 'Затраты'
  ClientHeight = 234
  ClientWidth = 648
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 84
    Top = 24
    Width = 154
    Height = 13
    Caption = 'Мощность оборудования, КВТ'
    Transparent = True
  end
  object Label2: TLabel
    Left = 114
    Top = 44
    Width = 124
    Height = 13
    Caption = 'Спецоборудование, КВТ'
    Transparent = True
  end
  object Label3: TLabel
    Left = 127
    Top = 64
    Width = 116
    Height = 13
    Caption = 'Аренда цеховая, м.кв.'
    Transparent = True
  end
  object Label4: TLabel
    Left = 132
    Top = 84
    Width = 105
    Height = 13
    Caption = 'Аренда офиса, м.кв.'
    Transparent = True
  end
  object Label5: TLabel
    Left = 125
    Top = 104
    Width = 114
    Height = 13
    Caption = 'Зп. Рабочих, за 1м.кв.'
    Transparent = True
  end
  object Label6: TLabel
    Left = 160
    Top = 124
    Width = 80
    Height = 13
    Caption = 'Зп. цеха, в мес.'
    Transparent = True
  end
  object Label7: TLabel
    Left = 151
    Top = 144
    Width = 87
    Height = 13
    Caption = 'Зп. офиса, в мес.'
    Transparent = True
  end
  object Label8: TLabel
    Left = 131
    Top = 164
    Width = 108
    Height = 13
    Caption = 'Доп. расходы, в мес.'
    Transparent = True
  end
  object Label9: TLabel
    Left = 308
    Top = 4
    Width = 26
    Height = 13
    Caption = 'Цена'
    Transparent = True
  end
  object Label10: TLabel
    Left = 352
    Top = 4
    Width = 55
    Height = 13
    Caption = 'Время раб.'
    Transparent = True
  end
  object Label11: TLabel
    Left = 424
    Top = 4
    Width = 38
    Height = 13
    Caption = 'в месяц'
    Transparent = True
  end
  object Label12: TLabel
    Left = 476
    Top = 4
    Width = 164
    Height = 13
    Caption = 'Объем выпускаемой прод. м.кв.'
    Transparent = True
  end
  object Label13: TLabel
    Left = 592
    Top = 24
    Width = 38
    Height = 13
    Caption = 'в месяц'
    Transparent = True
  end
  object Label14: TLabel
    Left = 484
    Top = 148
    Width = 122
    Height = 13
    Caption = 'Сумма затрат на 1 м.кв.'
    Transparent = True
  end
  object Label16: TLabel
    Left = 146
    Top = 184
    Width = 90
    Height = 13
    Caption = 'Транспорт, в мес.'
    Transparent = True
  end
  object Label17: TLabel
    Left = 8
    Top = 208
    Width = 97
    Height = 13
    Caption = 'Цены указывать в '
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsUnderline]
    ParentFont = False
    Transparent = True
  end
  object SpeedButton1: TSpeedButton
    Left = 560
    Top = 204
    Width = 69
    Height = 25
    Caption = 'Закрыть'
    OnClick = SpeedButton1Click
  end
  object Label18: TLabel
    Left = 484
    Top = 160
    Width = 113
    Height = 13
    Caption = 'продукции из расчета'
    Transparent = True
  end
  object Label19: TLabel
    Left = 484
    Top = 172
    Width = 129
    Height = 13
    Caption = 'окупаемости всех затрат'
    Transparent = True
  end
  object Label20: TLabel
    Left = 484
    Top = 184
    Width = 44
    Height = 13
    Caption = 'за 1 мес.'
    Transparent = True
  end
  object Label15: TLabel
    Left = 496
    Top = 44
    Width = 117
    Height = 13
    Caption = 'Рабочих дней в месяце'
    Transparent = True
  end
  object Label21: TLabel
    Left = 384
    Top = 204
    Width = 24
    Height = 13
    Caption = 'Итог'
    Transparent = True
  end
  object Edit1: TEdit
    Left = 244
    Top = 20
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 0
    OnExit = Edit1Exit
  end
  object Edit2: TEdit
    Left = 244
    Top = 40
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 1
    OnExit = Edit1Exit
  end
  object Edit3: TEdit
    Left = 244
    Top = 60
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 2
    OnExit = Edit1Exit
  end
  object Edit4: TEdit
    Left = 244
    Top = 80
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 3
    OnExit = Edit1Exit
  end
  object Edit5: TEdit
    Left = 300
    Top = 100
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 4
    OnExit = Edit1Exit
  end
  object Edit6: TEdit
    Left = 300
    Top = 120
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 5
    OnExit = Edit1Exit
  end
  object Edit7: TEdit
    Left = 300
    Top = 140
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 6
    OnExit = Edit1Exit
  end
  object Edit9: TEdit
    Left = 300
    Top = 20
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 7
    OnExit = Edit1Exit
  end
  object Edit10: TEdit
    Left = 300
    Top = 40
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 8
    OnExit = Edit1Exit
  end
  object Edit11: TEdit
    Left = 300
    Top = 60
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 9
    OnExit = Edit1Exit
  end
  object Edit12: TEdit
    Left = 300
    Top = 80
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 10
    OnExit = Edit1Exit
  end
  object Edit16: TEdit
    Left = 300
    Top = 160
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 11
    OnExit = Edit1Exit
  end
  object Edit17: TEdit
    Left = 356
    Top = 20
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 12
    OnExit = Edit1Exit
  end
  object Edit18: TEdit
    Left = 356
    Top = 40
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 13
    OnExit = Edit1Exit
  end
  object Edit22: TEdit
    Left = 412
    Top = 20
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 14
  end
  object Edit23: TEdit
    Left = 412
    Top = 40
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 15
  end
  object Edit24: TEdit
    Left = 412
    Top = 60
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 16
  end
  object Edit25: TEdit
    Left = 412
    Top = 80
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 17
  end
  object Edit26: TEdit
    Left = 412
    Top = 100
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 18
  end
  object Edit27: TEdit
    Left = 412
    Top = 120
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 19
  end
  object Edit28: TEdit
    Left = 412
    Top = 140
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 20
  end
  object Edit29: TEdit
    Left = 412
    Top = 160
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 21
  end
  object Edit30: TEdit
    Left = 412
    Top = 200
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 22
  end
  object Edit31: TEdit
    Left = 484
    Top = 200
    Width = 61
    Height = 19
    Ctl3D = False
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentCtl3D = False
    ParentFont = False
    ReadOnly = True
    TabOrder = 23
  end
  object Edit32: TEdit
    Left = 528
    Top = 20
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 24
    OnExit = Edit1Exit
  end
  object Edit36: TEdit
    Left = 300
    Top = 180
    Width = 45
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 25
    OnExit = Edit1Exit
  end
  object Edit37: TEdit
    Left = 412
    Top = 180
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    ReadOnly = True
    TabOrder = 26
  end
  object CBox1: TComboBox
    Left = 108
    Top = 204
    Width = 69
    Height = 21
    Style = csDropDownList
    Ctl3D = False
    ItemHeight = 0
    ParentCtl3D = False
    TabOrder = 27
    OnChange = CBox1Change
  end
  object Edit13: TEdit
    Left = 528
    Top = 60
    Width = 61
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 28
    OnExit = Edit1Exit
  end
end
