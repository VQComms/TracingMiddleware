namespace TracingMiddleware.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public class TracingMiddlewareTests
    {
        [Fact]
        public void Should_Log_Incoming_Request_Keys() 
        {
            //Given
            var logged = false;
            var requestList = new List<string>();

            var options = GetTracingMiddlewareOptions();
            options.Log = (s, o) =>
            {
                logged = true;
                requestList.Add(s);
            };

            var tracing = GetTracingMiddleware(GetNextFunc(), options);
            var environment = new Dictionary<string, object>
            {
                {"owin.RequestHeaders", new Dictionary<string, string[]>() {{"Accept", new[] {"application/json"}}}},
                {"owin.RequestPath", "/"}
            };

            //When
            var task = tracing.Invoke(environment);

            //Then
            Assert.True(logged);
            Assert.Equal(2, requestList.Count);

        }

        [Fact]
        public void Should_Log_Response_Keys()
        {
            //Given
            var logged = false;
            var requestList = new List<string>();

            var options = GetTracingMiddlewareOptions();
            options.Log = (s, o) =>
            {
                if (s.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                logged = true;
                requestList.Add(s);
            };

            var tracing = GetTracingMiddleware(GetNextFunc(addResponseKeys: true), options);

            var environment = new Dictionary<string, object>
            {
                {"owin.RequestHeaders", new Dictionary<string, string[]>() {{"Accept", new[] {"application/json"}}}},
                {"owin.RequestPath", "/"}
            };

            //When
            var task = tracing.Invoke(environment);

            //Then
            Assert.True(logged);
            Assert.Equal(1, requestList.Count);
        }

        [Fact]
        public void Should_Log_Other_Keys()
        {
            //Given
            var logged = false;
            var requestList = new List<string>();

            var options = GetTracingMiddlewareOptions();
            options.Log = (s, o) =>
            {
                if (s.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                logged = true;
                requestList.Add(s);
            };

            Func<IDictionary<string, object>, Task> func = objects =>
               {
                   objects.Add("owin.ResponseStatusCode", 200);
                   objects.Add("server.user", "VincentVega");
                   return Task.FromResult(123);
               };

            var tracing = GetTracingMiddleware(func, options);

            var environment = new Dictionary<string, object>
            {
                {"owin.RequestHeaders", new Dictionary<string, string[]>() {{"Accept", new[] {"application/json"}}}},
                {"owin.RequestPath", "/"}
            };

            //When
            var task = tracing.Invoke(environment);

            //Then
            Assert.True(logged);
            Assert.Equal(2, requestList.Count);
        }

        public Func<IDictionary<string, object>, Task> GetNextFunc(bool addResponseKeys = false)
        {
            if (addResponseKeys)
            {
                return objects =>
                {
                    objects.Add("owin.ResponseStatusCode", 200);
                    return Task.FromResult(123);
                };
            }

            return objects => Task.FromResult(123);
        }

        public TracingMiddleware GetTracingMiddleware(Func<IDictionary<string, object>, Task> nextFunc, TracingMiddlewareOptions tracingMiddlewareOptions = null)
        {
            tracingMiddlewareOptions = tracingMiddlewareOptions ?? GetTracingMiddlewareOptions();
            return new TracingMiddleware(nextFunc, tracingMiddlewareOptions);
        }

        private TracingMiddlewareOptions GetTracingMiddlewareOptions()
        {
            return new TracingMiddlewareOptions() { };
        }
    }
}
