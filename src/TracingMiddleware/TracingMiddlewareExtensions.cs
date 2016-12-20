using Microsoft.AspNetCore.Builder;

namespace TracingMiddleware
{
    public static class TracingMiddlewareExtensions
    {
        public static IApplicationBuilder UseTracingMiddleware(this IApplicationBuilder builder, TracingMiddlewareOptions tracingOptions = null)
        {
            var options = tracingOptions ?? TracingMiddlewareOptions.Default;

            var tracer = new TracingMiddlewareOptionsTracer(() => options);

            return builder.UseMiddleware<TracingMiddleware>(tracer);
        }
    }
}
