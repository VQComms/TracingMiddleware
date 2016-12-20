namespace TracingMiddleware
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

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
                        LogRequest(context, requestId);
                        LogResponse(context, requestId);
                        LogConnection(context, requestId);
                    }

                    tracer.Trace(requestId, $"Request completed in {stopWatch.ElapsedMilliseconds} ms for path {path}");
                }
            }
        }

        private void LogConnection(HttpContext context, string requestId)
        {
            this.tracer.Trace(requestId, $"Connection {nameof(context.Connection.LocalIpAddress)}", context.Connection.LocalIpAddress);
            this.tracer.Trace(requestId, $"Connection {nameof(context.Connection.LocalPort)}", context.Connection.LocalPort);
            this.tracer.Trace(requestId, $"Connection {nameof(context.Connection.RemoteIpAddress)}", context.Connection.RemoteIpAddress);
            this.tracer.Trace(requestId, $"Connection {nameof(context.Connection.RemotePort)}", context.Connection.RemotePort);
        }

        private void LogResponse(HttpContext context, string requestId)
        {
            this.tracer.Trace(requestId, $"Response {nameof(context.Response.Body)}", context.Response.Body);
            this.tracer.Trace(requestId, $"Response {nameof(context.Response.HasStarted)}", context.Response.HasStarted);
            this.tracer.Trace(requestId, $"Response {nameof(context.Response.Headers)}", context.Response.Headers);
            this.tracer.Trace(requestId, $"Response {nameof(context.Response.StatusCode)}", context.Response.StatusCode);
        }

        private void LogRequest(HttpContext context, string requestId)
        {
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.Cookies)}", context.Request.Cookies);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.Host)}", context.Request.Host);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.ContentType)}", context.Request.ContentType);
            this.tracer.Trace(requestId, $"Request {nameof(context.User)}", context.User);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.Method)}", context.Request.Method);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.Body)}", context.Request.Body);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.Headers)}", context.Request.Headers);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.Path)}", context.Request.Path);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.PathBase)}", context.Request.PathBase);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.Protocol)}", context.Request.Protocol);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.QueryString)}", context.Request.QueryString);
            this.tracer.Trace(requestId, $"Request {nameof(context.Request.Scheme)}", context.Request.Scheme);
        }
    }
}
