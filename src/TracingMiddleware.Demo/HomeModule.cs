namespace TracingMiddleware.Demo
{
    using Nancy;

    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get["/"] = _ => "I'll be there in three shakes of a lamb's tail.";
        }
    }
}
