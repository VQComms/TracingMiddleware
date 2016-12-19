namespace TracingMiddleware
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    internal class SafeTracer : ITracer
    {
        private readonly ITracer inner;

        internal SafeTracer(ITracer inner)
        {
            this.inner = inner;
        }

        public IEnumerable<Func<HttpContext, bool>> Filters
        {
            get { return inner.Filters; }
        }

        public bool IsEnabled
        {
            get { return inner.IsEnabled; }
        }

        public void Trace(string requestId, string message)
        {
            try
            {
                inner.Trace(requestId, message);
            }
            catch { }
        }

        public void Trace(string requestId, string key, object value)
        {
            try
            {
                inner.Trace(requestId, key, value);
            }
            catch { }
        }
    }
}