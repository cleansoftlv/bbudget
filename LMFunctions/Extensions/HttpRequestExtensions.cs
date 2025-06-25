using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker;
using Services.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LMFunctions.Extensions
{
    public static class HttpRequestExtensions
    {
        public static int? GetUserId(this FunctionContext context)
        {
            if (context.Items.TryGetValue("UserId", out var userId))
            {
                if (userId is int id)
                {
                    return id;
                }
            }

            return null;
        }

        public static void AddNewAuthCookie(this HttpRequest req,
            int userId,
            AuthService authService,
            FunctionContext fcontext)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(authService.GetCookiesExpirationMinutes())
            };
            req.HttpContext.Response.Cookies.Append(
                Constants.AuthCookieName,
                authService.GenerateJwt(userId),
                cookieOptions
            );
            fcontext.Items[Constants.SkipSlidingAuthKey] = true;
        }

        public static void RemoveAuthCookie(this HttpRequest req,
            AuthService authService,
            FunctionContext fcontext)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromSeconds(0)
            };
            req.HttpContext.Response.Cookies.Append(
                Constants.AuthCookieName,
                "_",
                cookieOptions
            );
            fcontext.Items[Constants.SkipSlidingAuthKey] = true;
        }

        public static SsoId GetStaticAppAuthPrinciple(this HttpRequest req, AuthService authService)
        {
            if (!req.Headers.TryGetValue("x-ms-client-principal", out var header))
                return null;

            var headerData = header[0];
            var principal = authService.GetStaticWebAppPrincipal(headerData);
            if (principal == null)
            {
                return null;
            }

            return authService.GetStaticWebAppUserSso(principal);
        }

        public static string GetClientIp(this FunctionContext context, HttpRequest req)
        {
            if (context.Items.TryGetValue("ClientIp", out var ip))
            {
                return (string)ip;
            }

            var ipStr = DoGetIp(context, req) ?? "";
            context.Items["ClientId"] = ipStr;
            return ipStr;
        }

        private static string DoGetIp(FunctionContext fcontext, HttpRequest req)
        {
            var hdr = (string)fcontext.BindingContext.BindingData["Headers"];
            if (hdr is null)
            {
                return null;
            }

            var xff = JsonSerializer.Deserialize<XffHeader>(hdr.ToString(), _serializerOptions);
            if (xff?.Value != null)
            {
                var ipStr = GetClientIpFromXForwardedFor(xff.Value);
                fcontext.Items["ClientIp"] = ipStr;
                return ipStr;
            }

            var forwardedFor = req.Headers["X-Forwarded-For"];
            if (forwardedFor.Count > 0)
            {
                var ipStr = GetClientIpFromXForwardedFor(forwardedFor[0]);
                fcontext.Items["ClientIp"] = ipStr;
                return ipStr;
            }
            return req.HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private static string GetClientIpFromXForwardedFor(string xff)
        {
            //Skips right most ip, it will be from static web apps proxy
            //uses spans to avoid allocations
            //takes last ip from the list
            var span = xff.AsSpan().Trim(',');
            var sepIndex = span.LastIndexOf(',');
            if (sepIndex >= 0)
            {
                span = span.Slice(0, sepIndex);
                sepIndex = span.LastIndexOf(',');
                if (sepIndex >= 0)
                {
                    span = span.Slice(sepIndex + 1);
                }
            }
            span = span.Trim().Trim('[').Trim(']');
            sepIndex = span.LastIndexOf(':');
            if (sepIndex >= 0)
            {
                int cnt = 0;
                for (int i = 0; i < span.Length; i++)
                {
                    if (span[i] == ':')
                        cnt++;
                }
                if (cnt == 1/*Ip v4 with port*/ || cnt == 8/*Ip v6 with port*/)
                {
                    span = span.Slice(0, sepIndex);
                }
            }
            return span.ToString();
        }

        private record XffHeader([property: JsonPropertyName("X-Forwarded-For")] string Value);
        private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    }
}
