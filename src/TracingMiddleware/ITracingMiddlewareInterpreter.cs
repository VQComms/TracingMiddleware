namespace TracingMiddleware
{
    using System;

    public interface ITracingMiddlewareInterpreter
    {
        void Interpret(string key, object value);
        Action<string, string> Log { get; set; }
    }
}
