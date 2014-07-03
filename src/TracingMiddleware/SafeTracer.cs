namespace TracingMiddleware
{
    using System;
    using System.Collections.Generic;

    internal class SafeTracer : ITracer
    {
        private readonly ITracer inner;

        internal SafeTracer(ITracer inner)
        {
            this.inner = inner;
        }

        public IEnumerable<Func<IDictionary<string, object>, bool>> Filters
        {
            get { return inner.Filters; }
        }

        public bool IsEnabled
        {
            get { return inner.IsEnabled; }
        }

        public void Trace(string message)
        {
            try
            {
                inner.Trace(message);
            }
            catch { }
        }

        public void Trace(string key, object value)
        {
            try
            {
                inner.Trace(key, value);
            }
            catch { }
        }
    }
}