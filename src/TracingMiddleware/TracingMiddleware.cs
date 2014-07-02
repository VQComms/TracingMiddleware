namespace TracingMiddleware
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class TracingMiddleware
    {
        private readonly Func<IDictionary<string, object>, Task> nextFunc;
        private readonly TracingMiddlewareOptions tracingMiddlewareOptions;

        public TracingMiddleware(  Func<IDictionary<string, object>, Task> nextFunc, TracingMiddlewareOptions tracingMiddlewareOptions)
        {
            this.nextFunc = nextFunc;
            this.tracingMiddlewareOptions = tracingMiddlewareOptions;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var requestItems = environment.Where(x => x.Key.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase));
            
            foreach (var item in requestItems)
            {
                this.tracingMiddlewareOptions.Log(item.Key, item.Value);
            }

            await this.nextFunc(environment);

            var responseItems = environment.Where(x => !x.Key.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase));

            foreach (var item in responseItems)
            {
                this.tracingMiddlewareOptions.Log(item.Key, item.Value);
            }
        }
    }
}
