using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using RegexCrafter.Utils;

namespace RegexCrafter.CraftsMethods;

public class MapState : CraftState
{
    public CurrencyMethodCraftType CurrencyMethodCraftTypeMethodCraft = CurrencyMethodCraftType.Chaos;
    public int TypeChisel;
    public bool UseAddQuality = true;
}

public class Map(RegexCrafter core) : Craft<MapState>(core)
{
    private const string LogName = "CraftMap";

    private readonly string[] _chiselList =
    [
        CurrencyNames.CartographersChisel, CurrencyNames.ChiselOfProliferation, CurrencyNames.ChiselOfProcurement,
        CurrencyNames.ChiselOfScarabs, CurrencyNames.ChiselOfDivination, CurrencyNames.ChiselOfAvarice
    ];

    private readonly CurrencyMethodCraftType[] _typeMethodCraft =
        [CurrencyMethodCraftType.Chaos, CurrencyMethodCraftType.ScouringAndAlchemy];

    public override MapState CraftState { get; set; } = new();

    public override string Name { get; } = "Map";

    public override void DrawSettings()
    {
        base.DrawSettings();
        var selectedMethod = (int)CraftState.CurrencyMethodCraftTypeMethodCraft;
        if (ImGui.Combo("Type Method craft", ref selectedMethod,
                _typeMethodCraft.Select(x => x.GetDescription()).ToArray(), _typeMethodCraft.Length))
            CraftState.CurrencyMethodCraftTypeMethodCraft = (CurrencyMethodCraftType)selectedMethod;
        ImGui.Checkbox("Use Add Quality", ref CraftState.UseAddQuality);
        ImGui.Combo("Type chisel", ref CraftState.TypeChisel, _chiselList, _chiselList.Length);
        ImGui.Separator();
        if (CraftState.RegexPatterns.Count == 0) CraftState.RegexPatterns.Add(string.Empty);
        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));
        ImGui.LabelText("##MainConditionsMap", "Main Conditions");
        for (var i = 0; i < CraftState.RegexPatterns.Count; i++)
        {
            var patternTemp = CraftState.RegexPatterns[i];
            if (ImGui.InputText($"Your regex pattern {i}", ref patternTemp, 1024))
                CraftState.RegexPatterns[i] = patternTemp;
            ImGui.SameLine();
            if (!ImGui.Button($"Remove##{i}")) continue;
            GlobalLog.Debug($"Remove pattern:{CraftState.RegexPatterns[i]}.", LogName);
            CraftState.RegexPatterns.RemoveAt(i);
            //tempPatternList.Add(i);
        }

        if (ImGui.Button("Add Regex Pattern")) CraftState.RegexPatterns.Add(string.Empty);
    }

    public override async SyncTask<bool> Start()
    {
        if (!await CurrencyUseHelper.IdentifyItems()) return false;

        // apply chisel
        if (CraftState.UseAddQuality && !await Scripts.UseCurrencyOnMultipleItems(_chiselList[CraftState.TypeChisel],
                x => x.IsMap && !x.IsCorrupted, RegexQualityCondition)) return false;

        switch (CraftState.CurrencyMethodCraftTypeMethodCraft)
        {
            case CurrencyMethodCraftType.Chaos:
                return await ChaosSpam();
            case CurrencyMethodCraftType.ScouringAndAlchemy:
                return await ScouringAndAlchemy();
            default:
                GlobalLog.Error("Cannot find type method craft.", LogName);
                return false;
        }
    }

    private bool RegexQualityCondition(InventoryItemData item)
    {
        return _chiselList[CraftState.TypeChisel] switch
        {
            CurrencyNames.CartographersChisel => RegexUtils.MatchesPattern(item.ClipboardText, "lity:.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfAvarice => RegexUtils.MatchesPattern(item.ClipboardText, "urr.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfDivination => RegexUtils.MatchesPattern(item.ClipboardText, "div.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfProcurement => RegexUtils.MatchesPattern(item.ClipboardText, "ty\\).*([2-9].|1..)%"),
            CurrencyNames.ChiselOfScarabs => RegexUtils.MatchesPattern(item.ClipboardText, "sca.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfProliferation =>
                RegexUtils.MatchesPattern(item.ClipboardText, "ze\\).*([2-9].|1..)%"),
            _ => RegexUtils.MatchesPattern(item.ClipboardText, "Quality:*.*([2-9].|1..)%")
        };
    }

    private async SyncTask<bool> ScouringAndAlchemy()
    {
        if (Settings.CraftPlace == CraftPlaceType.MousePosition)
        {
            if (!Scripts.TryGetHoveredItem(out var item)) return false;
            if (item.IsCorrupted || !item.IsMap) return false;
            if (CraftState.UseAddQuality &&
                !await Scripts.UseCurrencyToSingleItem(item, _chiselList[CraftState.TypeChisel], RegexQualityCondition))
                return false;
            if (RegexCondition(item)) return true;
            while (!RegexCondition(item))
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (item.Rarity is ItemRarity.Rare or ItemRarity.Magic)
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfScouring,
                            x => x.Rarity == ItemRarity.Normal))
                        return false;

                if (item.Rarity != ItemRarity.Normal) continue;

                if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                        x => x.Rarity == ItemRarity.Rare)) return false;
            }

            return true;
        }

        var maps = await GetValidMaps();

        if (maps == null) return false;

        while (maps.Count > 0)
        {
            CancellationToken.ThrowIfCancellationRequested();
            //apply scouring
            if (!await CurrencyUseHelper.ScouringItems(RegexCondition)) return false;

            // apply orb of alchemy
            if (!await CurrencyUseHelper.AlchemyItems(RegexCondition)) return false;

            maps = await GetValidMaps();
            if (maps == null) return false;
        }

        return true;
    }

    private SyncTask<List<InventoryItemData>> GetValidMaps()
    {
        return Scripts.TryGetUsedItems(x =>
            DoneCraftItem.All(s => s.Entity.Address != x.Entity.Address) && !x.IsCorrupted && x.IsMap);
    }

    private async SyncTask<bool> ChaosSpam()
    {
        if (Settings.CraftPlace == CraftPlaceType.MousePosition)
        {
            var (isSuccess, item) = await Scripts.WaitForHoveredItem(
                hoverItem => hoverItem != null,
                "Get the initial hovered item");

            if (!isSuccess)
            {
                GlobalLog.Error("### No hovered item found!", LogName);
                return false;
            }

            if (item.IsCorrupted || !item.IsMap) return false;
            if (CraftState.UseAddQuality &&
                !await Scripts.UseCurrencyToSingleItem(item, _chiselList[CraftState.TypeChisel], RegexQualityCondition))
                return false;
            if (RegexCondition(item)) return true;

            switch (item.Rarity)
            {
                case ItemRarity.Normal:
                    // 1. Use alchemy
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                            x => x.Rarity == ItemRarity.Rare))
                        return false;
                    // 2. After alchemy use chaos 
                    return await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.ChaosOrb, RegexCondition);

                case ItemRarity.Magic:
                    // 1. Scouring to Normal 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfScouring,
                            x => x.Rarity == ItemRarity.Normal))
                        return false;
                    // 2. Alchemy to Rare 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                            x => x.Rarity == ItemRarity.Rare))
                        return false;
                    // 3. spam chaos
                    return await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.ChaosOrb, RegexCondition);
                case ItemRarity.Rare:
                    // 1. spam chaos
                    return await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.ChaosOrb, RegexCondition);
                default:
                    // else return false 
                    return false;
            }
        }

        if (!await CurrencyUseHelper.ScouringItems(x => x.IsMap, RegexCondition))
            return false;
        // apply orb of alchemy
        if (!await CurrencyUseHelper.AlchemyItems(x => x.IsMap, RegexCondition)) return false;
        // chaos spam
        return await CurrencyUseHelper.ChaosSpamItems(x => x.IsMap, RegexCondition);
    }
}