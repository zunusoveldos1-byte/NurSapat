object MaterialsListCompDlg: TMaterialsListCompDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Список материалов'
  ClientHeight = 580
  ClientWidth = 807
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
    Left = 728
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 728
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LView: TADGListView
    Left = 4
    Top = 4
    Width = 717
    Height = 573
    Columns = <>
    ReadOnly = False
    TabOrder = 2
  end
end
