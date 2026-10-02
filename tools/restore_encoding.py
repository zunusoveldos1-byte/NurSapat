# -*- coding: utf-8 -*-
"""Restore MainWorkspaceWindow.axaml and fix encoding across the repo."""
from __future__ import annotations

import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

ORDER_MENU = """
                <MenuItem Header="Заказ">
                    <MenuItem Header="Заказчик" Command="{Binding SelectOrderCustomerCommand}"/>
                    <MenuItem Header="Дилер" Command="{Binding SelectOrderDealerCommand}"/>
                    <MenuItem Header="Режим цены" Command="{Binding TogglePriceModeCommand}"/>
                    <Separator/>
                    <MenuItem Header="Удалить конструкцию" Command="{Binding RemoveCurrentConstructionCommand}"/>
                </MenuItem>
"""

EDIT_MENU = """
                <MenuItem Header="{DynamicResource Edit_Menu_Edit}">
                    <MenuItem Header="{DynamicResource Edit_Menu_CopyDrawing}">
                        <MenuItem Header="{DynamicResource Edit_Menu_CopyWithoutDimensions}"
                                  Command="{Binding OpenCopyDrawingWithoutDimensionsCommand}"/>
                        <MenuItem Header="{DynamicResource Edit_Menu_CopyWithDimensions}"
                                  Command="{Binding OpenCopyDrawingWithDimensionsCommand}"/>
                    </MenuItem>
                    <Separator/>
                    <MenuItem Header="{DynamicResource Edit_Menu_ChangeConstructionColor}"
                              Command="{Binding OpenConstructionColorDialogCommand}"/>
                    <MenuItem Header="{DynamicResource Edit_Menu_ChangeOrderColor}"
                              Command="{Binding OpenOrderColorDialogCommand}"/>
                </MenuItem>
"""

VIEW_MENU = """
                <MenuItem Header="Вид">
                    <MenuItem Header="Таблица затрат" Command="{Binding CostTableCommand}"/>
                    <MenuItem Header="Сообщения" Command="{Binding MessagesCommand}"/>
                </MenuItem>
"""

DICT_EXTRA = """
                    <MenuItem Header="Графические обозначения" Command="{Binding GraphicSymbolsCommand}"/>
                    <MenuItem Header="Цвета" Command="{Binding ColorsCommand}"/>
"""


def restore_main_workspace() -> None:
    src = ROOT / "tools" / "mainworkspace_d2d81e9.axaml"
    text = src.read_text(encoding="utf-8")

    text = text.replace(
        '<Grid RowDefinitions="40,Auto,Auto,*">',
        '<Grid RowDefinitions="40,Auto,28,Auto,*">',
    )

  # Replace old Edit menu block
    text = re.sub(
        r'<MenuItem Header="Правка">.*?</MenuItem>\s*\n\s*<MenuItem Header="Вид">.*?</MenuItem>',
        EDIT_MENU.strip() + "\n\n" + VIEW_MENU.strip(),
        text,
        count=1,
        flags=re.DOTALL,
    )

    # Insert Order menu after File menu
    text = text.replace(
        "                </MenuItem>\n\n                <MenuItem Header=\"{DynamicResource Edit_Menu_Edit}\">",
        "                </MenuItem>\n\n" + ORDER_MENU.strip() + "\n\n                <MenuItem Header=\"{DynamicResource Edit_Menu_Edit}\">",
    )

    # Add dictionary items before Settings menu
    text = text.replace(
        '                    <MenuItem Header="Наименования конструкций" Command="{Binding StructureNamesCommand}"/>\n                </MenuItem>',
        '                    <MenuItem Header="Наименования конструкций" Command="{Binding StructureNamesCommand}"/>'
        + DICT_EXTRA
        + "\n                </MenuItem>",
    )

    # Status row + move tabs/content down
    status_row = """
        <Border Grid.Row="2"
                Background="#F8FAFC"
                BorderBrush="#E2E8F0"
                BorderThickness="0,0,0,1"
                Padding="16,0">
            <TextBlock Text="{Binding ConfigurationStatusText}"
                       FontSize="12"
                       Foreground="#64748B"
                       VerticalAlignment="Center"/>
        </Border>
"""
    text = text.replace(
        '        <!-- Вкладки -->\n        <Border Grid.Row="2"',
        status_row + '\n        <!-- Вкладки -->\n        <Border Grid.Row="3"',
    )
    text = text.replace(
        '        <!-- Рабочая область -->\n        <Border Grid.Row="3"',
        '        <!-- Рабочая область -->\n        <Border Grid.Row="4"',
    )

    text = text.replace('FontFamily="Inter"', 'FontFamily="Inter, Segoe UI"')

    target = ROOT / "NurSapat.App" / "Views" / "MainWorkspaceWindow.axaml"
    target.write_text(text, encoding="utf-8")
    print("restored", target)


def cp1251_to_utf8(rel: str) -> None:
    path = ROOT / rel
    text = path.read_bytes().decode("cp1251")
    path.write_text(text, encoding="utf-8")
    print("converted cp1251", rel)


def restore_from_git(rel: str, commit: str = "d2d81e9") -> bool:
    try:
        data = subprocess.check_output(["git", "-C", str(ROOT), "show", f"{commit}:{rel}"])
        text = data.decode("utf-8")
        (ROOT / rel).write_text(text, encoding="utf-8")
        print("restored from git", rel)
        return True
    except Exception as exc:
        print("git restore failed", rel, exc)
        return False


def fix_dialog_cs_files() -> None:
    mapping = {
        "NurSapat.App/Views/Dialogs/AutoSaveRecoveryWindow.axaml.cs": None,
        "NurSapat.App/Views/Dialogs/PrintDialogWindow.axaml.cs": None,
        "NurSapat.App/Views/Dialogs/PrintSettingsWindow.axaml.cs": None,
        "NurSapat.App/Views/Dialogs/UnsavedChangesWindow.axaml.cs": None,
        "NurSapat.App/Views/LoginView.axaml.cs": None,
        "NurSapat.Cad.UI/Dialogs/TemplatePickerWindow.axaml.cs": None,
    }
    for rel in mapping:
        if not restore_from_git(rel, "d2d81e9"):
            restore_from_git(rel, "602bfae")


def fix_materials_view() -> None:
    path = ROOT / "NurSapat.App" / "Views" / "MaterialsView.axaml"
    text = path.read_bytes().decode("cp1251")
    text = text.replace("Добавить", "Добавить").replace("Удалить", "Удалить")
    # fix common button labels if still broken
    text = text.replace('Content="+ "', 'Content="+ Добавить"')
    text = text.replace('Content="?? "', 'Content="🗑 Удалить"')
    text = re.sub(r'Content="\+\s*[^"]*"', 'Content="+ Добавить"', text, count=1)
    text = re.sub(r'Content="[^"]*Удалить[^"]*"', 'Content="Удалить"', text)
    if "" in text or "\ufffd" in text:
        text = text.replace('Content="+  "', 'Content="+ Добавить"')
        text = re.sub(
            r'<Button Command="\{Binding AddMaterialCommand\}" Content="[^"]*"\s*/>',
            '<Button Command="{Binding AddMaterialCommand}" Content="+ Добавить" />',
            text,
        )
        text = re.sub(
            r'<Button Command="\{Binding DeleteSelectedCommand\}" Content="[^"]*"\s*/>',
            '<Button Command="{Binding DeleteSelectedCommand}" Content="Удалить" />',
            text,
        )
    path.write_text(text, encoding="utf-8")
    print("fixed", path)


def main() -> None:
    restore_main_workspace()
    for rel in [
        "NurSapat.App/App.axaml.cs",
        "NurSapat.Cad.Core/Snapping/CadDocumentSnapProvider.cs",
        "NurSapat.Cad.Core/Viewport/CadDocumentBoundsProvider.cs",
        "NurSapat.Cad.UI/Dialogs/ImpostCountWindow.axaml.cs",
    ]:
        cp1251_to_utf8(rel)
    fix_materials_view()
    fix_dialog_cs_files()

    # write .editorconfig
    editorconfig = ROOT / ".editorconfig"
    if not editorconfig.exists():
        editorconfig.write_text(
            "root = true\n\n[*]\ncharset = utf-8\nend_of_line = crlf\ninsert_final_newline = true\n",
            encoding="utf-8",
        )
        print("created .editorconfig")


if __name__ == "__main__":
    main()
