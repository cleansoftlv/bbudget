using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Services.Auth;

namespace LMFunctions.Extensions
{
    public class AuthMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly AuthService _authService;
        public AuthMiddleware(AuthService authService)
        {
            _authService = authService;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var httpContext = await context.GetHttpRequestDataAsync();

            int? userId = null;
            bool renewAuth = false;
            bool needsRemoveCookie = false;
            if (httpContext?.Cookies != null)
            {

                var authCookie = httpContext.Cookies.FirstOrDefault(x => x.Name == Constants.AuthCookieName);

                if (authCookie?.Value != null)
                {
                    try
                    {
                        (var principal, var expirationDate) = ValidateCookie(authCookie); // Create this method
                        if (principal != null)
                        {
                            userId = AuthService.GetUsetId(principal.Identity as ClaimsIdentity);
                            context.Items["User"] = principal;
                            context.Items["UserId"] = userId;
                            if (userId != null && _authService.NeedsRenew(expirationDate))
                            {
                                renewAuth = true;
                            }
                        }
                        else
                        {
                            needsRemoveCookie = true;
                        }
                    }
                    catch 
                    {
                        needsRemoveCookie = true;
                    }
                }
            }

            await next(context);

            if (renewAuth && userId != null && !context.Items.ContainsKey(Constants.SkipSlidingAuthKey))
            {
                var response = context.GetHttpResponseData() ?? httpContext.CreateResponse();
                var newToken = _authService.GenerateJwt(userId.Value);
                response.Cookies.Append(new HttpCookie(Constants.AuthCookieName, newToken)
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSite.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(_authService.GetCookiesExpirationMinutes())
                });
            }
            if (needsRemoveCookie && !context.Items.ContainsKey(Constants.SkipSlidingAuthKey))
            {
                var response = context.GetHttpResponseData() ?? httpContext.CreateResponse();
                response.Cookies.Append(new HttpCookie(Constants.AuthCookieName, "")
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSite.Strict,
                    Expires = DateTime.UtcNow.AddDays(-1)
                });
            }
        }

        private (ClaimsPrincipal principal, DateTime expirationDate) ValidateCookie(IHttpCookie cookie)
        {
            var token = cookie.Value;
            if (string.IsNullOrEmpty(token))
                return default;
            return _authService.ValidateJwt(token); // Call the async method synchronously
        }

    }
}
