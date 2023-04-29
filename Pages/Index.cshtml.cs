using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace movieGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private List<Movie> _movies;
    private readonly string _apiKey;
    private dynamic? movieData;
    private int currentPage = 1;

    public List<string> StreamingServices { get; private set; }
    public Movie? CurrentMovie { get; private set; } 
    
    public IndexModel(ILogger<IndexModel> logger, IConfiguration config, IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _logger = logger;
        _config = config;
        _httpClient = httpClientFactory.CreateClient();
        _movies = new List<Movie>();
        _apiKey = _config["ApiKey"]!;
        _cache = cache;
        StreamingServices = new List<string>();
        CurrentMovie = null;
    }

    public async Task<IActionResult> OnGet()
    {
        await ShowMovie();
        return Page();
    }
    private async Task ShowMovie()
    {
        CurrentMovie = await GetMovie();
        if(CurrentMovie is null)
        {
            CurrentMovie = await GetMovie();
        }
    }

    private async Task<Movie> GetMovie()
    {
        try
        {
            if (_movies.Count >= 20 * currentPage)
            {
                currentPage++;
                movieData = null;
            }

            if (movieData == null)
            {
                string url = $"https://api.themoviedb.org/3/movie/top_rated?api_key={_apiKey}&language=en-US&page=" + currentPage;

                var response = await _httpClient.GetStringAsync(url);
                movieData = JObject.Parse(response)["results"]!;
            }

            int count = movieData.Count;
            var rand = new Random();
            int i = rand.Next(count);

            foreach(Movie m in _movies)
            {
                if (m.Id == (int)movieData[i].id) return await GetMovie();
            }

            Movie movie = new Movie
            (
                (int)movieData[i].id,
                (string)movieData[i].title,
                (double)movieData[i].vote_average,
                ((string)movieData[i].release_date).Remove(4),
                (string)movieData[i].overview,
                "https://image.tmdb.org/t/p/original" + (string)movieData[i].poster_path
            );

            _movies.Add(movie);
            await GetStreamingServices(movie.Id);

            return movie;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error occurred: {ErrorMessage}", ex.Message);
            return null!;
        }
        catch (JsonException ex)
        {
            _logger.LogError("JSON deserialization error occurred: {ErrorMessage}", ex.Message);
            return null!;
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred: {ErrorMessage}", ex.Message);
            return null!;
        }
    }

    private async Task GetStreamingServices(int id)
    {
        StreamingServices.Clear();

        string cacheKey = $"streaming_{id}";
        if (_cache.TryGetValue(cacheKey, out List<string>? cachedStreamingServices))
        {
            StreamingServices = cachedStreamingServices!;
            return;
        }
        
        string url = $"https://api.themoviedb.org/3/movie/{id}/watch/providers?api_key={_apiKey}";
        var response = await _httpClient.GetStringAsync(url);

        dynamic streamingData = JObject.Parse(response)["results"]!;
        if (streamingData is null) return;

        dynamic streamingDataSE = streamingData["SE"]!;
        if (streamingDataSE is null) return;

        dynamic flatRateStreamingDataSE = streamingDataSE["flatrate"];
        if (flatRateStreamingDataSE is null) return;

        if(flatRateStreamingDataSE is null) return;

        foreach(var item in flatRateStreamingDataSE)
        {
            if(item["logo_path"] is null) return;

            string fullPath = "https://image.tmdb.org/t/p/original" + item["logo_path"];
            StreamingServices.Add(fullPath);
        }

        _cache.Set(cacheKey, StreamingServices, TimeSpan.FromMinutes(5));
    }
}
