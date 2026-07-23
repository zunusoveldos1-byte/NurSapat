namespace NurSapat.Core.Models.Construction;

/// <summary>
/// Тип обработки торца профиля при стыковке.
/// </summary>
public enum EndCutType
{
    /// <summary>Угол 45° (сварка, сухари) — прямоугольные рамы и створки.</summary>
    Mitre45,

    /// <summary>Торцевой бесшовный стык 0°/180° — гнутые кольцевые профили.</summary>
    ButtJoint0,

    /// <summary>Торцевая фрезеровка под контур рамы — импосты.</summary>
    ImpostMilling
}
