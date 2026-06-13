object ZakazPloplataListDlg: TZakazPloplataListDlg
  Left = 227
  Top = 108
  Caption = 'Оплаты'
  ClientHeight = 276
  ClientWidth = 385
  Color = clBtnFace
  Constraints.MinHeight = 100
  Constraints.MinWidth = 300
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  ShowHint = True
  OnCreate = FormCreate
  OnDestroy = FormDestroy
  OnHide = FormHide
  OnResize = FormResize
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object LView: TADGListView
    Left = 2
    Top = 36
    Width = 381
    Height = 237
    Columns = <>
    ReadOnly = False
    TabOrder = 0
    OnDblClick = OplataEditExecute
  end
  object ToolBar1: TToolBar
    Left = 2
    Top = 0
    Width = 146
    Height = 36
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 30
    ButtonWidth = 31
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList24
    TabOrder = 1
    object ToolButton1: TToolButton
      Left = 0
      Top = 0
      Action = OplataAdd
    end
    object ToolButton3: TToolButton
      Left = 31
      Top = 0
      Action = OplataEdit
    end
    object ToolButton4: TToolButton
      Left = 62
      Top = 0
      Width = 8
      ImageIndex = 2
      Style = tbsSeparator
    end
    object ToolButton2: TToolButton
      Left = 70
      Top = 0
      Action = OplataDel
    end
    object ToolButton5: TToolButton
      Left = 101
      Top = 0
      Width = 8
      ImageIndex = 2
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 109
      Top = 0
      Action = OplataExit
    end
  end
  object LabeledEdit1: TLabeledEdit
    Left = 220
    Top = 8
    Width = 65
    Height = 21
    EditLabel.Width = 31
    EditLabel.Height = 13
    EditLabel.Caption = 'Сумма'
    LabelPosition = lpLeft
    ReadOnly = True
    TabOrder = 2
  end
  object Edit1: TEdit
    Left = 288
    Top = 8
    Width = 57
    Height = 21
    ReadOnly = True
    TabOrder = 3
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 12
    Top = 40
    object OplataAdd: TAction
      Caption = 'OplataAdd'
      Hint = 'Добавить оплату'
      ImageIndex = 0
      OnExecute = OplataAddExecute
    end
    object OplataEdit: TAction
      Caption = 'OplataEdit'
      Hint = 'Редактировать оплату'
      ImageIndex = 7
      OnExecute = OplataEditExecute
    end
    object OplataDel: TAction
      Caption = 'OplataDel'
      Hint = 'Удалить оплату'
      ImageIndex = 5
      OnExecute = OplataDelExecute
    end
    object OplataExit: TAction
      Caption = 'OplataExit'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = OplataExitExecute
    end
  end
end
