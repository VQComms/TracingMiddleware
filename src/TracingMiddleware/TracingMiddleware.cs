namespace TracingMiddleware
{
    using System;
    using System.Linq;

    using MidFunc = System.Func<
       System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>,
       System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>
       >;

    public static class TracingMiddleware
    {
        public static MidFunc Tracing(TracingMiddlewareOptions tracingMiddlewareOptions)
        {
            return
                next =>
                    async environment =>
                    {
                        var requestItems =
                            environment.Where(x => x.Key.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase));

                        foreach (var item in requestItems)
                        {
                            tracingMiddlewareOptions.Log(item.Key, item.Value);
                        }

                        await next(environment);

                        var responseItems =
                            environment.Where(x => !x.Key.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase));

                        foreach (var item in responseItems)
                        {
                            tracingMiddlewareOptions.Log(item.Key, item.Value);
                        }
                    };
        }
    }
}
