using TarihStrateji.Core.Domain.Serialization;

namespace TarihStrateji.Core.Domain.Geography
{
    /// <summary><see cref="TargetRef"/>'in kanonik ikili biçimi: tür (bayt), kimlik, enlem E6, boylam E6.</summary>
    public static class TargetRefCodec
    {
        public static void Write(CanonicalWriter w, TargetRef target)
        {
            w.WriteByte((byte)target.Kind);
            w.WriteUInt64(target.EntityId);
            w.WriteInt32(target.Point.LatitudeE6);
            w.WriteInt32(target.Point.LongitudeE6);
        }

        public static TargetRef Read(CanonicalReader r)
        {
            byte kind = r.ReadByte();
            ulong id = r.ReadUInt64();
            var point = new GeoPoint(r.ReadInt32(), r.ReadInt32());
            if (kind == (byte)TargetKind.None)
            {
                return TargetRef.None;
            }

            if (kind == (byte)TargetKind.Point)
            {
                return TargetRef.AtPoint(point);
            }

            if (kind > (byte)TargetKind.AirBase || id == 0UL)
            {
                throw new System.ArgumentException(DomainErrorKeys.KayitBozuk);
            }

            return TargetRef.Entity((TargetKind)kind, id);
        }
    }
}
