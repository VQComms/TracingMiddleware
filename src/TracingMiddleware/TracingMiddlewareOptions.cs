namespace TracingMiddleware
{
    public class TracingMiddlewareOptions
    {
        public TracingMiddlewareOptions()
        {
            Interpreter = new TracingMiddlewareInterpreter();
        }
        
        public ITracingMiddlewareInterpreter Interpreter { get; set; }
    }
}