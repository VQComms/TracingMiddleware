namespace TracingMiddleware
{
    using Owin;

    public static class TracingMiddlewareExtensions
    {
        public static IAppBuilder UseTracingMiddleware(this IAppBuilder app, TracingMiddlewareOptions options)
        {
            return app.Use(typeof(TracingMiddleware), options);
        }
    }
}