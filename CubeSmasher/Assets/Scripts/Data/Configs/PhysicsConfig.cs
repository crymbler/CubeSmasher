using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Физика деталей: какие модели спавнить, как часто, насколько крупные
    /// и как они раскалываются на осколки.
    /// </summary>
    [CreateAssetMenu(fileName = "PhysicsConfig", menuName = "CubeSmasher/Physics Config")]
    public class PhysicsConfig : ScriptableObject
    {
        [Header("Модели деталей")]
        [Tooltip("Перетащите сюда все модели деталей (Mesh)")]
        public CarPart[] PartPrefabs;

        [Header("Спавн")]
        [Tooltip("Радиус разброса деталей относительно центра стола")]
        public float SpawnRadius = 4f;

        [Tooltip("Сколько деталей появляется на столе одновременно")]
        public int StartPartsPerWave = 1;

        [Tooltip("Размер детали поколения 0 (Gen 0)")]
        public float InitialPartScale = 1f;

        [Header("Пул объектов (лимит для WebGL)")]
        [Tooltip("Сколько объектов создаётся заранее")]
        public int PoolCapacity = 250;

        [Header("Раскол деталей")]
        [Tooltip("Шанс раскола. Поколение 0 делится всегда")]
        [Range(0f, 1f)]
        public float BaseSplitChance = 0.85f;

        [Tooltip("Во сколько раз уменьшается осколок (0.5 = в два раза)")]
        public float SplitScaleMultiplier = 0.5f;

        [Tooltip("Максимальное количество поколений деталей (2 = 3 поколения)")]
        public int MaxGenerations = 3;

        // Прочность считает FractureCalculator по своим константам,
        // поэтому поля ниже пока не читаются кодом — оставлены для баланса.

        [Header("Прочность деталей (пока не используется)")]
        public float BasePartHP = 20f;
        public float PartHPMultiplier = 1.45f;
    }
}
