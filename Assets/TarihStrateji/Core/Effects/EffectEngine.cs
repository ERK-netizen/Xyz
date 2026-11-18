using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Effects
{
    /// <summary>
    /// Süreli/kalıcı etkilerin tek kaynağı. Etkiler kimlik sırasında tutulur; değerlendirme
    /// (hedef, istatistik) kovası üzerinden yapılır ve sonuç ekleme sırasından bağımsızdır.
    /// Süre dolumları günlük P2 fazında <see cref="ExpireBefore"/> ile işlenir.
    /// </summary>
    public sealed class EffectEngine
    {
        private readonly StableIdMap<EffectId, EffectInstance> _effects;
        private readonly Dictionary<BucketKey, List<EffectId>> _buckets = new Dictionary<BucketKey, List<EffectId>>();
        private readonly StableIdAllocator _ids;

        public EffectEngine()
            : this(new StableIdAllocator())
        {
        }

        private EffectEngine(StableIdAllocator ids)
        {
            _ids = ids;
            _effects = new StableIdMap<EffectId, EffectInstance>();
        }

        public int Count => _effects.Count;

        public StableIdAllocator Ids => _ids;

        /// <summary>Kimlik sırasında tüm etkiler.</summary>
        public StableIdMap<EffectId, EffectInstance> All => _effects;

        /// <summary>
        /// Etki ekler. <paramref name="durationDays"/> &gt; 0 ise etki başlangıç dahil o kadar gün
        /// sürer; 0 kalıcıdır.
        /// </summary>
        public Result<EffectId> Add(
            EffectTarget target,
            string statKey,
            EffectOperation operation,
            Fixed64 value,
            GameDate startsOn,
            int durationDays,
            string sourceKey)
        {
            if (string.IsNullOrEmpty(statKey))
            {
                return Result<EffectId>.Failure(EffectErrorKeys.IstatistikAnahtariBos);
            }

            if (!target.IsValid || target.Scope > EffectScope.Character)
            {
                return Result<EffectId>.Failure(EffectErrorKeys.HedefGecersiz);
            }

            if (operation != EffectOperation.Add && operation != EffectOperation.Percent)
            {
                return Result<EffectId>.Failure(EffectErrorKeys.IslemGecersiz);
            }

            if (durationDays < 0 || (durationDays > 0 && startsOn.DayNumber + (long)durationDays - 1L > GameDate.MaxValue.DayNumber))
            {
                return Result<EffectId>.Failure(EffectErrorKeys.SureGecersiz);
            }

            GameDate last = durationDays == 0 ? GameDate.MaxValue : startsOn.AddDays(durationDays - 1);
            var id = new EffectId(_ids.Allocate());
            Insert(new EffectInstance(id, target, statKey, operation, value, startsOn, last, sourceKey));
            return Result<EffectId>.Success(id);
        }

        public bool TryGet(EffectId id, out EffectInstance effect)
        {
            return _effects.TryGetValue(id, out effect);
        }

        public bool Remove(EffectId id)
        {
            if (!_effects.TryGetValue(id, out EffectInstance effect))
            {
                return false;
            }

            _effects.Remove(id);
            var key = new BucketKey(effect.Target, effect.StatKey);
            List<EffectId> bucket = _buckets[key];
            bucket.Remove(id);
            if (bucket.Count == 0)
            {
                _buckets.Remove(key);
            }

            return true;
        }

        /// <summary>
        /// Son etkin günü <paramref name="today"/>'den önce olan etkileri kaldırır (P2). Kaldırılan
        /// kimlikler artan sırayla <paramref name="expired"/>'a eklenir; sayıları döner.
        /// </summary>
        public int ExpireBefore(GameDate today, List<EffectId> expired)
        {
            int before = expired.Count;
            for (int i = 0; i < _effects.Count; i++)
            {
                EffectInstance effect = _effects.ValueAt(i);
                if (effect.LastActiveDay < today)
                {
                    expired.Add(effect.Id);
                }
            }

            for (int i = before; i < expired.Count; i++)
            {
                Remove(expired[i]);
            }

            return expired.Count - before;
        }

        /// <summary>Tek hedef için değer: (taban + Σtoplam) × max(0, 1 + Σyüzde).</summary>
        public Fixed64 Evaluate(EffectTarget target, string statKey, Fixed64 baseValue, GameDate today)
        {
            Fixed64 add = Fixed64.Zero;
            Fixed64 percent = Fixed64.Zero;
            Accumulate(target, statKey, today, ref add, ref percent);
            return Combine(baseValue, add, percent);
        }

        /// <summary>
        /// Birden çok hedefin etkilerini birlikte uygular (ör. global + ülke + bölge + hücre).
        /// Aynı hedef iki kez verilirse iki kez sayılır; çağıran tekil liste verir.
        /// </summary>
        public Fixed64 Evaluate(IReadOnlyList<EffectTarget> targets, string statKey, Fixed64 baseValue, GameDate today)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            Fixed64 add = Fixed64.Zero;
            Fixed64 percent = Fixed64.Zero;
            for (int i = 0; i < targets.Count; i++)
            {
                Accumulate(targets[i], statKey, today, ref add, ref percent);
            }

            return Combine(baseValue, add, percent);
        }

        /// <summary>Kayıttan geri yükleme: kimlik dağıtıcısının durumu ve etkiler.</summary>
        internal static EffectEngine Restore(StableIdAllocator ids, List<EffectInstance> effects)
        {
            var engine = new EffectEngine(ids);
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Id.Value >= ids.NextValue)
                {
                    throw new ArgumentException("Etki kimliği dağıtıcıdan büyük.");
                }

                engine.Insert(effects[i]);
            }

            return engine;
        }

        private void Insert(EffectInstance effect)
        {
            _effects.Add(effect.Id, effect);
            var key = new BucketKey(effect.Target, effect.StatKey);
            if (!_buckets.TryGetValue(key, out List<EffectId> bucket))
            {
                bucket = new List<EffectId>(2);
                _buckets.Add(key, bucket);
            }

            int index = bucket.BinarySearch(effect.Id);
            bucket.Insert(index < 0 ? ~index : index, effect.Id);
        }

        private void Accumulate(EffectTarget target, string statKey, GameDate today, ref Fixed64 add, ref Fixed64 percent)
        {
            if (!_buckets.TryGetValue(new BucketKey(target, statKey), out List<EffectId> bucket))
            {
                return;
            }

            for (int i = 0; i < bucket.Count; i++)
            {
                EffectInstance effect = _effects[bucket[i]];
                if (!effect.IsActiveOn(today))
                {
                    continue;
                }

                if (effect.Operation == EffectOperation.Add)
                {
                    add += effect.Value;
                }
                else
                {
                    percent += effect.Value;
                }
            }
        }

        private static Fixed64 Combine(Fixed64 baseValue, Fixed64 add, Fixed64 percent)
        {
            Fixed64 factor = Fixed64.Max(Fixed64.Zero, Fixed64.One + percent);
            return (baseValue + add) * factor;
        }

        private readonly struct BucketKey : IEquatable<BucketKey>
        {
            private readonly EffectTarget _target;
            private readonly string _stat;

            public BucketKey(EffectTarget target, string stat)
            {
                _target = target;
                _stat = stat;
            }

            public bool Equals(BucketKey other) =>
                _target == other._target && string.Equals(_stat, other._stat, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is BucketKey other && Equals(other);

            public override int GetHashCode() => (_target.GetHashCode() * 31) ^ StringComparer.Ordinal.GetHashCode(_stat);
        }
    }
}
