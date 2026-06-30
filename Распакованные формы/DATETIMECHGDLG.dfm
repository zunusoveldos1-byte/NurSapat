object DateTimeChgDlg: TDateTimeChgDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Изменение даты и времени'
  ClientHeight = 160
  ClientWidth = 249
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnClose = FormClose
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 172
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 172
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object MonthCalendar1: TMonthCalendar
    Left = 4
    Top = 4
    Width = 162
    Height = 153
    Date = 39173.604202800930000000
    TabOrder = 2
  end
  object LabeledEdit1: TLabeledEdit
    Left = 172
    Top = 136
    Width = 73
    Height = 21
    EditLabel.Width = 30
    EditLabel.Height = 13
    EditLabel.Caption = 'время'
    TabOrder = 3
  end
end
