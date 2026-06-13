object SkladProdListDlg: TSkladProdListDlg
  Left = 0
  Top = 0
  BorderStyle = bsDialog
  Caption = 'Склад готовой продукции'
  ClientHeight = 489
  ClientWidth = 591
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'Tahoma'
  Font.Style = []
  OldCreateOrder = False
  Position = poScreenCenter
  OnCreate = FormCreate
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object LView: TADGListView
    Left = 0
    Top = 0
    Width = 589
    Height = 489
    Columns = <>
    ReadOnly = False
    TabOrder = 0
  end
end
