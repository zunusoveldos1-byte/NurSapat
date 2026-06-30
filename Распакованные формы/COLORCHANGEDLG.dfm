object ColorChangeDlg: TColorChangeDlg
  Left = 290
  Top = 229
  BorderStyle = bsDialog
  Caption = 'Сменить элементы'
  ClientHeight = 562
  ClientWidth = 823
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 396
    Top = 36
    Width = 26
    Height = 13
    Caption = 'Цвет'
  end
  object Label2: TLabel
    Left = 388
    Top = 12
    Width = 31
    Height = 13
    Caption = 'Серия'
  end
  object SpeedButton1: TSpeedButton
    Left = 708
    Top = 8
    Width = 23
    Height = 22
    Hint = 'настройка замены элементов'
    Caption = '...'
    OnClick = SpeedButton1Click
  end
  object OKBtn: TButton
    Left = 744
    Top = 4
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 744
    Top = 32
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object ComboBox1: TComboBox
    Left = 424
    Top = 32
    Width = 189
    Height = 21
    Style = csDropDownList
    DropDownCount = 30
    ItemHeight = 0
    TabOrder = 2
    OnChange = ComboBox1Change
  end
  object LView: TADGListView
    Left = 4
    Top = 64
    Width = 817
    Height = 493
    Columns = <>
    ReadOnly = False
    TabOrder = 3
    OnDblClick = LViewDblClick
  end
  object ComboBox2: TComboBox
    Left = 424
    Top = 8
    Width = 281
    Height = 21
    Style = csOwnerDrawFixed
    DropDownCount = 30
    ItemHeight = 15
    TabOrder = 4
    OnChange = ComboBox1Change
    OnDrawItem = ComboBox2DrawItem
  end
  object LabeledEdit1: TLabeledEdit
    Left = 160
    Top = 32
    Width = 197
    Height = 21
    EditLabel.Width = 72
    EditLabel.Height = 13
    EditLabel.Caption = 'Текущий цвет'
    LabelPosition = lpLeft
    ReadOnly = True
    TabOrder = 5
  end
  object LabeledEdit2: TLabeledEdit
    Left = 160
    Top = 8
    Width = 197
    Height = 21
    EditLabel.Width = 77
    EditLabel.Height = 13
    EditLabel.Caption = 'Текущая серия'
    LabelPosition = lpLeft
    ReadOnly = True
    TabOrder = 6
  end
end
