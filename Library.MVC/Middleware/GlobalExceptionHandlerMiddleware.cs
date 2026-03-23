using Serilog;

namespace Library.MVC.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception in request path: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var errorId = Guid.NewGuid().ToString();
        var errorPage = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Oops! Something went wrong</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; background-color: #f8f9fa; }}
        .container {{ max-width: 600px; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #dc3545; }}
        .error-id {{ background-color: #e9ecef; padding: 10px; border-radius: 4px; margin: 20px 0; font-family: monospace; }}
        .details {{ color: #666; margin-top: 20px; }}
        a {{ color: #007bff; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>⚠️ Oops! Something went wrong</h1>
        <p>We encountered an unexpected error while processing your request.</p>
        <div class='error-id'>
            <strong>Error ID:</strong> {errorId}
        </div>
        <div class='details'>
            <p>Our team has been notified. Please reference the error ID above when contacting support.</p>
            <p><a href='/'>Return to Home</a></p>
        </div>
    </div>
</body>
</html>";

        return context.Response.WriteAsync(errorPage);
    }
}
