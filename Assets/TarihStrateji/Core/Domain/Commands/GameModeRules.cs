namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Mod ayrımının tek doğrulama noktası (CLAUDE.md §2.6). Her emir, alan doğrulamasından
    /// önce buradan geçer.
    /// </summary>
    public static class GameModeRules
    {
        public static ValidationResult CheckAuthority(GameMode mode, GameCommand command)
        {
            if (command == null)
            {
                return ValidationResult.Fail(DomainErrorKeys.EmirBos);
            }

            switch (mode)
            {
                case GameMode.Observer:
                    return ValidationResult.Fail(DomainErrorKeys.GozlemciEmirVeremez);

                case GameMode.Normal:
                    if (command.Authority != CommandAuthority.Player)
                    {
                        return ValidationResult.Fail(DomainErrorKeys.HileNormalModda);
                    }

                    break;

                case GameMode.Sandbox:
                    break;

                default:
                    return ValidationResult.Fail(DomainErrorKeys.ModBilinmiyor);
            }

            if (command.Authority == CommandAuthority.Player && !command.Issuer.IsValid)
            {
                return ValidationResult.Fail(DomainErrorKeys.EmirSahibiGecersiz);
            }

            return ValidationResult.Ok;
        }
    }
}
