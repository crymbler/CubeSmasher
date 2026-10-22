using UnityEngine;

namespace CubeSmasher.UI
{
    /// <summary>
    /// Палитра интерфейса. Основа — цвета гаража со скриншотов владельца:
    /// тёмный металл (2E3350, 3D4470, 1A2438), светлый металл (7B8FE0, 8FA8E8),
    /// неон (9CFF3D, F451F6).
    ///
    /// Правило текста: на тёмном металле — белый, на неоне и светлом металле — тёмный.
    /// Менять значения здесь, а не в префабах.
    /// </summary>
    public static class UITheme
    {
        // --- Металл ---
        public static readonly Color MetalDarkest = C(0x1A2438);
        public static readonly Color MetalDark = C(0x2E3350);
        public static readonly Color MetalMid = C(0x3D4470);
        public static readonly Color MetalBlue = C(0x4A5080);
        public static readonly Color MetalLight = C(0x7B8FE0);
        public static readonly Color MetalSheen = C(0x8FA8E8);

        // --- Неон ---
        public static readonly Color NeonGreen = C(0x9CFF3D);
        public static readonly Color NeonGreenLight = C(0xC4FF8A);
        public static readonly Color NeonGreenDark = C(0x5E9E22);
        public static readonly Color NeonPink = C(0xF451F6);
        public static readonly Color NeonPinkLight = C(0xFF9BFA);
        public static readonly Color NeonPinkDark = C(0xA832A8);

        // --- Фон сцены ---
        public static readonly Color SkyTop = C(0x8FD3FF);
        public static readonly Color SkyBottom = C(0xE8F6FF);

        // --- Панели: тёмный металл ---
        public static readonly Color Panel = MetalDark;
        public static readonly Color PanelHeader = MetalMid;
        public static readonly Color PanelContent = MetalDarkest;
        public static readonly Color PanelBorder = MetalLight;
        public static readonly Color PanelShadow = C(0x0B1020, 150);

        // --- Кнопки ---
        public static readonly Color ButtonLight = NeonGreenLight;
        public static readonly Color ButtonMain = NeonGreen;
        public static readonly Color ButtonDark = NeonGreenDark;
        public static readonly Color ButtonOutline = MetalDarkest;

        public static readonly Color ButtonSecondaryLight = MetalSheen;
        public static readonly Color ButtonSecondaryMain = MetalLight;
        public static readonly Color ButtonSecondaryDark = MetalBlue;

        public static readonly Color ButtonDangerLight = NeonPinkLight;
        public static readonly Color ButtonDangerMain = NeonPink;
        public static readonly Color ButtonDangerDark = NeonPinkDark;

        public static readonly Color Accent = NeonGreen;
        public static readonly Color AccentPressed = NeonGreenDark;
        public static readonly Color AccentLight = NeonGreenLight;
        public static readonly Color AccentSecond = NeonPink;

        // --- Текст ---
        public static readonly Color TextPrimary = C(0xFFFFFF);
        public static readonly Color TextSecondary = MetalSheen;
        public static readonly Color TextMuted = C(0x8A93B8);
        public static readonly Color TextOnAccent = MetalDarkest;

        // --- Состояния ---
        public static readonly Color Positive = NeonGreen;

        /// <summary>Красный кружок-уведомление: доступна машина или прокачка.</summary>
        public static readonly Color Notification = C(0xFF3B3B);
        public static readonly Color NotificationRim = C(0x8C1414);
        public static readonly Color Negative = NeonPink;
        public static readonly Color Disabled = MetalBlue;

        // --- Валюты ---
        public static readonly Color SoftCurrency = C(0xFFE040);
        public static readonly Color SoftCurrencyRim = C(0xC9A000);
        public static readonly Color HardCurrency = NeonPink;

        // --- Прогресс ---
        public static readonly Color ProgressTrack = MetalDarkest;
        public static readonly Color ProgressShadow = C(0x0B1020);
        public static readonly Color ProgressFill = NeonGreen;
        public static readonly Color ProgressFillTop = NeonGreenLight;

        // --- Метрики ---
        public const int PanelRadius = 0;
        public const int ButtonRadius = 0;
        public const int BorderWidth = 4;
        public const int BevelHeight = 8;
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
