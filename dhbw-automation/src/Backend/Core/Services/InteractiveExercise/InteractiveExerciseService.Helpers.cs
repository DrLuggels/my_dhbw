using System.Text.Json;

namespace DHBWAutomation.Backend.Core.Services.InteractiveExercise;

public partial class InteractiveExerciseService
{
    private void ConfigureHtmlSanitizer()
    {
        _htmlSanitizer.AllowedTags.Clear();
        _htmlSanitizer.AllowedTags.Add("p");
        _htmlSanitizer.AllowedTags.Add("br");
        _htmlSanitizer.AllowedTags.Add("strong");
        _htmlSanitizer.AllowedTags.Add("b");
        _htmlSanitizer.AllowedTags.Add("em");
        _htmlSanitizer.AllowedTags.Add("i");
        _htmlSanitizer.AllowedTags.Add("u");
        _htmlSanitizer.AllowedTags.Add("ul");
        _htmlSanitizer.AllowedTags.Add("ol");
        _htmlSanitizer.AllowedTags.Add("li");
        _htmlSanitizer.AllowedTags.Add("code");
        _htmlSanitizer.AllowedTags.Add("pre");
        _htmlSanitizer.AllowedTags.Add("span");
        _htmlSanitizer.AllowedTags.Add("sub");
        _htmlSanitizer.AllowedTags.Add("sup");

        _htmlSanitizer.AllowedAttributes.Clear();
        _htmlSanitizer.AllowedAttributes.Add("class");
    }

    private string SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        return _htmlSanitizer.Sanitize(html);
    }

    private static string GetStringOrDefault(JsonElement el, string prop, string? defaultValue)
    {
        return el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? defaultValue ?? ""
            : defaultValue ?? "";
    }

    private static int GetIntOrDefault(JsonElement el, string prop, int defaultValue)
    {
        return el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number
            ? p.GetInt32()
            : defaultValue;
    }

    private static int? GetIntOrNull(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number
            ? p.GetInt32()
            : null;
    }

    private static bool GetBoolOrDefault(JsonElement el, string prop, bool defaultValue)
    {
        if (!el.TryGetProperty(prop, out var p)) return defaultValue;
        return p.ValueKind == JsonValueKind.True || (p.ValueKind == JsonValueKind.False ? false : defaultValue);
    }

    private static List<string> GetStringArrayOrDefault(JsonElement el, string prop)
    {
        var list = new List<string>();
        if (el.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String)
                    list.Add(item.GetString() ?? "");
        }
        return list;
    }
}
