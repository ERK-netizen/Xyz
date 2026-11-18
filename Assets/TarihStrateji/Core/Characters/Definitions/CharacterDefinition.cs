using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;

namespace TarihStrateji.Core.Characters.Definitions
{
    /// <summary>
    /// Tarihsel karakter (içerik verisi). [<see cref="ServiceFrom"/>, <see cref="ServiceUntil"/>]
    /// hizmet penceresidir (CLAUDE.md §2.6: atamalar tarih penceresine bağlı); pencere dışında
    /// atanamaz, görevdeyse pencere bitince görevden alınır. Özellikleri, atandığı sürece ülkeye
    /// uygulanır.
    /// </summary>
    public sealed class CharacterDefinition
    {
        private readonly EffectGrant[] _traits;

        public CharacterDefinition(CharacterId id, string key, CountryId country, CharacterRole roles, GameDate serviceFrom, GameDate serviceUntil, int minimumEra, EffectGrant[] traits)
        {
            Id = id;
            Key = key;
            Country = country;
            Roles = roles;
            ServiceFrom = serviceFrom;
            ServiceUntil = serviceUntil;
            MinimumEra = minimumEra;
            _traits = traits ?? new EffectGrant[0];
        }

        public CharacterId Id { get; }

        public string Key { get; }

        public CountryId Country { get; }

        public CharacterRole Roles { get; }

        public GameDate ServiceFrom { get; }

        public GameDate ServiceUntil { get; }

        public int MinimumEra { get; }

        public IReadOnlyList<EffectGrant> Traits => _traits;

        public bool Has(CharacterRole role) => (Roles & role) == role;

        public bool InService(GameDate day) => day.IsWithin(ServiceFrom, ServiceUntil);
    }
}
