using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Views;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Dokunmatik seçim durumu (yalnız sunum). Dokunuş hücre seçer; kendi birliği seçiliyken başka
    /// hücreye dokunmak o hücreyi hedef yapar (yürü / saldır), birliğin hücresine dokunmak hedefi
    /// kaldırır. Birlik yok olursa veya artık oyuncunun değilse seçim temizlenir; hareket ederse
    /// izlenir.
    /// </summary>
    public sealed class SelectionModel
    {
        public const int None = -1;

        public int Cell { get; private set; } = None;

        public ulong Unit { get; private set; }

        public int UnitCell { get; private set; } = None;

        public int Target { get; private set; } = None;

        public bool HasUnit => Unit != 0UL;

        /// <summary>Her değişimde artar (çizim ve panel önbellekleri için).</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public void Tap(int cell)
        {
            if (cell < 0)
            {
                Clear();
                return;
            }

            Cell = cell;
            Target = HasUnit && cell != UnitCell ? cell : None;
            Notify();
        }

        public void SelectUnit(ulong unitId, int cell)
        {
            Unit = unitId;
            UnitCell = cell;
            Cell = cell;
            Target = None;
            Notify();
        }

        public void ClearUnit()
        {
            if (!HasUnit)
            {
                return;
            }

            Unit = 0UL;
            UnitCell = None;
            Target = None;
            Notify();
        }

        public void Clear()
        {
            Cell = None;
            Unit = 0UL;
            UnitCell = None;
            Target = None;
            Notify();
        }

        /// <summary>Durum değişiminden sonra seçili birliği görünür birlik listesine göre günceller.</summary>
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
                    if (unit.CellIndex != UnitCell)
                    {
                        bool followed = Cell == UnitCell;
                        UnitCell = unit.CellIndex;
                        if (followed)
                        {
                            Cell = UnitCell;
                        }

                        if (Target == UnitCell)
                        {
                            Target = None;
                        }

                        Notify();
                    }

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
