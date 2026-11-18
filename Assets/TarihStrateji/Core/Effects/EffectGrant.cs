using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Effects
{
    /// <summary>
    /// İçerik verisindeki tek bir etki tanımı (teknoloji, karakter özelliği, kriz…). Hedef,
    /// başlangıç ve süre uygulama anında verilir.
    /// </summary>
    [Serializable]
    public readonly struct EffectGrant
    {
        public EffectGrant(string statKey, EffectOperation operation, Fixed64 value)
        {
            StatKey = statKey;
            Operation = operation;
            Value = value;
        }

        public string StatKey { get; }

        public EffectOperation Operation { get; }

        public Fixed64 Value { get; }

        public bool IsValid => !string.IsNullOrEmpty(StatKey) && (Operation == EffectOperation.Add || Operation == EffectOperation.Percent);
    }
}
