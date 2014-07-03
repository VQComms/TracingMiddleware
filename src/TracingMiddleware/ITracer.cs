namespace TracingMiddleware
{
    using System;
    using System.Collections.Generic;

    public interface ITracer
    {
        IEnumerable<Func<IDictionary<string, object>, bool>> Filters { get; }

        bool IsEnabled { get; }

        void Trace(string requestId,string message);

        void Trace(string requestId, string key, object value);
    }
}