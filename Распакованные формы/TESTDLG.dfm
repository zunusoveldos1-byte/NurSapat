object TestDlg: TTestDlg
  Left = 221
  Top = 231
  Caption = 'Сообщения'
  ClientHeight = 150
  ClientWidth = 573
  Color = clBtnFace
  ParentFont = True
  FormStyle = fsStayOnTop
  OldCreateOrder = False
  Position = poDesigned
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object LBox: TListBox
    Left = 0
    Top = 0
    Width = 573
    Height = 150
    Align = alClient
    ExtendedSelect = False
    ItemHeight = 13
    TabOrder = 0
  end
end
