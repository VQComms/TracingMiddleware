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
        public static MidFunc Tracing(TracingMiddlewareOptions tracingMiddlewareOptions = null)
        {
            tracingMiddlewareOptions = tracingMiddlewareOptions ?? new TracingMiddlewareOptions();
            return
                next =>
                    async environment =>
                    {
                        var stopWatch = new Stopwatch();
                        tracingMiddlewareOptions.Interpreter.Interpret("Request Start", "");

                        var requestItems =
                            environment.Where(x => x.Key.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase));

                        foreach (var item in requestItems)
                        {
                            tracingMiddlewareOptions.Interpreter.Interpret(item.Key, item.Value);
                        }

                        stopWatch.Start();

                        await next(environment);

                        stopWatch.Stop();

                        var responseItems =
                            environment.Where(x => !x.Key.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase));

                        foreach (var item in responseItems)
                        {
                            tracingMiddlewareOptions.Interpreter.Interpret(item.Key, item.Value);
                        }

                        tracingMiddlewareOptions.Interpreter.Interpret("Request Finished", "");
                        tracingMiddlewareOptions.Interpreter.Interpret("Execution Time", stopWatch.ElapsedMilliseconds);
                    };
        }
    }
}
