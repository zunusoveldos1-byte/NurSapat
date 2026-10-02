object ColInfoListDlg: TColInfoListDlg
  Left = 227
  Top = 108
  BorderStyle = bsDialog
  Caption = 'Редактор колонок'
  ClientHeight = 326
  ClientWidth = 595
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
  object OKBtn: TButton
    Left = 516
    Top = 4
    Width = 75
    Height = 25
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 0
  end
  object CancelBtn: TButton
    Left = 516
    Top = 32
    Width = 75
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
  object LView: TADGListView
    Left = 0
    Top = 44
    Width = 505
    Height = 277
    Columns = <>
    ReadOnly = False
    TabOrder = 2
    OnDblClick = TableColEditExecute
  end
  object ToolBar2: TToolBar
    Left = 4
    Top = 4
    Width = 177
    Height = 36
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 30
    ButtonWidth = 31
    Color = clBtnFace
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList24
    ParentColor = False
    TabOrder = 3
    object ToolButton5: TToolButton
      Left = 0
      Top = 0
      Action = TableColAdd
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton6: TToolButton
      Left = 31
      Top = 0
      Action = TableColEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton3: TToolButton
      Left = 62
      Top = 0
      Width = 8
      Caption = 'ToolButton3'
      ImageIndex = 8
      Style = tbsSeparator
    end
    object ToolButton1: TToolButton
      Left = 70
      Top = 0
      Action = TableColUp
    end
    object ToolButton2: TToolButton
      Left = 101
      Top = 0
      Action = TableColDown
    end
    object ToolButton7: TToolButton
      Left = 132
      Top = 0
      Width = 8
      ImageIndex = 2
      Style = tbsSeparator
    end
    object ToolButton8: TToolButton
      Left = 140
      Top = 0
      Action = TableColDel
      ParentShowHint = False
      ShowHint = True
    end
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 524
    Top = 68
    object TableColAdd: TAction
      Caption = 'Добавить колонку'
      Hint = 'Добавить колонку'
      ImageIndex = 0
      OnExecute = TableColAddExecute
    end
    object TableColEdit: TAction
      Caption = 'Редактировать колонку'
      Hint = 'Редактировать колонку'
      ImageIndex = 7
      OnExecute = TableColEditExecute
    end
    object TableColDel: TAction
      Caption = 'Удалить колонку'
      Hint = 'Удалить колонку'
      ImageIndex = 5
      OnExecute = TableColDelExecute
    end
    object TableColUp: TAction
      Caption = 'Поднять вверх'
      Hint = 'Поднять вверх'
      ImageIndex = 17
      OnExecute = TableColUpExecute
    end
    object TableColDown: TAction
      Caption = 'Опустить вниз'
      Hint = 'Опустить вниз'
      ImageIndex = 6
      OnExecute = TableColUpExecute
    end
  end
end
