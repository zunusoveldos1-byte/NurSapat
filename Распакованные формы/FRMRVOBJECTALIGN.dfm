inherited frmRVObjectAlign: TfrmRVObjectAlign
  Left = 277
  Top = 169
  BorderStyle = bsDialog
  Caption = 'Position'
  ClientHeight = 388
  ClientWidth = 593
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object rg: TRVOfficeRadioGroup
    Left = 7
    Top = 13
    Width = 578
    Height = 323
    ItemIndex = 0
    Columns = 4
    Items = <
      item
        ImageIndex = 0
        Caption = 'bottom to base line of text'
      end
      item
        ImageIndex = 2
        Caption = 'top to line top'
      end
      item
        ImageIndex = 1
        Caption = 'middle to base line of text'
      end
      item
        ImageIndex = 4
        Caption = 'middle to line middle'
      end
      item
        ImageIndex = 5
        Caption = 'to left side'
      end
      item
        ImageIndex = 3
        Caption = 'bottom to line bottom'
      end
      item
        ImageIndex = 6
        Caption = 'to right side'
      end>
    Images = ImageList1
    Square = True
    Caption = 'Align'
    TabOrder = 0
  end
  object btnOk: TButton
    Left = 380
    Top = 354
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 1
  end
  object btnCancel: TButton
    Left = 483
    Top = 354
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 2
  end
  object ImageList1: TImageList
    Height = 89
    Width = 83
    Left = 498
    Top = 244
  end
end
