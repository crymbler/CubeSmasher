using System;
using System.Collections.Generic;
using CubeSmasher.Core.Interfaces;
using CubeSmasher.Data.Configs;
using CubeSmasher.Gameplay;
using YG;

/// <summary>
/// Состояние скинов: что куплено, что выбрано, сколько твёрдой валюты.
///
/// Мягкая валюта идёт через IWallet, твёрдая — отдельным счётчиком в сохранении.
/// Пока SkinConfig.HardCurrencyEnabled выключен, премиальные скины не продаются
/// и твёрдая валюта не тратится: игру можно выпустить без доната и включить позже.
/// </summary>
public class SkinService : ISkinService
{
    private readonly SkinConfig _config;
    private readonly IWallet _wallet;
    private readonly ISaveService _saveService;
    private readonly List<string> _owned = new List<string>();

    public event Action<string> SelectionChanged;
    public event Action<string> SkinPurchased;

    public SkinService(SkinConfig config, IWallet wallet, ISaveService saveService)
    {
        _config = config;
        _wallet = wallet;
        _saveService = saveService;

        LoadOwned();
        EnsureStarterSkin();
        ApplySelectedPalette();
    }

    public SkinData[] AllSkins =>
        _config != null && _config.Skins != null ? _config.Skins : Array.Empty<SkinData>();

    public bool HardCurrencyEnabled => _config != null && _config.HardCurrencyEnabled;

    public int HardCurrency => YG2.saves.hardCurrency;

    public string SelectedId => YG2.saves.selectedSkinId ?? string.Empty;

    public bool IsOwned(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        SkinData? skin = Find(id);
        if (skin.HasValue && skin.Value.IsDefault) return true;

        return _owned.Contains(id);
    }

    public bool IsAvailable(string id, int stage)
    {
        SkinData? found = Find(id);
        if (!found.HasValue) return false;

        SkinData skin = found.Value;
        if (skin.IsPremium && !HardCurrencyEnabled) return false;

        return stage >= skin.UnlockStage;
    }

    public bool TryBuy(string id, int stage)
    {
        if (!IsAvailable(id, stage) || IsOwned(id)) return false;

        SkinData skin = Find(id).Value;

        if (skin.IsPremium)
        {
            if (!HardCurrencyEnabled) return false;
            if (HardCurrency < skin.HardPrice) return false;

            YG2.saves.hardCurrency -= skin.HardPrice;
        }
        else
        {
            if (!_wallet.TrySpend(skin.SoftPrice)) return false;
        }

        _owned.Add(id);
        SaveOwned();
        ApplySelectedPalette();

        SkinPurchased?.Invoke(id);
        return true;
    }

    public bool TrySelect(string id)
    {
        if (!IsOwned(id)) return false;
        if (SelectedId == id) return true;

        YG2.saves.selectedSkinId = id;
        _saveService.Save();

        ApplySelectedPalette();
        SelectionChanged?.Invoke(id);
        return true;
    }

    /// <summary>
    /// Пересчитывает палитру для ColorChanger. Вызывается при старте, покупке
    /// и выборе скина, чтобы детали перекрашивались в новые цвета сразу.
    /// </summary>
    public void ApplySelectedPalette()
    {
        SkinData? skin = Find(SelectedId);

        if (!skin.HasValue)
        {
            SkinPaletteState.Current = null; // скин не выбран: цвет остаётся случайным
            return;
        }

        SkinData data = skin.Value;
        SkinPaletteState.Current = new SkinPalette(
            data.HueMin, data.HueMax, data.Saturation, data.Brightness);
    }

    private SkinData? Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        SkinData[] skins = AllSkins;
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i].Id == id) return skins[i];
        }
        return null;
    }

    /// <summary>
    /// Новому игроку выдаёт стартовый скин и стартовую твёрдую валюту.
    /// Флаг в сохранении не даёт выдать это повторно.
    /// </summary>
    private void EnsureStarterSkin()
    {
        if (YG2.saves.skinGranted) return;

        if (_config != null && _config.StartingHardCurrency > 0)
        {
            YG2.saves.hardCurrency += _config.StartingHardCurrency;
        }

        if (string.IsNullOrEmpty(SelectedId))
        {
            SkinData[] skins = AllSkins;
            for (int i = 0; i < skins.Length; i++)
            {
                if (!skins[i].IsDefault) continue;

                YG2.saves.selectedSkinId = skins[i].Id;
                break;
            }
        }

        YG2.saves.skinGranted = true;
        _saveService.Save();
    }

    private void LoadOwned()
    {
        _owned.Clear();
        if (YG2.saves.ownedSkins != null) _owned.AddRange(YG2.saves.ownedSkins);
    }

    private void SaveOwned()
    {
        YG2.saves.ownedSkins = new List<string>(_owned);
        _saveService.Save();
    }
}
