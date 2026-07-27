object RaskrSpisDlg: TRaskrSpisDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Списание обрезков'
  ClientHeight = 566
  ClientWidth = 651
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
    Left = 16
    Top = 36
    Width = 129
    Height = 13
    Caption = 'Склад обрезков профиля'
  end
  object Label2: TLabel
    Left = 16
    Top = 464
    Width = 120
    Height = 13
    Caption = 'Склад обрезков стекла'
  end
  object Label9: TLabel
    Left = 16
    Top = 12
    Width = 153
    Height = 13
    Caption = 'Обработка обрезков профиля'
  end
  object Label23: TLabel
    Left = 16
    Top = 440
    Width = 144
    Height = 13
    Caption = 'Обработка обрезков стекла'
  end
  object Label3: TLabel
    Left = 16
    Top = 84
    Width = 250
    Height = 13
    Caption = 'Дата документа со списанием обрезков профиля'
  end
  object Label4: TLabel
    Left = 16
    Top = 512
    Width = 241
    Height = 13
    Caption = 'Дата документа со списанием обрезков стекла'
  end
  object Bevel1: TBevel
    Left = 4
    Top = 424
    Width = 645
    Height = 5
  end
  object OKBtn: TButton
    Left = 284
    Top = 536
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CheckBox1: TCheckBox
    Left = 16
    Top = 60
    Width = 337
    Height = 17
    Caption = 'Списывать обрезки профиля со склада обрезков профиля'
    TabOrder = 1
    OnClick = ComboBox3Change
  end
  object CheckBox2: TCheckBox
    Left = 16
    Top = 488
    Width = 337
    Height = 17
    Caption = 'Списывать обрезки стекла со склада обрезков стекла'
    TabOrder = 2
    OnClick = ComboBox3Change
  end
  object ComboBox1: TComboBox
    Left = 172
    Top = 32
    Width = 165
    Height = 21
    Style = csDropDownList
    ItemHeight = 0
    TabOrder = 3
  end
  object ComboBox2: TComboBox
    Left = 172
    Top = 460
    Width = 165
    Height = 21
    Style = csDropDownList
    ItemHeight = 0
    TabOrder = 4
  end
  object ComboBox3: TComboBox
    Left = 172
    Top = 8
    Width = 473
    Height = 21
    Style = csDropDownList
    ItemHeight = 0
    TabOrder = 5
    OnChange = ComboBox3Change
  end
  object ComboBox4: TComboBox
    Left = 172
    Top = 436
    Width = 473
    Height = 21
    Style = csDropDownList
    ItemHeight = 0
    TabOrder = 6
    OnChange = ComboBox3Change
  end
  object BtnEdit1: TBtnEdit
    Left = 272
    Top = 80
    Width = 137
    Height = 21
    TabOrder = 7
    OnBtnClick = BtnEdit1BtnClick
  end
  object BtnEdit2: TBtnEdit
    Left = 272
    Top = 508
    Width = 137
    Height = 21
    TabOrder = 8
    OnBtnClick = BtnEdit1BtnClick
  end
  object LView: TADGListView
    Left = 4
    Top = 128
    Width = 641
    Height = 293
    Columns = <>
    ReadOnly = False
    TabOrder = 9
    OnDblClick = LViewDblClick
  end
  object CheckBox3: TCheckBox
    Left = 16
    Top = 104
    Width = 397
    Height = 17
    Caption = 'Кроить профиля только определенного этапа'
    TabOrder = 10
    OnClick = ComboBox3Change
  end
  object LabeledEdit1: TLabeledEdit
    Left = 604
    Top = 104
    Width = 41
    Height = 21
    EditLabel.Width = 69
    EditLabel.Height = 13
    EditLabel.Caption = 'Этап раскроя'
    LabelPosition = lpLeft
    TabOrder = 11
    OnChange = LabeledEdit1Change
  end
end
