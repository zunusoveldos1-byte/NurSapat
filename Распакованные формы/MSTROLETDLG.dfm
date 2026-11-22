object MstRoletDlg: TMstRoletDlg
  Left = 254
  Top = 154
  BorderStyle = bsDialog
  Caption = 'Мастер создания ролет'
  ClientHeight = 332
  ClientWidth = 540
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object NextBut: TSpeedButton
    Left = 368
    Top = 296
    Width = 81
    Height = 25
    Caption = 'Готово'
    OnClick = NextButClick
  end
  object CancelBut: TSpeedButton
    Left = 452
    Top = 296
    Width = 81
    Height = 25
    Caption = 'Отмена'
    OnClick = CancelButClick
  end
  object Book: TPageControl
    Left = 8
    Top = 4
    Width = 521
    Height = 281
    ActivePage = Page2
    TabHeight = 1
    TabOrder = 0
    TabWidth = 1
    object Page1: TTabSheet
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object Label7: TLabel
        Left = 16
        Top = 12
        Width = 63
        Height = 13
        Caption = 'Ширина, мм.'
      end
      object Label8: TLabel
        Left = 17
        Top = 36
        Width = 60
        Height = 13
        Caption = 'Высота, мм.'
      end
      object Edit2: TEdit
        Left = 84
        Top = 32
        Width = 61
        Height = 21
        TabOrder = 0
        Text = '1000'
      end
      object Edit1: TEdit
        Left = 84
        Top = 8
        Width = 61
        Height = 21
        TabOrder = 1
        Text = '1000'
      end
    end
    object Page2: TTabSheet
      ImageIndex = 1
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object Label9: TLabel
        Left = 68
        Top = 12
        Width = 67
        Height = 13
        Caption = 'Ширина, мм.:'
      end
      object Label11: TLabel
        Tag = -700
        Left = 144
        Top = 12
        Width = 18
        Height = 13
        Caption = 'info'
      end
      object Label12: TLabel
        Tag = -700
        Left = 144
        Top = 32
        Width = 18
        Height = 13
        Caption = 'info'
      end
      object Label10: TLabel
        Left = 68
        Top = 32
        Width = 64
        Height = 13
        Caption = 'Высота, мм.:'
      end
      object Label13: TLabel
        Left = 68
        Top = 56
        Width = 65
        Height = 13
        Caption = 'Цвет короба'
      end
      object Label14: TLabel
        Left = 60
        Top = 80
        Width = 71
        Height = 13
        Caption = 'Цвет ламелей'
      end
      object Label1: TLabel
        Left = 108
        Top = 104
        Width = 24
        Height = 13
        Caption = 'Верх'
        Transparent = True
      end
      object Label2: TLabel
        Left = 87
        Top = 128
        Width = 44
        Height = 13
        Caption = 'Боковые'
        Transparent = True
      end
      object Label6: TLabel
        Left = 53
        Top = 152
        Width = 78
        Height = 13
        Caption = 'Нижняя планка'
        Transparent = True
      end
      object Label3: TLabel
        Left = 6
        Top = 176
        Width = 122
        Height = 13
        Caption = 'Ламели без перфорации'
        Transparent = True
      end
      object Label4: TLabel
        Left = 12
        Top = 200
        Width = 116
        Height = 13
        Caption = 'Ламели с перфорацией'
        Transparent = True
      end
      object Label5: TLabel
        Left = 22
        Top = 224
        Width = 108
        Height = 13
        Caption = 'Специальные ламели'
        Transparent = True
      end
      object Label17: TLabel
        Left = 468
        Top = 60
        Width = 11
        Height = 13
        Caption = '%'
      end
      object Label16: TLabel
        Left = 388
        Top = 60
        Width = 34
        Height = 13
        Caption = 'кол-во'
      end
      object Label15: TLabel
        Left = 288
        Top = 80
        Width = 85
        Height = 13
        Caption = 'Ламели в коробе'
      end
      object ComboBox7: TComboBox
        Left = 140
        Top = 52
        Width = 129
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 0
        OnChange = ComboBox7Change
      end
      object ComboBox8: TComboBox
        Left = 140
        Top = 76
        Width = 129
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 1
        OnChange = ComboBox7Change
      end
      object ComboBox1: TComboBox
        Left = 140
        Top = 100
        Width = 237
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 2
        OnChange = ComboBox1Change
      end
      object ComboBox2: TComboBox
        Left = 140
        Top = 124
        Width = 237
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 3
        OnChange = ComboBox1Change
      end
      object ComboBox3: TComboBox
        Left = 140
        Top = 148
        Width = 237
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 4
        OnChange = ComboBox1Change
      end
      object ComboBox4: TComboBox
        Left = 140
        Top = 172
        Width = 237
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 5
        OnChange = ComboBox1Change
      end
      object ComboBox5: TComboBox
        Left = 140
        Top = 196
        Width = 237
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 6
        OnChange = ComboBox1Change
      end
      object ComboBox6: TComboBox
        Left = 140
        Top = 220
        Width = 237
        Height = 21
        Style = csDropDownList
        ItemHeight = 0
        TabOrder = 7
        OnChange = ComboBox1Change
      end
      object Edit5: TEdit
        Left = 384
        Top = 220
        Width = 45
        Height = 21
        TabOrder = 8
        Text = '0'
      end
      object Edit4: TEdit
        Left = 384
        Top = 196
        Width = 45
        Height = 21
        TabOrder = 9
        Text = '0'
      end
      object Edit3: TEdit
        Left = 384
        Top = 172
        Width = 45
        Height = 21
        ReadOnly = True
        TabOrder = 10
        Text = '0'
      end
      object Edit7: TEdit
        Left = 448
        Top = 172
        Width = 45
        Height = 21
        ReadOnly = True
        TabOrder = 11
        Text = '0'
      end
      object Edit8: TEdit
        Left = 448
        Top = 196
        Width = 45
        Height = 21
        ReadOnly = True
        TabOrder = 12
        Text = '0'
      end
      object Edit9: TEdit
        Left = 448
        Top = 220
        Width = 45
        Height = 21
        ReadOnly = True
        TabOrder = 13
        Text = '0'
      end
      object UpDown3: TUpDown
        Left = 429
        Top = 220
        Width = 15
        Height = 21
        Associate = Edit5
        TabOrder = 14
        OnChangingEx = UpDown1ChangingEx
      end
      object UpDown2: TUpDown
        Left = 429
        Top = 196
        Width = 15
        Height = 21
        Associate = Edit4
        TabOrder = 15
        OnChangingEx = UpDown1ChangingEx
      end
      object Edit6: TEdit
        Left = 384
        Top = 76
        Width = 45
        Height = 21
        TabOrder = 16
        Text = '0'
      end
      object UpDown1: TUpDown
        Left = 429
        Top = 76
        Width = 15
        Height = 21
        Associate = Edit6
        TabOrder = 17
        OnChangingEx = UpDown1ChangingEx
      end
    end
  end
end
