object RestoreDlg: TRestoreDlg
  Left = 293
  Top = 266
  BorderStyle = bsDialog
  Caption = 'Утилиты базы данных'
  ClientHeight = 461
  ClientWidth = 353
  Color = clBtnFace
  ParentFont = True
  KeyPreview = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnKeyDown = FormKeyDown
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 8
    Top = 4
    Width = 333
    Height = 41
    Alignment = taCenter
    AutoSize = False
    Caption = 
      'Внимание!!! Неграмотное выполнение данных утилит может привести ' +
      'к потере всей информации. Перед выполнением проконсультируйтесь ' +
      'в службе технической поддержки.'
    WordWrap = True
  end
  object SpeedButton3: TSpeedButton
    Left = 12
    Top = 360
    Width = 329
    Height = 29
    Caption = 'Считать программу ключа'
    Visible = False
    OnClick = SpeedButton3Click
  end
  object SpeedButton4: TSpeedButton
    Left = 12
    Top = 392
    Width = 329
    Height = 29
    Caption = 'Записать программу ключа'
    Visible = False
    OnClick = SpeedButton4Click
  end
  object SpeedButton5: TSpeedButton
    Left = 12
    Top = 424
    Width = 329
    Height = 29
    Caption = 'Обновить прошивку ключа'
    Visible = False
    OnClick = SpeedButton5Click
  end
  object GroupBox1: TGroupBox
    Left = 4
    Top = 52
    Width = 345
    Height = 53
    Caption = 'Утилита №1'
    TabOrder = 0
    object SpeedButton1: TSpeedButton
      Left = 8
      Top = 16
      Width = 329
      Height = 29
      Caption = 'Восстановить текущую базу данных системными средствами'
      OnClick = SpeedButton1Click
    end
  end
  object GroupBox2: TGroupBox
    Left = 4
    Top = 108
    Width = 345
    Height = 245
    Caption = 'Утилита №2'
    TabOrder = 1
    object SpeedButton2: TSpeedButton
      Left = 8
      Top = 16
      Width = 329
      Height = 29
      Caption = 'Восстановить базу данных из архива'
      OnClick = SpeedButton2Click
    end
    object Label2: TLabel
      Left = 12
      Top = 48
      Width = 98
      Height = 13
      Caption = 'Перечень архивов:'
    end
    object ListBox1: TListBox
      Left = 8
      Top = 64
      Width = 329
      Height = 173
      ItemHeight = 13
      TabOrder = 0
    end
  end
end
