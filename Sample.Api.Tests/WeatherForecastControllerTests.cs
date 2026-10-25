using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sample.Api.Tests;

// Izmantoju WebApplicationFactory, tas palaiž visu API atmiņā (routing, binding, validācija, JSON),
// bez īsta servera un specifiska porta, tāpēc ka tas ir ātri un strādā iekš CI.
//
// Alternatīvas:
// * testēt kontrolieri tieši - nepārbauda routing un JSON, nav integrācijas tests
// * API uz īsta porta - lēnāk, CI var būt portu konflikti, bet tas arī nav slikts variants
// * Postman/.http - vajag palaistu serveri, ārpus test suite
public class WeatherForecastControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WeatherForecastControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // datums ir random seeds, tāpēc rezultāts vienmēr ir vienāds
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

    // result mainās katru dienu, tāpēc pārbaudu tikai iespējamās vērtības
    [Fact]
    public async Task Get_WithoutDate_ReturnsFiveDaysStartingTomorrow()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        string[] knownSummaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        var forecasts = await _client.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);

        // Ap 00:00 serverim jau var būt nākamā diena.
        Assert.InRange(forecasts[0].Date, today.AddDays(1), today.AddDays(2));

        for (var i = 0; i < forecasts.Length; i++)
        {
            Assert.Equal(forecasts[0].Date.AddDays(i), forecasts[i].Date);
            Assert.InRange(forecasts[i].TemperatureC, -20, 54);
            Assert.Contains(forecasts[i].Summary, knownSummaries);
        }
    }

    // nederīgs datums atgriež 400 vēl pirms metode tiek izsaukta un pieliku vēl papildu variantus 
    // sliktam izsaukumam
    [Theory]
    [InlineData("1997-02-30")]
    [InlineData("67")]
    [InlineData("jamonIberico")]
    public async Task Get_WithInvalidDate_ReturnsBadRequest(string date)
    {
        var response = await _client.GetAsync($"/weatherforecast?date={date}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // papildu pārbauda vai endpoint ir tikai GET
    [Fact]
    public async Task Post_ReturnsMethodNotAllowed()
    {
        var response = await _client.PostAsync("/weatherforecast", null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}