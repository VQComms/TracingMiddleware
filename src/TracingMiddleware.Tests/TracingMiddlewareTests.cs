namespace TracingMiddleware.Tests
{
    using System;
    using System.Collections.Generic;
    using MidFunc = System.Func<
      System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>,
      System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>
      >;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;


    public class TracingMiddlewareTests
    {
        [Fact]
        public async Task Should_Log_Incoming_Request_Keys()
        {
            //Given
            var logged = false;
            var requestList = new List<string>();

            Action<string, string> traceAction = (key, value) =>
            {
                logged = true;
                requestList.Add(value);
            };

            var options = GetTracingMiddlewareOptions(traceAction);


            var tracingpipeline = CreateTracingOwinPipeline(GetNextFunc(), options);

            var environment = GetEnvironment();

            //When
            await tracingpipeline(environment);

            //Then
            Assert.True(logged);
            Assert.Equal(5, requestList.Count);   //Add 2 to env above, middleware adds requestid, and middleware logs start/stop
        }

        [Fact]
        public async Task Should_Log_Response_Keys()
        {
            //Given
            var logged = false;
            var requestList = new List<string>();

            Action<string, string> traceAction = (key, value) =>
            {
                if (value.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                logged = true;
                requestList.Add(value);
            };

            var options = GetTracingMiddlewareOptions(traceAction);

            var tracingpipeline = CreateTracingOwinPipeline(GetNextFuncWithOwinResponseKeys(), options);

            var environment = GetEnvironment();

            //When
            await tracingpipeline(environment);

            //Then
            Assert.True(logged);
            Assert.Equal(3, requestList.Count);
        }

        [Fact]
        public async Task Should_Log_Other_Keys()
        {
            //Given
            var logged = false;
            var requestList = new List<string>();

            Action<string, string> traceAction = (key, value) =>
            {
                if (value.StartsWith("owin.request", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                logged = true;
                requestList.Add(value);
            };

            var options = GetTracingMiddlewareOptions(traceAction);

            var next = GetNextFuncWithOwinResponseAndServerKeys();

            var tracingpipeline = CreateTracingOwinPipeline(next, options);

            var environment = GetEnvironment();

            //When
            await tracingpipeline(environment);

            //Then
            Assert.True(logged);
            Assert.Equal(4, requestList.Count);
        }

        [Fact]
        public async Task Should_Log_Execution_Time()
        {
            //Given
            var requestList = new List<string>();

            Action<string, string> traceAction = (key, value) =>
            {
                requestList.Add(value);
            };

            var options = GetTracingMiddlewareOptions(traceAction);

            var next = GetNextFunc();

            var tracingpipeline = CreateTracingOwinPipeline(next, options);

            var environment = GetEnvironment();

            //When
            await tracingpipeline(environment);

            //Then
            Assert.True(requestList.Any(x => x.StartsWith("Request completed")));
        }

        private Dictionary<string, object> GetEnvironment()
        {
            var environment = new Dictionary<string, object>
            {
                {"owin.RequestHeaders", new Dictionary<string, string[]>() {{"Accept", new[] {"application/json"}}}},
                {"owin.RequestPath", "/"}
            };
            return environment;
        }

        public AppFunc GetNextFunc()
        {
            return env => Task.FromResult(123);
        }

        private AppFunc GetNextFuncWithOwinResponseKeys()
        {
            return env =>
            {
                env.Add("owin.ResponseStatusCode", 200);
                return Task.FromResult(123);
            };
        }

        private AppFunc GetNextFuncWithOwinResponseAndServerKeys()
        {
            return env =>
            {
                env.Add("owin.ResponseStatusCode", 200);
                env.Add("server.user", "VincentVega");
                return Task.FromResult(123);
            };
        }

        public AppFunc CreateTracingOwinPipeline(AppFunc nextFunc, TracingMiddlewareOptions tracingMiddlewareOptions = null)
        {
            tracingMiddlewareOptions = tracingMiddlewareOptions ?? GetTracingMiddlewareOptions((s, s1) => TracingMiddlewareOptions.DefaultTrace(s, s1));
            return TracingMiddleware.Tracing(tracingMiddlewareOptions)(nextFunc);
        }

        private TracingMiddlewareOptions GetTracingMiddlewareOptions(Action<string, string> traceAction)
        {
            return new TracingMiddlewareOptions((id, message) => traceAction(id, message));
        }


    }
}
