namespace DHBWAutomation.Backend.Core.Services.LearningAnalytics;

public partial class LearningAnalyticsService
{
    private void ConfigureHtmlSanitizer()
    {
        _htmlSanitizer.AllowedTags.Clear();

        // Whitelist safe formatting tags
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
        _htmlSanitizer.AllowedTags.Add("div");
        _htmlSanitizer.AllowedTags.Add("h1");
        _htmlSanitizer.AllowedTags.Add("h2");
        _htmlSanitizer.AllowedTags.Add("h3");
        _htmlSanitizer.AllowedTags.Add("h4");
        _htmlSanitizer.AllowedTags.Add("h5");
        _htmlSanitizer.AllowedTags.Add("h6");
        _htmlSanitizer.AllowedTags.Add("blockquote");
        _htmlSanitizer.AllowedTags.Add("sub");
        _htmlSanitizer.AllowedTags.Add("sup");

        _htmlSanitizer.AllowedAttributes.Clear();
        _htmlSanitizer.AllowedAttributes.Add("class");
        _htmlSanitizer.AllowedAttributes.Add("style");

        _htmlSanitizer.AllowedCssProperties.Clear();
        _htmlSanitizer.AllowedCssProperties.Add("color");
        _htmlSanitizer.AllowedCssProperties.Add("background-color");
        _htmlSanitizer.AllowedCssProperties.Add("font-weight");
        _htmlSanitizer.AllowedCssProperties.Add("font-style");
        _htmlSanitizer.AllowedCssProperties.Add("text-decoration");
        _htmlSanitizer.AllowedCssProperties.Add("margin");
        _htmlSanitizer.AllowedCssProperties.Add("padding");

        _logger.LogInformation("HTML Sanitizer configured with safe tags whitelist");
    }

    private string SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _htmlSanitizer.Sanitize(html);
    }
}
