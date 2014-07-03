namespace TracingMiddleware
{
    internal class SafeTracer : ITracer
    {
        private readonly ITracer _inner;

        internal SafeTracer(ITracer inner)
        {
            _inner = inner;
        }

        public bool IsEnabled
        {
            get { return _inner.IsEnabled; }
        }

        public void Trace(string message)
        {
            try
            {
                _inner.Trace(message);
            }
            catch { }
        }

        public void Trace(string key, object value)
        {
            try
            {
                _inner.Trace(key, value);
            }
            catch { }
        }
    }
}