namespace QuickCashJobAPI.Helpers
{
    using System;
    using System.Text;

    public static class ApiHelper
    {
        public static string GetEncodedApiKey(string apiKey)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey + ":"));
        }
    }

}
