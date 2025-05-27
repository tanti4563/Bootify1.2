using System.Configuration;

namespace FerryBookingSystem.Models
{
    public static class ApiConfig
    {
        public static string BaseUrl => ConfigurationManager.AppSettings["ApiBaseUrl"];
        public static string AuthorizationHeader => ConfigurationManager.AppSettings["ApiAuthHeader"];
        public static string ApiKey => ConfigurationManager.AppSettings["ApiKey"];
        public static string SecretKey => ConfigurationManager.AppSettings["SecretKey"];
    }
}