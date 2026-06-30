object ListElemDlg: TListElemDlg
  Left = 253
  Top = 60
  BorderStyle = bsDialog
  Caption = 'Перечень элементов'
  ClientHeight = 485
  ClientWidth = 549
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnClose = FormClose
  OnCreate = FormCreate
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 468
    Top = 0
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 468
    Top = 28
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object LBox: TListBox
    Left = 0
    Top = 24
    Width = 461
    Height = 461
    Style = lbOwnerDrawVariable
    ItemHeight = 13
    MultiSelect = True
    TabOrder = 2
    OnDblClick = LBoxDblClick
    OnDrawItem = LBoxDrawItem
    OnMeasureItem = LBoxMeasureItem
  end
  object ComboBox3: TComboBox
    Left = 296
    Top = 0
    Width = 165
    Height = 21
    Hint = 'окно выбора серии'
    Style = csDropDownList
    DropDownCount = 30
    ItemHeight = 13
    TabOrder = 3
    OnChange = ComboBox3Change
  end
end
