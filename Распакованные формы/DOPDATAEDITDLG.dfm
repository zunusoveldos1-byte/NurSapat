object DopDataEditDlg: TDopDataEditDlg
  Left = 550
  Top = 326
  BorderStyle = bsDialog
  Caption = 'Редактор настроек материала'
  ClientHeight = 688
  ClientWidth = 876
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object DobLabel3: TLabel
    Left = 128
    Top = 28
    Width = 80
    Height = 13
    Caption = 'Зависимость'
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -11
    Font.Name = 'MS Sans Serif'
    Font.Style = [fsBold]
    ParentFont = False
  end
  object OKBtn: TButton
    Left = 796
    Top = 4
    Width = 77
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 7
  end
  object CancelBtn: TButton
    Left = 796
    Top = 32
    Width = 77
    Height = 25
    Cancel = True
    Caption = 'Отмена'
    ModalResult = 2
    TabOrder = 8
  end
  object NB4: TPageControl
    Left = 28
    Top = 68
    Width = 277
    Height = 169
    ActivePage = NB4Nepr
    Style = tsButtons
    TabHeight = 1
    TabOrder = 1
    TabWidth = 1
    object NB4Const: TTabSheet
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object DobLabel15: TLabel
        Left = 56
        Top = 48
        Width = 41
        Height = 13
        Caption = 'позиция'
      end
      object DobLabel4: TLabel
        Left = 104
        Top = 8
        Width = 60
        Height = 13
        Caption = 'Постоянная'
      end
      object DobLabel10: TLabel
        Left = 172
        Top = 28
        Width = 55
        Height = 13
        Caption = '(м,шт.,кг.)'
      end
      object DobEdit2: TEdit
        Left = 100
        Top = 24
        Width = 66
        Height = 19
        Ctl3D = False
        ParentCtl3D = False
        TabOrder = 0
        OnChange = DobEdit2Change
      end
      object DobEdit1: TEdit
        Left = 100
        Top = 44
        Width = 66
        Height = 19
        Ctl3D = False
        ParentCtl3D = False
        TabOrder = 1
        OnChange = DobEdit2Change
      end
    end
    object NB4Nepr: TTabSheet
      ImageIndex = 1
      object DobLabel5: TLabel
        Left = 96
        Top = 8
        Width = 69
        Height = 13
        Caption = 'Непрерывная'
      end
      object DobLabel11: TLabel
        Left = 168
        Top = 28
        Width = 70
        Height = 13
        Caption = 'на 1 (ед.изм.)'
      end
      object DobSButton1: TSpeedButton
        Left = 172
        Top = 4
        Width = 81
        Height = 21
        Caption = 'Дискретная'
        Font.Charset = DEFAULT_CHARSET
        Font.Color = clBlack
        Font.Height = -11
        Font.Name = 'MS Sans Serif'
        Font.Style = []
        ParentFont = False
        OnClick = DobSButton1Click
      end
      object Label36: TLabel
        Left = 202
        Top = 81
        Width = 47
        Height = 13
        Caption = 'Е-модуль'
      end
      object Label37: TLabel
        Left = 187
        Top = 105
        Width = 75
        Height = 13
        Caption = 'сталь - 210000'
      end
      object Label38: TLabel
        Left = 194
        Top = 93
        Width = 67
        Height = 13
        Caption = 'алюм - 70000'
      end
      object DobEdit3: TEdit
        Left = 100
        Top = 24
        Width = 65
        Height = 19
        Ctl3D = False
        ParentCtl3D = False
        TabOrder = 0
        OnChange = DobEdit2Change
      end
      object LabeledEdit1: TLabeledEdit
        Left = 100
        Top = 48
        Width = 65
        Height = 19
        Ctl3D = False
        EditLabel.Width = 38
        EditLabel.Height = 13
        EditLabel.Caption = 'Ix, см.4'
        LabelPosition = lpLeft
        ParentCtl3D = False
        TabOrder = 1
        OnChange = DobEdit2Change
      end
      object LabeledEdit2: TLabeledEdit
        Left = 100
        Top = 68
        Width = 65
        Height = 19
        Ctl3D = False
        EditLabel.Width = 38
        EditLabel.Height = 13
        EditLabel.Caption = 'Iy, см.4'
        LabelPosition = lpLeft
        ParentCtl3D = False
        TabOrder = 2
        OnChange = DobEdit2Change
      end
      object LabeledEdit3: TLabeledEdit
        Left = 100
        Top = 88
        Width = 65
        Height = 19
        Ctl3D = False
        EditLabel.Width = 47
        EditLabel.Height = 13
        EditLabel.Caption = 'Е-модуль'
        LabelPosition = lpLeft
        ParentCtl3D = False
        TabOrder = 3
        OnChange = DobEdit2Change
      end
      object LabeledEdit4: TLabeledEdit
        Left = 100
        Top = 108
        Width = 65
        Height = 19
        Ctl3D = False
        EditLabel.Width = 50
        EditLabel.Height = 13
        EditLabel.Caption = 'група арм'
        LabelPosition = lpLeft
        ParentCtl3D = False
        TabOrder = 4
        OnChange = DobEdit2Change
      end
      object LabeledEdit9: TLabeledEdit
        Left = 100
        Top = 128
        Width = 65
        Height = 19
        Ctl3D = False
        EditLabel.Width = 100
        EditLabel.Height = 13
        EditLabel.Caption = 'минимальная длина'
        LabelPosition = lpLeft
        ParentCtl3D = False
        TabOrder = 5
        OnChange = DobEdit2Change
      end
    end
    object NB4Digital: TTabSheet
      ImageIndex = 2
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object DobSButton2: TSpeedButton
        Left = 152
        Top = 4
        Width = 81
        Height = 21
        Caption = 'Непрерывная'
        Font.Charset = DEFAULT_CHARSET
        Font.Color = clBlack
        Font.Height = -11
        Font.Name = 'MS Sans Serif'
        Font.Style = []
        ParentFont = False
        OnClick = DobSButton2Click
      end
      object DobLabel8: TLabel
        Left = 84
        Top = 8
        Width = 61
        Height = 13
        Caption = 'Дискретная'
      end
      object DobSGrid1: TStringGrid
        Left = 48
        Top = 29
        Width = 168
        Height = 81
        ColCount = 3
        Ctl3D = False
        DefaultColWidth = 48
        DefaultRowHeight = 18
        FixedCols = 0
        RowCount = 11
        Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goEditing, goAlwaysShowEditor, goThumbTracking]
        ParentCtl3D = False
        ScrollBars = ssVertical
        TabOrder = 0
        OnSelectCell = DobSGrid1SelectCell
      end
    end
    object NB4Init: TTabSheet
      ImageIndex = 3
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
    end
    object NB4Prof: TTabSheet
      ImageIndex = 4
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object Label11: TLabel
        Left = 112
        Top = 4
        Width = 49
        Height = 13
        Caption = 'Корекция'
      end
      object Label19: TLabel
        Left = 88
        Top = 76
        Width = 12
        Height = 13
        Caption = 'dy'
      end
      object Label18: TLabel
        Left = 88
        Top = 56
        Width = 12
        Height = 13
        Caption = 'dx'
      end
      object DobLabel12: TLabel
        Tag = -700
        Left = 172
        Top = 28
        Width = 18
        Height = 13
        Caption = 'info'
      end
      object DobEdit4: TEdit
        Left = 104
        Top = 24
        Width = 66
        Height = 19
        Ctl3D = False
        ParentCtl3D = False
        TabOrder = 0
        OnChange = DobEdit2Change
      end
      object DobEdit5: TEdit
        Left = 104
        Top = 52
        Width = 66
        Height = 19
        Ctl3D = False
        ParentCtl3D = False
        TabOrder = 1
        OnChange = DobEdit2Change
      end
      object DobEdit6: TEdit
        Left = 104
        Top = 72
        Width = 66
        Height = 19
        Ctl3D = False
        ParentCtl3D = False
        TabOrder = 2
        OnChange = DobEdit2Change
      end
    end
    object NB4DigitalWH: TTabSheet
      ImageIndex = 5
      ExplicitLeft = 0
      ExplicitTop = 0
      ExplicitWidth = 0
      ExplicitHeight = 0
      object Label45: TLabel
        Left = 96
        Top = 56
        Width = 63
        Height = 13
        Caption = 'Ширина, мм.'
      end
      object Label46: TLabel
        Left = 96
        Top = 76
        Width = 60
        Height = 13
        Caption = 'Высота, мм.'
      end
      object DobEdit12: TLabeledEdit
        Left = 184
        Top = 52
        Width = 49
        Height = 19
        Ctl3D = False
        EditLabel.Tag = -700
        EditLabel.Width = 16
        EditLabel.Height = 13
        EditLabel.Caption = '<='
        LabelPosition = lpLeft
        ParentCtl3D = False
        TabOrder = 2
        OnChange = DobEdit2Change
      end
      object DobEdit14: TLabeledEdit
        Left = 184
        Top = 72
        Width = 49
        Height = 19
        Ctl3D = False
        EditLabel.Tag = -700
        EditLabel.Width = 16
        EditLabel.Height = 13
        EditLabel.Caption = '<='
        LabelPosition = lpLeft
        ParentCtl3D = False
        TabOrder = 4
        OnChange = DobEdit2Change
      end
      object DobEdit11: TLabeledEdit
        Left = 28
        Top = 52
        Width = 49
        Height = 19
        Ctl3D = False
        EditLabel.Tag = -700
        EditLabel.Width = 8
        EditLabel.Height = 13
        EditLabel.Caption = '<'
        LabelPosition = lpRight
        ParentCtl3D = False
        TabOrder = 1
        OnChange = DobEdit2Change
      end
      object DobEdit13: TLabeledEdit
        Left = 28
        Top = 72
        Width = 49
        Height = 19
        Ctl3D = False
        EditLabel.Tag = -700
        EditLabel.Width = 8
        EditLabel.Height = 13
        EditLabel.Caption = '<'
        LabelPosition = lpRight
        ParentCtl3D = False
        TabOrder = 3
        OnChange = DobEdit2Change
      end
      object DobEdit10: TLabeledEdit
        Left = 108
        Top = 16
        Width = 49
        Height = 19
        Ctl3D = False
        EditLabel.Tag = -700
        EditLabel.Width = 60
        EditLabel.Height = 13
        EditLabel.Caption = 'Кол-во, шт.'
        LabelPosition = lpLeft
        ParentCtl3D = False
        TabOrder = 0
        OnChange = DobEdit2Change
      end
    end
  end
  object DobComboBox2: TComboBox
    Left = 4
    Top = 44
    Width = 337
    Height = 21
    Style = csDropDownList
    Ctl3D = False
    DropDownCount = 20
    ItemHeight = 13
    ParentCtl3D = False
    TabOrder = 0
    OnChange = DobComboBox2Change
  end
  object Edit1: TEdit
    Left = 4
    Top = 4
    Width = 77
    Height = 21
    TabStop = False
    ReadOnly = True
    TabOrder = 5
  end
  object BtnEdit1: TBtnEdit
    Left = 84
    Top = 4
    Width = 257
    Height = 21
    TabStop = False
    ReadOnly = True
    TabOrder = 6
    OnBtnClick = BtnEdit1BtnClick
    OnDblClick = BtnEdit1BtnClick
  end
  object GroupBox1: TGroupBox
    Left = 440
    Top = 120
    Width = 433
    Height = 141
    Caption = 'Графическое отображение материала'
    TabOrder = 4
    object Label1: TLabel
      Left = 8
      Top = 20
      Width = 135
      Height = 13
      Caption = 'Графическое обозначение'
    end
    object Image1: TPaintBox
      Left = 304
      Top = 12
      Width = 121
      Height = 121
    end
    object Label2: TLabel
      Left = 8
      Top = 44
      Width = 111
      Height = 13
      Caption = 'Графическая позиция'
    end
    object ComboBox1: TComboBox
      Left = 148
      Top = 16
      Width = 153
      Height = 21
      Style = csDropDownList
      Ctl3D = False
      DropDownCount = 12
      ItemHeight = 13
      ParentCtl3D = False
      TabOrder = 0
      OnChange = DobComboBox2Change
    end
    object ComboBox2: TComboBox
      Left = 148
      Top = 40
      Width = 153
      Height = 21
      Style = csDropDownList
      Ctl3D = False
      DropDownCount = 12
      ItemHeight = 13
      ParentCtl3D = False
      TabOrder = 1
      OnChange = DobComboBox2Change
    end
  end
  object GroupBox2: TGroupBox
    Left = 4
    Top = 420
    Width = 433
    Height = 265
    Caption = 'Дополнительные параметры для настройки фурнитурных позиций'
    TabOrder = 3
    object Label6: TLabel
      Left = 8
      Top = 20
      Width = 62
      Height = 13
      Caption = 'Тип позиции'
    end
    object LabeledEdit5: TLabeledEdit
      Left = 136
      Top = 38
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Width = 127
      EditLabel.Height = 13
      EditLabel.Caption = 'Кол-во ответных планок'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 0
      OnChange = LabeledEdit5Change
    end
    object LabeledEdit6: TLabeledEdit
      Left = 136
      Top = 58
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Width = 104
      EditLabel.Height = 13
      EditLabel.Caption = 'Кол-во шурупов №1'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 1
      OnChange = LabeledEdit5Change
    end
    object LabeledEdit7: TLabeledEdit
      Left = 136
      Top = 78
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Width = 104
      EditLabel.Height = 13
      EditLabel.Caption = 'Кол-во шурупов №2'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 2
      OnChange = LabeledEdit5Change
    end
    object CheckBox1: TCheckBox
      Left = 192
      Top = 40
      Width = 229
      Height = 17
      Caption = 'Отображать высоту ручки'
      TabOrder = 3
      OnClick = CheckBox1Click
    end
    object LabeledEdit10: TLabeledEdit
      Left = 388
      Top = 102
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Width = 192
      EditLabel.Height = 13
      EditLabel.Caption = 'Высота константного положения, мм.'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 4
    end
    object CheckBox4: TCheckBox
      Left = 192
      Top = 60
      Width = 229
      Height = 17
      Caption = 'Подрезка по ширине'
      TabOrder = 5
      OnClick = CheckBox1Click
    end
    object CheckBox5: TCheckBox
      Left = 192
      Top = 80
      Width = 229
      Height = 17
      Caption = 'Подрезка по высоте'
      TabOrder = 6
      OnClick = CheckBox1Click
    end
    object ComboBox5: TComboBox
      Left = 192
      Top = 16
      Width = 237
      Height = 21
      Style = csDropDownList
      Ctl3D = False
      ItemHeight = 13
      ParentCtl3D = False
      TabOrder = 7
      OnChange = DobComboBox2Change
    end
    object LabeledEdit15: TLabeledEdit
      Left = 136
      Top = 122
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Width = 80
      EditLabel.Height = 13
      EditLabel.Caption = 'Размер №1, мм.'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 8
    end
    object LabeledEdit16: TLabeledEdit
      Left = 388
      Top = 122
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Width = 195
      EditLabel.Height = 13
      EditLabel.Caption = 'Коррекция подрезки размера №1, мм.'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 9
    end
    object LabeledEdit17: TLabeledEdit
      Left = 136
      Top = 142
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Width = 80
      EditLabel.Height = 13
      EditLabel.Caption = 'Размер №2, мм.'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 10
    end
    object LabeledEdit18: TLabeledEdit
      Left = 388
      Top = 142
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Width = 195
      EditLabel.Height = 13
      EditLabel.Caption = 'Коррекция подрезки размера №2, мм.'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 11
    end
    object Button1: TButton
      Left = 4
      Top = 124
      Width = 45
      Height = 37
      Caption = 'Подсказка'
      TabOrder = 12
      WordWrap = True
      OnClick = Button1Click
    end
  end
  object GroupBox3: TGroupBox
    Left = 4
    Top = 264
    Width = 433
    Height = 153
    Caption = 'Дополнительные условия'
    TabOrder = 2
    object Label51: TLabel
      Left = 8
      Top = 20
      Width = 45
      Height = 13
      Caption = 'Профиль'
    end
    object Label50: TLabel
      Left = 8
      Top = 44
      Width = 64
      Height = 13
      Caption = 'Доп условие'
    end
    object Label49: TLabel
      Left = 320
      Top = 44
      Width = 57
      Height = 13
      Caption = 'позиция №'
    end
    object Label3: TLabel
      Left = 8
      Top = 68
      Width = 64
      Height = 13
      Caption = 'Открывание'
    end
    object Label4: TLabel
      Left = 8
      Top = 92
      Width = 42
      Height = 13
      Caption = 'Система'
    end
    object Label5: TLabel
      Left = 304
      Top = 114
      Width = 68
      Height = 13
      Caption = 'толщина, мм.'
    end
    object DobComboBox4: TComboBox
      Left = 76
      Top = 16
      Width = 237
      Height = 21
      Style = csDropDownList
      Ctl3D = False
      DropDownCount = 16
      ItemHeight = 13
      ParentCtl3D = False
      TabOrder = 0
      OnChange = DobComboBox2Change
    end
    object DobComboBox3: TComboBox
      Left = 76
      Top = 40
      Width = 237
      Height = 21
      Style = csDropDownList
      Ctl3D = False
      DropDownCount = 30
      ItemHeight = 13
      ParentCtl3D = False
      TabOrder = 2
      OnChange = DobComboBox2Change
    end
    object DobEdit111: TEdit
      Left = 380
      Top = 40
      Width = 49
      Height = 19
      Ctl3D = False
      ParentCtl3D = False
      TabOrder = 3
      OnChange = DobEdit2Change
    end
    object ComboBox3: TComboBox
      Left = 76
      Top = 64
      Width = 237
      Height = 21
      Style = csDropDownList
      Ctl3D = False
      ItemHeight = 13
      ParentCtl3D = False
      TabOrder = 4
      OnChange = DobComboBox2Change
    end
    object ComboBox4: TComboBox
      Left = 76
      Top = 88
      Width = 237
      Height = 21
      Style = csDropDownList
      Ctl3D = False
      ItemHeight = 13
      ParentCtl3D = False
      TabOrder = 5
      OnChange = DobComboBox2Change
    end
    object LabeledEdit8: TLabeledEdit
      Left = 380
      Top = 16
      Width = 49
      Height = 19
      Ctl3D = False
      EditLabel.Width = 61
      EditLabel.Height = 13
      EditLabel.Hint = 
        'група профилезависимых материалов (отв. планка, адаптор ножниц, ' +
        'планка микропроветривания)'
      EditLabel.Caption = 'гр. профзав'
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 1
      OnChange = DobEdit2Change
    end
    object CheckBox3: TCheckBox
      Left = 76
      Top = 112
      Width = 169
      Height = 17
      Caption = 'при толщине заполнения'
      TabOrder = 6
      OnClick = CheckBox1Click
    end
    object LabeledEdit12: TLabeledEdit
      Left = 392
      Top = 112
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Tag = -700
      EditLabel.Width = 16
      EditLabel.Height = 13
      EditLabel.Caption = '<='
      LabelPosition = lpLeft
      ParentCtl3D = False
      TabOrder = 7
      OnChange = DobEdit2Change
    end
    object LabeledEdit14: TLabeledEdit
      Left = 252
      Top = 112
      Width = 37
      Height = 19
      Ctl3D = False
      EditLabel.Tag = -700
      EditLabel.Width = 8
      EditLabel.Height = 13
      EditLabel.Caption = '<'
      LabelPosition = lpRight
      ParentCtl3D = False
      TabOrder = 8
      OnChange = DobEdit2Change
    end
    object CheckBox6: TCheckBox
      Left = 76
      Top = 132
      Width = 353
      Height = 17
      Caption = 'автоматически менять цвет материала по таблице'
      TabOrder = 9
      OnClick = CheckBox1Click
    end
  end
  object LabeledEdit11: TLabeledEdit
    Left = 396
    Top = 220
    Width = 37
    Height = 19
    Ctl3D = False
    EditLabel.Width = 80
    EditLabel.Height = 13
    EditLabel.Hint = 
      'коррекция "эффективного" размера у профиля, применяется для расч' +
      'ета "эффективного" размера в фасадных системах при подборе уплот' +
      'нителей'
    EditLabel.Caption = 'кор. эффектив.'
    EditLabel.ParentShowHint = False
    EditLabel.ShowHint = True
    LabelPosition = lpLeft
    ParentCtl3D = False
    ParentShowHint = False
    ShowHint = False
    TabOrder = 9
    OnChange = DobEdit2Change
  end
  object LabeledEdit13: TLabeledEdit
    Left = 164
    Top = 244
    Width = 269
    Height = 19
    Ctl3D = False
    EditLabel.Width = 157
    EditLabel.Height = 13
    EditLabel.Caption = 'Примечание для производства'
    EditLabel.ParentShowHint = False
    EditLabel.ShowHint = True
    LabelPosition = lpLeft
    ParentCtl3D = False
    ParentShowHint = False
    ShowHint = False
    TabOrder = 10
    OnChange = DobEdit2Change
  end
  object GroupBox4: TGroupBox
    Left = 440
    Top = 264
    Width = 433
    Height = 421
    Caption = 'Сопоставление цветов'
    TabOrder = 11
    object LView: TADGListView
      Left = 4
      Top = 40
      Width = 425
      Height = 377
      Columns = <>
      ReadOnly = False
      TabOrder = 0
      OnDblClick = LViewDblClick
    end
    object CheckBox7: TCheckBox
      Left = 8
      Top = 16
      Width = 353
      Height = 17
      Caption = 'если совпадения нет - не считать материал'
      TabOrder = 1
      OnClick = CheckBox1Click
    end
  end
end
