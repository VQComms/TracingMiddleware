namespace TracingMiddleware
{
    public interface ITracer
    {
        bool IsEnabled { get; }

        void Trace(string message);

        void Trace(string key, object value);
    }
}