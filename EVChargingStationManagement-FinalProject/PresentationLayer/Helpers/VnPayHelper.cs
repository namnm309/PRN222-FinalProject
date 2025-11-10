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
			// VNPay requires: 
			// 1. Sort parameters by key alphabetically (already sorted by SortedDictionary) ✅
			// 2. Build string: key1=value1&key2=value2
			// 3. URL encode ALL values, replace %20 with + (VNPay requirement)
			// 4. Exclude vnp_SecureHash and vnp_SecureHashType ✅
			// 5. Exclude empty/null values ✅
			// 6. Use UTF-8 encoding ✅
			// 7. Hash with HMACSHA512 ✅
			// 8. NO "?" in query string, only "&" and "=" ✅
			var queryString = new StringBuilder();
			foreach (var kv in vnpParams)
			{
				// Skip secure hash fields and empty values
				if (kv.Key == "vnp_SecureHash" || kv.Key == "vnp_SecureHashType" || string.IsNullOrEmpty(kv.Value))
					continue;
				
				if (queryString.Length > 0)
					queryString.Append("&"); // Use "&" not "?"
				
				// URL encode value and replace %20 with + (VNPay requirement)
				var encodedValue = WebUtility.UrlEncode(kv.Value).Replace("%20", "+");
				queryString.Append($"{kv.Key}={encodedValue}");
			}

			var data = queryString.ToString();
			
			// Hash with HMACSHA512 using UTF-8
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


