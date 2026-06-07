using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
using Vdlcrm.Services;
using Vdlcrm.Model;

namespace Vdlcrm.Web.Middleware;

public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ApiLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Sirf /api/ se start hone wale requests ko log karein, swagger ya static files ko nahi
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var stopWatch = Stopwatch.StartNew();

        // 1. Request Body read karna
        context.Request.EnableBuffering(); // Stream ko multiple times read karne ke liye allow karta hai
        var requestBody = await ReadStreamInChunks(context.Request.Body);
        context.Request.Body.Position = 0; // Stream ko wapas start pe set karna zaroori hai agle middleware ke liye

        // Sensitive info (jaise passwords) ko mask karna
        if (context.Request.Path.Value != null && 
            (context.Request.Path.Value.Contains("login", StringComparison.OrdinalIgnoreCase) || 
             context.Request.Path.Value.Contains("password", StringComparison.OrdinalIgnoreCase)))
        {
            requestBody = "*** SENSITIVE DATA HIDDEN ***";
        }

        // 2. Response Body padhne ke liye original stream ko temporarily replace karna padega
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            // Baki application ko chalne do (Controller tak jayega aur wapas aayega)
            await _next(context);
        }
        finally
        {
            stopWatch.Stop();

            // 3. Response Body read karna
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            // 4. Database me log save karna
            var userId = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
            var apiLog = new ApiLog
            {
                Method = context.Request.Method,
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                RequestBody = requestBody,
                ResponseBody = responseBody,
                StatusCode = context.Response.StatusCode,
                ExecutionTimeMs = stopWatch.ElapsedMilliseconds,
                UserId = userId
            };

            try
            {
                string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);
                
                string logFilePath = Path.Combine(logDirectory, $"api_logs_{DateTime.UtcNow:yyyyMMdd}.txt");
                string jsonLogEntry = JsonSerializer.Serialize(apiLog) + Environment.NewLine;
                await File.AppendAllTextAsync(logFilePath, jsonLogEntry);
            }
            catch { /* Agar log write hone me error aaye to API fail nahi honi chahiye */ }

            // Original stream me response wapas daalna taaki client ko mil sake
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> ReadStreamInChunks(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
        return await reader.ReadToEndAsync();
    }
}