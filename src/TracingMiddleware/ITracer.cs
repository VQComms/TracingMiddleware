namespace TracingMiddleware
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    public interface ITracer
    {
        IEnumerable<Func<HttpContext, bool>> Filters { get; }

        bool IsEnabled { get; }

        void Trace(string requestId,string message);

        void Trace(string requestId, string key, object value);
    }
}