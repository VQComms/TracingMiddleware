namespace TracingMiddleware.Demo
{
    using Owin;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app
                .Use(TracingMiddleware.Tracing())
                .UseNancy();
        }
    }
}
