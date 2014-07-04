namespace TracingMiddleware.Demo
{
    using System;
    using System.Runtime.ExceptionServices;
    using Nancy;
    using Nancy.ErrorHandling;
    using Nancy.Extensions;
    using Nancy.Owin;

    public class RethrowStatusCodeHandler : IStatusCodeHandler
    {
        public bool HandlesStatusCode(Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            Exception exception;
            if (!context.TryGetException(out exception) || exception == null)
            {
                return false;
            }

            return statusCode == HttpStatusCode.InternalServerError;
        }

        public void Handle(Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            Exception innerException = ((Exception)context.Items[NancyEngine.ERROR_EXCEPTION]).InnerException;
            ExceptionDispatchInfo
                .Capture(innerException)
                .Throw();
        }
    }
}
