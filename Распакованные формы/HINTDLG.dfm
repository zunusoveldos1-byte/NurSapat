object HintDlg: THintDlg
  Left = 336
  Top = 175
  ActiveControl = OKBtn
  BorderStyle = bsDialog
  Caption = 'Обратите внимание !!!'
  ClientHeight = 99
  ClientWidth = 383
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnHide = FormHide
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 300
    Top = 8
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object SText1: TStaticText
    Left = 12
    Top = 12
    Width = 281
    Height = 49
    AutoSize = False
    Color = clBtnFace
    ParentColor = False
    TabOrder = 1
  end
  object CheckBox1: TCheckBox
    Left = 8
    Top = 72
    Width = 229
    Height = 17
    Caption = 'Больше не выводить данную подсказку'
    TabOrder = 2
  end
end
