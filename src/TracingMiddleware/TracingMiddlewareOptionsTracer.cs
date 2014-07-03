namespace TracingMiddleware
{
    using System;

    internal class TracingMiddlewareOptionsTracer : ITracer
    {
        private readonly Func<TracingMiddlewareOptions> _getOptions;

        internal TracingMiddlewareOptionsTracer(Func<TracingMiddlewareOptions> getOptions)
        {
            _getOptions = getOptions;
        }

        public bool IsEnabled
        {
            get { return _getOptions().IsEnabled; }
        }

        public void Trace(string message)
        {
            _getOptions().Trace(message);
        }

        public void Trace(string key, object value)
        {
            TypeFormat typeFormat = _getOptions().GetFormatter(key, value.GetType());
            if (typeFormat == null)
            {
                return;
            }
            TracingMiddlewareOptions options = _getOptions();
            options.Trace(options.MessageFormat(key, typeFormat(value)));
        }
    }
}