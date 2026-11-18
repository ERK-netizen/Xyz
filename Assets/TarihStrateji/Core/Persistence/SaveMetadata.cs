using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Persistence
{
    /// <summary>
    /// Kayıt yuvası listesi için, yükü çözmeden okunabilen özet bilgi. Bütünlük özetinin
    /// kapsamındadır (değiştirilemez).
    /// </summary>
    public sealed class SaveMetadata
    {
        public SaveMetadata(string label, GameDate gameDate, GameMode mode, CountryId playerCountry, int stateSchemaVersion)
        {
            Label = label ?? string.Empty;
            GameDate = gameDate;
            Mode = mode;
            PlayerCountry = playerCountry;
            StateSchemaVersion = stateSchemaVersion;
        }

        /// <summary>Oyuncunun verdiği ad (görüntüleme için; kural kodunda kullanılmaz).</summary>
        public string Label { get; }

        public GameDate GameDate { get; }

        public GameMode Mode { get; }

        public CountryId PlayerCountry { get; }

        public int StateSchemaVersion { get; }

        internal byte[] Encode()
        {
            var w = new CanonicalWriter(128);
            w.WriteString(Label);
            w.WriteDate(GameDate);
            w.WriteByte((byte)Mode);
            w.WriteId(PlayerCountry);
            w.WriteInt32(StateSchemaVersion);
            return w.ToArray();
        }

        internal static SaveMetadata Decode(byte[] data)
        {
            var r = new CanonicalReader(data);
            string label = r.ReadString();
            GameDate date = r.ReadDate();
            var mode = (GameMode)r.ReadByte();
            var player = new CountryId(r.ReadUInt64());
            int schema = r.ReadInt32();
            r.ExpectEnd();
            if (mode != GameMode.Normal && mode != GameMode.Sandbox && mode != GameMode.Observer)
            {
                throw new FormatException(PersistenceErrorKeys.SurumGecersiz);
            }

            return new SaveMetadata(label, date, mode, player, schema);
        }
    }
}
