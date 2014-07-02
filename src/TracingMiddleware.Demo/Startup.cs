namespace TracingMiddleware.Demo
{
    using System;
    using Owin;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //This is all optional you can use it without options and a defaultinterpreter will write to console
            var interpreter = new MyInterpreter {Log = (s, s1) => Console.WriteLine("WOOP" + s, s1)};
            var options = new TracingMiddlewareOptions {Interpreter = interpreter};
            
            app
                .Use(TracingMiddleware.Tracing(options))
                .UseNancy();
        }
    }

    public class MyInterpreter : ITracingMiddlewareInterpreter
    {
        public void Interpret(string key, object value)
        {
            var data = value == null ? "" : value.ToString();
            Log(key, data);
        }

        public Action<string, string> Log { get; set; }
    }
}
