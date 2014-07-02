namespace TracingMiddleware
{
    using System;
    using System.Diagnostics;
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
                        var stopWatch = new Stopwatch();
                        tracingMiddlewareOptions.Log("Request Start", null);
                        

                        var requestItems =
                            environment.Where(x => x.Key.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase));

                        foreach (var item in requestItems)
                        {
                            tracingMiddlewareOptions.Log(item.Key, item.Value);
                        }
                        
                        stopWatch.Start();
                        
                        await next(environment);
                        
                        stopWatch.Stop();

                        var responseItems =
                            environment.Where(x => !x.Key.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase));

                        foreach (var item in responseItems)
                        {
                            tracingMiddlewareOptions.Log(item.Key, item.Value);
                        }

                        tracingMiddlewareOptions.Log("Request Finished", null);
                        tracingMiddlewareOptions.Log("Execution Time", stopWatch.ElapsedMilliseconds);
                    };
        }
    }
}
