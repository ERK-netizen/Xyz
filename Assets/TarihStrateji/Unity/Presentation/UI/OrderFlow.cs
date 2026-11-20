using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Views;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Mobil emir akışı (Adım 16, §19.3): birlik seç → hedefe dokun → "Buraya yürü"/"Saldır" önizlemeyi açar
    /// (rota, süre, ikmal, risk; <see cref="IGameReadModel.PreviewOrder"/>) → "Onayla" emri üretir, "Vazgeç"
    /// kapatır. Yalnız sunum durumudur, kural yazmaz: geçerlilik ve sayılar çekirdekten gelir, emir yine
    /// <see cref="IGameCommandGateway"/> doğrulamasından geçer. Önizleme açıkken seçilen birlik ya da hedef
    /// değişirse kapanır; durum değişince (gün geçti) aynı emir yeniden önizlenir, geçersizleşirse sebebiyle açık
    /// kalır ama onaylanamaz.
    /// </summary>
    public sealed class OrderFlow
    {
        private readonly List<GeoPoint> _path = new List<GeoPoint>(32);

        public bool IsOpen { get; private set; }

        public bool IsAttack { get; private set; }

        public ulong Unit { get; private set; }

        public TargetRef Target { get; private set; }

        public OrderPreview Preview { get; private set; }

        /// <summary>Rota noktaları (birliğin konumundan hedefe); kapalı ya da geçersizse boş.</summary>
        public IReadOnlyList<GeoPoint> Path => _path;

        /// <summary>Önizleme geçerli ve onaylanabilir mi.</summary>
        public bool CanConfirm => IsOpen && Preview.IsValid;

        /// <summary>Her değişimde artar (rota çizimi ve panel önbellekleri için).</summary>
        public int Version { get; private set; }

        public event Action Changed;

        /// <summary>Önizlemeyi açar; geçersiz emir de sebebiyle gösterilir. Dönüş: onaylanabilir mi.</summary>
        public bool Open(IGameReadModel model, ulong unit, TargetRef target, bool attack)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (unit == 0UL || !target.IsValid)
            {
                Close();
                return false;
            }

            Unit = unit;
            Target = target;
            IsAttack = attack;
            IsOpen = true;
            Preview = model.PreviewOrder(unit, target, attack, _path);
            Notify();
            return Preview.IsValid;
        }

        /// <summary>Durum değişince açık önizlemeyi aynı emirle yeniden hesaplar.</summary>
        public void Refresh(IGameReadModel model)
        {
            if (!IsOpen)
            {
                return;
            }

            Preview = model.PreviewOrder(Unit, Target, IsAttack, _path);
            Notify();
        }

        /// <summary>Seçim değişti: önizlenen birlik/hedef artık seçili değilse kapanır.</summary>
        public void FollowSelection(ulong selectedUnit, TargetRef selectedTarget)
        {
            if (IsOpen && (selectedUnit != Unit || !selectedTarget.Equals(Target)))
            {
                Close();
            }
        }

        /// <summary>Onay: geçerliyse emri üretir ve akışı kapatır; değilse null döner, akış açık kalır.</summary>
        public GameCommand Confirm(IGameCommandFactory orders)
        {
            if (orders == null)
            {
                throw new ArgumentNullException(nameof(orders));
            }

            if (!CanConfirm)
            {
                return null;
            }

            GameCommand command = IsAttack ? orders.Attack(Unit, Target) : orders.MoveUnit(Unit, Target);
            Close();
            return command;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            Unit = 0UL;
            Target = TargetRef.None;
            Preview = default;
            _path.Clear();
            Notify();
        }

        private void Notify()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
