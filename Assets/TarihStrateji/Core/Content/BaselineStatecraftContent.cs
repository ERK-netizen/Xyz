using System;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Politics.Definitions;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// 1920'ler Türk–Yunan cephesi siyaset ve karakterleri. Hizmet pencereleri
    /// tarihsel görev başlangıcı ile ölüm/emeklilik tarihleridir.
    /// </summary>
    public static class BaselineStatecraftContent
    {
        public static PoliticsCatalog CreatePolitics()
        {
            var forms = new[]
            {
                new GovernmentFormDefinition(BaselineStatecraft.Republic, "yonetim.cumhuriyet", 4 * 365 + 1, F("0.55")),
                new GovernmentFormDefinition(BaselineStatecraft.ConstitutionalMonarchy, "yonetim.mesrutiMonarsi", 4 * 365 + 1, F("0.6")),
                new GovernmentFormDefinition(BaselineStatecraft.SingleParty, "yonetim.tekParti", 0, F("0.5")),
            };
            var parties = new[]
            {
                new PartyDefinition(BaselineStatecraft.RepublicanPeoples, "parti.cumhuriyetHalkFirkasi", BaselineStatecraft.Turkey, Ideology.Nationalist),
                new PartyDefinition(BaselineStatecraft.Progressive, "parti.terakkiperverCumhuriyetFirkasi", BaselineStatecraft.Turkey, Ideology.Liberal),
                new PartyDefinition(BaselineStatecraft.GreekLiberal, "parti.yunanLiberalParti", BaselineStatecraft.Greece, Ideology.Liberal),
                new PartyDefinition(BaselineStatecraft.GreekPeoples, "parti.yunanHalkPartisi", BaselineStatecraft.Greece, Ideology.Monarchist),
            };
            var institutions = new[]
            {
                new InstitutionDefinition(BaselineStatecraft.Army, "kurum.ordu", F("0.1"), F("20000")),
                new InstitutionDefinition(BaselineStatecraft.Parliament, "kurum.meclis", F("0.1"), F("15000")),
                new InstitutionDefinition(BaselineStatecraft.Judiciary, "kurum.yargi", F("0.05"), F("10000")),
            };
            var crises = new[]
            {
                new CrisisDefinition(BaselineStatecraft.EconomicDepression, "kriz.ekonomikBunalim", F("0.4"), F("0.01"), 90, F("-0.05"),
                    new[] { new EffectGrant("ekonomi.uretimVerimi", EffectOperation.Percent, F("-0.1")) }),
                new CrisisDefinition(BaselineStatecraft.Uprising, "kriz.isyan", F("0.25"), F("0.02"), 60, F("-0.05"),
                    new[] { new EffectGrant("ekonomi.vergiVerimi", EffectOperation.Percent, F("-0.2")) }),
            };
            var parameters = new PoliticsParameters(F("0.02"), F("0.01"), F("0.0005"), F("0.3"));
            Result<PoliticsCatalog> catalog = PoliticsCatalog.Create(forms, parties, institutions, crises, parameters);
            if (!catalog.IsSuccess)
            {
                throw new InvalidOperationException("Temel siyaset içeriği geçersiz: " + catalog.ErrorKey);
            }

            return catalog.Value;
        }

        public static CharacterCatalog CreateCharacters(int eraCount)
        {
            var characters = new[]
            {
                Character(BaselineStatecraft.MustafaKemal, "karakter.mustafaKemal", BaselineStatecraft.Turkey, CharacterRole.Leader | CharacterRole.General,
                    D(1919, 5, 19), D(1938, 11, 10), Add("siyaset.istikrarHedefi", "0.08"), Pct("kara.saldiri", "0.05")),
                Character(BaselineStatecraft.IsmetInonu, "karakter.ismetInonu", BaselineStatecraft.Turkey, CharacterRole.Leader | CharacterRole.General | CharacterRole.Minister,
                    D(1920, 1, 1), D(1973, 12, 25), Add("siyaset.istikrarHedefi", "0.03"), Pct("ekonomi.vergiVerimi", "0.05")),
                Character(BaselineStatecraft.FevziCakmak, "karakter.fevziCakmak", BaselineStatecraft.Turkey, CharacterRole.General,
                    D(1920, 1, 1), D(1950, 4, 10), Pct("kara.savunma", "0.10")),
                Character(BaselineStatecraft.KazimKarabekir, "karakter.kazimKarabekir", BaselineStatecraft.Turkey, CharacterRole.General,
                    D(1920, 1, 1), D(1948, 1, 26), Pct("kara.savunma", "0.05"), Pct("kara.saldiri", "0.05")),
                Character(BaselineStatecraft.CelalBayar, "karakter.celalBayar", BaselineStatecraft.Turkey, CharacterRole.Minister,
                    D(1921, 1, 1), D(1986, 8, 22), Pct("ekonomi.uretimVerimi", "0.05")),
                Character(BaselineStatecraft.EleftheriosVenizelos, "karakter.eleftheriosVenizelos", BaselineStatecraft.Greece, CharacterRole.Leader,
                    D(1910, 10, 18), D(1936, 3, 18), Add("siyaset.istikrarHedefi", "0.05")),
                Character(BaselineStatecraft.AnastasiosPapoulas, "karakter.anastasiosPapoulas", BaselineStatecraft.Greece, CharacterRole.General,
                    D(1919, 1, 1), D(1922, 5, 24), Pct("kara.saldiri", "0.05")),
            };
            Result<CharacterCatalog> catalog = CharacterCatalog.Create(characters, eraCount);
            if (!catalog.IsSuccess)
            {
                throw new InvalidOperationException("Temel karakter içeriği geçersiz: " + catalog.ErrorKey);
            }

            return catalog.Value;
        }

        private static CharacterDefinition Character(Domain.Ids.CharacterId id, string key, Domain.Ids.CountryId country, CharacterRole roles, GameDate from, GameDate until, params EffectGrant[] traits)
        {
            return new CharacterDefinition(id, key, country, roles, from, until, 0, traits);
        }

        private static GameDate D(int y, int m, int d) => GameDate.FromYmd(y, m, d);

        private static EffectGrant Pct(string stat, string value) => new EffectGrant(stat, EffectOperation.Percent, F(value));

        private static EffectGrant Add(string stat, string value) => new EffectGrant(stat, EffectOperation.Add, F(value));

        private static Fixed64 F(string text) => Fixed64.Parse(text);
    }
}
