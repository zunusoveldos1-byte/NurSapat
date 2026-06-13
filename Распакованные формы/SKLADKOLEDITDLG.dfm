object SkladKolEditDlg: TSkladKolEditDlg
  Left = 354
  Top = 334
  BorderStyle = bsDialog
  Caption = 'Редактор материала'
  ClientHeight = 461
  ClientWidth = 641
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 244
    Top = 0
    Width = 73
    Height = 13
    Caption = 'Наименование'
    Transparent = True
  end
  object Label2: TLabel
    Left = 24
    Top = 140
    Width = 39
    Height = 13
    Caption = 'Валюта'
    Transparent = True
  end
  object Label3: TLabel
    Left = 36
    Top = 92
    Width = 26
    Height = 13
    Caption = 'Цена'
    Transparent = True
  end
  object Label5: TLabel
    Left = 445
    Top = 0
    Width = 26
    Height = 13
    Caption = 'Цвет'
    Transparent = True
  end
  object Label6: TLabel
    Left = 28
    Top = 116
    Width = 31
    Height = 13
    Caption = 'Сумма'
    Transparent = True
  end
  object Label7: TLabel
    Left = 93
    Top = 0
    Width = 43
    Height = 13
    Caption = 'Артикул'
    Transparent = True
  end
  object Label8: TLabel
    Left = 23
    Top = 44
    Width = 41
    Height = 13
    Caption = 'ед. изм.'
    Transparent = True
  end
  object OKBtn: TButton
    Left = 556
    Top = 8
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 2
  end
  object CancelBtn: TButton
    Left = 556
    Top = 36
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 3
  end
  object ComboBox2: TComboBox
    Left = 68
    Top = 136
    Width = 77
    Height = 21
    Style = csDropDownList
    ItemHeight = 13
    TabOrder = 1
    OnChange = ComboBox2Change
  end
  object Edit1: TEdit
    Left = 68
    Top = 88
    Width = 77
    Height = 21
    TabOrder = 0
    OnChange = Edit1Change
  end
  object Edit4: TEdit
    Left = 68
    Top = 16
    Width = 93
    Height = 21
    TabStop = False
    ReadOnly = True
    TabOrder = 4
    OnDblClick = BtnEdit1BtnClick
  end
  object BtnEdit1: TBtnEdit
    Left = 160
    Top = 16
    Width = 233
    Height = 21
    TabStop = False
    ReadOnly = True
    TabOrder = 5
    OnBtnClick = BtnEdit1BtnClick
    OnDblClick = BtnEdit1BtnClick
  end
  object Edit5: TEdit
    Left = 68
    Top = 112
    Width = 77
    Height = 21
    TabStop = False
    ReadOnly = True
    TabOrder = 6
  end
  object ComboBox1: TComboBox
    Left = 392
    Top = 16
    Width = 145
    Height = 21
    Style = csDropDownList
    DropDownCount = 15
    ItemHeight = 13
    TabOrder = 7
    OnChange = ComboBox1Change
  end
  object Edit3: TEdit
    Left = 68
    Top = 40
    Width = 77
    Height = 21
    TabStop = False
    ReadOnly = True
    TabOrder = 8
  end
  object LabeledEdit1: TLabeledEdit
    Left = 68
    Top = 64
    Width = 77
    Height = 21
    EditLabel.Width = 59
    EditLabel.Height = 13
    EditLabel.Caption = 'количество'
    LabelPosition = lpLeft
    TabOrder = 9
    OnChange = Edit1Change
  end
  object LabeledEdit2: TLabeledEdit
    Left = 220
    Top = 64
    Width = 77
    Height = 21
    EditLabel.Width = 60
    EditLabel.Height = 13
    EditLabel.Caption = 'высота, мм.'
    LabelPosition = lpLeft
    TabOrder = 10
    OnChange = Edit1Change
  end
  object GroupBox1: TGroupBox
    Left = 8
    Top = 168
    Width = 497
    Height = 285
    Caption = 'Движение материала'
    TabOrder = 11
    object ToolBar1: TToolBar
      Left = 6
      Top = 15
      Width = 31
      Height = 30
      Align = alNone
      ButtonHeight = 30
      ButtonWidth = 31
      Images = SahredDMod.ButList24
      TabOrder = 0
      object ToolButton1: TToolButton
        Left = 0
        Top = 0
        Hint = 'Показать информацию по данному материалу'
        ImageIndex = 23
        OnClick = ToolButton1Click
      end
    end
    object LView: TADGListView
      Left = 8
      Top = 48
      Width = 481
      Height = 229
      Columns = <>
      ReadOnly = False
      TabOrder = 1
    end
  end
  object LabeledEdit3: TLabeledEdit
    Left = 456
    Top = 64
    Width = 77
    Height = 21
    EditLabel.Width = 63
    EditLabel.Height = 13
    EditLabel.Caption = 'код обрезка'
    LabelPosition = lpLeft
    TabOrder = 12
    OnChange = LabeledEdit3Change
  end
end
