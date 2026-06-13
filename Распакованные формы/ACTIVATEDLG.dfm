object ActivateDlg: TActivateDlg
  Left = 256
  Top = 200
  BorderStyle = bsDialog
  Caption = 'Активация программы'
  ClientHeight = 63
  ClientWidth = 595
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 512
    Top = 8
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object LabeledEdit1: TLabeledEdit
    Left = 104
    Top = 8
    Width = 397
    Height = 21
    EditLabel.Width = 84
    EditLabel.Height = 13
    EditLabel.Caption = 'Серийный номер'
    LabelPosition = lpLeft
    TabOrder = 1
  end
  object LabeledEdit2: TLabeledEdit
    Left = 104
    Top = 32
    Width = 397
    Height = 21
    EditLabel.Width = 77
    EditLabel.Height = 13
    EditLabel.Caption = 'Код активации'
    LabelPosition = lpLeft
    TabOrder = 2
  end
end
