using System.ComponentModel.DataAnnotations;
using System.Drawing;
namespace movieGenerator;

public class Movie
{
    public Movie(int id, string title, double rating, 
                string date, string summary, string poster)
    {
        Id = id;
        Title = title;
        Rating = rating;
        Date = date;
        Summary = summary;
        Poster = poster;
    }

    public int Id {get; init; }
    public string Title {get; init; }
    public double Rating {get; init; }
    public string Date {get; init; }
    public string Summary {get; init; }
    public string Poster {get; init; }
}