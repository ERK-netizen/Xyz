using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;

namespace TarihStrateji.Core.Effects
{
    /// <summary><see cref="EffectEngine"/>'in kanonik anlık görüntüsü (kimlik sırasında).</summary>
    public static class EffectSnapshotCodec
    {
        public const uint Tag = 0x54434645; // "EFCT"

        public static void Write(EffectEngine engine, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteUInt64(engine.Ids.NextValue);
            w.WriteInt32(engine.Count);
            foreach (KeyValuePair<EffectId, EffectInstance> pair in engine.All)
            {
                EffectInstance e = pair.Value;
                w.WriteId(e.Id);
                w.WriteByte((byte)e.Target.Scope);
                w.WriteUInt64(e.Target.Id);
                w.WriteString(e.StatKey);
                w.WriteByte((byte)e.Operation);
                w.WriteFixed64(e.Value);
                w.WriteDate(e.StartsOn);
                w.WriteDate(e.LastActiveDay);
                w.WriteString(e.SourceKey);
            }
        }

        public static Result<EffectEngine> Read(CanonicalReader r)
        {
            r.ExpectTag(Tag);
            ulong next = r.ReadUInt64();
            if (next == 0UL)
            {
                return Result<EffectEngine>.Failure(DomainErrorKeys.KayitBozuk);
            }

            int count = r.ReadCount(r.Remaining);
            var effects = new List<EffectInstance>(count);
            for (int i = 0; i < count; i++)
            {
                var id = new EffectId(r.ReadUInt64());
                var target = new EffectTarget((EffectScope)r.ReadByte(), r.ReadUInt64());
                string stat = r.ReadString();
                var op = (EffectOperation)r.ReadByte();
                var value = r.ReadFixed64();
                var starts = r.ReadDate();
                var last = r.ReadDate();
                string source = r.ReadString();
                if (!id.IsValid || !target.IsValid || target.Scope > EffectScope.Character || string.IsNullOrEmpty(stat)
                    || op > EffectOperation.Percent || last < starts)
                {
                    return Result<EffectEngine>.Failure(DomainErrorKeys.KayitBozuk);
                }

                effects.Add(new EffectInstance(id, target, stat, op, value, starts, last, source));
            }

            try
            {
                return Result<EffectEngine>.Success(EffectEngine.Restore(new StableIdAllocator(next), effects));
            }
            catch (ArgumentException)
            {
                return Result<EffectEngine>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }
    }
}
