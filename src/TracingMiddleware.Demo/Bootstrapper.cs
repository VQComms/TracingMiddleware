namespace TracingMiddleware.Demo
{
    using Nancy;
    using Nancy.Bootstrapper;

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        //protected override NancyInternalConfiguration InternalConfiguration
        //{
        //    get
        //    {
        //        return NancyInternalConfiguration.WithOverrides(config =>
        //            config.StatusCodeHandlers = new[] { typeof(RethrowStatusCodeHandler) });
        //    }
        //}
    }
}
