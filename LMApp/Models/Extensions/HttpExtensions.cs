using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Extensions
{
    public static class HttpExtensions
    {
        public static async ValueTask EnsureSuccessStatusCodeIncludeBody(this HttpResponseMessage msg)
        {
            if (!msg.IsSuccessStatusCode)
            {
                var body = await msg.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request failed with status code {msg.StatusCode}.Response - {body}");
            }
        }

        public static void EnsureSuccessStatusCodeNamed(this HttpResponseMessage msg, string name)
        {
            if (!msg.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Request failed with status code {msg.StatusCode}.", null, msg.StatusCode)
                {
                    Data = { [ClientConstants.ClientNameKey] = name }
                };
            }
        }

        public static void LogIfRequired(this Exception e, ILogger logger)
        {
            if (!(e is HttpRequestException))
            {
                logger.LogError(e, "Handeled error (not http request)");
            }
        }

        public static string GetDescriptionForUser(this Exception e)
        {
            return e switch
            {
                HttpRequestException hre when (string)hre.Data[ClientConstants.ClientNameKey] == "Api" =>
                    hre.StatusCode switch
                    {
                        System.Net.HttpStatusCode.ExpectationFailed
                            => "BBudget API respone was unexpected.",
                        System.Net.HttpStatusCode.Unauthorized
                            => "Authentication failed. Please login again.",
                        System.Net.HttpStatusCode.BadRequest
                            => "BBuget API returned an error.",
                        System.Net.HttpStatusCode.TooManyRequests
                            => "BBuget API rate limit exceeded. Please wait 1 minute and try again.",
                        null
                            => "Request failed due network error. Is device offline?",
                        _
                            => $"BBuget API failed to process request ({(int)hre.StatusCode})."
                    },
                HttpRequestException hre =>
                    hre.StatusCode switch {
                        System.Net.HttpStatusCode.ExpectationFailed 
                            => "Lunch Money API respone was unexpected.",
                        System.Net.HttpStatusCode.Unauthorized
                            => "Authentication failed. Please check if API token is still valid.",
                        System.Net.HttpStatusCode.BadRequest
                            => "Lunch Money API returned an error.",
                        System.Net.HttpStatusCode.TooManyRequests
                            => "Lunch Money API rate limit exceeded. Please wait 1 minute and try again.",
                        null 
                            => "Request failed due network error. Is device offline?",
                        _ 
                            => $"Lunch Money API failed to process request ({(int)hre.StatusCode})."
                    },
                _ => "Internal app error occurred."
            };
        }
    }
}
