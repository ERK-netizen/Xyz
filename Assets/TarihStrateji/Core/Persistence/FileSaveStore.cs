using System;
using System.Collections.Generic;
using System.IO;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;

namespace TarihStrateji.Core.Persistence
{
    /// <summary>
    /// Kayıt yuvaları (CLAUDE.md §2.6, save-systems). Yazım atomiktir:
    /// <c>yuva.tsk.tmp</c> yazılır ve diske zorlanır (<c>Flush(true)</c> = fsync), mevcut kayıt
    /// <c>yuva.tsk.bak</c> olur, geçici dosya yerine taşınır. Yükleme sırası: ana dosya → (ana
    /// yoksa veya bozuksa) tam yazılmış geçici dosya → yedek. Daha yeni sürümlü bir ana dosya
    /// eski yedeğe sessizce düşmeden reddedilir.
    /// </summary>
    public sealed class FileSaveStore
    {
        public const string Extension = ".tsk";
        public const int MaxSlotNameLength = 40;

        private readonly string _directory;
        private readonly SaveMigrationChain _migrations;

        public FileSaveStore(string directory, SaveMigrationChain migrations = null)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Kayıt klasörü gerekli.", nameof(directory));
            }

            _directory = directory;
            _migrations = migrations ?? new SaveMigrationChain();
        }

        public string Directory => _directory;

        /// <summary>Yuva adı: 1–40 karakter, küçük ASCII harf, rakam, '_' veya '-'.</summary>
        public static bool IsValidSlotName(string slot)
        {
            if (string.IsNullOrEmpty(slot) || slot.Length > MaxSlotNameLength)
            {
                return false;
            }

            for (int i = 0; i < slot.Length; i++)
            {
                char c = slot[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }

        public ValidationResult Save(string slot, SaveMetadata metadata, byte[] payload)
        {
            if (!IsValidSlotName(slot))
            {
                return ValidationResult.Fail(PersistenceErrorKeys.YuvaAdiGecersiz);
            }

            byte[] file = SaveFileFormat.Encode(metadata, payload);
            string path = PathOf(slot);
            string tmp = path + ".tmp";
            string bak = path + ".bak";
            try
            {
                System.IO.Directory.CreateDirectory(_directory);
                using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(file, 0, file.Length);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    if (File.Exists(bak))
                    {
                        File.Delete(bak);
                    }

                    File.Move(path, bak);
                }

                File.Move(tmp, path);
                return ValidationResult.Ok;
            }
            catch (IOException)
            {
                return ValidationResult.Fail(PersistenceErrorKeys.Yazilamadi);
            }
            catch (UnauthorizedAccessException)
            {
                return ValidationResult.Fail(PersistenceErrorKeys.Yazilamadi);
            }
        }

        /// <summary>
        /// Yuvayı yükler. <paramref name="accept"/> verilirse (ör. durumun çözülüp değişmezlerden
        /// geçmesi) reddettiği aday atlanır ve sıradaki denenir.
        /// </summary>
        public SaveLoadResult Load(string slot, Func<SaveFile, bool> accept = null)
        {
            if (!IsValidSlotName(slot))
            {
                return new SaveLoadResult(SaveLoadStatus.NotFound, null, PersistenceErrorKeys.YuvaAdiGecersiz);
            }

            string path = PathOf(slot);
            string[] candidates = { path, path + ".tmp", path + ".bak" };
            SaveLoadStatus[] statuses = { SaveLoadStatus.Loaded, SaveLoadStatus.RecoveredFromTemp, SaveLoadStatus.RecoveredFromBackup };
            bool anyExisted = false;
            string lastError = PersistenceErrorKeys.Bulunamadi;
            for (int i = 0; i < candidates.Length; i++)
            {
                byte[] bytes;
                try
                {
                    if (!File.Exists(candidates[i]))
                    {
                        continue;
                    }

                    anyExisted = true;
                    bytes = File.ReadAllBytes(candidates[i]);
                }
                catch (IOException)
                {
                    lastError = PersistenceErrorKeys.Okunamadi;
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    lastError = PersistenceErrorKeys.Okunamadi;
                    continue;
                }

                Result<SaveFile> decoded = SaveFileFormat.Decode(bytes, _migrations);
                if (!decoded.IsSuccess)
                {
                    if (i == 0 && decoded.ErrorKey == PersistenceErrorKeys.SurumDahaYeni)
                    {
                        return new SaveLoadResult(SaveLoadStatus.TooNew, null, decoded.ErrorKey);
                    }

                    lastError = decoded.ErrorKey;
                    continue;
                }

                if (accept != null && !accept(decoded.Value))
                {
                    lastError = PersistenceErrorKeys.IcerikReddedildi;
                    continue;
                }

                return new SaveLoadResult(statuses[i], decoded.Value, null);
            }

            return anyExisted
                ? new SaveLoadResult(SaveLoadStatus.Corrupt, null, lastError)
                : new SaveLoadResult(SaveLoadStatus.NotFound, null, PersistenceErrorKeys.Bulunamadi);
        }

        /// <summary>Klasördeki yuva adları (sıralı karşılaştırmayla sıralı).</summary>
        public List<string> ListSlots()
        {
            var slots = new List<string>();
            if (!System.IO.Directory.Exists(_directory))
            {
                return slots;
            }

            string[] files = System.IO.Directory.GetFiles(_directory, "*" + Extension);
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileName(files[i]);
                if (name.EndsWith(Extension, StringComparison.Ordinal))
                {
                    string slot = name.Substring(0, name.Length - Extension.Length);
                    if (IsValidSlotName(slot))
                    {
                        slots.Add(slot);
                    }
                }
            }

            slots.Sort(StringComparer.Ordinal);
            return slots;
        }

        public bool Delete(string slot)
        {
            if (!IsValidSlotName(slot))
            {
                return false;
            }

            string path = PathOf(slot);
            bool any = false;
            string[] files = { path, path + ".tmp", path + ".bak" };
            for (int i = 0; i < files.Length; i++)
            {
                if (File.Exists(files[i]))
                {
                    File.Delete(files[i]);
                    any = true;
                }
            }

            return any;
        }

        public string PathOf(string slot)
        {
            return Path.Combine(_directory, slot + Extension);
        }
    }
}
