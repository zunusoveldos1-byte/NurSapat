object ProizvodListDlg: TProizvodListDlg
  Left = 415
  Top = 237
  BorderStyle = bsDialog
  Caption = 'Производители'
  ClientHeight = 438
  ClientWidth = 394
  Color = clBtnFace
  ParentFont = True
  OldCreateOrder = True
  Position = poScreenCenter
  OnCreate = FormCreate
  OnHide = FormHide
  OnShow = FormShow
  PixelsPerInch = 96
  TextHeight = 13
  object ToolBar1: TToolBar
    Left = 0
    Top = 0
    Width = 77
    Height = 37
    Align = alNone
    BorderWidth = 1
    ButtonHeight = 30
    ButtonWidth = 31
    EdgeBorders = [ebLeft, ebTop, ebRight, ebBottom]
    EdgeInner = esNone
    Images = SahredDMod.ButList24
    TabOrder = 0
    object ToolButton3: TToolButton
      Left = 0
      Top = 0
      Action = ProizvodEdit
      ParentShowHint = False
      ShowHint = True
    end
    object ToolButton4: TToolButton
      Left = 31
      Top = 0
      Width = 8
      ImageIndex = 7
      Style = tbsSeparator
    end
    object ToolButton6: TToolButton
      Left = 39
      Top = 0
      Action = ProizvodClose
      ParentShowHint = False
      ShowHint = True
    end
  end
  object LView: TADGListView
    Left = 0
    Top = 40
    Width = 393
    Height = 397
    Columns = <>
    ReadOnly = False
    TabOrder = 1
    OnDblClick = ProizvodEditExecute
  end
  object ActionList1: TActionList
    Images = SahredDMod.ButList24
    Left = 204
    Top = 4
    object ProizvodEdit: TAction
      Caption = 'Редактировать производителя'
      Hint = 'Редактировать производителя'
      ImageIndex = 7
      OnExecute = ProizvodEditExecute
    end
    object ProizvodClose: TAction
      Caption = 'Закрыть'
      Hint = 'Закрыть'
      ImageIndex = 22
      OnExecute = ProizvodCloseExecute
    end
  end
end
