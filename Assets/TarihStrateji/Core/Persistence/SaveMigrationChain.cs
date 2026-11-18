using System;
using System.Collections.Generic;

namespace TarihStrateji.Core.Persistence
{
    /// <summary>Sürüm n → n+1 göçlerinin sıralı zinciri.</summary>
    public sealed class SaveMigrationChain
    {
        private readonly Dictionary<int, ISaveMigration> _migrations = new Dictionary<int, ISaveMigration>();

        public void Register(ISaveMigration migration)
        {
            if (migration == null)
            {
                throw new ArgumentNullException(nameof(migration));
            }

            if (migration.FromVersion < 1 || _migrations.ContainsKey(migration.FromVersion))
            {
                throw new ArgumentException("Göç sürümü geçersiz veya tekrar kayıtlı.", nameof(migration));
            }

            _migrations.Add(migration.FromVersion, migration);
        }

        /// <summary><paramref name="fromVersion"/>'dan <paramref name="toVersion"/>'a sırayla göç ettirir.</summary>
        public bool TryUpgrade(int fromVersion, int toVersion, ref byte[] metadata, ref byte[] payload, out string errorKey)
        {
            errorKey = null;
            for (int v = fromVersion; v < toVersion; v++)
            {
                if (!_migrations.TryGetValue(v, out ISaveMigration migration))
                {
                    errorKey = PersistenceErrorKeys.GocEksik;
                    return false;
                }

                if (!migration.TryMigrate(metadata, payload, out byte[] m, out byte[] p) || m == null || p == null)
                {
                    errorKey = PersistenceErrorKeys.GocEksik;
                    return false;
                }

                metadata = m;
                payload = p;
            }

            return true;
        }
    }
}
