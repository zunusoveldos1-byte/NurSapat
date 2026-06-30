inherited frmRVListGallery2: TfrmRVListGallery2
  Left = 91
  Top = 138
  HelpContext = 91200
  ActiveControl = rg1
  BorderStyle = bsDialog
  Caption = 'Bullets and Numbering'
  ClientHeight = 409
  ClientWidth = 491
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object lblLevel: TLabel
    Left = 22
    Top = 377
    Width = 29
    Height = 13
    Anchors = [akLeft, akBottom]
    Caption = '&Level:'
    FocusControl = seLevel
  end
  object pc: TPageControl
    Left = 9
    Top = 6
    Width = 472
    Height = 357
    ActivePage = tsBullets
    TabOrder = 0
    object tsBullets: TTabSheet
      Caption = 'Bullets'
      object rg1: TRVOfficeRadioGroup
        Left = 7
        Top = 5
        Width = 327
        Height = 311
        ItemIndex = 0
        Columns = 3
        Items = <
          item
            ImageIndex = 0
            Caption = 'none'
          end
          item
            ImageIndex = 1
            Caption = 'disc'
          end
          item
            ImageIndex = 2
            Caption = 'circle'
          end
          item
            ImageIndex = 3
            Caption = 'square'
          end>
        Images = il1
        OnDblClickItem = rg2DblClickItem
        Caption = 'List Type'
        TabOrder = 0
      end
    end
    object tsNumbering: TTabSheet
      Caption = 'Numbering'
      ImageIndex = 1
      object gbNumbering: TGroupBox
        Left = 338
        Top = 5
        Width = 114
        Height = 311
        Caption = ' Numbering '
        TabOrder = 1
        object seStartFrom: TRVSpinEdit
          Left = 39
          Top = 70
          Width = 57
          Height = 22
          Increment = 1.000000000000000000
          MaxValue = 1000.000000000000000000
          Value = 1.000000000000000000
          TabOrder = 2
          OnChange = seStartFromChange
        end
        object rbContinue: TRadioButton
          Left = 10
          Top = 21
          Width = 87
          Height = 17
          Caption = '&Continue'
          Checked = True
          TabOrder = 0
          TabStop = True
        end
        object rbStartFrom: TRadioButton
          Left = 10
          Top = 49
          Width = 87
          Height = 17
          Caption = '&Start from:'
          TabOrder = 1
        end
      end
      object rg2: TRVOfficeRadioGroup
        Left = 7
        Top = 5
        Width = 327
        Height = 311
        ItemIndex = 0
        Columns = 3
        Items = <
          item
            ImageIndex = 0
            Caption = 'none'
          end
          item
            ImageIndex = 4
            Caption = 'decimal'
          end
          item
            ImageIndex = 5
            Caption = 'upper alpha'
          end
          item
            ImageIndex = 6
            Caption = 'lower alpha'
          end
          item
            ImageIndex = 7
            Caption = 'upper roman'
          end
          item
            ImageIndex = 8
            Caption = 'lower roman'
          end>
        Images = il1
        OnDblClickItem = rg2DblClickItem
        Caption = 'List Type'
        TabOrder = 0
      end
    end
  end
  object seLevel: TRVSpinEdit
    Left = 115
    Top = 372
    Width = 57
    Height = 22
    Increment = 1.000000000000000000
    MaxValue = 10.000000000000000000
    Value = 1.000000000000000000
    Anchors = [akLeft, akBottom]
    TabOrder = 1
  end
  object btnOk: TButton
    Left = 280
    Top = 374
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 2
  end
  object btnCancel: TButton
    Left = 383
    Top = 374
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 3
  end
  object il1: TImageList
    Height = 98
    Width = 90
    Left = 161
    Top = 109
  end
end
