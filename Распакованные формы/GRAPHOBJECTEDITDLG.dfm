object GraphObjectEditDlg: TGraphObjectEditDlg
  Left = 94
  Top = 64
  BorderStyle = bsDialog
  Caption = 'Редактор графических обозначений'
  ClientHeight = 481
  ClientWidth = 700
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object Label1: TLabel
    Left = 416
    Top = 436
    Width = 73
    Height = 13
    Caption = 'Наименование'
    Transparent = True
  end
  object Label2: TLabel
    Left = 415
    Top = 460
    Width = 75
    Height = 13
    Caption = 'Тип материала'
    Transparent = True
    Visible = False
  end
  object Label3: TLabel
    Left = 56
    Top = 32
    Width = 47
    Height = 13
    Caption = 'Объекты'
    Transparent = True
  end
  object Image1: TPaintBox
    Left = 172
    Top = 48
    Width = 525
    Height = 381
    OnMouseDown = Image1MouseDown
    OnMouseMove = Image1MouseMove
    OnMouseUp = Image1MouseUp
  end
  object ListBox1: TListBox
    Left = 0
    Top = 48
    Width = 169
    Height = 381
    ItemHeight = 13
    TabOrder = 0
    OnClick = ListBox1Click
  end
  object Edit1: TEdit
    Left = 500
    Top = 432
    Width = 197
    Height = 21
    TabOrder = 1
  end
  object TypBox: TComboBox
    Left = 500
    Top = 456
    Width = 197
    Height = 21
    Style = csDropDownList
    DropDownCount = 30
    ItemHeight = 0
    TabOrder = 2
    Visible = False
  end
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 257
    Height = 31
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 25
    ButtonWidth = 25
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList
    TabOrder = 3
    object AddButton: TToolButton
      Left = 0
      Top = 0
      Hint = 'Добавить объект'
      ImageIndex = 0
      OnClick = AddButtonClick
    end
    object DeleteButton: TToolButton
      Left = 25
      Top = 0
      Hint = 'Удалить объект'
      ImageIndex = 1
    end
    object ToolButton1: TToolButton
      Left = 50
      Top = 0
      Width = 8
      ImageIndex = 2
      Style = tbsSeparator
    end
    object ZoomButton: TToolButton
      Tag = 1
      Left = 58
      Top = 0
      Grouped = True
      ImageIndex = 3
      Style = tbsCheck
      OnClick = ZoomButtonClick
    end
    object MoveButton: TToolButton
      Tag = 2
      Left = 83
      Top = 0
      Grouped = True
      ImageIndex = 4
      Style = tbsCheck
      OnClick = ZoomButtonClick
    end
    object LineButton: TToolButton
      Tag = 3
      Left = 108
      Top = 0
      Grouped = True
      ImageIndex = 8
      Style = tbsCheck
      OnClick = ZoomButtonClick
    end
    object PolLineButton: TToolButton
      Tag = 4
      Left = 133
      Top = 0
      Grouped = True
      ImageIndex = 9
      Style = tbsCheck
      OnClick = ZoomButtonClick
    end
    object PolGonButton: TToolButton
      Tag = 5
      Left = 158
      Top = 0
      Grouped = True
      ImageIndex = 10
      Style = tbsCheck
      OnClick = ZoomButtonClick
    end
    object ToolButton6: TToolButton
      Left = 183
      Top = 0
      Width = 8
      ImageIndex = 8
      Style = tbsSeparator
    end
    object AutoButton: TToolButton
      Tag = 6
      Left = 191
      Top = 0
      ImageIndex = 4
      OnClick = AutoButtonClick
    end
    object ToolButton3: TToolButton
      Left = 216
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object CloseButton: TToolButton
      Left = 224
      Top = 0
      ImageIndex = 6
      OnClick = CloseButtonClick
    end
  end
end
