using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sample.Api.Tests;

// Using WebApplicationFactory, it runs the whole API in memory (routing, binding, validation, JSON),
// without a real server or a specific port, so it's fast and works in CI.
//
// Alternatives:
// * testing the controller directly - doesn't check routing and JSON, not an integration test
// * API on a real port - needed when testing real network stuff (HTTPS, browser tests with Playwright),
//   but for this API it adds nothing over the in-memory server
// * Postman/.http - needs a running server, outside the test suite
public class WeatherForecastControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WeatherForecastControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // date is the Random seed, so the result is always the same
    [Fact]
    public async Task Get_WithDate_ReturnsSameForecastEveryTime()
    {
        var expected = new[]
        {
            new WeatherForecast { Date = new DateOnly(1997, 2, 22), TemperatureC = 32, Summary = "Mild" },
            new WeatherForecast { Date = new DateOnly(1997, 2, 23), TemperatureC = 11, Summary = "Freezing" },
            new WeatherForecast { Date = new DateOnly(1997, 2, 24), TemperatureC = -5, Summary = "Sweltering" },
            new WeatherForecast { Date = new DateOnly(1997, 2, 25), TemperatureC = -7, Summary = "Sweltering" },
            new WeatherForecast { Date = new DateOnly(1997, 2, 26), TemperatureC = 41, Summary = "Bracing" },
        };

        var response = await _client.GetAsync("/weatherforecast?date=1997-02-21");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actual = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();

        Assert.Equal(expected, actual);
    }

    // result changes every day, so only checking the possible values
    [Fact]
    public async Task Get_WithoutDate_ReturnsFiveDaysStartingTomorrow()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        string[] knownSummaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        var forecasts = await _client.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);

        // around midnight the server may already be on the next day.
        Assert.InRange(forecasts[0].Date, today.AddDays(1), today.AddDays(2));

        for (var i = 0; i < forecasts.Length; i++)
        {
            Assert.Equal(forecasts[0].Date.AddDays(i), forecasts[i].Date);
            Assert.InRange(forecasts[i].TemperatureC, -20, 54);
            Assert.Contains(forecasts[i].Summary, knownSummaries);
        }
    }

    // invalid date returns 400 before the method is even called, added a few more bad request variants
    [Theory]
    [InlineData("1997-02-30")]
    [InlineData("67")]
    [InlineData("jamonIberico")]
    public async Task Get_WithInvalidDate_ReturnsBadRequest(string date)
    {
        var response = await _client.GetAsync($"/weatherforecast?date={date}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // extra check that the endpoint is GET only
    [Fact]
    public async Task Post_ReturnsMethodNotAllowed()
    {
        var response = await _client.PostAsync("/weatherforecast", null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}