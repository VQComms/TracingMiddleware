namespace TracingMiddleware.Demo
{
    using System;
    using Nancy;

    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get("/", _ => "I'll be there in three shakes of a lamb's tail.");

            Get("/error", _ => { throw new Exception("oops"); });

            Get("/notfound", _ => 404);
        }
    }
}
