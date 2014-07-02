namespace TracingMiddleware
{
    public class TracingMiddlewareOptions
    {
        public TracingMiddlewareOptions()
        {
            Interpreter = new DefaultTracingMiddlewareInterpreter();
        }
        
        public ITracingMiddlewareInterpreter Interpreter { get; set; }
    }
}