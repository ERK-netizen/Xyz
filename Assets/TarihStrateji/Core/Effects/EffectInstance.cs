using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Effects
{
    /// <summary>
    /// Tek bir etki (düz veri). [<see cref="StartsOn"/>, <see cref="LastActiveDay"/>] kapalı
    /// aralığında etkindir; kalıcı etkilerde son gün <see cref="GameDate.MaxValue"/>'dur.
    /// </summary>
    public sealed class EffectInstance
    {
        internal EffectInstance(
            EffectId id,
            EffectTarget target,
            string statKey,
            EffectOperation operation,
            Fixed64 value,
            GameDate startsOn,
            GameDate lastActiveDay,
            string sourceKey)
        {
            Id = id;
            Target = target;
            StatKey = statKey;
            Operation = operation;
            Value = value;
            StartsOn = startsOn;
            LastActiveDay = lastActiveDay;
            SourceKey = sourceKey;
        }

        public EffectId Id { get; }

        public EffectTarget Target { get; }

        /// <summary>İstatistik anahtarı, ör. <c>uretim.sanayi</c>; sıralı (ordinal) karşılaştırılır.</summary>
        public string StatKey { get; }

        public EffectOperation Operation { get; }

        public Fixed64 Value { get; }

        public GameDate StartsOn { get; }

        public GameDate LastActiveDay { get; }

        /// <summary>Kaynağın yerelleştirme anahtarı (olay, teknoloji, karakter…); boş olabilir.</summary>
        public string SourceKey { get; }

        public bool IsPermanent => LastActiveDay == GameDate.MaxValue;

        public bool IsActiveOn(GameDate day) => day >= StartsOn && day <= LastActiveDay;
    }
}
