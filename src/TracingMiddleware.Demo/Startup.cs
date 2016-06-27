namespace TracingMiddleware.Demo
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Owin;
    using Microsoft.AspNetCore.Builder;
    using Nancy.Owin;

    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            //You can use defaultoptions
            var defaultOptions = TracingMiddlewareOptions.Default;
            
            //You can use custom options
            Func<IDictionary<string, object>, bool> internalexceptionfilter = environment =>
            {
                var owinkvp = environment.FirstOrDefault(x => x.Key == "owin.ResponseStatusCode" && ((int)x.Value == 500 || (int)x.Value == 404));
                return !owinkvp.Equals(default(KeyValuePair<string, object>));
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
                    .Include(key => key.StartsWith("owin.")); //Trace only keys that start with OWIN

            var alt = TracingMiddlewareOptions.Default.AddFilter(internalexceptionfilter);

            //Pass to your App
            app.UseOwin(x =>
            {
                x.Invoke(TracingMiddleware.Tracing(alt));
                x.UseNancy();
            });
        }

        private string MessageFormat(string key, string value)
        {
            return "Key : " + key + " Value : " + value;
        }
    }
}
