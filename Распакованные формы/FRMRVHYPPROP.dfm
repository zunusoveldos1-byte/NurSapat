inherited frmRVHypProp: TfrmRVHypProp
  Left = 251
  Top = 168
  HelpContext = 90450
  BorderStyle = bsDialog
  Caption = 'Hyperlink Attributes'
  ClientHeight = 306
  ClientWidth = 362
  OldCreateOrder = True
  Position = poScreenCenter
  PixelsPerInch = 96
  TextHeight = 13
  object gbNormal: TGroupBox
    Left = 12
    Top = 10
    Width = 337
    Height = 109
    Caption = 'Normal colors'
    TabOrder = 0
    object lblText: TLabel
      Left = 28
      Top = 33
      Width = 24
      Height = 13
      Caption = '&Text:'
      FocusControl = cmbText
    end
    object lblTextBack: TLabel
      Left = 28
      Top = 70
      Width = 61
      Height = 13
      Caption = '&Background:'
      FocusControl = cmbTextBack
    end
    object cmbText: TRVColorCombo
      Left = 152
      Top = 26
      Width = 156
      Height = 26
      AutoColor = clWindowText
      Indeterminate = True
      TabOrder = 0
      ParentColor = False
    end
    object cmbTextBack: TRVColorCombo
      Left = 152
      Top = 63
      Width = 156
      Height = 26
      Indeterminate = True
      TabOrder = 1
      ParentColor = False
    end
  end
  object gbHover: TGroupBox
    Left = 12
    Top = 124
    Width = 337
    Height = 137
    Caption = 'Active colors'
    TabOrder = 1
    object lblHoverText: TLabel
      Left = 28
      Top = 33
      Width = 24
      Height = 13
      Caption = 'T&ext:'
      FocusControl = cmbHoverText
    end
    object lblHoverTextBack: TLabel
      Left = 28
      Top = 70
      Width = 61
      Height = 13
      Caption = 'B&ackground:'
      FocusControl = cmbHoverTextBack
    end
    object cmbHoverText: TRVColorCombo
      Left = 152
      Top = 26
      Width = 156
      Height = 26
      AutoColor = clWindowText
      Indeterminate = True
      TabOrder = 0
      ParentColor = False
    end
    object cmbHoverTextBack: TRVColorCombo
      Left = 152
      Top = 63
      Width = 156
      Height = 26
      Indeterminate = True
      TabOrder = 1
      ParentColor = False
    end
    object cbAsNormal: TCheckBox
      Left = 28
      Top = 106
      Width = 238
      Height = 17
      Caption = '&Same as normal'
      TabOrder = 2
      OnClick = cbAsNormalClick
    end
  end
  object btnOk: TButton
    Left = 145
    Top = 269
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Caption = 'OK'
    Default = True
    ModalResult = 1
    TabOrder = 2
  end
  object btnCancel: TButton
    Left = 251
    Top = 269
    Width = 100
    Height = 25
    Anchors = [akRight, akBottom]
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 3
  end
end
