namespace movieGenerator;
using Newtonsoft.Json;

public class Movie
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("vote_average")]
    public double Rating { get; set; }

    [JsonProperty("release_date")]
    public string? Date { get; set; }

    [JsonProperty("overview")]
    public string? Summary { get; set; }

    [JsonProperty("poster_path")]
    public string? Poster { get; set; }
}