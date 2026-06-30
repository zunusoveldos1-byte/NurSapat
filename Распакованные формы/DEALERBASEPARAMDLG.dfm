object DealerBaseParamDlg: TDealerBaseParamDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Параметры формирования дилерской базы данных'
  ClientHeight = 186
  ClientWidth = 384
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCloseQuery = FormCloseQuery
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object OKBtn: TButton
    Left = 304
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 304
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LabeledEdit1: TLabeledEdit
    Left = 100
    Top = 8
    Width = 121
    Height = 21
    EditLabel.Width = 36
    EditLabel.Height = 13
    EditLabel.Caption = 'пароль'
    LabelPosition = lpLeft
    PasswordChar = '*'
    TabOrder = 2
  end
  object LabeledEdit2: TLabeledEdit
    Left = 100
    Top = 32
    Width = 121
    Height = 21
    EditLabel.Width = 82
    EditLabel.Height = 13
    EditLabel.Caption = 'подтверждение'
    LabelPosition = lpLeft
    PasswordChar = '*'
    TabOrder = 3
  end
  object CheckBox1: TCheckBox
    Left = 8
    Top = 64
    Width = 365
    Height = 17
    Caption = 'в дилерской базе удалить фильтры из справочника заказов'
    TabOrder = 4
  end
  object CheckBox2: TCheckBox
    Left = 8
    Top = 84
    Width = 365
    Height = 17
    Hint = 
      'на данном этапе редактирование курсов в любом случае недоступно ' +
      'в безлимитной дилерской программе'
    Caption = 'в дилерской программе разрешить редактирование курсов валют'
    TabOrder = 5
  end
  object DateTimePicker1: TDateTimePicker
    Left = 195
    Top = 104
    Width = 93
    Height = 21
    Date = 40283.565787557870000000
    Time = 40283.565787557870000000
    TabOrder = 6
  end
  object CheckBox3: TCheckBox
    Left = 8
    Top = 104
    Width = 181
    Height = 17
    Caption = 'ограничить срок действия'
    TabOrder = 7
    OnClick = CheckBox3Click
  end
  object CheckBox4: TCheckBox
    Left = 8
    Top = 124
    Width = 365
    Height = 17
    Hint = 
      'по окончании срока действия дилерская версия не будет запускатьс' +
      'я'
    Caption = 'жеское ограничение срока действия'
    TabOrder = 8
    OnClick = CheckBox3Click
  end
  object CheckBox5: TCheckBox
    Left = 8
    Top = 144
    Width = 365
    Height = 17
    Hint = 
      'по окончании срока действия дилерская версия не будет запускатьс' +
      'я'
    Caption = 'включить защиту от копированя в безлимитной дилерской'
    TabOrder = 9
    OnClick = CheckBox3Click
  end
  object CheckBox6: TCheckBox
    Left = 8
    Top = 164
    Width = 365
    Height = 17
    Hint = 
      'по окончании срока действия дилерская версия не будет запускатьс' +
      'я'
    Caption = 'при первом запуске запросить дилерский пароль'
    TabOrder = 10
    OnClick = CheckBox3Click
  end
end
