object DrawLinesEditDlg: TDrawLinesEditDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Редактор линии'
  ClientHeight = 136
  ClientWidth = 242
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 160
    Top = 8
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 160
    Top = 36
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object LabeledEdit1: TLabeledEdit
    Left = 64
    Top = 8
    Width = 49
    Height = 21
    EditLabel.Width = 35
    EditLabel.Height = 13
    EditLabel.Caption = 'X1, мм.'
    LabelPosition = lpLeft
    TabOrder = 2
  end
  object LabeledEdit2: TLabeledEdit
    Left = 64
    Top = 32
    Width = 49
    Height = 21
    EditLabel.Width = 35
    EditLabel.Height = 13
    EditLabel.Caption = 'Y1, мм.'
    LabelPosition = lpLeft
    TabOrder = 3
  end
  object LabeledEdit3: TLabeledEdit
    Left = 64
    Top = 56
    Width = 49
    Height = 21
    EditLabel.Width = 35
    EditLabel.Height = 13
    EditLabel.Caption = 'X2, мм.'
    LabelPosition = lpLeft
    TabOrder = 4
  end
  object LabeledEdit4: TLabeledEdit
    Left = 64
    Top = 80
    Width = 49
    Height = 21
    EditLabel.Width = 35
    EditLabel.Height = 13
    EditLabel.Caption = 'Y2, мм.'
    LabelPosition = lpLeft
    TabOrder = 5
  end
  object LabeledEdit5: TLabeledEdit
    Left = 64
    Top = 104
    Width = 49
    Height = 21
    EditLabel.Width = 45
    EditLabel.Height = 13
    EditLabel.Caption = 'толщина'
    LabelPosition = lpLeft
    TabOrder = 6
  end
end
