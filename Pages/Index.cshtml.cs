using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace movieGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private static List<Movie> _topMovieList = new List<Movie>();
    private static List<Movie> _popMovieList = new List<Movie>();
    private static int currentPage = 0;

    public static readonly string imagePath = "https://image.tmdb.org/t/p/original";
    public static List<string> streamingServices = new List<string>();
    public static Movie? currentMovie = null;
    public static string filter = "top_rated";
    
    public IndexModel(ILogger<IndexModel> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = config["ApiKey"]!;
    }
    
    public void OnGetUpdateFilter(string _filter)
    {
        filter = _filter;
    }
    public async Task<IActionResult> OnGet()
    {
        await GetMovie();
        return Page();
    }

    private async Task GetMovie()
    {
        try
        {
            var movieList = filter == "top_rated" ? _topMovieList : _popMovieList;

            if (movieList.Count == 0)
            {
                currentPage++;
                currentMovie = null;
            }

            if (currentMovie is null)
            {
                string url = $"https://api.themoviedb.org/3/movie/{filter}?api_key={_apiKey}&language=en-US&page=" + currentPage;
                
                var json = await _httpClient.GetStringAsync(url);
                var jsonObj = JObject.Parse(json);
                var results = JArray.Parse(jsonObj["results"]!.ToString());
                
                foreach(var result in results)
                {
                    Movie movie = JsonConvert.DeserializeObject<Movie>(result.ToString())!;
                    movieList.Add(movie);
                }
            }

            Random rand = new Random();
            int i = rand.Next(movieList.Count);

            currentMovie = movieList[i];
            movieList.Remove(currentMovie);

            await GetStreamingServices(currentMovie!.Id);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error occurred: {ErrorMessage}", ex.Message);
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            _logger.LogError("JSON deserialization error occurred: {ErrorMessage}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred: {ErrorMessage}", ex.Message);
        }
    }

    private async Task GetStreamingServices(int id)
    {
        streamingServices.Clear();
        
        string url = $"https://api.themoviedb.org/3/movie/{id}/watch/providers?api_key={_apiKey}";
        var response = await _httpClient.GetStringAsync(url);

        var streamingData = JObject.Parse(response)["results"]!["SE"]!["flatrate"]!;

        foreach(var item in streamingData)
        {
            string fullPath = "https://image.tmdb.org/t/p/original" + item["logo_path"];
            streamingServices.Add(fullPath);
        }
    }
}
