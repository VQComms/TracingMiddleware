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
            var localTracer = new SafeTracer(this.tracer);
            if (!localTracer.IsEnabled)
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
                localTracer.Trace(requestId, $"Request Start: {context.Request.Method} {path}");

                var stopWatch = Stopwatch.StartNew();

                try
                {
                    await this.nextFunc(context).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    localTracer.Trace(requestId, exception.ToString());
                    throw;
                }
                finally
                {
                    stopWatch.Stop();

                    if (localTracer.Filters.All(filter => filter.Invoke(context)))
                    {
                        LogRequest(localTracer, context, requestId);
                        LogResponse(localTracer, context, requestId);
                        LogConnection(localTracer, context, requestId);
                    }

                    localTracer.Trace(requestId, $"Request completed in {stopWatch.ElapsedMilliseconds} ms for path {context.Request.Method} {path}");
                }
            }
        }

        private void LogConnection(ITracer tracer, HttpContext context, string requestId)
        {
            tracer.Trace(requestId, $"Connection {nameof(context.Connection.LocalIpAddress)}", context.Connection.LocalIpAddress);
            tracer.Trace(requestId, $"Connection {nameof(context.Connection.LocalPort)}", context.Connection.LocalPort);
            tracer.Trace(requestId, $"Connection {nameof(context.Connection.RemoteIpAddress)}", context.Connection.RemoteIpAddress);
            tracer.Trace(requestId, $"Connection {nameof(context.Connection.RemotePort)}", context.Connection.RemotePort);
        }

        private void LogResponse(ITracer tracer, HttpContext context, string requestId)
        {
            tracer.Trace(requestId, $"Response {nameof(context.Response.Body)}", context.Response.Body);
            tracer.Trace(requestId, $"Response {nameof(context.Response.HasStarted)}", context.Response.HasStarted);
            tracer.Trace(requestId, $"Response {nameof(context.Response.Headers)}", context.Response.Headers);
            tracer.Trace(requestId, $"Response {nameof(context.Response.StatusCode)}", context.Response.StatusCode);
        }

        private void LogRequest(ITracer tracer, HttpContext context, string requestId)
        {
            tracer.Trace(requestId, $"Request {nameof(context.Request.Cookies)}", context.Request.Cookies);
            tracer.Trace(requestId, $"Request {nameof(context.Request.Host)}", context.Request.Host);
            tracer.Trace(requestId, $"Request {nameof(context.Request.ContentType)}", context.Request.ContentType);
            tracer.Trace(requestId, $"Request {nameof(context.User)}", context.User);
            tracer.Trace(requestId, $"Request {nameof(context.Request.Method)}", context.Request.Method);
            tracer.Trace(requestId, $"Request {nameof(context.Request.Body)}", context.Request.Body);
            tracer.Trace(requestId, $"Request {nameof(context.Request.Headers)}", context.Request.Headers);
            tracer.Trace(requestId, $"Request {nameof(context.Request.Path)}", context.Request.Path);
            tracer.Trace(requestId, $"Request {nameof(context.Request.PathBase)}", context.Request.PathBase);
            tracer.Trace(requestId, $"Request {nameof(context.Request.Protocol)}", context.Request.Protocol);
            tracer.Trace(requestId, $"Request {nameof(context.Request.QueryString)}", context.Request.QueryString);
            tracer.Trace(requestId, $"Request {nameof(context.Request.Scheme)}", context.Request.Scheme);
        }
    }
}
