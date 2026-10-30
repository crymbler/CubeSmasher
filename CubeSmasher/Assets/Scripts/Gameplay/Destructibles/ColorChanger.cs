using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ColorChanger : MonoBehaviour
{
    private Material _material;

    private void Awake()
    {
        // Кэшируем материал один раз, чтобы не дергать GetComponent при каждом спавне
        _material = GetComponent<Renderer>().material;
    }

    public void ApplyAcidColor()
    {
        // Если выбран скин, он задаёт свой диапазон оттенков (см. SkinPalette).
        // Иначе остаётся прежняя кислотная палитра из GDD.
        CubeSmasher.Gameplay.SkinPalette? skin = CubeSmasher.Gameplay.SkinPaletteState.Current;
        if (skin.HasValue)
        {
            _material.color = skin.Value.Sample();
            return;
        }

        float randomHue = Random.Range(0f, 1f);
        float fixedSaturation = Random.Range(0.8f, 1.0f);
        float fixedValue = Random.Range(0.85f, 1.0f);

        _material.color = Color.HSVToRGB(randomHue, fixedSaturation, fixedValue);
    }
}