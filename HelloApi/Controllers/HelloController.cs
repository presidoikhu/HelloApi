using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using IpData;

namespace HelloApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloController : ControllerBase
    {
        private readonly ILogger<HelloController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _openWeatherMapApiKey = "6fe1ca33aca1670ffe2682335b421400";
        private readonly string _ipDataApiKey = "d34a4ea0aafed07b2d595479a6af37a324df0cec4f769bcad9912137";

        public HelloController(ILogger<HelloController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string visitor_name)
        {
            try
            {
                string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                string location;
                double temperature;

                if (string.IsNullOrEmpty(clientIp) || clientIp == "::1" || clientIp == "127.0.0.1")
                {
                    // Use default location for testing purposes
                    clientIp = "127.0.0.1"; // Localhost for testing purposes
                    location = "New York"; // Default location for testing
                }
                else
                {
                    // Retrieve location using IpData API
                    location = await GetLocationFromIp(clientIp);
                }

                // Retrieve temperature using OpenWeatherMap API
                temperature = await GetTemperature(location);

                // Prepare response object
                var greeting = $"Hello, {visitor_name}!, the temperature is {temperature} degrees Celsius in {location}";
                var response = new { client_ip = clientIp, location, greeting };

                return Ok(response);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error occurred while making HTTP request.");
                return StatusCode(500, "An error occurred while making an HTTP request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        private async Task<string> GetLocationFromIp(string clientIp)
        {
            try
            {
                var ipDataClient = new IpDataClient(_ipDataApiKey);
                var ipInfo = await ipDataClient.Lookup(clientIp);
                return ipInfo.City;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching location from IpData API.");
                throw;
            }
        }

        private async Task<double> GetTemperature(string location)
        {
            try
            {
                var weatherClient = new RestClient($"https://api.openweathermap.org/data/2.5/weather?q={location}&appid={_openWeatherMapApiKey}&units=metric");
                var weatherRequest = new RestRequest();
                var weatherResponse = await weatherClient.ExecuteAsync(weatherRequest);

                if (!weatherResponse.IsSuccessful)
                {
                    _logger.LogError($"Weather API request failed with status code: {weatherResponse.StatusCode}");
                    throw new HttpRequestException("Weather API request failed.");
                }

                var weatherData = JObject.Parse(weatherResponse.Content);
                return weatherData["main"]["temp"].Value<double>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching temperature from OpenWeatherMap API.");
                throw; 
            }
        }
    }
}
