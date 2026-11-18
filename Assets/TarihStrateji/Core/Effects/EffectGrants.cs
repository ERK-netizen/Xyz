using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Effects
{
    /// <summary>Etki tanımı listelerini uygular ve kaldırır.</summary>
    public static class EffectGrants
    {
        /// <summary>
        /// Tüm tanımları hedefe uygular; oluşan kimlikler <paramref name="applied"/>'a eklenir.
        /// Biri başarısız olursa o çağrıda eklenenler geri alınır (hep ya da hiç).
        /// </summary>
        public static Result<int> Apply(
            EffectEngine engine,
            EffectTarget target,
            IReadOnlyList<EffectGrant> grants,
            GameDate startsOn,
            int durationDays,
            string sourceKey,
            List<EffectId> applied)
        {
            if (engine == null || grants == null || applied == null)
            {
                throw new ArgumentNullException(engine == null ? nameof(engine) : grants == null ? nameof(grants) : nameof(applied));
            }

            int before = applied.Count;
            for (int i = 0; i < grants.Count; i++)
            {
                Result<EffectId> added = engine.Add(target, grants[i].StatKey, grants[i].Operation, grants[i].Value, startsOn, durationDays, sourceKey);
                if (!added.IsSuccess)
                {
                    for (int k = applied.Count - 1; k >= before; k--)
                    {
                        engine.Remove(applied[k]);
                        applied.RemoveAt(k);
                    }

                    return Result<int>.Failure(added.ErrorKey);
                }

                applied.Add(added.Value);
            }

            return Result<int>.Success(applied.Count - before);
        }

        /// <summary>Kimlikleri kaldırır; zaten yok olanları sessizce geçer (süresi dolmuş olabilir).</summary>
        public static void Remove(EffectEngine engine, IReadOnlyList<EffectId> ids)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                engine.Remove(ids[i]);
            }
        }

        public static bool AllValid(IReadOnlyList<EffectGrant> grants)
        {
            for (int i = 0; i < grants.Count; i++)
            {
                if (!grants[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
