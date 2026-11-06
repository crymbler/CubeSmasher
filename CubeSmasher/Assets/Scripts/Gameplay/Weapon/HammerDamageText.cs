using UnityEngine;

/// <summary>
/// Всплывающие цифры урона: создаёт попап и красит его в цвет обычного или критического урона.
/// </summary>
public class HammerDamageText
{
    private readonly DamagePopup _prefab;

    /// <summary>Цвет обычного урона. Задаётся из HammerConfig.</summary>
    public Color NormalColor { get; set; } = Color.white;

    /// <summary>Цвет критического урона. Задаётся из HammerConfig.</summary>
    public Color CritColor { get; set; } = Color.red;

    public HammerDamageText(DamagePopup prefab)
    {
        _prefab = prefab;
    }

    public void Show(Vector3 worldPosition, float damage, bool isCrit)
    {
        if (_prefab == null) return;

        Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
        DamagePopup popup = Object.Instantiate(_prefab, worldPosition + offset, Quaternion.identity);
        popup.Setup(damage, isCrit ? CritColor : NormalColor);
    }
}
