namespace TracingMiddleware
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;

    public class TracingMiddleware
    {
        private readonly RequestDelegate nextFunc;

        private readonly TracingMiddlewareOptions options;

        private ITracer tracer;

        public TracingMiddleware(RequestDelegate nextFunc, ITracer tracer)
        {
            this.nextFunc = nextFunc;
            this.tracer = tracer;
        }

        public async Task Invoke(HttpContext context)
        {
            this.tracer = new SafeTracer(this.tracer);
            if (!tracer.IsEnabled)
            {
                await this.nextFunc(context).ConfigureAwait(false);
            }
            else
            {
                string requestId;
                if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
                {
                    requestId = context.TraceIdentifier;
                }
                else
                {
                    requestId = Guid.NewGuid().ToString();
                    context.TraceIdentifier = requestId;
                }

                var path = context.Request.Path + context.Request.QueryString;
                tracer.Trace(requestId, "Request Start: " + path);

                var stopWatch = Stopwatch.StartNew();

                try
                {
                    await this.nextFunc(context).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    tracer.Trace(requestId, exception.ToString());
                    throw;
                }
                finally
                {
                    stopWatch.Stop();

                    if (tracer.Filters.All(filter => filter.Invoke(context)))
                    {
                        this.tracer.Trace(requestId, $"Request {nameof(context.Request.Cookies)}", context.Request.Cookies);
                        this.tracer.Trace(requestId, $"Request {nameof(context.Request.Host)}", context.Request.Host);
                        this.tracer.Trace(requestId, $"Request {nameof(context.Request.ContentType)}", context.Request.ContentType);
                        this.tracer.Trace(requestId, $"Request {nameof(context.User)}", context.User);
                        this.tracer.Trace(requestId, $"Request {nameof(context.Request.Cookies)}", context.Request.Cookies);

                        var req = context.Features.Get<IHttpRequestFeature>();
                        if (req != null)
                        {
                            this.tracer.Trace(requestId, $"Request {nameof(req.Method)}", req.Method);
                            this.tracer.Trace(requestId, $"Request {nameof(req.Body)}", req.Body);
                            this.tracer.Trace(requestId, $"Request {nameof(req.Headers)}", req.Headers);
                            this.tracer.Trace(requestId, $"Request {nameof(req.Path)}", req.Path);
                            this.tracer.Trace(requestId, $"Request {nameof(req.PathBase)}", req.PathBase);
                            this.tracer.Trace(requestId, $"Request {nameof(req.Protocol)}", req.Protocol);
                            this.tracer.Trace(requestId, $"Request {nameof(req.QueryString)}", req.QueryString);
                            this.tracer.Trace(requestId, $"Request {nameof(req.Scheme)}", req.Scheme);
                        }

                        var res = context.Features.Get<IHttpResponseFeature>();
                        if (res != null)
                        {
                            this.tracer.Trace(requestId, $"Response {nameof(res.Body)}", res.Body);
                            this.tracer.Trace(requestId, $"Response {nameof(res.HasStarted)}", res.HasStarted);
                            this.tracer.Trace(requestId, $"Response {nameof(res.Headers)}", res.Headers);
                            this.tracer.Trace(requestId, $"Response {nameof(res.ReasonPhrase)}", res.ReasonPhrase);
                            this.tracer.Trace(requestId, $"Response {nameof(res.StatusCode)}", res.StatusCode);
                        }

                        var conn = context.Features.Get<IHttpConnectionFeature>();
                        if (conn != null)
                        {
                            this.tracer.Trace(requestId, $"Connection {nameof(conn.ConnectionId)}", conn.ConnectionId);
                            this.tracer.Trace(requestId, $"Connection {nameof(conn.LocalIpAddress)}", conn.LocalIpAddress);
                            this.tracer.Trace(requestId, $"Connection {nameof(conn.LocalPort)}", conn.LocalPort);
                            this.tracer.Trace(requestId, $"Connection {nameof(conn.RemoteIpAddress)}", conn.RemoteIpAddress);
                            this.tracer.Trace(requestId, $"Connection {nameof(conn.RemotePort)}", conn.RemotePort);
                        }
                    }

                    tracer.Trace(requestId, string.Format("Request completed in {0} ms for path {1}", stopWatch.ElapsedMilliseconds, path));
                }
            }
        }
    }
}
