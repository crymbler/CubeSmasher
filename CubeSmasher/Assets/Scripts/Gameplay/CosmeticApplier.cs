using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using CubeSmasher.Infrastructure;
using UnityEngine;

namespace CubeSmasher.Gameplay
{
    /// <summary>
    /// Показывает надетый предмет группы: молот на SwingPivot, тарелку на Dish.
    ///
    /// Молоты — разные префабы, поэтому ставится модель. Тарелки отличаются
    /// материалом, поэтому если у предмета нет ModelPrefab, меняется материал
    /// у существующей модели, а меш остаётся.
    ///
    /// На сцене у SwingPivot уже лежат три молота (два выключены). Скрипт
    /// не удаляет их, а только включает нужный и выключает остальные:
    /// удаление сломало бы ссылки и потребовало бы Instantiate каждый раз.
    /// Модель, которой на сцене нет, создаётся из префаба.
    /// </summary>
    public class CosmeticApplier : MonoBehaviour
    {
        [Tooltip("Группа из CosmeticConfig: hammer или dish")]
        [SerializeField] private string _groupId = "hammer";

        [Tooltip("Куда ставить модель, если префаба нет среди детей. Пусто — этот объект")]
        [SerializeField] private Transform _anchor;

        [Tooltip("Модель, которая показывает предмет по умолчанию. Если пусто — берётся первый ребёнок")]
        [SerializeField] private GameObject _defaultModel;

        private readonly Dictionary<string, GameObject> _models = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, Material> _appliedMaterials = new Dictionary<string, Material>();

        private ICosmeticService _service;
        private string _currentId = string.Empty;

        private Transform Anchor => _anchor != null ? _anchor : transform;

        private void Start()
        {
            if (!ServiceLocator.Instance.TryGet(out ICosmeticService service))
            {
                // Без конфига косметики сцена работает как раньше: всё, что лежит
                // на объекте, остаётся как есть.
                return;
            }

            _service = service;
            _service.Equipped += HandleEquipped;

            IndexExistingModels();
            ApplyCurrent();
        }

        private void OnDestroy()
        {
            if (_service != null) _service.Equipped -= HandleEquipped;
        }

        private void HandleEquipped(string groupId)
        {
            if (groupId == _groupId) ApplyCurrent();
        }

        /// <summary>Собирает модели, уже лежащие под якорем, по имени префаба.</summary>
        private void IndexExistingModels()
        {
            CosmeticGroup[] groups = _service.Groups;
            int count = 0;

            for (int g = 0; g < groups.Length; g++)
            {
                if (groups[g].Id != _groupId || groups[g].Items == null) continue;

                CosmeticItem[] items = groups[g].Items;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].ModelPrefab == null) continue;

                    // Имя объекта на сцене совпадает с именем префаба: Model_0, Model_1...
                    string prefabName = items[i].ModelPrefab.name;
                    Transform found = FindDeep(Anchor, prefabName);

                    if (found != null)
                    {
                        _models[items[i].Id] = found.gameObject;
                        count++;
                    }
                }
            }

            if (count > 0) return;

            // На сцене моделей нет: берём первого ребёнка как модель по умолчанию
            if (_defaultModel == null && Anchor.childCount > 0)
            {
                _defaultModel = Anchor.GetChild(0).gameObject;
            }
        }

        private void ApplyCurrent()
        {
            string id = _service.GetEquipped(_groupId);
            if (string.IsNullOrEmpty(id)) id = FindDefaultId();

            if (id == _currentId) return;
            _currentId = id;

            CosmeticItem? item = FindItem(id);
            if (!item.HasValue) return;

            // 1. Режим материала: меш не меняется, меняется только цвет
            if (item.Value.ModelPrefab == null && item.Value.Material != null)
            {
                ApplyMaterial(item.Value);
                ShowModel(null);
                return;
            }

            // 2. Режим модели: включаем нужную, остальные выключаем
            GameObject model = GetOrCreateModel(item.Value);
            ShowModel(model);

            // Модель может нести свой материал, но по конфигу он не задан:
            // тогда цвет не трогаем, модель уже правильная.
            if (item.Value.Material != null) ApplyMaterialTo(model, item.Value.Material);
        }

        /// <summary>Включает одну модель и выключает все остальные под якорем.</summary>
        private void ShowModel(GameObject selected)
        {
            foreach (KeyValuePair<string, GameObject> pair in _models)
            {
                if (pair.Value != null) pair.Value.SetActive(pair.Value == selected);
            }

            if (_defaultModel != null && _defaultModel != selected)
            {
                _defaultModel.SetActive(false);
            }

            if (selected != null) selected.SetActive(true);
        }

        private GameObject GetOrCreateModel(CosmeticItem item)
        {
            if (_models.TryGetValue(item.Id, out GameObject existing) && existing != null)
            {
                return existing;
            }

            if (item.ModelPrefab == null) return _defaultModel;

            GameObject created = Instantiate(item.ModelPrefab, Anchor);
            created.name = item.ModelPrefab.name;
            _models[item.Id] = created;
            return created;
        }

        private void ApplyMaterial(CosmeticItem item)
        {
            GameObject target = _defaultModel;
            if (target == null && Anchor.childCount > 0) target = Anchor.GetChild(0).gameObject;
            if (target == null) return;

            ApplyMaterialTo(target, item.Material);
        }

        private static void ApplyMaterialTo(GameObject model, Material material)
        {
            if (model == null || material == null) return;

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }

        private string FindDefaultId()
        {
            CosmeticGroup[] groups = _service.Groups;

            for (int g = 0; g < groups.Length; g++)
            {
                if (groups[g].Id != _groupId || groups[g].Items == null) continue;

                for (int i = 0; i < groups[g].Items.Length; i++)
                {
                    if (groups[g].Items[i].IsDefault) return groups[g].Items[i].Id;
                }
            }
            return string.Empty;
        }

        private CosmeticItem? FindItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            CosmeticGroup[] groups = _service.Groups;
            for (int g = 0; g < groups.Length; g++)
            {
                if (groups[g].Id != _groupId || groups[g].Items == null) continue;

                for (int i = 0; i < groups[g].Items.Length; i++)
                {
                    if (groups[g].Items[i].Id == id) return groups[g].Items[i];
                }
            }
            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }
    }
}
