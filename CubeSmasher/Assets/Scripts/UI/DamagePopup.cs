using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro _textMesh;
    [SerializeField] private float _floatSpeed = 3f;
    [SerializeField] private float _lifetime = 1f;

    public void Setup(float damageAmount)
    {
        _textMesh.text = damageAmount.ToString("F0"); // F0 округляет до целого числа
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        float timer = 0;
        Color startColor = _textMesh.color;

        while (timer < 1f)
        {
            timer += Time.deltaTime / _lifetime;

            // Текст летит вверх
            transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

            // Текст плавно становится прозрачным
            startColor.a = 1f - timer;
            _textMesh.color = startColor;

            // Заставляем текст смотреть прямо в камеру
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

            yield return null;
        }

        Destroy(gameObject); // Уничтожаем объект после анимации
    }
}