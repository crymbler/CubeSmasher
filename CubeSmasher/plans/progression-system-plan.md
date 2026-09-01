# План: Система Milestone-прогрессии (v5 — финальная)

## 1. Архитектура

```
Wallet.Changed
     │
     ▼
  [LevelTracker]  ← загружает level из YG2, повышает только вверх
     │              сохраняет в YG2, вычисляет sliderValue
     │              дёргает LevelChanged(int level, float sliderValue)
     │
     ├──▶ SkinChanger      ── (level + 1) / 2 → Frog.SetSkin()
     ├──▶ BackgroundChanger ── level / 2 → Image.sprite
     └──▶ LevelView        ── sliderValue → Slider.value
```

**Формула sliderValue** = `(float)(balance / target)`, target — порог текущего уровня из кешированного массива `TierValues[level]`.

## 2. SkinUnlockTier enum

```csharp
public enum SkinUnlockTier : long
{
    Level1 = 1_000,
    Level2 = 10_000,
    Level3 = 100_000,
    Level4 = 1_000_000,
    Level5 = 10_000_000,
    Level6 = 100_000_000,
    Level7 = 1_000_000_000,
    Level8 = 10_000_000_000,
    Level9 = 50_000_000_000,
    Level10 = 150_000_000_000
}
```

## 3. Маппинг level → визуалы

| level | Порог        | skinIndex | bgIndex |
|-------|--------------|-----------|---------|
| 0     | 1,000        | 0         | 0       |
| 1     | 10,000       | 1         | 0       |
| 2     | 100,000      | 1         | 1       |
| 3     | 1,000,000    | 2         | 1       |
| 4     | 10,000,000   | 2         | 2       |
| 5     | 100,000,000  | 3         | 2       |
| 6     | 1,000,000,000| 3         | 3       |
| 7     | 10,000,000,000| 4        | 3       |
| 8     | 50,000,000,000| 4        | 4       |
| 9     | 150,000,000,000|5        | 4       |
| 10    | MAX          | 5         | 5       |

**Формулы:**
- **SkinChanger:** `int skinIndex = Mathf.Min((level + 1) / 2, 5);`
- **BackgroundChanger:** `int bgIndex = Mathf.Min(level / 2, 5);`

## 4. Все файлы

### A) [`SaveData.cs`](Assets/Scripts/DataSave/SaveData.cs) — добавить поле

```csharp
public int level = 0;
```

### B) `SkinUnlockTier.cs` — новый enum

Создать файл в `Assets/Scripts/`.

### C) [`LevelTracker.cs`](Assets/Scripts/LevelTracker.cs) — переписать

```csharp
using System;
using UnityEngine;
using YG;

public class LevelTracker : MonoBehaviour
{
    // Кешируем массив значений enum один раз на весь жизненный цикл приложения
    private static readonly long[] TierValues = (long[])Enum.GetValues(typeof(SkinUnlockTier));

    [SerializeField] private Wallet _wallet;

    // Событие: (int level, float sliderValue)
    public event Action<int, float> LevelChanged;

    private void Start()
    {
        _wallet.Changed += OnBalanceChanged;
        InvokeChanged();
    }

    private void OnDestroy() => _wallet.Changed -= OnBalanceChanged;

    private void OnBalanceChanged()
    {
        bool leveledUp = false;

        // Простое предусловие: пока баланс >= порога — повышаем уровень
        while (YG2.saves.level < TierValues.Length && _wallet.Balance >= TierValues[YG2.saves.level])
        {
            YG2.saves.level++;
            leveledUp = true;
        }

        if (leveledUp)
            YG2.SaveProgress();

        InvokeChanged();
    }

    private void InvokeChanged()
    {
        int index = Mathf.Clamp(YG2.saves.level, 0, TierValues.Length - 1);
        long target = TierValues[index];

        // Простое деление balance / target, без Clamp01
        float sliderValue = (float)(_wallet.Balance / target);

        LevelChanged?.Invoke(YG2.saves.level, sliderValue);
    }
}
```

### D) [`SkinChanger.cs`](Assets/Scripts/SkinChanger.cs) — переписать

Простая формула: `(level + 1) / 2`, clamped до 5.

```csharp
public class SkinChanger : MonoBehaviour
{
    [SerializeField] private LevelTracker _levelTracker;
    [SerializeField] private Frog _frog;

    private void OnEnable()  => _levelTracker.LevelChanged += OnLevelChanged;
    private void OnDisable() => _levelTracker.LevelChanged -= OnLevelChanged;
    private void Start()     => OnLevelChanged(0, 0f);

    private void OnLevelChanged(int level, float sliderValue)
    {
        int skinIndex = Mathf.Min((level + 1) / 2, 5);
        _frog.SetSkin(skinIndex);
    }
}
```

### E) [`BackgroundChanger.cs`](Assets/Scripts/BackgroundChanger.cs) — переписать

Простая формула: `level / 2`, clamped до 5.

```csharp
public class BackgroundChanger : MonoBehaviour
{
    [SerializeField] private LevelTracker _levelTracker;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Sprite[] _backgroundSprites;

    private void OnEnable()  => _levelTracker.LevelChanged += OnLevelChanged;
    private void OnDisable() => _levelTracker.LevelChanged -= OnLevelChanged;
    private void Start()     => OnLevelChanged(0, 0f);

    private void OnLevelChanged(int level, float sliderValue)
    {
        int bgIndex = Mathf.Min(level / 2, 5);

        if (bgIndex >= 0 && bgIndex < _backgroundSprites.Length)
            _backgroundImage.sprite = _backgroundSprites[bgIndex];
    }
}
```

### F) [`LevelView.cs`](Assets/Scripts/LevelView.cs) — переписать

```csharp
public class LevelView : MonoBehaviour
{
    [SerializeField] private LevelTracker _levelTracker;
    [SerializeField] private Slider _slider;

    private void OnEnable()  => _levelTracker.LevelChanged += OnLevelChanged;
    private void OnDisable() => _levelTracker.LevelChanged -= OnLevelChanged;
    private void Start()     => OnLevelChanged(0, 0f);

    private void OnLevelChanged(int _, float sliderValue)
    {
        _slider.value = sliderValue;
    }
}
```

## 5. Пример работы слайдера

- **Загрузка:** level = 2 (из YG2), balance = 50 000
  - `int index = Mathf.Clamp(2, 0, 9)` → index = 2
  - `long target = TierValues[2]` → target = 100 000
  - `slider = (float)(50 000 / 100 000)` → slider = 0.5

- **Потратили 30 000:** balance = 20 000
  - `slider = (float)(20 000 / 100 000)` → slider = 0.2

- **Достигли 100 000:** balance = 100 000
  - `while (2 < 10 && 100 000 >= TierValues[2])` → 100 000 >= 100 000 → **true**
  - level = 3, YG2.saves.level = 3, сохранение
  - `index = Mathf.Clamp(3, 0, 9)` → index = 3
  - `target = TierValues[3]` → target = 1 000 000
  - `slider = (float)(100 000 / 1 000 000)` → slider = 0.1

## 6. Файлы — сводка

| Файл | Действие |
|------|----------|
| [`Assets/Scripts/DataSave/SaveData.cs`](Assets/Scripts/DataSave/SaveData.cs) | ✏️ Добавить `public int level = 0;` |
| `Assets/Scripts/SkinUnlockTier.cs` | ✨ Новый enum |
| [`Assets/Scripts/LevelTracker.cs`](Assets/Scripts/LevelTracker.cs) | ✏️ Переписать |
| [`Assets/Scripts/LevelView.cs`](Assets/Scripts/LevelView.cs) | ✏️ Переписать |
| [`Assets/Scripts/SkinChanger.cs`](Assets/Scripts/SkinChanger.cs) | ✏️ Переписать |
| [`Assets/Scripts/BackgroundChanger.cs`](Assets/Scripts/BackgroundChanger.cs) | ✏️ Переписать |