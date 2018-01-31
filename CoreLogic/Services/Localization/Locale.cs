using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.CoreLogic.Services.Localization {

	// TODO: replace with RegionInfo?
	public sealed class Locale {

		public static readonly Locale EN = new Locale() { Code = "en", Native = "English"};
		public static readonly Locale RU = new Locale() { Code = "ru", Native = "Русский" };

		public string Code { get; private set; }
		public string Native { get; private set; }
	}
}
