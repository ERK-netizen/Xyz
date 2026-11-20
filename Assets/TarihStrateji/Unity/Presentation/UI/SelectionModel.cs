using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Views;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Dokunmatik seçim durumu (yalnız sunum, ADR-011). Dokunuş bir harita nesnesini odaklar
    /// (<see cref="Focus"/>); kendi kara birliği seçiliyken başka bir yere dokunmak orayı hedef yapar
    /// (<see cref="Target"/>: birlik, şehir veya nokta → yürü / saldır), birliğin kendisine dokunmak hedefi
    /// kaldırır. Birlik yok olursa veya artık oyuncunun değilse seçim temizlenir.
    /// </summary>
    public sealed class SelectionModel
    {
        public MapPick Focus { get; private set; }

        public ulong Unit { get; private set; }

        public TargetRef Target { get; private set; }

        public bool HasUnit => Unit != 0UL;

        public bool HasTarget => Target.IsValid;

        /// <summary>Her değişimde artar (çizim ve panel önbellekleri için).</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public void Tap(MapPick pick)
        {
            if (pick.Kind == MapPickKind.None)
            {
                Clear();
                return;
            }

            Focus = pick;
            bool self = pick.Kind == MapPickKind.Unit && pick.Unit == Unit;
            Target = HasUnit && !self ? pick.ToTarget() : TargetRef.None;
            Notify();
        }

        public void SelectUnit(ulong unitId)
        {
            Unit = unitId;
            Target = TargetRef.None;
            Notify();
        }

        public void ClearUnit()
        {
            if (!HasUnit)
            {
                return;
            }

            Unit = 0UL;
            Target = TargetRef.None;
            Notify();
        }

        public void Clear()
        {
            Focus = MapPick.None;
            Unit = 0UL;
            Target = TargetRef.None;
            Notify();
        }

        /// <summary>Durum değişiminden sonra seçili birliğin hâlâ oyuncunun kara birliği olduğunu doğrular.</summary>
        public void Reconcile(List<UnitView> visibleUnits)
        {
            if (!HasUnit)
            {
                return;
            }

            for (int i = 0; i < visibleUnits.Count; i++)
            {
                UnitView unit = visibleUnits[i];
                if (unit.Id == Unit && unit.IsOwn && unit.Kind == UnitViewKind.Land)
                {
                    return;
                }
            }

            ClearUnit();
        }

        private void Notify()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
