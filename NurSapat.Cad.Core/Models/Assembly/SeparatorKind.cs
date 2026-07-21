namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Роль разделителя в дереве FrameNode.
/// Impost — обычный импост внутри подрамы.
/// Transition — переходной профиль между независимыми подрамами.
/// </summary>
public enum SeparatorKind
{
    None,
    Impost,
    Transition
}
