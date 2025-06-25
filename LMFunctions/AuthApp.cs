using FunctionCommon;
using LMFunctions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services.Auth;
using Services.Helpers;
using Shared;
using Shared.Login;
using System.Text.Json;

namespace LMFunctions
{
    public class AuthApp
    {
        private readonly AuthService _authService;
        private readonly Throttler _throttler;

        public AuthApp(
            AuthService authService,
            Throttler throttler)
        {
            _authService = authService;
            _throttler = throttler;
        }

        [Function("Auth_Me")]
        public async Task<IActionResult> Me(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "auth/me")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }
            var authUserInfo = await _authService.GetUserAuthInfoUpdateActivity(userId.Value);
            if (authUserInfo == null)
            {
                req.RemoveAuthCookie(_authService, fcontext);
                return new ObjectResultWithStatus(StatusCodes.Status404NotFound, new ApiError
                {
                    error = "user not found",
                });
            }
            return new OkObjectResult(authUserInfo);
        }

        [Function("Auth_CheckStaticAppAuth")]
        public IActionResult CheckStaticAppAuth(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/staticauthcheck")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            var ssoId = req.GetStaticAppAuthPrinciple(_authService);
            if (ssoId == null)
            {
                return new ObjectResult(new ApiError
                {
                    error = "user is not authenticated",
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
            else
            {
                return new OkObjectResult(new OkOnly());
            }
        }

        [Function("Auth_Login")]
        public async Task<IActionResult> Login(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            var ssoId = req.GetStaticAppAuthPrinciple(_authService);
            if (ssoId == null)
            {
                return new ObjectResult(new ApiError
                {
                    error = "user is not authenticated",
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
            var userId = await _authService.GetUserIdBySsoId(ssoId);
            if (userId == null)
            {
                userId = await _authService.CreateNewUserWithSso(ssoId);
            }
            req.AddNewAuthCookie(userId.Value, _authService, fcontext);
            var authUserInfo = await _authService.GetUserAuthInfoUpdateActivity(userId.Value);
            return new OkObjectResult(authUserInfo);
        }

        [Function("Auth_AddSso")]
        public async Task<IActionResult> AddSso(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/addsso")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }

            var ip = fcontext.GetClientIp(req);
            if (!_throttler.TryRegisterRequest($"LmTokenLogin#{ip}", 10)
                || !_throttler.TryRegisterRequest($"LmTokenLogin#u{userId}", 10))
            {
                return new ObjectResultWithStatus(StatusCodes.Status429TooManyRequests, new ApiError
                {
                    error = "too many requests",
                });
            }

            var ssoId = req.GetStaticAppAuthPrinciple(_authService);
            if (ssoId == null)
            {
                return new ObjectResult(new ApiError
                {
                    error = "user is not authenticated",
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
            var addRes = await _authService.AddSso(userId.Value, ssoId);
            if (addRes.result == SsoAddResult.Error)
            {
                return new ObjectResultWithStatus(StatusCodes.Status412PreconditionFailed, new ApiError
                {
                    error = "failed to add credentials",
                });
            }

            return new OkObjectResult(new AddSsoResponse
            {
                Response = addRes.result,
                Sso = addRes.newSsoId != null ? new SsoInfo
                {
                    Name = addRes.newSsoId.Name,
                    Provider = addRes.newSsoId.Provider,
                    SsoId = addRes.newSsoId.Id
                } : null
            });
        }


        [Function("Auth_Logout")]
        public IActionResult Logout(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            req.RemoveAuthCookie(_authService, fcontext);
            return new OkObjectResult(new OkOnly());
        }

        [Function("Auth_DeleteProfile")]
        public async Task<IActionResult> DeleteProfile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/deleteprofile")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }
            var deleted = await _authService.DeleteUserProfile(userId.Value);
            if (!deleted)
            {
                return new ObjectResultWithStatus(StatusCodes.Status404NotFound, new ApiError
                {
                    error = "user not found",
                });
            }
            req.RemoveAuthCookie(_authService, fcontext);
            return new OkObjectResult(new OkOnly());
        }

        [Function("Auth_DeleteSso")]
        public async Task<IActionResult> DeleteSso(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/deletesso")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }
            var ssoId = await req.ReadFromJsonAsync<SsoIdInfo>();
            if (ssoId == null)
            {
                return new ObjectResultWithStatus(StatusCodes.Status400BadRequest, new ApiError
                {
                    error = "no body",
                });
            }

            var deleted = await _authService.DeleteSso(userId.Value, ssoId);
            if (!deleted)
            {
                return new ObjectResultWithStatus(StatusCodes.Status404NotFound, new ApiError
                {
                    error = "user not found",
                });
            }
            return new OkObjectResult(new OkOnly());
        }

        [Function("Auth_LmTokenLogin")]
        public async Task<IActionResult> LmTokenLogin(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/lmtokenlogin")]
            HttpRequest req,
          FunctionContext fcontext)
        {
            var ip = fcontext.GetClientIp(req);
            if (!_throttler.TryRegisterRequest($"LmTokenLogin#{ip}", 10))
            {
                return new ObjectResultWithStatus(StatusCodes.Status429TooManyRequests, new ApiError
                {
                    error = "too many requests",
                });
            }

            var request = await req.ReadFromJsonAsync<LmTokenLoginRequest>();
            if (request == null || string.IsNullOrEmpty(request.token))
            {
                return new ObjectResultWithStatus(
                    StatusCodes.Status400BadRequest,
                    new ApiError
                    {
                        error = "no body, or no token",
                    });
            }
            var authInfo = await _authService.LmTokenLogin(request.token);
            if (authInfo == null)
            {
                return new ObjectResultWithStatus(
                    StatusCodes.Status401Unauthorized,
                    new ApiError
                    {
                        error = "token is not recognized by LM",
                    });
            }
            req.AddNewAuthCookie(authInfo.Id, _authService, fcontext);
            return new OkObjectResult(authInfo);
        }

        [Function("Auth_LmAddSso")]
        public async Task<IActionResult> LmAddSso(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/lmaddsso")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }
            var request = await req.ReadFromJsonAsync<LmTokenLoginRequest>();
            if (request == null || string.IsNullOrEmpty(request.token))
            {
                return new ObjectResultWithStatus(
                    StatusCodes.Status400BadRequest,
                    new ApiError
                    {
                        error = "no body, or no token",
                    });
            }
            var addResult = await _authService.LmTokenSsoAdd(userId.Value, request.token);
            if (addResult.result == SsoAddResult.Error)
            {
                return new ObjectResultWithStatus(
                    StatusCodes.Status401Unauthorized,
                    new ApiError
                    {
                        error = "token is not recognized by LM",
                    });
            }
            return new OkObjectResult(new AddSsoResponse
            {
                Response = addResult.result,
                Sso = addResult.newSsoId != null ? new SsoInfo
                {
                    Name = addResult.newSsoId.Name,
                    Provider = addResult.newSsoId.Provider,
                    SsoId = addResult.newSsoId.Id
                } : null
            });
        }

        [Function(nameof(LMAccountDelete))]
        public async Task<IActionResult> LMAccountDelete(
         [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "auth/lmaccount/{id:int}")]
            HttpRequest req,
            int id,
            FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }

            var deleted = await _authService.DeleteLMAccount(id, userId.Value);
            if (!deleted)
            {
                return new ObjectResultWithStatus(StatusCodes.Status404NotFound, new ApiError
                {
                    error = "LMAccount not found",
                });
            }
            return new OkObjectResult(new SignedResponse
            {
                NewSignature = await _authService.LoadAuthInfoSignature(userId.Value)
            });
        }

        [Function(nameof(LMAccountActivate))]
        public async Task<IActionResult> LMAccountActivate(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/lmaccount/{id:int}/activate")]
            HttpRequest req,
            int id,
            FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }

            await _authService.ActivateLMAccount(id, userId.Value);
            return new OkObjectResult(new OkOnly());
        }

        [Function(nameof(LMAccountCreate))]
        public async Task<IActionResult> LMAccountCreate(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/lmaccount")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }

            LMAccountInfo lmAccount;
            try
            {
                lmAccount = await req.ReadFromJsonAsync<LMAccountInfo>();
            }
            catch (JsonException)
            {
                return new ObjectResultWithStatus(StatusCodes.Status400BadRequest, new ApiError
                {
                    error = "passed json cannot be converted to LMAccountInfo",
                });
            }
            if (lmAccount == null)
            {
                return new ObjectResultWithStatus(StatusCodes.Status400BadRequest, new ApiError
                {
                    error = "body is empty",
                });
            }

            var valRes = _authService.ValidateLmAccountInfo(lmAccount);
            if (!valRes.ok)
            {
                return new ObjectResultWithStatus(StatusCodes.Status400BadRequest, new ApiError
                {
                    error = valRes.error,
                });
            }

            var account = await _authService.AddLMAccountNonActive(userId.Value, lmAccount);
            if (lmAccount.IsActive)
            {
                await _authService.ActivateLMAccount(account.Id, userId.Value);
            }
            var newSign = await _authService.LoadAuthInfoSignature(userId.Value);
            return new OkObjectResult(new AddLmAccountResponse
            {
                Id = account.Id,
                NewSignature = newSign,
                IsPaidLicense = account.IsPaidLicense,
                LicenseTill = account.LicenseTill
            });
        }

        [Function(nameof(LMAccountUpdateSettings))]
        public async Task<IActionResult> LMAccountUpdateSettings(
       [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/lmaccount/{id:int}/settings")]
            HttpRequest req,
            int id,
          FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }

            LMAccountSettings settings;
            try
            {
                settings = await req.ReadFromJsonAsync<LMAccountSettings>();
            }
            catch (JsonException)
            {
                return new ObjectResultWithStatus(StatusCodes.Status400BadRequest, new ApiError
                {
                    error = "passed json cannot be converted to LMAccountSettings",
                });
            }
            if (settings == null)
            {
                return new ObjectResultWithStatus(StatusCodes.Status400BadRequest, new ApiError
                {
                    error = "body is empty",
                });
            }

            var valRes = _authService.ValidateLmAccountSettings(settings);

            if (!valRes.ok)
            {
                return new ObjectResultWithStatus(StatusCodes.Status400BadRequest, new ApiError
                {
                    error = valRes.error,
                });
            }

            var res = await _authService.UpdateLMAccountSettings(userId.Value, id, settings);
            if (!res)
            {
                return new ObjectResultWithStatus(StatusCodes.Status404NotFound, new ApiError
                {
                    error = "LMAccount not found",
                });
            }
            return new OkObjectResult(new OkOnly());
        }


        [Function("Auth_Test")]
        public IActionResult Test(
         [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "auth/test")]
            HttpRequest req,
            FunctionContext fcontext)
        {
            req.AddNewAuthCookie(48, _authService, fcontext);
            return new OkObjectResult("Welcome to Azure Functions! Cookie is set");
        }
    }
}
