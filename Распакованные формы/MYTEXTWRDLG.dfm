object MyTextWrDlg: TMyTextWrDlg
  Left = 227
  Top = 108
  BorderIcons = [biSystemMenu]
  BorderStyle = bsDialog
  Caption = 'Введите регистрационный номер'
  ClientHeight = 163
  ClientWidth = 382
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object SpeedButton1: TSpeedButton
    Left = 4
    Top = 8
    Width = 77
    Height = 25
    Caption = 'Очистить'
    OnClick = SpeedButton1Click
  end
  object OKBtn: TButton
    Left = 300
    Top = 8
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
    OnClick = OKBtnClick
  end
  object Memo1: TMemo
    Left = 4
    Top = 40
    Width = 373
    Height = 117
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -16
    Font.Name = 'Arial'
    Font.Style = []
    ParentFont = False
    TabOrder = 1
    OnKeyPress = Memo1KeyPress
  end
end
