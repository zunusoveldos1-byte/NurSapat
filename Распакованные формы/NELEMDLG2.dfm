object NElemDlg2: TNElemDlg2
  Left = 271
  Top = 208
  BorderIcons = [biSystemMenu, biMaximize]
  Caption = 'Настройка элементов конструкций'
  ClientHeight = 543
  ClientWidth = 777
  Color = clWindow
  Constraints.MinHeight = 554
  Constraints.MinWidth = 785
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = NElemDlg2Create
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnShow = NElemDlg2Show
  PixelsPerInch = 96
  TextHeight = 13
  object Panel1: TPanel
    Left = 0
    Top = 0
    Width = 777
    Height = 543
    Align = alClient
    BevelOuter = bvNone
    Color = clWhite
    TabOrder = 0
    OnResize = Panel1Resize
    object Label31: TLabel
      Left = 115
      Top = 114
      Width = 126
      Height = 13
      Caption = 'Перечень элементов'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -11
      Font.Name = 'MS Sans Serif'
      Font.Style = [fsBold]
      ParentFont = False
    end
    object Label11: TLabel
      Left = 103
      Top = 54
      Width = 44
      Height = 13
      Caption = 'Раздел'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -11
      Font.Name = 'MS Sans Serif'
      Font.Style = [fsBold]
      ParentFont = False
    end
    object NB1: TPageControl
      Left = 273
      Top = 0
      Width = 505
      Height = 277
      ActivePage = NB1Dobav
      Style = tsButtons
      TabHeight = 1
      TabOrder = 4
      TabWidth = 1
      OnChanging = NB1Changing
      object NB1Korob: TTabSheet
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object KorLabel8: TLabel
          Left = 207
          Top = 18
          Width = 45
          Height = 13
          Caption = 'Профиль'
        end
        object KorLabel1: TLabel
          Left = 36
          Top = 36
          Width = 33
          Height = 13
          Caption = 'Левый'
        end
        object KorLabel2: TLabel
          Left = 36
          Top = 56
          Width = 42
          Height = 13
          Caption = 'Верхний'
        end
        object KorLabel3: TLabel
          Left = 36
          Top = 76
          Width = 39
          Height = 13
          Caption = 'Правый'
        end
        object KorLabel4: TLabel
          Left = 36
          Top = 96
          Width = 39
          Height = 13
          Caption = 'Нижний'
        end
        object KorImage11: TImage
          Left = 412
          Top = 72
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object KorImage12: TImage
          Left = 412
          Top = 52
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object KorImage22: TImage
          Left = 452
          Top = 32
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object KorImage21: TImage
          Left = 432
          Top = 32
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object KorImage31: TImage
          Left = 472
          Top = 52
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object KorImage32: TImage
          Left = 472
          Top = 72
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object KorImage42: TImage
          Left = 432
          Top = 92
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object KorImage41: TImage
          Left = 452
          Top = 92
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object KorEdit5: TBtnEdit
          Tag = 4
          Left = 84
          Top = 92
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 0
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object KorEdit4: TBtnEdit
          Tag = 3
          Left = 84
          Top = 72
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 1
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object KorEdit3: TBtnEdit
          Tag = 2
          Left = 84
          Top = 52
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 2
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object KorEdit2: TBtnEdit
          Tag = 1
          Left = 84
          Top = 32
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 3
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object GroupBox4: TGroupBox
          Left = 4
          Top = 172
          Width = 361
          Height = 93
          Caption = 'Ограничения размеров'
          TabOrder = 4
          object LabeledEdit1: TLabeledEdit
            Left = 156
            Top = 16
            Width = 41
            Height = 21
            EditLabel.Width = 130
            EditLabel.Height = 13
            EditLabel.Caption = 'ширина минимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 0
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit2: TLabeledEdit
            Left = 312
            Top = 16
            Width = 41
            Height = 21
            EditLabel.Width = 94
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 1
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit3: TLabeledEdit
            Left = 312
            Top = 40
            Width = 41
            Height = 21
            EditLabel.Width = 94
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 2
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit4: TLabeledEdit
            Left = 156
            Top = 40
            Width = 41
            Height = 21
            EditLabel.Width = 129
            EditLabel.Height = 13
            EditLabel.Caption = 'высота минимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 3
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit17: TLabeledEdit
            Left = 312
            Top = 64
            Width = 41
            Height = 21
            EditLabel.Width = 104
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, м.кв.'
            LabelPosition = lpLeft
            TabOrder = 4
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit18: TLabeledEdit
            Left = 156
            Top = 64
            Width = 41
            Height = 21
            EditLabel.Width = 148
            EditLabel.Height = 13
            EditLabel.Caption = 'площадь минимальная, м.кв.'
            LabelPosition = lpLeft
            TabOrder = 5
            OnChange = LabeledEdit1Change
          end
        end
      end
      object NB1Stvor: TTabSheet
        ImageIndex = 1
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object StvImage41: TImage
          Left = 452
          Top = 92
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object StvImage42: TImage
          Left = 432
          Top = 92
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object StvImage32: TImage
          Left = 472
          Top = 72
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object StvImage31: TImage
          Left = 472
          Top = 52
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object StvImage21: TImage
          Left = 432
          Top = 32
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object StvImage22: TImage
          Left = 452
          Top = 32
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object StvImage12: TImage
          Left = 412
          Top = 52
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object StvImage11: TImage
          Left = 412
          Top = 72
          Width = 18
          Height = 18
          OnClick = KorImage11Click
        end
        object Label27: TLabel
          Left = 36
          Top = 36
          Width = 33
          Height = 13
          Caption = 'Левый'
        end
        object Label28: TLabel
          Left = 36
          Top = 56
          Width = 42
          Height = 13
          Caption = 'Верхний'
        end
        object Label29: TLabel
          Left = 36
          Top = 76
          Width = 39
          Height = 13
          Caption = 'Правый'
        end
        object Label30: TLabel
          Left = 36
          Top = 96
          Width = 39
          Height = 13
          Caption = 'Нижний'
        end
        object Label32: TLabel
          Left = 207
          Top = 18
          Width = 45
          Height = 13
          Caption = 'Профиль'
        end
        object Label18: TLabel
          Left = 248
          Top = 4
          Width = 77
          Height = 13
          Caption = 'граф. обознач.'
        end
        object StvEdit1: TBtnEdit
          Tag = 1
          Left = 84
          Top = 32
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 0
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object StvEdit2: TBtnEdit
          Tag = 2
          Left = 84
          Top = 52
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 1
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object StvEdit3: TBtnEdit
          Tag = 3
          Left = 84
          Top = 72
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 2
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object StvEdit4: TBtnEdit
          Tag = 4
          Left = 84
          Top = 92
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 3
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ComboBox2: TComboBox
          Left = 328
          Top = 0
          Width = 169
          Height = 21
          Style = csDropDownList
          ItemHeight = 0
          TabOrder = 4
          OnChange = ComboBox2Change
        end
        object GroupBox5: TGroupBox
          Left = 4
          Top = 172
          Width = 361
          Height = 93
          Caption = 'Ограничения размеров'
          TabOrder = 5
          object LabeledEdit5: TLabeledEdit
            Left = 156
            Top = 16
            Width = 41
            Height = 21
            EditLabel.Width = 130
            EditLabel.Height = 13
            EditLabel.Caption = 'ширина минимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 0
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit6: TLabeledEdit
            Left = 312
            Top = 16
            Width = 41
            Height = 21
            EditLabel.Width = 94
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 1
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit7: TLabeledEdit
            Left = 312
            Top = 40
            Width = 41
            Height = 21
            EditLabel.Width = 94
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 2
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit8: TLabeledEdit
            Left = 156
            Top = 40
            Width = 41
            Height = 21
            EditLabel.Width = 129
            EditLabel.Height = 13
            EditLabel.Caption = 'высота минимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 3
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit19: TLabeledEdit
            Left = 312
            Top = 64
            Width = 41
            Height = 21
            EditLabel.Width = 104
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, м.кв.'
            LabelPosition = lpLeft
            TabOrder = 4
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit20: TLabeledEdit
            Left = 156
            Top = 64
            Width = 41
            Height = 21
            EditLabel.Width = 148
            EditLabel.Height = 13
            EditLabel.Caption = 'площадь минимальная, м.кв.'
            LabelPosition = lpLeft
            TabOrder = 5
            OnChange = LabeledEdit1Change
          end
        end
      end
      object NB1Impost: TTabSheet
        ImageIndex = 2
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object ImpLabel1: TLabel
          Left = 207
          Top = 10
          Width = 45
          Height = 13
          Caption = 'Профиль'
        end
        object ImpEdit2: TBtnEdit
          Tag = 1
          Left = 84
          Top = 24
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 0
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object CheckBox3: TCheckBox
          Left = 84
          Top = 60
          Width = 221
          Height = 17
          Caption = 'импост вертикальный'
          TabOrder = 1
          OnClick = CheckBox1Click
        end
        object CheckBox4: TCheckBox
          Left = 84
          Top = 76
          Width = 221
          Height = 17
          Caption = 'импост горизонтальный'
          TabOrder = 2
          OnClick = CheckBox1Click
        end
        object CheckBox5: TCheckBox
          Left = 84
          Top = 92
          Width = 221
          Height = 17
          Caption = 'импост наклонный'
          TabOrder = 3
          OnClick = CheckBox1Click
        end
        object CheckBox6: TCheckBox
          Left = 84
          Top = 108
          Width = 221
          Height = 17
          Caption = 'стойка/ригель в крайнем положении'
          TabOrder = 4
          OnClick = CheckBox1Click
        end
        object CheckBox7: TCheckBox
          Left = 84
          Top = 124
          Width = 221
          Height = 17
          Caption = 'шпросс вертикальный'
          TabOrder = 5
          OnClick = CheckBox1Click
        end
        object CheckBox8: TCheckBox
          Left = 84
          Top = 140
          Width = 221
          Height = 17
          Caption = 'шпросс горизонтальный'
          TabOrder = 6
          OnClick = CheckBox1Click
        end
        object CheckBox11: TCheckBox
          Left = 84
          Top = 156
          Width = 221
          Height = 17
          Caption = 'вварной импост'
          TabOrder = 7
          OnClick = CheckBox1Click
        end
        object CheckBox13: TCheckBox
          Left = 84
          Top = 172
          Width = 221
          Height = 17
          Caption = 'штульп'
          TabOrder = 8
          OnClick = CheckBox1Click
        end
      end
      object NB1Steklo: TTabSheet
        ImageIndex = 3
        object OstLabel1: TLabel
          Left = 32
          Top = 20
          Width = 37
          Height = 13
          Caption = 'Стекло'
        end
        object OstLabel2: TLabel
          Left = 32
          Top = 40
          Width = 47
          Height = 13
          Caption = 'Дистанц.'
        end
        object OstLabel3: TLabel
          Left = 32
          Top = 60
          Width = 37
          Height = 13
          Caption = 'Стекло'
        end
        object OstLabel4: TLabel
          Left = 32
          Top = 80
          Width = 47
          Height = 13
          Caption = 'Дистанц.'
        end
        object OstLabel7: TLabel
          Left = 32
          Top = 100
          Width = 37
          Height = 13
          Caption = 'Стекло'
        end
        object OstEdit2: TBtnEdit
          Tag = 1
          Left = 84
          Top = 16
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 0
          OnBtnClick = Edit2DblClick
          OnDblClick = Edit2DblClick
        end
        object OstEdit3: TBtnEdit
          Tag = 2
          Left = 84
          Top = 36
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 1
          OnBtnClick = Edit2DblClick
          OnDblClick = Edit2DblClick
        end
        object OstEdit4: TBtnEdit
          Tag = 3
          Left = 84
          Top = 56
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 2
          OnBtnClick = Edit2DblClick
          OnDblClick = Edit2DblClick
        end
        object OstEdit5: TBtnEdit
          Tag = 4
          Left = 84
          Top = 76
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 3
          OnBtnClick = Edit2DblClick
          OnDblClick = Edit2DblClick
        end
        object OstEdit6: TBtnEdit
          Tag = 5
          Left = 84
          Top = 96
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 4
          OnBtnClick = Edit2DblClick
          OnDblClick = Edit2DblClick
        end
        object OstEdit7: TLabeledEdit
          Tag = 2
          Left = 352
          Top = 116
          Width = 53
          Height = 19
          Ctl3D = False
          EditLabel.Width = 68
          EditLabel.Height = 13
          EditLabel.Caption = 'толщина, мм.'
          LabelPosition = lpLeft
          ParentCtl3D = False
          TabOrder = 5
          OnChange = IzgEdit2Exit
        end
        object CheckBox1: TCheckBox
          Left = 84
          Top = 120
          Width = 105
          Height = 17
          Caption = 'печать в заявке'
          TabOrder = 6
          OnClick = CheckBox1Click
        end
        object GroupBox6: TGroupBox
          Left = 4
          Top = 172
          Width = 361
          Height = 93
          Caption = 'Ограничения размеров'
          TabOrder = 7
          object LabeledEdit9: TLabeledEdit
            Left = 156
            Top = 16
            Width = 41
            Height = 21
            EditLabel.Width = 130
            EditLabel.Height = 13
            EditLabel.Caption = 'ширина минимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 0
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit10: TLabeledEdit
            Left = 312
            Top = 16
            Width = 41
            Height = 21
            EditLabel.Width = 94
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 1
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit11: TLabeledEdit
            Left = 312
            Top = 40
            Width = 41
            Height = 21
            EditLabel.Width = 94
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 2
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit12: TLabeledEdit
            Left = 156
            Top = 40
            Width = 41
            Height = 21
            EditLabel.Width = 129
            EditLabel.Height = 13
            EditLabel.Caption = 'высота минимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 3
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit21: TLabeledEdit
            Left = 312
            Top = 64
            Width = 41
            Height = 21
            EditLabel.Width = 104
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, м.кв.'
            LabelPosition = lpLeft
            TabOrder = 4
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit22: TLabeledEdit
            Left = 156
            Top = 64
            Width = 41
            Height = 21
            EditLabel.Width = 148
            EditLabel.Height = 13
            EditLabel.Caption = 'площадь минимальная, м.кв.'
            LabelPosition = lpLeft
            TabOrder = 5
            OnChange = LabeledEdit1Change
          end
        end
      end
      object NB1Gofra: TTabSheet
        ImageIndex = 4
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object GofLabel2: TLabel
          Left = 44
          Top = 28
          Width = 34
          Height = 13
          Caption = 'Слой 1'
        end
        object GofLabel3: TLabel
          Left = 44
          Top = 48
          Width = 34
          Height = 13
          Caption = 'Слой 2'
        end
        object GofEdit2: TBtnEdit
          Tag = 2
          Left = 84
          Top = 44
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 0
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object GofEdit3: TBtnEdit
          Tag = 1
          Left = 84
          Top = 24
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 1
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
      end
      object NB1Shtapik: TTabSheet
        ImageIndex = 5
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object Label5: TLabel
          Left = 212
          Top = 8
          Width = 34
          Height = 13
          Caption = 'Слой 1'
        end
        object Label6: TLabel
          Left = 212
          Top = 108
          Width = 34
          Height = 13
          Caption = 'Слой 2'
        end
        object Label8: TLabel
          Left = 36
          Top = 28
          Width = 33
          Height = 13
          Caption = 'Левый'
        end
        object Label9: TLabel
          Left = 36
          Top = 48
          Width = 42
          Height = 13
          Caption = 'Верхний'
        end
        object Label10: TLabel
          Left = 36
          Top = 68
          Width = 39
          Height = 13
          Caption = 'Правый'
        end
        object Label24: TLabel
          Left = 36
          Top = 88
          Width = 39
          Height = 13
          Caption = 'Нижний'
        end
        object Label4: TLabel
          Left = 36
          Top = 128
          Width = 33
          Height = 13
          Caption = 'Левый'
        end
        object Label39: TLabel
          Left = 36
          Top = 148
          Width = 42
          Height = 13
          Caption = 'Верхний'
        end
        object Label40: TLabel
          Left = 36
          Top = 168
          Width = 39
          Height = 13
          Caption = 'Правый'
        end
        object Label41: TLabel
          Left = 36
          Top = 188
          Width = 39
          Height = 13
          Caption = 'Нижний'
        end
        object ShtCBox1: TComboBox
          Left = 364
          Top = 208
          Width = 41
          Height = 28
          Style = csOwnerDrawFixed
          ItemHeight = 22
          TabOrder = 0
          OnChange = ShtCBox1Change
          OnDrawItem = StvCBox1DrawItem
        end
        object ShtEdit6: TBtnEdit
          Tag = 6
          Left = 84
          Top = 144
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 1
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ShtEdit5: TBtnEdit
          Tag = 5
          Left = 84
          Top = 124
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 2
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ShtEdit2: TBtnEdit
          Tag = 2
          Left = 84
          Top = 44
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 3
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ShtEdit1: TBtnEdit
          Tag = 1
          Left = 84
          Top = 24
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 4
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ShtEdit3: TBtnEdit
          Tag = 3
          Left = 84
          Top = 64
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 5
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ShtEdit4: TBtnEdit
          Tag = 4
          Left = 84
          Top = 84
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 6
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ShtEdit7: TBtnEdit
          Tag = 7
          Left = 84
          Top = 164
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 7
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ShtEdit8: TBtnEdit
          Tag = 8
          Left = 84
          Top = 184
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 8
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object ShtEdit9: TLabeledEdit
          Tag = 2
          Left = 236
          Top = 208
          Width = 53
          Height = 19
          Ctl3D = False
          EditLabel.Width = 148
          EditLabel.Height = 13
          EditLabel.Caption = 'толщина минимальная >, мм.'
          LabelPosition = lpLeft
          ParentCtl3D = False
          TabOrder = 9
          OnChange = IzgEdit2Exit
        end
        object ShtEdit10: TLabeledEdit
          Tag = 3
          Left = 236
          Top = 228
          Width = 53
          Height = 19
          Ctl3D = False
          EditLabel.Width = 161
          EditLabel.Height = 13
          EditLabel.Caption = 'толщина максимальная <=, мм.'
          LabelPosition = lpLeft
          ParentCtl3D = False
          TabOrder = 10
          OnChange = IzgEdit2Exit
        end
      end
      object NB1Dobav: TTabSheet
        ImageIndex = 6
        OnResize = NB1DobavResize
        object DobLabel1: TLabel
          Left = 167
          Top = 18
          Width = 68
          Height = 13
          Caption = 'Материалы'
          Font.Charset = DEFAULT_CHARSET
          Font.Color = clWindowText
          Font.Height = -11
          Font.Name = 'MS Sans Serif'
          Font.Style = [fsBold]
          ParentFont = False
        end
        object Label42: TLabel
          Left = 244
          Top = 12
          Width = 77
          Height = 13
          Caption = 'граф. обознач.'
        end
        object ComboBox1: TComboBox
          Left = 324
          Top = 8
          Width = 169
          Height = 21
          Style = csDropDownList
          ItemHeight = 13
          TabOrder = 0
          OnChange = ComboBox1Change
        end
        object ToolBar4: TToolBar
          Left = 4
          Top = 0
          Width = 161
          Height = 36
          Align = alNone
          BorderWidth = 1
          ButtonHeight = 30
          ButtonWidth = 31
          EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
          EdgeInner = esNone
          Images = SahredDMod.ButList24
          TabOrder = 2
          Transparent = True
          object ToolButton8: TToolButton
            Left = 0
            Top = 0
            Action = MaterAdd
          end
          object ToolButton9: TToolButton
            Left = 31
            Top = 0
            Action = MaterDelete
          end
          object ToolButton10: TToolButton
            Left = 62
            Top = 0
            Action = MaterUp
          end
          object ToolButton11: TToolButton
            Left = 93
            Top = 0
            Action = MaterDown
          end
          object ToolButton12: TToolButton
            Left = 124
            Top = 0
            Action = KonElemPrint
          end
        end
        object Panel2: TPanel
          Left = 4
          Top = 181
          Width = 489
          Height = 85
          BevelOuter = bvNone
          Color = clWhite
          TabOrder = 1
          object Label52: TLabel
            Left = 4
            Top = 8
            Width = 54
            Height = 13
            Caption = 'внутрений'
          end
          object Label53: TLabel
            Left = 4
            Top = 28
            Width = 30
            Height = 13
            Caption = 'центр'
          end
          object Label54: TLabel
            Left = 4
            Top = 48
            Width = 57
            Height = 13
            Caption = 'эффективн'
          end
          object Label58: TLabel
            Left = 4
            Top = 68
            Width = 58
            Height = 13
            Caption = 'кор. длины'
          end
          object Label60: TLabel
            Left = 128
            Top = 68
            Width = 65
            Height = 13
            Caption = 'кор штапика'
          end
          object Label57: TLabel
            Left = 128
            Top = 48
            Width = 62
            Height = 13
            Caption = 'кор створки'
          end
          object Label56: TLabel
            Left = 128
            Top = 28
            Width = 56
            Height = 13
            Caption = 'кор стекла'
          end
          object Label55: TLabel
            Left = 128
            Top = 8
            Width = 62
            Height = 13
            Caption = 'кор импоста'
          end
          object Label33: TLabel
            Left = 268
            Top = 8
            Width = 38
            Height = 13
            Caption = 'Ix, см.4'
          end
          object Label34: TLabel
            Left = 268
            Top = 28
            Width = 38
            Height = 13
            Caption = 'Iy, см.4'
          end
          object Label35: TLabel
            Left = 256
            Top = 48
            Width = 47
            Height = 13
            Caption = 'Е-модуль'
          end
          object Label38: TLabel
            Left = 396
            Top = 52
            Width = 67
            Height = 13
            Caption = 'алюм - 70000'
          end
          object Label37: TLabel
            Left = 396
            Top = 40
            Width = 75
            Height = 13
            Caption = 'сталь - 210000'
          end
          object Label36: TLabel
            Left = 408
            Top = 28
            Width = 47
            Height = 13
            Caption = 'Е-модуль'
          end
          object Edit1: TEdit
            Left = 68
            Top = 4
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 0
            OnChange = DobEditIyExit
          end
          object Edit2: TEdit
            Left = 68
            Top = 24
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 1
            OnChange = DobEditIyExit
          end
          object Edit3: TEdit
            Left = 68
            Top = 44
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 2
            OnChange = DobEditIyExit
          end
          object Edit7: TEdit
            Left = 68
            Top = 64
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 3
            OnChange = DobEditIyExit
          end
          object Edit4: TEdit
            Left = 196
            Top = 4
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 4
            OnChange = DobEditIyExit
          end
          object Edit5: TEdit
            Left = 196
            Top = 24
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 5
            OnChange = DobEditIyExit
          end
          object Edit6: TEdit
            Left = 196
            Top = 44
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 6
            OnChange = DobEditIyExit
          end
          object Edit9: TEdit
            Left = 196
            Top = 64
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 7
            OnChange = DobEditIyExit
          end
          object DobEditEMod: TEdit
            Left = 308
            Top = 44
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 10
            OnChange = DobEditIyExit
          end
          object DobEditIy: TEdit
            Left = 308
            Top = 24
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 9
            OnChange = DobEditIyExit
          end
          object DobEditIx: TEdit
            Left = 308
            Top = 4
            Width = 49
            Height = 19
            Ctl3D = False
            ParentCtl3D = False
            TabOrder = 8
            OnChange = DobEditIyExit
          end
          object CheckBox2: TCheckBox
            Left = 308
            Top = 68
            Width = 137
            Height = 17
            Caption = 'печать как комплекта'
            TabOrder = 11
            OnClick = CheckBox1Click
          end
        end
        object DobLBox: TADGListView
          Left = 4
          Top = 36
          Width = 489
          Height = 145
          Columns = <>
          ReadOnly = False
          PopupMenu = PopupMenuMaters
          TabOrder = 3
          OnDblClick = DobLBoxDblClick
        end
      end
      object NB1Init: TTabSheet
        ImageIndex = 8
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
      end
      object NB1Jaluz: TTabSheet
        ImageIndex = 9
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object Label7: TLabel
          Left = 207
          Top = 10
          Width = 31
          Height = 13
          Caption = 'Лента'
        end
        object JlzEdit1: TBtnEdit
          Tag = 1
          Left = 84
          Top = 24
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 0
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object JzlRGroup1: TRadioGroup
          Left = 276
          Top = 128
          Width = 141
          Height = 53
          Caption = 'Коррекция числа лент'
          ItemIndex = 0
          Items.Strings = (
            'Нет'
            'Четное число')
          TabOrder = 1
          OnClick = JzlCBox1Click
        end
      end
      object NB1Slide: TTabSheet
        ImageIndex = 10
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object Label12: TLabel
          Left = 207
          Top = 14
          Width = 43
          Height = 13
          Caption = 'Створка'
        end
        object Label13: TLabel
          Left = 36
          Top = 32
          Width = 33
          Height = 13
          Caption = 'Левый'
        end
        object Label14: TLabel
          Left = 36
          Top = 52
          Width = 42
          Height = 13
          Caption = 'Верхний'
        end
        object Label20: TLabel
          Left = 36
          Top = 72
          Width = 39
          Height = 13
          Caption = 'Правый'
        end
        object Label26: TLabel
          Left = 36
          Top = 92
          Width = 39
          Height = 13
          Caption = 'Нижний'
        end
        object Sl2CBox1: TComboBox
          Left = 364
          Top = 140
          Width = 41
          Height = 28
          Style = csOwnerDrawFixed
          ItemHeight = 22
          TabOrder = 0
          OnChange = ShtCBox1Change
          OnDrawItem = StvCBox1DrawItem
        end
        object Sl2Edit3: TBtnEdit
          Tag = 3
          Left = 84
          Top = 68
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 1
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object Sl2Edit2: TBtnEdit
          Tag = 2
          Left = 84
          Top = 48
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 2
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object Sl2Edit1: TBtnEdit
          Tag = 1
          Left = 84
          Top = 28
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 3
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object Sl2Edit4: TBtnEdit
          Tag = 4
          Left = 84
          Top = 88
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 4
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object GroupBox7: TGroupBox
          Left = 4
          Top = 172
          Width = 361
          Height = 93
          Caption = 'Ограничения размеров'
          TabOrder = 5
          object LabeledEdit13: TLabeledEdit
            Left = 156
            Top = 16
            Width = 41
            Height = 21
            EditLabel.Width = 130
            EditLabel.Height = 13
            EditLabel.Caption = 'ширина минимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 0
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit14: TLabeledEdit
            Left = 312
            Top = 16
            Width = 41
            Height = 21
            EditLabel.Width = 94
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 1
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit15: TLabeledEdit
            Left = 312
            Top = 40
            Width = 41
            Height = 21
            EditLabel.Width = 94
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 2
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit16: TLabeledEdit
            Left = 156
            Top = 40
            Width = 41
            Height = 21
            EditLabel.Width = 129
            EditLabel.Height = 13
            EditLabel.Caption = 'высота минимальная, мм.'
            LabelPosition = lpLeft
            TabOrder = 3
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit23: TLabeledEdit
            Left = 312
            Top = 64
            Width = 41
            Height = 21
            EditLabel.Width = 104
            EditLabel.Height = 13
            EditLabel.Caption = 'максимальная, м.кв.'
            LabelPosition = lpLeft
            TabOrder = 4
            OnChange = LabeledEdit1Change
          end
          object LabeledEdit24: TLabeledEdit
            Left = 156
            Top = 64
            Width = 41
            Height = 21
            EditLabel.Width = 148
            EditLabel.Height = 13
            EditLabel.Caption = 'площадь минимальная, м.кв.'
            LabelPosition = lpLeft
            TabOrder = 5
            OnChange = LabeledEdit1Change
          end
        end
      end
      object NB1StkMosk: TTabSheet
        ImageIndex = 11
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object Label23: TLabel
          Left = 52
          Top = 196
          Width = 27
          Height = 13
          Caption = 'кор y'
        end
        object Label22: TLabel
          Left = 52
          Top = 176
          Width = 27
          Height = 13
          Caption = 'кор x'
        end
        object Label17: TLabel
          Left = 68
          Top = 152
          Width = 12
          Height = 13
          Caption = 'dh'
        end
        object Label16: TLabel
          Left = 68
          Top = 132
          Width = 8
          Height = 13
          Caption = 'dl'
        end
        object Label15: TLabel
          Left = 29
          Top = 28
          Width = 47
          Height = 13
          Caption = 'Лев/прав'
        end
        object Label21: TLabel
          Left = 31
          Top = 48
          Width = 45
          Height = 13
          Caption = 'Верх/низ'
        end
        object Label25: TLabel
          Left = 48
          Top = 68
          Width = 31
          Height = 13
          Caption = 'Сетка'
        end
        object StmEdit4: TEdit
          Tag = 2
          Left = 84
          Top = 128
          Width = 53
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          TabOrder = 0
          OnChange = IzgEdit2Exit
        end
        object StmEdit5: TEdit
          Tag = 3
          Left = 84
          Top = 148
          Width = 53
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          TabOrder = 1
          OnChange = IzgEdit2Exit
        end
        object StmEdit6: TEdit
          Tag = 4
          Left = 84
          Top = 172
          Width = 53
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          TabOrder = 2
          OnChange = IzgEdit2Exit
        end
        object StmEdit7: TEdit
          Tag = 5
          Left = 84
          Top = 192
          Width = 53
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          TabOrder = 3
          OnChange = IzgEdit2Exit
        end
        object StmCBox1: TComboBox
          Left = 364
          Top = 128
          Width = 41
          Height = 28
          Style = csOwnerDrawFixed
          ItemHeight = 22
          TabOrder = 4
          OnChange = ShtCBox1Change
          OnDrawItem = StvCBox1DrawItem
        end
        object StmEdit3: TBtnEdit
          Tag = 3
          Left = 84
          Top = 64
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 5
          OnBtnClick = Edit2DblClick
          OnDblClick = Edit2DblClick
        end
        object StmEdit2: TBtnEdit
          Tag = 2
          Left = 84
          Top = 44
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 6
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object StmEdit1: TBtnEdit
          Tag = 1
          Left = 84
          Top = 24
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 7
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
      end
      object NB1Armirov: TTabSheet
        ImageIndex = 12
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
      end
      object NB1RolNapr: TTabSheet
        ImageIndex = 13
        ExplicitLeft = 0
        ExplicitTop = 0
        ExplicitWidth = 0
        ExplicitHeight = 0
        object Label43: TLabel
          Left = 40
          Top = 48
          Width = 38
          Height = 13
          Caption = 'правый'
        end
        object Label59: TLabel
          Left = 44
          Top = 28
          Width = 32
          Height = 13
          Caption = 'левый'
        end
        object RolNapr1: TBtnEdit
          Tag = 1
          Left = 84
          Top = 24
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 0
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
        object RolNapr2: TBtnEdit
          Tag = 2
          Left = 84
          Top = 44
          Width = 321
          Height = 19
          Ctl3D = False
          ParentCtl3D = False
          ReadOnly = True
          TabOrder = 1
          OnBtnClick = ProfNewClick
          OnDblClick = ProfNewClick
        end
      end
    end
    object GroupBox: TComboBox
      Left = 4
      Top = 68
      Width = 269
      Height = 21
      Style = csDropDownList
      DropDownCount = 30
      ItemHeight = 13
      TabOrder = 2
      OnChange = GroupBoxChange
    end
    object LBox: TListBox
      Left = 4
      Top = 132
      Width = 269
      Height = 405
      Ctl3D = False
      ItemHeight = 13
      ParentCtl3D = False
      PopupMenu = InsDelPMenu1
      TabOrder = 3
      OnClick = LBoxClick
      OnDblClick = KonElemEditExecute
    end
    object ProizvodCBox: TComboBox
      Left = 4
      Top = 4
      Width = 269
      Height = 21
      Hint = 'окно выбора производителя'
      Style = csDropDownList
      DropDownCount = 30
      ItemHeight = 13
      TabOrder = 0
      OnChange = GroupBoxChange
    end
    object ToolBar3: TToolBar
      Left = 5
      Top = 95
      Width = 100
      Height = 36
      Align = alNone
      BorderWidth = 1
      ButtonHeight = 30
      ButtonWidth = 31
      EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
      EdgeInner = esNone
      Images = SahredDMod.ButList24
      TabOrder = 8
      Transparent = True
      object ToolButton5: TToolButton
        Left = 0
        Top = 0
        Action = KonElemAdd
      end
      object ToolButton7: TToolButton
        Left = 31
        Top = 0
        Action = KonElemEdit
      end
      object ToolButton6: TToolButton
        Left = 62
        Top = 0
        Action = KonElemDel
      end
    end
    object ComboBox3: TComboBox
      Left = 4
      Top = 28
      Width = 269
      Height = 21
      Hint = 'окно выбора серии'
      Style = csDropDownList
      DropDownCount = 30
      ItemHeight = 13
      TabOrder = 1
      OnChange = ComboBox3Change
    end
    object GroupBox1: TGroupBox
      Left = 280
      Top = 276
      Width = 173
      Height = 184
      Caption = 'Серии'
      TabOrder = 5
      object ToolBar1: TToolBar
        Left = 8
        Top = 16
        Width = 68
        Height = 36
        Align = alNone
        BorderWidth = 1
        ButtonHeight = 30
        ButtonWidth = 31
        EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
        EdgeInner = esNone
        Images = SahredDMod.ButList24
        TabOrder = 0
        Transparent = True
        object ToolButton1: TToolButton
          Left = 0
          Top = 0
          Action = SeriesAdd
        end
        object ToolButton3: TToolButton
          Left = 31
          Top = 0
          Action = SeriesDelete
        end
      end
      object SeriesLBox: TListBox
        Left = 8
        Top = 52
        Width = 157
        Height = 125
        Style = lbOwnerDrawFixed
        Ctl3D = False
        ItemHeight = 15
        MultiSelect = True
        ParentCtl3D = False
        TabOrder = 1
        OnDrawItem = SeriesLBoxDrawItem
      end
    end
    object GroupBox2: TGroupBox
      Left = 460
      Top = 276
      Width = 313
      Height = 261
      Caption = 'Вспомогательные элементы'
      TabOrder = 7
      object ToolBar2: TToolBar
        Left = 8
        Top = 16
        Width = 68
        Height = 36
        Align = alNone
        BorderWidth = 1
        ButtonHeight = 30
        ButtonWidth = 31
        EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
        EdgeInner = esNone
        Images = SahredDMod.ButList24
        TabOrder = 0
        Transparent = True
        object ToolButton2: TToolButton
          Left = 0
          Top = 0
          Action = DobElmAdd
        end
        object ToolButton4: TToolButton
          Left = 31
          Top = 0
          Action = DobElmDel
        end
      end
      object ListBox5: TListBox
        Left = 8
        Top = 52
        Width = 297
        Height = 201
        Ctl3D = False
        ItemHeight = 13
        MultiSelect = True
        ParentCtl3D = False
        PopupMenu = PopupMenuDobElm
        TabOrder = 1
      end
    end
    object GroupBox3: TGroupBox
      Left = 280
      Top = 460
      Width = 173
      Height = 57
      Caption = 'Отображение'
      TabOrder = 6
      object CheckBox9: TCheckBox
        Left = 8
        Top = 16
        Width = 157
        Height = 17
        Caption = 'показывать элемент'
        TabOrder = 0
        OnClick = CheckBox1Click
      end
      object CheckBox10: TCheckBox
        Left = 8
        Top = 32
        Width = 157
        Height = 17
        Caption = 'выделить элемент'
        TabOrder = 1
        OnClick = CheckBox1Click
      end
    end
    object CheckBox12: TCheckBox
      Left = 288
      Top = 520
      Width = 157
      Height = 17
      Caption = 'признак двери'
      TabOrder = 9
      OnClick = CheckBox1Click
    end
  end
  object InsDelPMenu1: TPopupMenu
    Left = 56
    Top = 440
    object N1: TMenuItem
      Action = KonElemAdd
    end
    object N14: TMenuItem
      Action = KonElemEdit
    end
    object N3: TMenuItem
      Action = KonElemDel
    end
    object N2: TMenuItem
      Caption = '-'
    end
    object N6: TMenuItem
      Action = MasterMakeElem
    end
    object N17: TMenuItem
      Action = RashirToProf
    end
    object N21: TMenuItem
      Action = ToDopolnit
    end
    object N5: TMenuItem
      Caption = '-'
    end
    object N4: TMenuItem
      Caption = 'Удалить все элементы'
      OnClick = N4Click
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 24
    Top = 440
    object SeriesAdd: TAction
      Caption = 'Добавить серию'
      Hint = 'Добавить серию'
      ImageIndex = 0
      OnExecute = SeriesAddExecute
    end
    object SeriesDelete: TAction
      Caption = 'Удалить серию'
      Hint = 'Удалить серию'
      ImageIndex = 5
      OnExecute = SeriesDeleteExecute
    end
    object ViewAll: TAction
      ShortCut = 49217
      OnExecute = ViewAllExecute
    end
    object ToProfil: TAction
      ShortCut = 49232
      OnExecute = ToProfilExecute
    end
    object ReplaceAndDubl: TAction
      ShortCut = 49220
      OnExecute = ReplaceAndDublExecute
    end
    object ToDopolnit: TAction
      Caption = 'Перенести в "дополнительные элементы"'
      Hint = 'Перенести в "дополнительные элементы"'
      OnExecute = ToDopolnitExecute
    end
    object DobElmAdd: TAction
      Caption = 'Добавить элемент'
      Hint = 'Добавить элемент'
      ImageIndex = 0
      OnExecute = DobElmAddExecute
    end
    object DobElmDel: TAction
      Caption = 'Удалить элемент'
      Hint = 'Удалить элемент'
      ImageIndex = 5
      OnExecute = DobElmDelExecute
    end
    object DobElmReplace: TAction
      Caption = 'Заменить'
      OnExecute = DobElmAddExecute
    end
    object DobElmCopy: TAction
      Caption = 'Копировать'
      OnExecute = DobElmCopyExecute
      OnUpdate = DobElmCopyUpdate
    end
    object DobElmPaste: TAction
      Caption = 'Вставить'
      OnExecute = DobElmPasteExecute
      OnUpdate = DobElmPasteUpdate
    end
    object KonElemAdd: TAction
      Caption = 'Добавить элемент'
      Hint = 'Добавить элемент'
      ImageIndex = 0
      OnExecute = KonElemAddExecute
    end
    object KonElemDel: TAction
      Caption = 'Удалить элемент'
      Hint = 'Удалить элемент'
      ImageIndex = 5
      OnExecute = KonElemDelExecute
    end
    object KonElemEdit: TAction
      Caption = 'Редактировать наименование элемента'
      Hint = 'Редактировать наименование элемента'
      ImageIndex = 7
      OnExecute = KonElemEditExecute
    end
    object KonElemPrint: TAction
      Caption = 'Печатать элемент'
      Hint = 'Печатать элемент'
      ImageIndex = 14
      OnExecute = DobBitBtn1Click
    end
    object MaterAdd: TAction
      Caption = 'Добавить материал'
      Hint = 'Добавить материал'
      ImageIndex = 0
      OnExecute = MaterAddExecute
    end
    object MaterDelete: TAction
      Caption = 'Удалить материал'
      Hint = 'Удалить материал'
      ImageIndex = 5
      OnExecute = MaterDeleteExecute
    end
    object MaterCopy: TAction
      Caption = 'Копировать'
      OnExecute = MaterCopyExecute
    end
    object MaterPaste: TAction
      Caption = 'Вставить'
      OnExecute = MaterPasteExecute
    end
    object MaterUp: TAction
      Caption = 'Поднять вверх'
      Hint = 'Поднять вверх'
      ImageIndex = 17
      OnExecute = SpeedButton2Click
    end
    object MaterDown: TAction
      Caption = 'Опустить вниз'
      Hint = 'Опустить вниз'
      ImageIndex = 6
      OnExecute = SpeedButton2Click
    end
    object RashirToProf: TAction
      Caption = 'Перенести "расширитель" к "профилю"'
      OnExecute = RashirToProfExecute
    end
    object MasterMakeElem: TAction
      Caption = 'Мастер создания элемента'
      OnExecute = MasterMakeElemExecute
    end
  end
  object PopupMenuDobElm: TPopupMenu
    Left = 88
    Top = 440
    object N7: TMenuItem
      Action = DobElmAdd
    end
    object N8: TMenuItem
      Action = DobElmReplace
    end
    object N9: TMenuItem
      Caption = '-'
    end
    object N10: TMenuItem
      Action = DobElmDel
    end
    object N11: TMenuItem
      Caption = '-'
    end
    object N12: TMenuItem
      Action = DobElmCopy
    end
    object N13: TMenuItem
      Action = DobElmPaste
    end
  end
  object PopupMenuMaters: TPopupMenu
    Left = 120
    Top = 440
    object N18: TMenuItem
      Action = MaterAdd
    end
    object N19: TMenuItem
      Action = MaterDelete
    end
    object N20: TMenuItem
      Caption = '-'
    end
    object N15: TMenuItem
      Action = MaterCopy
    end
    object N16: TMenuItem
      Action = MaterPaste
    end
  end
end
