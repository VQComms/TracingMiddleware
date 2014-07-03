namespace TracingMiddleware
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    using MidFunc = System.Func<
       System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>,
       System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>
       >;

    public static class TracingMiddleware
    {
        public static MidFunc Tracing(TracingMiddlewareOptions options = null)
        {
            options = options ?? TracingMiddlewareOptions.Default;
            return Tracing(() => options);
        }

        public static MidFunc Tracing(Func<TracingMiddlewareOptions> getOptions)
        {
            if (getOptions == null) throw new ArgumentNullException("getOptions");

            return Tracing(new TracingMiddlewareOptionsTracer(getOptions));
        }

        public static MidFunc Tracing(ITracer tracer)
        {
            if (tracer == null) throw new ArgumentNullException("tracer");

            tracer = new SafeTracer(tracer);

            MidFunc traceRequest = next => async env =>
            {
                tracer.Trace("Request Start");
                var stopWatch = Stopwatch.StartNew();
                await next(env);
                stopWatch.Stop();

                if (tracer.Filters.All(filter => filter.Invoke(env)))
                {
                    foreach (var item in env)
                    {
                        tracer.Trace(item.Key, item.Value);
                    }
                }

                tracer.Trace(string.Format("Request completed in {0} ms", stopWatch.ElapsedMilliseconds));
            };

            return next => env => tracer.IsEnabled ? traceRequest(next)(env) : next(env);
        }
    }
}