object CorpoUtilDlg: TCorpoUtilDlg
  Left = 326
  Top = 253
  BorderIcons = [biSystemMenu]
  Caption = 'Корпоративный узел'
  ClientHeight = 248
  ClientWidth = 384
  Color = clBtnFace
  Constraints.MinHeight = 100
  Constraints.MinWidth = 300
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object SpeedButton1: TSpeedButton
    Left = 4
    Top = 4
    Width = 205
    Height = 25
    Caption = 'Отправить подготовленные заказы'
    OnClick = Button1Click
  end
  object SpeedButton2: TSpeedButton
    Left = 4
    Top = 32
    Width = 205
    Height = 25
    Caption = 'Принять заказы'
    OnClick = SpeedButton2Click
  end
  object OKBtn: TButton
    Left = 304
    Top = 4
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object ListBox1: TListBox
    Left = 4
    Top = 63
    Width = 377
    Height = 182
    ItemHeight = 13
    TabOrder = 1
  end
  object IdSMTP1: TIdSMTP
    OnStatus = IdSMTP1Status
    OnFailedRecipient = IdSMTP1FailedRecipient
    SASLMechanisms = <>
    Left = 236
    Top = 4
  end
  object IdPOP31: TIdPOP3
    OnStatus = IdPOP31Status
    AutoLogin = True
    SASLMechanisms = <>
    Left = 268
    Top = 4
  end
end
