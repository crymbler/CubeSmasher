using UnityEngine;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Скрывает HUD, пока открыто окно.
    ///
    /// Зачем отдельный класс: раньше каждое окно держало массив
    /// `_hideWhileOpen`, который надо было заполнять руками в каждом префабе.
    /// На практике он оставался пустым, и HUD просвечивал сквозь окна.
    ///
    /// Почему нельзя просто скрыть корень HUD: в `my hud.prefab` окна лежат
    /// ВНУТРИ того же объекта, что и HUD — `ShopWindow` и `GarageStrip` рядом
    /// с `TopHud` и `BottomNav`. Скрытие корня закрыло бы и само окно.
    /// Поэтому скрываются именно верхняя панель и нижнее меню.
    ///
    /// Счётчик не даёт одному окну показать HUD, пока открыто другое:
    /// магазин → скины → назад, HUD не мигает.
    /// </summary>
    public static class HudVisibility
    {
        /// <summary>Объекты, которые считаются HUD. Окна в этот список не входят.</summary>
        private static readonly string[] HudObjectNames = { "TopHud", "BottomNav" };

        private static int _openCount;
        private static GameObject[] _hudObjects;
        private static bool _searched;

        /// <summary>
        /// Скрыть или показать HUD для одного окна. Удобнее пары Push/Pop:
        /// окно не должно помнить, в каком состоянии оно было.
        /// </summary>
        public static void Toggle(bool hide)
        {
            if (hide) Push();
            else Pop();
        }

        /// <summary>Открыть окно: скрыть HUD. Парный вызов — <see cref="Pop"/>.</summary>
        public static void Push()
        {
            _openCount++;

            if (_openCount != 1) return; // HUD уже скрыт другим окном
            SetHudActive(false);
        }

        /// <summary>Закрыть окно: показать HUD, когда закрылись все.</summary>
        public static void Pop()
        {
            if (_openCount == 0) return;

            _openCount--;

            if (_openCount != 0) return;
            SetHudActive(true);
        }

        /// <summary>Сбросить состояние. Нужно при перезагрузке сцены.</summary>
        public static void Reset()
        {
            _openCount = 0;
            _hudObjects = null;
            _searched = false;
        }

        private static void SetHudActive(bool active)
        {
            GameObject[] hud = FindHudObjects();
            if (hud == null) return;

            for (int i = 0; i < hud.Length; i++)
            {
                if (hud[i] != null) hud[i].SetActive(active);
            }
        }

        /// <summary>
        /// Ищет верхнюю панель и нижнее меню по имени один раз за сессию.
        /// По имени, а не по ссылкам: набор объектов стабилен и задан префабом.
        /// </summary>
        private static GameObject[] FindHudObjects()
        {
            if (_searched) return _hudObjects;

            _searched = true;

            TopHudView view = Object.FindFirstObjectByType<TopHudView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogWarning("[HudVisibility] На сцене нет TopHudView: HUD не будет скрываться.");
                return null;
            }

            // Ищем от корня Canvas, чтобы не зацепить одноимённые объекты в окнах
            Transform canvasRoot = view.transform;
            while (canvasRoot.parent != null && canvasRoot.parent.GetComponent<Canvas>() == null)
            {
                canvasRoot = canvasRoot.parent;
            }

            var found = new System.Collections.Generic.List<GameObject>();

            foreach (string name in HudObjectNames)
            {
                Transform match = FindDeep(canvasRoot, name);
                if (match != null) found.Add(match.gameObject);
                else Debug.LogWarning("[HudVisibility] Не найден объект HUD: " + name);
            }

            _hudObjects = found.ToArray();
            return _hudObjects;
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
