using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace FerryBookingSystem.Helpers
{
    public static class VnPayHelper
    {
        // VNPay secret key provided by ferry company
        private const string SECRET_KEY = "0KASCDVFSAQY6BNYJHUWKBKJXM6";

        /// <summary>
        /// Validates VNPay payment signature
        /// </summary>
        /// <param name="receivedHash">The hash received from VNPay</param>
        /// <param name="vnpParams">Dictionary of VNPay parameters (excluding vnp_SecureHash)</param>
        /// <returns>True if signature is valid</returns>
        public static bool ValidateSignature(string receivedHash, Dictionary<string, string> vnpParams)
        {
            try
            {
                // Create the raw string for hashing
                var rspRaw = CreateRawString(vnpParams);
                
                // Generate hash using HMAC SHA512
                var computedHash = HmacSHA512(SECRET_KEY, rspRaw);
                
                // Compare hashes (case insensitive)
                return string.Equals(receivedHash, computedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating VNPay signature: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates payment URL for VNPay
        /// </summary>
        /// <param name="returnUrl">URL to return to after payment</param>
        /// <param name="orderInfo">Order information (booking code)</param>
        /// <param name="amount">Amount in VND</param>
        /// <param name="txnRef">Transaction reference</param>
        /// <returns>VNPay payment URL</returns>
        public static string CreatePaymentUrl(string returnUrl, string orderInfo, decimal amount, string txnRef)
        {
            try
            {
                var vnpParams = new Dictionary<string, string>
                {
                    {"vnp_Version", "2.1.0"},
                    {"vnp_Command", "pay"},
                    {"vnp_TmnCode", "YOUR_TMN_CODE"}, // Replace with actual TMN code
                    {"vnp_Amount", ((long)(amount * 100)).ToString()}, // Convert to VNPay format (add 00)
                    {"vnp_CurrCode", "VND"},
                    {"vnp_TxnRef", txnRef},
                    {"vnp_OrderInfo", orderInfo},
                    {"vnp_OrderType", "other"},
                    {"vnp_Locale", "vn"},
                    {"vnp_ReturnUrl", returnUrl},
                    {"vnp_IpAddr", GetClientIpAddress()},
                    {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")}
                };

                // Create raw string and hash
                var rspRaw = CreateRawString(vnpParams);
                var hash = HmacSHA512(SECRET_KEY, rspRaw);
                
                // Add hash to parameters
                vnpParams.Add("vnp_SecureHash", hash);

                // Build URL
                var baseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"; // Use production URL for live
                var queryString = string.Join("&", vnpParams.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
                
                return $"{baseUrl}?{queryString}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating VNPay payment URL: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates raw string from VNPay parameters for hashing
        /// </summary>
        /// <param name="vnpParams">VNPay parameters</param>
        /// <returns>Raw string for hashing</returns>
        private static string CreateRawString(Dictionary<string, string> vnpParams)
        {
            // Sort parameters by key and create query string
            var sortedParams = vnpParams
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .OrderBy(p => p.Key)
                .Select(p => $"{p.Key}={p.Value}");

            return string.Join("&", sortedParams);
        }

        /// <summary>
        /// Generates HMAC SHA512 hash
        /// </summary>
        /// <param name="key">Secret key</param>
        /// <param name="data">Data to hash</param>
        /// <returns>Hash string</returns>
        private static string HmacSHA512(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Gets client IP address
        /// </summary>
        /// <returns>Client IP address</returns>
        private static string GetClientIpAddress()
        {
            try
            {
                var context = HttpContext.Current;
                if (context?.Request != null)
                {
                    var ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    if (string.IsNullOrEmpty(ipAddress))
                    {
                        ipAddress = context.Request.ServerVariables["REMOTE_ADDR"];
                    }
                    if (string.IsNullOrEmpty(ipAddress))
                    {
                        ipAddress = context.Request.UserHostAddress;
                    }
                    return ipAddress ?? "127.0.0.1";
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// Parses VNPay payment date
        /// </summary>
        /// <param name="vnpPayDate">VNPay date string (yyyyMMddHHmm)</param>
        /// <returns>Parsed DateTime or current time if parsing fails</returns>
        public static DateTime ParsePaymentDate(string vnpPayDate)
        {
            try
            {
                if (!string.IsNullOrEmpty(vnpPayDate) && vnpPayDate.Length == 12)
                {
                    return DateTime.ParseExact(vnpPayDate, "yyyyMMddHHmm", null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing VNPay date '{vnpPayDate}': {ex.Message}");
            }
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Converts VNPay amount to decimal (removes 00 suffix)
        /// </summary>
        /// <param name="vnpAmount">VNPay amount string</param>
        /// <returns>Decimal amount in VND</returns>
        public static decimal ParseAmount(string vnpAmount)
        {
            try
            {
                if (decimal.TryParse(vnpAmount, out decimal amount))
                {
                    return amount / 100; // Remove the 00 suffix
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing VNPay amount '{vnpAmount}': {ex.Message}");
            }
            return 0;
        }
    }
}
