namespace TracingMiddleware
{
    using System;
    using System.Collections.Generic;

    internal class TracingMiddlewareOptionsTracer : ITracer
    {
        private readonly Func<TracingMiddlewareOptions> getOptions;

        internal TracingMiddlewareOptionsTracer(Func<TracingMiddlewareOptions> getOptions)
        {
            this.getOptions = getOptions;
        }

        public bool IsEnabled
        {
            get { return getOptions().IsEnabled; }
        }

        public IEnumerable<Func<IDictionary<string, object>, bool>> Filters
        {
            get { return getOptions().Filters; }
        }

        public void Trace(string message)
        {
            getOptions().Trace(message);
        }

        public void Trace(string key, object value)
        {
            if (value == null)
            {
                return;
            }

            TypeFormat typeFormat = getOptions().GetFormatter(key, value.GetType());
            if (typeFormat == null)
            {
                return;
            }
            TracingMiddlewareOptions options = getOptions();

            var trace = options.GetTrace(key);

            trace(options.MessageFormat(key, typeFormat(value)));
        }
    }
}