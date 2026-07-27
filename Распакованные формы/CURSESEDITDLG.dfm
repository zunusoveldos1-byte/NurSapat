object CursesEditDlg: TCursesEditDlg
  Left = 448
  Top = 255
  BorderStyle = bsDialog
  Caption = 'Редактор курсов'
  ClientHeight = 237
  ClientWidth = 337
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnClose = FormClose
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 8
    Top = 8
    Width = 88
    Height = 13
    Caption = 'Начало действия'
    ParentShowHint = False
    ShowAccelChar = False
    ShowHint = False
    Transparent = True
  end
  object OKBtn: TButton
    Left = 252
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 252
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 1
  end
  object Edit1: TEdit
    Left = 104
    Top = 4
    Width = 133
    Height = 21
    TabOrder = 2
    OnExit = Edit1Exit
  end
  object VEditor: TValueListEditor
    Left = 12
    Top = 32
    Width = 225
    Height = 193
    DefaultRowHeight = 16
    Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goColSizing, goEditing, goThumbTracking]
    Strings.Strings = (
      '=')
    TabOrder = 3
    TitleCaptions.Strings = (
      'Валюта'
      'Курс')
    OnSetEditText = VEditorSetEditText
    ColWidths = (
      98
      121)
  end
end
