using System.Diagnostics;

#pragma warning disable CS8618
namespace Identity.DTOs;

public class BadRequestResponse
{
    private const string DefaultTitle = "One or more validation errors occurred.";

    public BadRequestResponse()
        : this(DefaultTitle, new Dictionary<string, string[]>()) { }

    public BadRequestResponse(string title)
        : this(title, new Dictionary<string, string[]>()) { }

    public BadRequestResponse(Dictionary<string, string[]> errors)
        : this(DefaultTitle, errors) { }

    public BadRequestResponse(string title, Dictionary<string, string[]> errors)
    {
        Type = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1";
        Title = title;
        Status = 400;
        if (Activity.Current?.Id != null)
        {
            TraceId = Activity.Current.Id;
        }
        Errors = errors;
    }

    public string Type { get; }
    public string Title { get; set; }
    public int Status { get; }
    public string TraceId { get; set; }
    public Dictionary<string, string[]> Errors { get; set; }
}
