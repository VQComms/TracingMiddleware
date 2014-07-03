namespace TracingMiddleware
{
    using System;

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

        public void Trace(string message)
        {
            getOptions().Trace(message);
        }

        public void Trace(string key, object value)
        {
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