namespace TracingMiddleware.Demo
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Nancy.Owin;

    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            //You can use defaultoptions
            var defaultOptions = TracingMiddlewareOptions.Default;
            
            //You can use custom options
            Func<HttpContext, bool> internalexceptionfilter = context =>
            {
                return context.Response.StatusCode == 500 || context.Response.StatusCode == 404;
            };

            var filters = new[] { internalexceptionfilter };

            var otheroptions =
                new TracingMiddlewareOptions(
                    TracingMiddlewareOptions.DefaultTrace, //Console.WriteLine or overwrite with own trace handler
                    MessageFormat,
                    TracingMiddlewareOptions.DefaultTypeFormat, //object.ToString()
                    filters)
                    .ForType<IDictionary<string, string[]>>(
                        headers => string.Join(",",
                            headers.Select(
                                header => string.Format("[{0}:{1}]", header.Key, string.Join(",", header.Value))))) //Make nice with OWIN headers
                    .ForKey("owin.ResponseStatusCode", (requestId,value) => Console.WriteLine(requestId + " : *****" + value + "*****")) //Display status code differently
                    .Ignore<Stream>() //Ignore OWIN keys that are Stream types
                    .Ignore(key => key.StartsWith("")) //Ignore blank keys
                    .ForType<ClaimsPrincipal>(user=>string.Join(",", user.Claims.Select(x=>x.Type + ":" + x.Value)));
                    //.Include(key => key.StartsWith("owin.")); //Trace only keys that start with OWIN

            var alt = TracingMiddlewareOptions.Default.AddFilter(internalexceptionfilter);

            app.UseTracingMiddleware(otheroptions);

            //Pass to your App
            app.UseOwin(x =>
            {

                x.UseNancy();
            });
        }

        private string MessageFormat(string key, string value)
        {
            return "Key : " + key + " Value : " + value;
        }
    }
}
