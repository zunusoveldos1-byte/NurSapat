object PriozvodEditDlg: TPriozvodEditDlg
  Left = 220
  Top = 202
  BorderStyle = bsDialog
  Caption = 'Настройки производителя'
  ClientHeight = 93
  ClientWidth = 574
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clBlack
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label19: TLabel
    Left = 362
    Top = 32
    Width = 61
    Height = 13
    Hint = 'Торцевание палки профиля с одного края'
    Caption = 'Торцевание'
    Transparent = True
  end
  object Label20: TLabel
    Left = 357
    Top = 52
    Width = 66
    Height = 13
    Caption = 'Ширина реза'
    Transparent = True
  end
  object Label11: TLabel
    Left = 385
    Top = 72
    Width = 38
    Height = 13
    Hint = 'Уварка профиля для пластика с одной стороны, мм.'
    Caption = 'Уварка'
    Transparent = True
  end
  object Label4: TLabel
    Left = 12
    Top = 8
    Width = 79
    Height = 13
    Caption = 'Производитель'
    Transparent = True
  end
  object Label3: TLabel
    Left = 278
    Top = 4
    Width = 146
    Height = 26
    Caption = 'Коррекция с одной стороны для размера по фальцу'
    Transparent = True
    WordWrap = True
  end
  object Edit16: TEdit
    Left = 428
    Top = 28
    Width = 53
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 0
  end
  object Edit17: TEdit
    Left = 428
    Top = 48
    Width = 53
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 1
  end
  object Edit10: TEdit
    Left = 428
    Top = 68
    Width = 53
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 2
  end
  object Edit44: TEdit
    Left = 96
    Top = 4
    Width = 145
    Height = 21
    ReadOnly = True
    TabOrder = 3
  end
  object Button1: TButton
    Left = 496
    Top = 4
    Width = 75
    Height = 25
    Caption = 'Закрыть'
    Default = True
    ModalResult = 1
    TabOrder = 4
    OnClick = OKBtnClick
  end
  object Edit2: TEdit
    Left = 428
    Top = 8
    Width = 53
    Height = 19
    Ctl3D = False
    ParentCtl3D = False
    TabOrder = 5
  end
  object CheckBox1: TCheckBox
    Left = 12
    Top = 28
    Width = 73
    Height = 17
    Caption = 'активный'
    TabOrder = 6
  end
  object CheckBox2: TCheckBox
    Tag = -700
    Left = 12
    Top = 56
    Width = 73
    Height = 17
    Caption = 'фур. пл.'
    TabOrder = 7
    Visible = False
  end
  object CheckBox3: TCheckBox
    Tag = -700
    Left = 12
    Top = 72
    Width = 73
    Height = 17
    Caption = 'фур. ал.'
    TabOrder = 8
    Visible = False
  end
  object CheckBox4: TCheckBox
    Tag = -700
    Left = 92
    Top = 56
    Width = 73
    Height = 17
    Caption = 'фур. дер.'
    TabOrder = 9
    Visible = False
  end
  object CheckBox5: TCheckBox
    Tag = -700
    Left = 172
    Top = 56
    Width = 73
    Height = 17
    Caption = 'нуж. стек.'
    TabOrder = 10
    Visible = False
  end
  object CheckBox6: TCheckBox
    Tag = -700
    Left = 172
    Top = 72
    Width = 73
    Height = 17
    Caption = 'нуж. фурн.'
    TabOrder = 11
    Visible = False
  end
end
