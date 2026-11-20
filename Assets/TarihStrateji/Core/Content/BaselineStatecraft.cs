using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content
{
    /// <summary>Temel siyaset/karakter içeriğinin kimlikleri (kayıt biçiminin parçası; değiştirilmez).</summary>
    public static class BaselineStatecraft
    {
        public static readonly CountryId Turkey = new CountryId(1);
        public static readonly CountryId Greece = new CountryId(2);

        public static readonly GovernmentFormId Republic = new GovernmentFormId(1);
        public static readonly GovernmentFormId ConstitutionalMonarchy = new GovernmentFormId(2);
        public static readonly GovernmentFormId SingleParty = new GovernmentFormId(3);

        public static readonly PartyId RepublicanPeoples = new PartyId(1);
        public static readonly PartyId Progressive = new PartyId(2);
        public static readonly PartyId GreekLiberal = new PartyId(3);
        public static readonly PartyId GreekPeoples = new PartyId(4);
        public static readonly PartyId UnionAndProgress = new PartyId(5);
        public static readonly PartyId FreedomAndAccord = new PartyId(6);

        public static readonly InstitutionId Army = new InstitutionId(1);
        public static readonly InstitutionId Parliament = new InstitutionId(2);
        public static readonly InstitutionId Judiciary = new InstitutionId(3);

        public static readonly CrisisId EconomicDepression = new CrisisId(1);
        public static readonly CrisisId Uprising = new CrisisId(2);

        public static readonly CharacterId MustafaKemal = new CharacterId(1);
        public static readonly CharacterId IsmetInonu = new CharacterId(2);
        public static readonly CharacterId FevziCakmak = new CharacterId(3);
        public static readonly CharacterId KazimKarabekir = new CharacterId(4);
        public static readonly CharacterId CelalBayar = new CharacterId(5);
        public static readonly CharacterId EleftheriosVenizelos = new CharacterId(6);
        public static readonly CharacterId AnastasiosPapoulas = new CharacterId(7);
    }
}
