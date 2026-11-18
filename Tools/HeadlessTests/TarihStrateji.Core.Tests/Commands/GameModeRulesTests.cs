using System;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Tests.Commands
{
    [TestFixture]
    public class GameModeRulesTests
    {
        private sealed class PlayerOrder : GameCommand
        {
            public PlayerOrder(CountryId issuer) : base(issuer) { }

            public override CommandAuthority Authority => CommandAuthority.Player;

            public override string Kind => "emir.test.oyuncu";
        }

        private sealed class CheatOrder : GameCommand
        {
            public CheatOrder() : base(CountryId.None) { }

            public override CommandAuthority Authority => CommandAuthority.Sandbox;

            public override string Kind => "emir.test.hile";
        }

        private static readonly CountryId Turkey = new CountryId(1);

        [Test]
        public void Normal_AcceptsPlayerOrders_RejectsCheats()
        {
            Assert.That(GameModeRules.CheckAuthority(GameMode.Normal, new PlayerOrder(Turkey)).IsValid, Is.True);
            ValidationResult cheat = GameModeRules.CheckAuthority(GameMode.Normal, new CheatOrder());
            Assert.That(cheat.IsValid, Is.False);
            Assert.That(cheat.ErrorKey, Is.EqualTo(DomainErrorKeys.HileNormalModda));
        }

        [Test]
        public void Sandbox_AcceptsBoth()
        {
            Assert.That(GameModeRules.CheckAuthority(GameMode.Sandbox, new PlayerOrder(Turkey)).IsValid, Is.True);
            Assert.That(GameModeRules.CheckAuthority(GameMode.Sandbox, new CheatOrder()).IsValid, Is.True);
        }

        [Test]
        public void Observer_RejectsEverything()
        {
            Assert.That(GameModeRules.CheckAuthority(GameMode.Observer, new PlayerOrder(Turkey)).ErrorKey,
                Is.EqualTo(DomainErrorKeys.GozlemciEmirVeremez));
            Assert.That(GameModeRules.CheckAuthority(GameMode.Observer, new CheatOrder()).ErrorKey,
                Is.EqualTo(DomainErrorKeys.GozlemciEmirVeremez));
        }

        [Test]
        public void PlayerOrder_WithoutIssuer_IsRejected_AndNullOrder()
        {
            Assert.That(GameModeRules.CheckAuthority(GameMode.Normal, new PlayerOrder(CountryId.None)).ErrorKey,
                Is.EqualTo(DomainErrorKeys.EmirSahibiGecersiz));
            Assert.That(GameModeRules.CheckAuthority(GameMode.Normal, null).ErrorKey, Is.EqualTo(DomainErrorKeys.EmirBos));
            Assert.That(GameModeRules.CheckAuthority((GameMode)99, new PlayerOrder(Turkey)).ErrorKey,
                Is.EqualTo(DomainErrorKeys.ModBilinmiyor));
        }

        [Test]
        public void ValidationResult_And_Receipt_Contracts()
        {
            Assert.That(ValidationResult.Ok.IsValid, Is.True);
            Assert.That(ValidationResult.Ok.ErrorKey, Is.Null);
            Assert.Throws<ArgumentException>(() => ValidationResult.Fail(""));
            Assert.That(ValidationResult.Fail("hata.x"), Is.EqualTo(ValidationResult.Fail("hata.x")));

            GameDate day = GameDate.FromYmd(1920, 1, 1);
            CommandReceipt accepted = CommandReceipt.Accept(new CommandId(9), day);
            Assert.That(accepted.Accepted && accepted.CommandId.Value == 9 && accepted.Date == day, Is.True);
            CommandReceipt rejected = CommandReceipt.Reject(ValidationResult.Fail("hata.x"), day);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.CommandId, Is.EqualTo(CommandId.None));
            Assert.Throws<ArgumentException>(() => CommandReceipt.Accept(CommandId.None, day));
            Assert.Throws<ArgumentException>(() => CommandReceipt.Reject(ValidationResult.Ok, day));
        }
    }
}
