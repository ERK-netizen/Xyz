using System.Text;

namespace TarihStrateji.Core.Tests.Support
{
    /// <summary>
    /// Kaynak taraması için küçük bir C# sözcük ayıklayıcı: yorumları ve metin/karakter
    /// literallerini boşlukla değiştirir, satır sonlarını korur. Yasak sözcük testleri
    /// böylece yorum ve metinlerdeki kelimelerden yanlış alarm almaz.
    /// </summary>
    public static class CSharpSourceScanner
    {
        public static string StripCommentsAndStrings(string source)
        {
            var sb = new StringBuilder(source.Length);
            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';

                if (c == '/' && next == '/')
                {
                    while (i < source.Length && source[i] != '\n')
                    {
                        sb.Append(' ');
                        i++;
                    }

                    continue;
                }

                if (c == '/' && next == '*')
                {
                    i += 2;
                    sb.Append("  ");
                    while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                    {
                        sb.Append(source[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    i = System.Math.Min(i + 2, source.Length);
                    sb.Append("  ");
                    continue;
                }

                bool verbatim = (c == '@' && next == '"') || (c == '$' && next == '@') || (c == '@' && next == '$');
                if (verbatim)
                {
                    while (i < source.Length && source[i] != '"')
                    {
                        sb.Append(' ');
                        i++;
                    }

                    i++;
                    sb.Append(' ');
                    while (i < source.Length)
                    {
                        if (source[i] == '"' && i + 1 < source.Length && source[i + 1] == '"')
                        {
                            sb.Append("  ");
                            i += 2;
                            continue;
                        }

                        if (source[i] == '"')
                        {
                            sb.Append(' ');
                            i++;
                            break;
                        }

                        sb.Append(source[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    continue;
                }

                if (c == '"' || c == '\'' || (c == '$' && next == '"'))
                {
                    if (c == '$')
                    {
                        sb.Append(' ');
                        i++;
                    }

                    char quote = source[i];
                    sb.Append(' ');
                    i++;
                    while (i < source.Length && source[i] != quote && source[i] != '\n')
                    {
                        if (source[i] == '\\' && i + 1 < source.Length)
                        {
                            sb.Append("  ");
                            i += 2;
                            continue;
                        }

                        sb.Append(' ');
                        i++;
                    }

                    if (i < source.Length && source[i] == quote)
                    {
                        sb.Append(' ');
                        i++;
                    }

                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }
    }
}
