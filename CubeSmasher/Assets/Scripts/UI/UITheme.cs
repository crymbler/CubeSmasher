using UnityEngine;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Палитра и метрики интерфейса. Светлая воксельная стилистика: почти белые панели,
    /// янтарный акцент, голубой прогресс. Тёмно-синий используется только для текста.
    /// Значения — из docs/PLAN_V2.md, раздел 1. Менять здесь, а не в префабах.
    /// </summary>
    public static class UITheme
    {
        // --- Фон и небо ---
        public static readonly Color SkyTop = C(0x8FD3FF);
        public static readonly Color SkyBottom = C(0xE8F6FF);

        // --- Панели ---
        public static readonly Color Panel = C(0xFFFFFF, 255);
        public static readonly Color PanelBorder = C(0x7FA8C9); // темнее: белая панель на светлом фоне иначе не читается
        public static readonly Color PanelShadow = C(0x8FB4D0, 46);

        // --- Акценты ---
        public static readonly Color Accent = C(0xFFB020);          // главная кнопка, покупка
        public static readonly Color AccentPressed = C(0xE69500);   // нажатое состояние
        public static readonly Color AccentLight = C(0xFFC55C);     // подсветка под курсором
        public static readonly Color AccentSecond = C(0x4FC3F7);    // прогресс, иконки, ссылки

        // --- Текст ---
        public static readonly Color TextPrimary = C(0x12395C);
        public static readonly Color TextSecondary = C(0x6E8CA8);
        public static readonly Color TextOnAccent = C(0x12395C);

        // --- Состояния ---
        public static readonly Color Positive = C(0x58C873);        // хватает денег
        public static readonly Color Negative = C(0xFF7A6B);        // не хватает
        public static readonly Color Muted = C(0xA8B6C2);           // заблокировано
        public static readonly Color Disabled = C(0xC9D6E2);

        // --- Валюты ---
        public static readonly Color SoftCurrency = C(0xFFCC33);
        public static readonly Color SoftCurrencyRim = C(0xE8A600);
        public static readonly Color HardCurrency = C(0x8B6CF0);

        // --- Прогресс-бар ---
        public static readonly Color ProgressTrack = C(0xDCEFFF);
        public static readonly Color ProgressFillFrom = C(0x4FC3F7);
        public static readonly Color ProgressFillTo = C(0x8FE36B);

        // --- Метрики ---
        /// <summary>Радиус скругления панели, px при 1080 ширины.</summary>
        public const int PanelRadius = 24;

        /// <summary>Радиус скругления кнопки.</summary>
        public const int ButtonRadius = 16;

        /// <summary>Толщина обводки панели.</summary>
        public const int BorderWidth = 3;

        /// <summary>Отступ от краёв экрана с учётом выреза.</summary>
        public const int SafeMargin = 24;

        /// <summary>Создать цвет из 0xRRGGBB и прозрачности 0..255.</summary>
        public static Color C(uint rgb, byte alpha = 255)
        {
            return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, alpha);
        }

        /// <summary>Тот же цвет с другой прозрачностью.</summary>
        public static Color WithAlpha(Color color, byte alpha)
        {
            return new Color(color.r, color.g, color.b, alpha / 255f);
        }
    }
}
