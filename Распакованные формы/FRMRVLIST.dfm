inherited frmRVList: TfrmRVList
  Left = 247
  Top = 230
  BorderStyle = bsDialog
  Caption = 'frmRVList'
  ClientHeight = 319
  ClientWidth = 345
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object lbl: TLabel
    Left = 10
    Top = 8
    Width = 10
    Height = 13
    Caption = 'lbl'
    FocusControl = lst
  end
  object btnOk: TButton
    Left = 133
    Top = 284
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 1
  end
  object btnCancel: TButton
    Left = 237
    Top = 284
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 2
  end
  object lst: TListBox
    Left = 10
    Top = 28
    Width = 326
    Height = 246
    Anchors = [akLeft, akTop, akRight, akBottom]
    ItemHeight = 13
    TabOrder = 0
    OnDblClick = lstDblClick
  end
end
