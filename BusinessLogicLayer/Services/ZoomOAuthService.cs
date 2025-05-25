using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Utilities.Zoom;
using Microsoft.Extensions.Options;

namespace BusinessLogicLayer.Services
{
    public class ZoomOAuthService : IZoomOAuthService
    {
        private readonly ZoomOAuthOptions _options;
        private readonly HttpClient _httpClient;

        public ZoomOAuthService(IOptions<ZoomOAuthOptions> options, HttpClient httpClient)
        {
            _options = options.Value;
            _httpClient = httpClient;
        }

        public BaseResponse<string> GenerateZoomAuthorizeUrl()
        {
            var response = new BaseResponse<string>();
            try
            {
                var url = $"{_options.AuthEndpoint}?response_type=code&client_id={_options.ClientId}&redirect_uri={_options.RedirectUri}";
                response.Success = true;
                response.Data = url;
                response.Message = "Zoom authorize URL generated successfully.";
            }
            catch (Exception ex)
            {
                response.Message = "Failed to generate Zoom authorize URL.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<ZoomTokenResponse>> ExchangeCodeForTokenAsync(string code)
        {
            var response = new BaseResponse<ZoomTokenResponse>();
            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", _options.RedirectUri)
                });

                var httpResponse = await _httpClient.PostAsync(_options.TokenEndpoint, content);
                var json = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = "Failed to exchange code for access token.";
                    response.Errors.Add(json); // response from Zoom API
                    return response;
                }

                var token = JsonSerializer.Deserialize<ZoomTokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                response.Success = true;
                response.Data = token;
                response.Message = "Zoom token received successfully.";
            }
            catch (Exception ex)
            {
                response.Message = "Unexpected error during token exchange!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
