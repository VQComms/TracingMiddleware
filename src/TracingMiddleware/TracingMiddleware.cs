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
            if (getOptions == null)
                throw new ArgumentNullException("getOptions");

            return Tracing(new TracingMiddlewareOptionsTracer(getOptions));
        }

        public static MidFunc Tracing(ITracer tracer)
        {
            if (tracer == null)
                throw new ArgumentNullException("tracer");
            tracer = new SafeTracer(tracer);
            return next => async env =>
            {
                if (!tracer.IsEnabled)
                {
                    await next(env).ConfigureAwait(false);
                }
                else
                {
                    string requestId;
                    if (env.ContainsKey("owin.RequestId") && Guid.Parse(env["owin.RequestId"].ToString()) != Guid.Empty)
                    {
                        requestId = env["owin.RequestId"].ToString();
                    }
                    else
                    {
                        requestId = Guid.NewGuid().ToString();
                        env["owin.RequestId"] = requestId;
                    }
                    var path = env["owin.RequestPath"];
                    tracer.Trace(requestId, "Request Start: " + path);
                    var stopWatch = Stopwatch.StartNew();
                    try
                    {
                        await next(env).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        tracer.Trace(requestId, exception.Message);
                        throw;
                    }
                    finally
                    {
                        stopWatch.Stop();

                        if (tracer.Filters.All(filter => filter.Invoke(env)))
                        {
                            foreach (var item in env)
                            {
                                tracer.Trace(requestId, item.Key, item.Value);
                            }
                        }

                        tracer.Trace(requestId, string.Format("Request completed in {0} ms for path {1}", stopWatch.ElapsedMilliseconds, path));
                    }
                }
            };
        }
    }
}