# -*- coding: utf-8 -*-
import re
from pathlib import Path

path = Path(__file__).resolve().parents[1] / "NurSapat.Cad.UI" / "Views" / "CadEditorView.axaml"
text = path.read_text(encoding="utf-8")

start = text.index('<Button Classes.cadTool="True"')
end = text.index("</Button>", start) + len("</Button>")
button_template = text[start:end]
button_template = button_template.replace(
    '                                                IsVisible="{Binding IsTool}"\n', ""
)
button_template = button_template.replace("\n                                        ", "\n            ")

tool_data_template = (
    '        <DataTemplate x:Key="CadToolbarToolTemplate" DataType="vm:CadDesignToolButtonViewModel">\n'
    + button_template
    + "\n        </DataTemplate>\n"
)

marker = '        <conv:EnumEqualsConverter x:Key="EnumEq"/>\n'
if "CadToolbarToolTemplate" not in text:
    text = text.replace(marker, marker + tool_data_template)

new_toolbar = """                    <StackPanel VerticalAlignment="Top" Spacing="0" Width="204">
                        <TextBlock Classes="cadToolbarBlockHeader" Text="СОЗДАНИЕ"/>
                        <ItemsControl ItemsSource="{Binding CreationDesignTools}"
                                      ItemTemplate="{StaticResource CadToolbarToolTemplate}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <UniformGrid Columns="4"/>
                                </ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                        </ItemsControl>
                        <Border Classes="cadToolbarBlockSpacer">
                            <Border Height="1"
                                    Background="#E2E8F0"
                                    VerticalAlignment="Center"/>
                        </Border>
                        <TextBlock Classes="cadToolbarBlockHeader" Text="РЕДАКТИРОВАНИЕ"/>
                        <ItemsControl ItemsSource="{Binding EditingDesignTools}"
                                      ItemTemplate="{StaticResource CadToolbarToolTemplate}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <UniformGrid Columns="4"/>
                                </ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                        </ItemsControl>
"""

pattern = (
    r'                    <StackPanel VerticalAlignment="Top" Spacing="0">\s*'
    r'<ItemsControl ItemsSource="\{Binding DesignTools\}".*?</ItemsControl>\n'
)
text2, n = re.subn(pattern, new_toolbar, text, count=1, flags=re.DOTALL)
if n != 1:
    raise SystemExit(f"Replace failed: {n}")

text = text2
text = text.replace('ColumnDefinitions="196,*"', 'ColumnDefinitions="212,*"')
text = text.replace('ColumnDefinitions="180,*" Height="30"', 'ColumnDefinitions="212,*" Height="30"')
text = text.replace('Property="Margin" Value="3.5"/>', 'Property="Margin" Value="3"/>')

path.write_text(text, encoding="utf-8")
print("OK")
