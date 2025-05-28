using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace FerryBookingSystem.Services
{
    public static class VnPayHelper
    {
        private const string SecretKey = "0KASCDVFSAQY6BNYJHUWKBKJXM6"; // Secret key from VNPAY

        public static bool ValidateSignature(string inputHash, Dictionary<string, string> requestData)
        {
            // Remove signature and sort data
            requestData.Remove("vnp_SecureHash");

            // Create raw data string from sorted parameters
            var queryBuilder = new StringBuilder();

            // This is the modified part - use Key and Value properties directly
            foreach (var item in requestData.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                string key = item.Key;
                string value = item.Value;

                if (!string.IsNullOrEmpty(value))
                {
                    queryBuilder.Append(key);
                    queryBuilder.Append('=');
                    queryBuilder.Append(HttpUtility.UrlEncode(value));
                    queryBuilder.Append('&');
                }
            }

            // Remove the last '&'
            string rspRaw = queryBuilder.ToString().TrimEnd('&');

            // Calculate HMAC-SHA512
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            byte[] inputBytes = Encoding.UTF8.GetBytes(rspRaw);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var b in hashValue)
                {
                    hash.Append(b.ToString("x2"));
                }
            }

            // Compare calculated hash with received hash
            return string.Equals(inputHash, hash.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}