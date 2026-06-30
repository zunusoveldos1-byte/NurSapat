object StateDlg: TStateDlg
  Left = 561
  Top = 429
  BorderStyle = bsNone
  Caption = 'Состояние'
  ClientHeight = 37
  ClientWidth = 285
  Color = 8454143
  ParentFont = True
  FormStyle = fsStayOnTop
  OldCreateOrder = True
  OnCreate = FormCreate
  PixelsPerInch = 96
  TextHeight = 13
  object Bevel1: TBevel
    Left = 0
    Top = 0
    Width = 285
    Height = 37
    Align = alClient
    Style = bsRaised
    ExplicitHeight = 95
  end
  object Label3: TLabel
    Left = 201
    Top = 4
    Width = 28
    Height = 13
    AutoSize = False
    Transparent = True
  end
  object CancelBtn: TSpeedButton
    Left = 232
    Top = 4
    Width = 49
    Height = 29
    Caption = 'Отмена'
    OnClick = CancelBtnClick
    OnMouseEnter = CancelBtnMouseEnter
    OnMouseLeave = CancelBtnMouseLeave
  end
  object Timer1: TTimer
    Enabled = False
    Interval = 500
    OnTimer = Timer1Timer
    Left = 60
    Top = 4
  end
end
