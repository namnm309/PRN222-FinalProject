using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace PresentationLayer.Helpers
{
	public static class VnPayHelper
	{
		public static SortedDictionary<string, string> ExtractParams(IQueryCollection query)
		{
			var dict = new SortedDictionary<string, string>();
			foreach (var kv in query)
			{
				if (string.IsNullOrWhiteSpace(kv.Value)) continue;
				dict[kv.Key] = kv.Value.ToString();
			}
			// remove secure fields for hashing
			dict.Remove("vnp_SecureHash");
			dict.Remove("vnp_SecureHashType");
			return dict;
		}

		public static string BuildHash(SortedDictionary<string, string> vnpParams, string hashSecret)
		{
			var data = string.Join("&", vnpParams
				.Where(kv => !string.IsNullOrEmpty(kv.Value))
				.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

			using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret));
			var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
			return string.Concat(hashBytes.Select(b => b.ToString("x2")));
		}

		public static string BuildPaymentUrl(string baseUrl, SortedDictionary<string, string> vnpParams)
		{
			var query = string.Join("&", vnpParams
				.Where(kv => !string.IsNullOrEmpty(kv.Value))
				.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

			return $"{baseUrl}?{query}";
		}

		public static bool VerifyHash(IQueryCollection query, string hashSecret)
		{
			var receivedHash = query["vnp_SecureHash"].ToString();
			var dataParams = ExtractParams(query);
			var calculated = BuildHash(dataParams, hashSecret);
			return string.Equals(receivedHash, calculated, StringComparison.OrdinalIgnoreCase);
		}
	}
}


