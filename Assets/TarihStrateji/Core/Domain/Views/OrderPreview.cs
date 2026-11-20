using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>
    /// Emir önizlemesi (TS-GDD-TDD-3.1 §19.3, mobil akış: seç → hedef → önizleme → onay). Emir gönderilmeden
    /// çekirdek kurallarıyla hesaplanır, durumu değiştirmez. Süre gün cinsinden tahmindir (günlük hareket
    /// bütçesi; temas veya rota kapanması hareketi erken durdurabilir); ikmal hedef düğümdeki bugünkü ikmal
    /// seviyesidir; düşman gücü yalnız oyuncunun bildiği temaslar ve hedef şehrin garnizonudur (savaş sisi).
    /// Rota noktaları ayrı listeye yazılır.
    /// </summary>
    public readonly struct OrderPreview
    {
        public const int RiskNone = 0;
        public const int RiskLow = 1;
        public const int RiskMedium = 2;
        public const int RiskHigh = 3;

        public OrderPreview(bool isValid, string errorKey, bool isAttack, int days, Fixed64 supplyAtTarget, Fixed64 ownPower, Fixed64 enemyPower,
            bool enemyEstimated, int risk)
        {
            IsValid = isValid;
            ErrorKey = errorKey;
            IsAttack = isAttack;
            Days = days;
            SupplyAtTarget = supplyAtTarget;
            OwnPower = ownPower;
            EnemyPower = enemyPower;
            EnemyEstimated = enemyEstimated;
            Risk = risk;
        }

        public static OrderPreview Invalid(string errorKey) =>
            new OrderPreview(false, errorKey, false, 0, Fixed64.Zero, Fixed64.Zero, Fixed64.Zero, false, RiskNone);

        public bool IsValid { get; }

        /// <summary>Geçersizse doğrulama hata anahtarı (emir gönderilse alınacak hata).</summary>
        public string ErrorKey { get; }

        public bool IsAttack { get; }

        /// <summary>Hedefe varış için tahmini gün (saldırı ve yerinde emir için 0).</summary>
        public int Days { get; }

        /// <summary>Hedef düğümdeki bugünkü ikmal seviyesi (0–1).</summary>
        public Fixed64 SupplyAtTarget { get; }

        /// <summary>Birliğin saldırı gücü (teşkilat, ikmal ve güç dahil).</summary>
        public Fixed64 OwnPower { get; }

        /// <summary>Hedefte bilinen düşman savunma gücü (temaslar + düşman şehri garnizonu).</summary>
        public Fixed64 EnemyPower { get; }

        /// <summary>Düşman gücünde kesin olmayan (uzak temas) bir pay var mı.</summary>
        public bool EnemyEstimated { get; }

        /// <summary><see cref="RiskNone"/> … <see cref="RiskHigh"/>: güç oranı ≥ 2 düşük, ≥ 1 orta, altı yüksek.</summary>
        public int Risk { get; }
    }
}
