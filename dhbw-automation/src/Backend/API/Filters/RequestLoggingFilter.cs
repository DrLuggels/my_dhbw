using Microsoft.AspNetCore.Mvc.Filters;

namespace DHBWAutomation.Backend.API.Filters;

public class RequestLoggingFilter : IActionFilter
{
    private readonly ILogger<RequestLoggingFilter> _logger;

    public RequestLoggingFilter(ILogger<RequestLoggingFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        
        _logger.LogInformation("========== REQUEST LOGGING FILTER - ACTION EXECUTING ==========");
        _logger.LogInformation("Request Path: {Path}", request.Path);
        _logger.LogInformation("Request Method: {Method}", request.Method);
        _logger.LogInformation("Request ContentType: {ContentType}", request.ContentType ?? "null");
        _logger.LogInformation("Request ContentLength: {ContentLength}", request.ContentLength?.ToString() ?? "null");
        _logger.LogInformation("Request HasFormContentType: {HasFormContentType}", request.HasFormContentType);
        
        _logger.LogInformation("Query Parameters:");
        foreach (var query in request.Query)
        {
            _logger.LogInformation("  {Key} = {Value}", query.Key, query.Value);
        }
        
        _logger.LogInformation("Headers:");
        foreach (var header in request.Headers)
        {
            // Don't log sensitive headers
            if (header.Key.ToLower() == "authorization")
                _logger.LogInformation("  {Key} = [REDACTED]", header.Key);
            else
                _logger.LogInformation("  {Key} = {Value}", header.Key, header.Value);
        }
        
        _logger.LogInformation("Action Arguments:");
        foreach (var arg in context.ActionArguments)
        {
            _logger.LogInformation("  {Key} ({Type}): {Value}", 
                arg.Key, 
                arg.Value?.GetType().Name ?? "null",
                arg.Value?.ToString() ?? "null");
        }
        
        _logger.LogInformation("Form Data Available: {HasForm}", request.HasFormContentType);
        if (request.HasFormContentType && request.Form != null)
        {
            _logger.LogInformation("Form Fields:");
            foreach (var field in request.Form)
            {
                _logger.LogInformation("  {Key} = {Value}", field.Key, field.Value);
            }
            
            _logger.LogInformation("Form Files: {FileCount}", request.Form.Files.Count);
            foreach (var file in request.Form.Files)
            {
                _logger.LogInformation("  File Name: {FileName}, Length: {Length}, ContentType: {ContentType}", 
                    file.FileName, file.Length, file.ContentType);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _logger.LogInformation("========== REQUEST LOGGING FILTER - ACTION EXECUTED ==========");
        _logger.LogInformation("Response StatusCode: {StatusCode}", context.HttpContext.Response.StatusCode);
        
        if (context.Exception != null)
        {
            _logger.LogError(context.Exception, "Action executed with exception");
        }
    }
}
