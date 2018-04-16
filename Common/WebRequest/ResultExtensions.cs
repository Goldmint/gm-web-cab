﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Goldmint.Common.WebRequest {

	public static class ResultExtensions {

		public static async Task<bool> ToJson(this Result res, Dictionary<string, string> into) {
			var raw = await res.ToRawString();
			return Json.ParseInto(raw, into);
		}

		public static async Task<TJsonHolder> ToJson<TJsonHolder>(this Result res) where TJsonHolder : class {
			var raw = await res.ToRawString();
			return Json.Parse<TJsonHolder>(raw);
		}
	}
}
