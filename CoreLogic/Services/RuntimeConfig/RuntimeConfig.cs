﻿using FluentValidation;
using System.Numerics;

namespace Goldmint.CoreLogic.Services.RuntimeConfig {

	public sealed class RuntimeConfig {

		public GoldSection Gold { get; set; } = new GoldSection();
		public EthereumSection Ethereum { get; set; } = new EthereumSection();
		public string Stamp { get; set; } = "default";

        public static IValidator<RuntimeConfig> GetValidator() {
			var v = new InlineValidator<RuntimeConfig>() { CascadeMode = CascadeMode.Continue };
			v.RuleFor(_ => _.Gold).NotNull().SetValidator(GoldSection.GetValidator());
			v.RuleFor(_ => _.Ethereum).NotNull().SetValidator(EthereumSection.GetValidator());
			return v;
		}

		// ---

		public class GoldSection {

			public bool AllowTradingOverall { get; set; } = true;
			public bool AllowTradingEth { get; set; } = true;
			public bool AllowBuyingCreditCard { get; set; } = true;
			public bool AllowSellingCreditCard { get; set; } = true;
			public SafeRateSection SafeRate { get; set; } = new SafeRateSection();
			public PaymentMehtodsSection PaymentMehtods { get; set; } = new PaymentMehtodsSection();

			public static IValidator<GoldSection> GetValidator() {
				var v = new InlineValidator<GoldSection>() { CascadeMode = CascadeMode.Continue };
				v.RuleFor(_ => _.SafeRate).NotNull().SetValidator(SafeRateSection.GetValidator());
				v.RuleFor(_ => _.PaymentMehtods).NotNull().SetValidator(PaymentMehtodsSection.GetValidator());
				return v;
			}

			public class SafeRateSection {

				public AssetSection Gold { get; set; } = new AssetSection();
				public AssetSection Eth { get; set; } = new AssetSection();

				public static IValidator<SafeRateSection> GetValidator() {
					var v = new InlineValidator<SafeRateSection>() { CascadeMode = CascadeMode.Continue };
					v.RuleFor(_ => _.Gold).NotNull().SetValidator(AssetSection.GetValidator());
					v.RuleFor(_ => _.Eth).NotNull().SetValidator(AssetSection.GetValidator());
					return v;
				}

				// ---

				public class AssetSection {

					public double BuyEthGoldChangeThreshold { get; set; } = 0.15d;
					public double SellEthGoldChangeThreshold { get; set; } = 0.15d;

					public static IValidator<AssetSection> GetValidator() {
						var v = new InlineValidator<AssetSection>() { CascadeMode = CascadeMode.Continue };
						v.RuleFor(_ => _.BuyEthGoldChangeThreshold).GreaterThan(0);
						v.RuleFor(_ => _.SellEthGoldChangeThreshold).GreaterThan(0);
						return v;
					}
				}
			}

			public class PaymentMehtodsSection {

				public double EthDepositMinEther { get; set; } = 0.01d;
				public double EthDepositMaxEther { get; set; } = 1000d;
				public double EthWithdrawMinEther { get; set; } = 0.01d;
				public double EthWithdrawMaxEther { get; set; } = 5d;

				public double CreditCardDepositMinUsd { get; set; } = 10.00d;
				public double CreditCardDepositMaxUsd { get; set; } = 1000.00d;
				public double CreditCardWithdrawMinUsd { get; set; } = 10.00d;
				public double CreditCardWithdrawMaxUsd { get; set; } = 1000.00d;

				public static IValidator<PaymentMehtodsSection> GetValidator() {
					var v = new InlineValidator<PaymentMehtodsSection>() { CascadeMode = CascadeMode.Continue };

					v.RuleFor(_ => _.EthDepositMinEther).GreaterThan(0);
					v.RuleFor(_ => _.EthDepositMaxEther).GreaterThan(0);
					v.RuleFor(_ => _.EthWithdrawMinEther).GreaterThan(0);
					v.RuleFor(_ => _.EthWithdrawMaxEther).GreaterThan(0);

					v.RuleFor(_ => _.CreditCardDepositMinUsd).GreaterThan(0);
					v.RuleFor(_ => _.CreditCardDepositMaxUsd).GreaterThan(0);
					v.RuleFor(_ => _.CreditCardWithdrawMinUsd).GreaterThan(0);
					v.RuleFor(_ => _.CreditCardWithdrawMinUsd).GreaterThan(0);

					return v;
				}
			}
		}

		public class EthereumSection {

			public long Gas { get; set; } = 250000;
			public string HarvestFromBlock { get; set; } = "0";

			public static IValidator<EthereumSection> GetValidator() {
				var v = new InlineValidator<EthereumSection>() { CascadeMode = CascadeMode.Continue };
				v.RuleFor(_ => _.Gas).GreaterThanOrEqualTo(100000);
				v.RuleFor(_ => _.HarvestFromBlock).NotEmpty().Must(_ => BigInteger.TryParse(_, out var _));
				return v;
			}
		}
	}
}
