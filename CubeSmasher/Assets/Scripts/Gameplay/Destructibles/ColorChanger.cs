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
        // Генерация "леденцовых" цветов по GDD
        float randomHue = Random.Range(0f, 1f);
        float fixedSaturation = Random.Range(0.8f, 1.0f);
        float fixedValue = Random.Range(0.85f, 1.0f);

        _material.color = Color.HSVToRGB(randomHue, fixedSaturation, fixedValue);
    }
}