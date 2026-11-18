using System;

namespace TarihStrateji.Core.Domain
{
    /// <summary>
    /// Başarı değeri veya Türkçe hata anahtarı taşıyan açık sonuç tipi (CLAUDE.md §4).
    /// Kural kodunda <c>null</c> ve istisna yerine beklenen hatalar için kullanılır.
    /// </summary>
    public readonly struct Result<T>
    {
        private readonly T _value;
        private readonly string _errorKey;

        private Result(T value, string errorKey)
        {
            _value = value;
            _errorKey = errorKey;
        }

        public bool IsSuccess => _errorKey == null;

        /// <summary>Başarılıysa <c>null</c>.</summary>
        public string ErrorKey => _errorKey;

        /// <summary>Başarısız sonuçta okunursa hata fırlatır.</summary>
        public T Value
        {
            get
            {
                if (_errorKey != null)
                {
                    throw new InvalidOperationException("Başarısız sonucun değeri okunamaz: " + _errorKey);
                }

                return _value;
            }
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(value, null);
        }

        public static Result<T> Failure(string errorKey)
        {
            if (string.IsNullOrEmpty(errorKey))
            {
                throw new ArgumentException("Hata anahtarı boş olamaz.", nameof(errorKey));
            }

            return new Result<T>(default, errorKey);
        }

        public bool TryGetValue(out T value)
        {
            value = _value;
            return _errorKey == null;
        }

        public override string ToString() => IsSuccess ? "Basarili(" + _value + ")" : "Basarisiz(" + _errorKey + ")";
    }
}
