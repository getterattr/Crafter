﻿using System;
using System.Collections.Generic;

namespace RegexCrafter.Helpers;

public enum CurrencyTabType
{
    None,
    General,
    Exotic
}

public static class CurrencyNames
{
    [Currency(CurrencyTabType.General)]
    public const string ChaosOrb = "Chaos Orb";
    [Currency(CurrencyTabType.General)]
    public const string OrbOfScouring = "Orb of Scouring";
    [Currency(CurrencyTabType.General)]
    public const string OrbOfAlchemy = "Orb of Alchemy";
    [Currency(CurrencyTabType.General)]
    public const string CartographersChisel = "Cartographer's Chisel";
    [Currency(CurrencyTabType.General)]
    public const string ExaltedOrb = "Exalted Orb";
    [Currency(CurrencyTabType.General)]
    public const string DivineOrb = "Divine Orb";
    [Currency(CurrencyTabType.General)]
    public const string ScrollOfWisdom = "Scroll of Wisdom";
    [Currency(CurrencyTabType.Exotic)]
    public const string ChiselOfAvarice = "Maven's Chisel of Avarice";
    [Currency(CurrencyTabType.Exotic)]
    public const string ChiselOfDivination = "Maven's Chisel of Divination";
    [Currency(CurrencyTabType.Exotic)]
    public const string ChiselOfProcurement = "Maven's Chisel of Procurement";
    [Currency(CurrencyTabType.Exotic)]
    public const string ChiselOfScarabs = "Maven's Chisel of Scarabs";
    [Currency(CurrencyTabType.Exotic)]
    public const string ChiselOfProliferation = "Maven's Chisel of Proliferation";

    private static Dictionary<string, CurrencyTabType> _currencyTypeCache;
    public static CurrencyTabType GetCurrencyType(string currencyName)
    {
        if (_currencyTypeCache != null) return _currencyTypeCache.GetValueOrDefault(currencyName, CurrencyTabType.None);
        _currencyTypeCache = [];

        // find all fields with CurrencyAttribute
        var fields = typeof(CurrencyNames).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        foreach (var field in fields)
        {
            // get CurrencyAttribute from field
            var attribute = (CurrencyAttribute)Attribute.GetCustomAttribute(field, typeof(CurrencyAttribute));
            if (attribute == null) continue;
            // get value of field
            var fieldValue = field.GetValue(null)?.ToString();
            if (fieldValue != null)
            {
                // add to cache
                _currencyTypeCache[fieldValue] = attribute.CurrencyType;
            }
        }
        return _currencyTypeCache.GetValueOrDefault(currencyName, CurrencyTabType.None);
    }
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class CurrencyAttribute(CurrencyTabType currencyType) : Attribute
{
    public CurrencyTabType CurrencyType { get; } = currencyType;
}
