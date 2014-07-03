namespace TracingMiddleware.Demo
{
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
            
            app
                .Use(TracingMiddleware.Tracing(options))
                .UseNancy();
        }
    }
}
