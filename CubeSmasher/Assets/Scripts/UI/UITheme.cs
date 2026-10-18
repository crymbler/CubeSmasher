using UnityEngine;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Палитра интерфейса. Две группы:
    ///  - Гараж (из скриншотов владельца): металл и неон. Используется для кнопок и заголовков.
    ///  - Светлая база: панели и текст, чтобы UI читался на небе арены.
    /// Значения — из docs/PLAN_V2.md. Менять здесь, а не в префабах.
    /// </summary>
    public static class UITheme
    {
        // --- Гараж: металл ---
        public static readonly Color MetalDark = C(0x1A2438);     // глубокий фон панелей гаража
        public static readonly Color MetalMid = C(0x3D4470);      // металлические рамки
        public static readonly Color MetalLight = C(0x7B8FE0);    // светлый металл, подсветка граней
        public static readonly Color MetalSheen = C(0x8FA8E8);    // блик
        public static readonly Color MetalBlue = C(0x2E3350);     // отражающий металл, тень

        // --- Гараж: неон ---
        public static readonly Color NeonGreen = C(0x9CFF3D);     // кнопки-акценты, «Купить»
        public static readonly Color NeonPink = C(0xF451F6);      // подсветка, активное состояние
        public static readonly Color NeonDeep = C(0x4A5080);      // промежуточный тон

        // --- Светлая база (панели и текст) ---
        public static readonly Color SkyTop = C(0x8FD3FF);
        public static readonly Color SkyBottom = C(0xE8F6FF);
        public static readonly Color Panel = C(0xFFFFFF, 255);
        public static readonly Color PanelBorder = C(0x7FA8C9);
        public static readonly Color PanelShadow = C(0x2E3350, 60);

        // --- Кнопки: фаска voxel-стиля (верх светлее, низ темнее) ---
        public static readonly Color ButtonLight = C(0xB8FF7A);   // верхняя полоса неона
        public static readonly Color ButtonMain = NeonGreen;      // основная поверхность
        public static readonly Color ButtonDark = C(0x5E9E22);    // нижний край
        public static readonly Color ButtonOutline = MetalBlue;   // контур 4 px

        public static readonly Color ButtonSecondaryLight = C(0xA8B8FF);
        public static readonly Color ButtonSecondaryMain = MetalLight;
        public static readonly Color ButtonSecondaryDark = C(0x4A5080);

        public static readonly Color Accent = NeonGreen;
        public static readonly Color AccentPressed = C(0x7ACC2A);
        public static readonly Color AccentLight = C(0xB8FF7A);
        public static readonly Color AccentSecond = MetalLight;

        // --- Текст ---
        public static readonly Color TextPrimary = C(0x1A2438);
        public static readonly Color TextSecondary = C(0x4A5080);
        public static readonly Color TextOnAccent = C(0x1A2438);
        public static readonly Color TextOnMetal = C(0xFFFFFF);

        // --- Состояния ---
        public static readonly Color Positive = NeonGreen;
        public static readonly Color Negative = NeonPink;
        public static readonly Color Muted = C(0x8A93B8);
        public static readonly Color Disabled = C(0xC9D0E6);

        // --- Валюты ---
        public static readonly Color SoftCurrency = C(0xFFE040);
        public static readonly Color SoftCurrencyRim = C(0xC9A000);
        public static readonly Color HardCurrency = NeonPink;

        // --- Прогресс ---
        public static readonly Color ProgressTrack = C(0x2E3350, 200);
        public static readonly Color ProgressFillFrom = MetalLight;
        public static readonly Color ProgressFillTo = NeonGreen;

        // --- Метрики (voxel: квадратные углы, толстая фаска) ---
        public const int PanelRadius = 0;      // без скруглений
        public const int ButtonRadius = 0;
        public const int BorderWidth = 4;
        public const int BevelHeight = 8;      // высота фаски
        public const int SafeMargin = 24;

        public static Color C(uint rgb, byte alpha = 255)
        {
            return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, alpha);
        }

        public static Color WithAlpha(Color color, byte alpha)
        {
            return new Color(color.r, color.g, color.b, alpha / 255f);
        }
    }
}
