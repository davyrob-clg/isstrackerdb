

using MySqlConnector;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IssTrackerDB;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
        
        // Set a user agent (required by some APIs)
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "IssTracker/1.0 (Linux; C#)");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var apiUrl = _configuration["IssTracking:ApiUrl"];
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var pollInterval = _configuration.GetValue<int>("IssTracking:PollIntervalSeconds");

        _logger.LogInformation("ISS Tracker Service Started.");
        _logger.LogInformation($"URL {apiUrl}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Fetch Data
                var response = await _httpClient.GetAsync(apiUrl, stoppingToken);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync(stoppingToken);
                var issData = JsonSerializer.Deserialize<IssData>(json);

                if (issData != null)
                {
                    // 2. Log to Console
                    _logger.LogInformation($"ISS Position: Lat: {issData.Latitude}, Lon: {issData.Longitude}");

                    // 3. Save to MySQL
                    await SaveToDatabaseAsync(issData, connectionString!, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching or saving ISS data");
                _logger.LogError($"Big problems abound: {ex.Message}");
	            Environment.Exit(1);
            }

            // 4. Wait before next poll
            await Task.Delay(TimeSpan.FromSeconds(pollInterval), stoppingToken);
        }
    }

    private async Task SaveToDatabaseAsync(IssData data, string connectionString, CancellationToken token)
    {
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(token);

        var query = @"INSERT INTO iss_positions (latitude, longitude, altitude, velocity, timestamp) 
                      VALUES (@lat, @lon, @alt, @vel, @time)";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@lat", data.Latitude);
        command.Parameters.AddWithValue("@lon", data.Longitude);
        command.Parameters.AddWithValue("@alt", data.Altitude);
        command.Parameters.AddWithValue("@vel", data.Velocity);
        command.Parameters.AddWithValue("@time", data.Timestamp);

        await command.ExecuteNonQueryAsync(token);
    }
}

// Data Model matching the JSON response from api.wheretheiss.at
public class IssData
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("altitude")]
    public double Altitude { get; set; }

    [JsonPropertyName("velocity")]
    public double Velocity { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
