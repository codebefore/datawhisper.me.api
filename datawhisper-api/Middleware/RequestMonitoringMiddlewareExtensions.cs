using DataWhisper.API.Configuration;

namespace DataWhisper.API.Middleware
{
    public static class RequestMonitoringMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestMonitoring(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestMonitoringMiddleware>();
        }

        public static IApplicationBuilder UseRequestMonitoring(
            this IApplicationBuilder app,
            Action<MonitoringConfiguration> configure)
        {
            var config = new MonitoringConfiguration();
            configure?.Invoke(config);

            return app.UseMiddleware<RequestMonitoringMiddleware>(config);
        }
    }
}
