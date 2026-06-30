object ExpZakazesInfoDlg: TExpZakazesInfoDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Экспорт информации по группе заказов'
  ClientHeight = 115
  ClientWidth = 323
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  PixelsPerInch = 96
  TextHeight = 13
  object Button1: TButton
    Left = 8
    Top = 8
    Width = 305
    Height = 41
    Caption = 
      'Экспорт списания материалов во внешний склад, файлы формата "DBF' +
      '"'
    TabOrder = 0
    WordWrap = True
    OnClick = Button1Click
  end
  object Button2: TButton
    Left = 8
    Top = 60
    Width = 305
    Height = 41
    Caption = 
      'Экспорт материалов в файл формата "CML" для системы электронной ' +
      'торговли компании "ТБМ"'
    TabOrder = 1
    WordWrap = True
    OnClick = Button2Click
  end
end
