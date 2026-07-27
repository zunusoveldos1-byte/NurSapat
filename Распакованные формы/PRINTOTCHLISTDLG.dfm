object PrintOtchListDlg: TPrintOtchListDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Печатные формы'
  ClientHeight = 497
  ClientWidth = 480
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 400
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 400
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LView: TADGListView
    Left = 0
    Top = 0
    Width = 393
    Height = 497
    Columns = <>
    HideSelection = False
    MultiSelect = True
    ReadOnly = True
    TabOrder = 2
  end
end
