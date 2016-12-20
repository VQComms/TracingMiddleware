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
            Func<HttpContext, bool> internalexceptionfilter = context => { return context.Response.StatusCode == 500 || context.Response.StatusCode == 404; };

            var filters = new[] { internalexceptionfilter };

            var otheroptions =
                new TracingMiddlewareOptions(
                        TracingMiddlewareOptions.DefaultTrace, //Console.WriteLine or overwrite with own trace handler
                        MessageFormat,
                        TracingMiddlewareOptions.DefaultTypeFormat, //object.ToString()
                        filters)
                    .ForType<IHeaderDictionary>(
                        headers => string.Join(",",
                            headers.Select(header => string.Format("[{0}:{1}]", header.Key, string.Join(",", header.Value)))))
                    .ForType<Microsoft.AspNetCore.Http.Internal.RequestCookieCollection>(
                        headers => string.Join(",",
                            headers.Select(header => string.Format("[{0}:{1}]", header.Key, string.Join(",", header.Value)))))
                    .ForType<ClaimsPrincipal>(user => string.Join(",", user.Claims.Select(x => x.Type + ":" + x.Value))) //Show claims
                    .ForKey("Response StatusCode", (requestId, value) => Console.WriteLine(requestId + " : *****" + value + "*****")) //Display status code differently
                    .Ignore<Stream>() //Ignore keys that are Stream types
                    .Ignore(key => key.StartsWith("plop")); //Ignore keys that start with plop

            var alt = TracingMiddlewareOptions.Default.AddFilter(internalexceptionfilter);

            app.UseTracingMiddleware(otheroptions);

            app.Use(async (context, next) =>
            {
                context.User.AddIdentity(new ClaimsIdentity(new List<Claim>() { new Claim("Name", "FRED") }));
                await next();
            });

            //Pass to your App
            app.UseOwin(x => { x.UseNancy(); });
        }

        private string MessageFormat(string key, string value)
        {
            return "Key : " + key + " Value : " + value;
        }
    }
}
