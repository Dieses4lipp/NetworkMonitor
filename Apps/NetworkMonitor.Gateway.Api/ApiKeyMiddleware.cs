using System.Security.Cryptography;
using System.Text;

namespace NetworkMonitor.Gateway.Api
{
    public class ApiKeyMiddleware
    {
        public const string HeaderName = "X-Api-Key";

        private readonly RequestDelegate _next;
        private readonly byte[] _expectedKey;

        public ApiKeyMiddleware(RequestDelegate next, string apiKey)
        {
            _next = next;
            _expectedKey = Encoding.UTF8.GetBytes(apiKey);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(HeaderName, out var provided)
                || !IsValid(provided.ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing or invalid API key.");
                return;
            }

            await _next(context);
        }

        private bool IsValid(string provided)
        {
            var providedBytes = Encoding.UTF8.GetBytes(provided);
            return CryptographicOperations.FixedTimeEquals(providedBytes, _expectedKey);
        }
    }
}
