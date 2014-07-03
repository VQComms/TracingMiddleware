namespace TracingMiddleware.Demo
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Owin;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //This is all optional you can use it without options and a defaultinterpreter will write to console
            var options = TracingMiddlewareOptions.Default
                .Ignore(key => key.StartsWith(""))
                .Include(key => key.StartsWith("owin."))
                
                .Ignore<Stream>();

            var otheroptions =
                new TracingMiddlewareOptions(Trace, MessageFormat, DefaultTypeFormat, IsEnabled).ForKey(
                    "owin.ResponseStatusCode", s => Debug.WriteLine(s));
            
            app
                .Use(TracingMiddleware.Tracing(otheroptions))
                .UseNancy();
        }

        private bool IsEnabled()
        {
            return true;
        }

        private string DefaultTypeFormat(object value)
        {
            return value.ToString();
        }

        private string MessageFormat(string key, string value)
        {
            return "Key : " + key + " Value : " + value;
        }

        private void Trace(string message)
        {
            Console.WriteLine(message);
        }
    }
}
