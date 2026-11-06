using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Стартовые значения молота: то, с чего начинается игра, до апгрейдов и бонусов машины.
    /// Итог считает StatService: значение апгрейда плюс бонус машины; без апгрейда берётся база отсюда.
    /// </summary>
    [CreateAssetMenu(fileName = "HammerConfig", menuName = "CubeSmasher/Hammer Config")]
    public class HammerConfig : ScriptableObject
    {
        [Header("Урон")]
        [Tooltip("Урон одного удара без апгрейдов")]
        public float BaseDamage = 10f;

        [Header("Радиус удара")]
        [Tooltip("Радиус, в котором молот достаёт детали")]
        public float BaseHitRadius = 1.5f;
        public float MinHitRadius = 0.5f;
        public float MaxHitRadius = 6f;

        [Header("Скорость удара")]
        [Tooltip("1 = обычная скорость. Больше 1 — молот бьёт быстрее: длительности фаз делятся на это число")]
        public float BaseAttackSpeed = 1f;

        [Header("Крит")]
        [Tooltip("Шанс крита, от 0 до 1. 0.1 = 10%")]
        [Range(0f, 1f)] public float BaseCritChance = 0f;

        [Tooltip("Во сколько раз урон крита больше обычного")]
        public float BaseCritDamage = 2f;

        [Header("Цвета всплывающих цифр")]
        public Color NormalDamageColor = Color.white;
        public Color CritDamageColor = Color.red;
    }
}
