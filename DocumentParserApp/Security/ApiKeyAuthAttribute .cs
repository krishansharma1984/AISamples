using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "x-api-key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetService<IConfiguration>();
        var apiKey = config["ApiKey"];
        var requestKey = context.HttpContext.Request.Headers[ApiKeyHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(requestKey) || requestKey != apiKey)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing API key" });
            return;
        }

        await next();
    }
}
