using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace DocumentParserApi.Services
{
    public class StepRouteMapService
    {
        private readonly Dictionary<string, string> _routeMap;

        public StepRouteMapService(IConfiguration configuration)
        {
            _routeMap = configuration
                .GetSection("FormRouteMappings")
                .Get<Dictionary<string, string>>() ?? new();
        }

        public string? MatchRoute(string userMessage)
        {
            userMessage = userMessage.ToLower();
            foreach (var kvp in _routeMap)
            {
                if (userMessage.Contains(kvp.Key.ToLower()))
                    return kvp.Value;
            }
            return null;
        }
    }
}
