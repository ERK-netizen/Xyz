using System;

namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Doğrulama sonucu. Hata metni değil, Türkçe yerelleştirme anahtarı taşır
    /// (ör. <c>hata.emir.menzilDisi</c>).
    /// </summary>
    [Serializable]
    public readonly struct ValidationResult : IEquatable<ValidationResult>
    {
        public static readonly ValidationResult Ok = new ValidationResult(null);

        private readonly string _errorKey;

        private ValidationResult(string errorKey)
        {
            _errorKey = errorKey;
        }

        public bool IsValid => _errorKey == null;

        /// <summary>Geçerliyse <c>null</c>.</summary>
        public string ErrorKey => _errorKey;

        public static ValidationResult Fail(string errorKey)
        {
            if (string.IsNullOrEmpty(errorKey))
            {
                throw new ArgumentException("Hata anahtarı boş olamaz.", nameof(errorKey));
            }

            return new ValidationResult(errorKey);
        }

        public bool Equals(ValidationResult other) => string.Equals(_errorKey, other._errorKey, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ValidationResult other && Equals(other);

        public override int GetHashCode() => _errorKey == null ? 0 : StringComparer.Ordinal.GetHashCode(_errorKey);

        public override string ToString() => IsValid ? "Gecerli" : "Gecersiz(" + _errorKey + ")";
    }
}
