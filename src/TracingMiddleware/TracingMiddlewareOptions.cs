namespace TracingMiddleware
{
    using System;

    public class TracingMiddlewareOptions
    {
        public Action<string, object> Log { get; set; }
    }
}