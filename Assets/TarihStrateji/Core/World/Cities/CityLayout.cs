using System;
using System.Collections.Generic;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.World.Cities
{
    /// <summary>
    /// Sektör yerleşim kuralları. Merkez her halka yuvasına, her halka yuvası iki yanındakine
    /// bitişiktir. Bir şehir merkezden başlayıp <c>facing</c> yönünden saat yönünün tersine
    /// ardışık halka yuvalarını kullanır; böylece 2–6 sektör her zaman bitişik bir küme olur.
    /// </summary>
    public static class CityLayout
    {
        public const int MinSectors = 2;
        public const int MaxSectors = 6;

        public static SectorSlot RingSlot(HexDirection direction)
        {
            return (SectorSlot)((int)direction + 1);
        }

        public static bool TryGetDirection(SectorSlot slot, out HexDirection direction)
        {
            if (slot == SectorSlot.Center)
            {
                direction = HexDirection.East;
                return false;
            }

            direction = (HexDirection)((int)slot - 1);
            return true;
        }

        /// <summary>Kademe için yuva listesi: [Merkez, facing, facing+1, …].</summary>
        public static SectorSlot[] SlotsFor(CityTier tier, HexDirection facing)
        {
            int count = (int)tier;
            if (count < MinSectors || count > MaxSectors)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), WorldErrorKeys.SehirSektorSayisi);
            }

            var slots = new SectorSlot[count];
            slots[0] = SectorSlot.Center;
            for (int i = 1; i < count; i++)
            {
                slots[i] = RingSlot(HexDirections.RotateCounterClockwise(facing, i - 1));
            }

            return slots;
        }

        public static bool AreAdjacent(SectorSlot a, SectorSlot b)
        {
            if (a == b)
            {
                return false;
            }

            if (a == SectorSlot.Center || b == SectorSlot.Center)
            {
                return true;
            }

            int diff = Math.Abs((int)a - (int)b);
            return diff == 1 || diff == 5;
        }

        /// <summary>Yuvalar benzersiz ve bitişiklik grafında tek bileşen mi.</summary>
        public static bool IsConnected(IReadOnlyList<SectorSlot> slots)
        {
            if (slots == null || slots.Count == 0)
            {
                return false;
            }

            int mask = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                int bit = 1 << (int)slots[i];
                if ((mask & bit) != 0)
                {
                    return false;
                }

                mask |= bit;
            }

            int reached = 1 << (int)slots[0];
            bool grew = true;
            while (grew)
            {
                grew = false;
                for (int i = 0; i < slots.Count; i++)
                {
                    int bit = 1 << (int)slots[i];
                    if ((reached & bit) != 0)
                    {
                        continue;
                    }

                    for (int j = 0; j < slots.Count; j++)
                    {
                        if ((reached & (1 << (int)slots[j])) != 0 && AreAdjacent(slots[i], slots[j]))
                        {
                            reached |= bit;
                            grew = true;
                            break;
                        }
                    }
                }
            }

            return reached == mask;
        }

        /// <summary>
        /// <paramref name="side"/> yönünden (şehrin o yöndeki komşusundan) gelen bir saldırının
        /// ilk gireceği yuva: o yöne bakan halka yuvası; yoksa dönme mesafesi en yakın halka
        /// yuvası (eşitlikte saat yönünün tersi önce); halka yoksa merkez.
        /// </summary>
        public static SectorSlot EntrySlotFrom(HexDirection side, IReadOnlyList<SectorSlot> slots)
        {
            for (int distance = 0; distance <= 3; distance++)
            {
                SectorSlot ccw = RingSlot(HexDirections.RotateCounterClockwise(side, distance));
                if (Contains(slots, ccw))
                {
                    return ccw;
                }

                SectorSlot cw = RingSlot(HexDirections.RotateCounterClockwise(side, -distance));
                if (Contains(slots, cw))
                {
                    return cw;
                }
            }

            return SectorSlot.Center;
        }

        private static bool Contains(IReadOnlyList<SectorSlot> slots, SectorSlot slot)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == slot)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
