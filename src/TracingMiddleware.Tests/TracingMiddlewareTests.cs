namespace TracingMiddleware.Tests
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;
    using System.Net.Http;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;
    using System.Linq;

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

            var client = this.GetClient(options);

            //When
            await client.GetAsync("/");

            //Then
            Assert.True(logged);
            Assert.Equal(18, requestList.Count); //Add 2 to env above, middleware adds requestid, and middleware logs start/stop
        }

        [Fact]
        public async Task Should_Create_RequestId_If_Environment_Key_Doesnt_Exist_Or_Empty()
        {
            //Given
            var logged = false;
            var requestList = new Dictionary<string, string>();

            Action<string, string> traceAction = (key, value) =>
            {
                logged = true;
                requestList.Add(key, value);
            };

            var options = GetTracingMiddlewareOptions(traceAction);

            var client = this.GetClient(options, removeRequestId: true);

            //When
            await client.GetAsync("/");

            var loggedItem = Guid.Parse(requestList.First().Key);

            //Then
            Assert.True(logged);
            Assert.True(loggedItem != Guid.Empty);
        }

        [Fact]
        public async Task Should_Log_Incoming_Request_Path()
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

            var client = this.GetClient(options);

            await client.GetAsync("/some/place?q=nice");

            //Then
            Assert.True(logged);
            Assert.Equal("Request Start: /some/place?q=nice", requestList.First()); //Add 2 to env above, middleware adds requestid, and middleware logs start/stop
        }

        [Fact]
        public async Task Should_Log_Response_Keys()
        {
            //Given
            var logged = false;
            var requestList = new List<string>();

            Action<string, string> traceAction = (key, value) =>
            {
                if (!value.StartsWith("Response", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                logged = true;
                requestList.Add(value);
            };

            var options = GetTracingMiddlewareOptions(traceAction);

            var client = this.GetClient(options);

            //When
            await client.GetAsync("/");

            //Then
            Assert.True(logged);
            Assert.Equal(4, requestList.Count);
        }

        [Fact]
        public async Task Should_Log_Execution_Time()
        {
            //Given
            var requestList = new List<string>();

            Action<string, string> traceAction = (key, value) => { requestList.Add(value); };

            var options = GetTracingMiddlewareOptions(traceAction);

            var client = this.GetClient(options);

            //When
            await client.GetAsync("/");

            //Then
            Assert.True(requestList.Any(x => x.StartsWith("Request completed")));
        }

        [Fact]
        public async Task Should_Not_Log_If_Filter_Conditions_Not_Met()
        {
            //Given
            var requestList = new List<string>();

            Action<string, string> traceAction = (key, value) => { requestList.Add(value); };

            var options = GetTracingMiddlewareOptions(traceAction);

            Func<HttpContext, bool> internalexceptionfilter = context => context.Response.StatusCode == 500;

            options.AddFilter(internalexceptionfilter);

            var client = this.GetClient(options);

            //When
            await client.GetAsync("/");

            //Then
            Assert.Equal(2, requestList.Count);
        }

        [Fact]
        public async Task Should_Log_Exceptions()
        {
            //Given
            var requestList = new List<string>();

            Action<string, string> traceAction = (key, value) => { requestList.Add(value); };

            var options = GetTracingMiddlewareOptions(traceAction);

            var client = this.GetClient(options, throwException: true);

            //When
            try
            {
                await client.GetAsync("/");
            }
            catch (Exception ex)
            {
            }

            //Then
            Assert.True(requestList.Any(x => x.Contains("my app broke")));
        }

        private TracingMiddlewareOptions GetTracingMiddlewareOptions(Action<string, string> traceAction)
        {
            return new TracingMiddlewareOptions((id, message) => traceAction(id, message));
        }

        private HttpClient GetClient(TracingMiddlewareOptions options = null, bool throwException = false, bool removeRequestId = false)
        {
            var server = new TestServer(new WebHostBuilder().Configure(app =>
            {
                if (removeRequestId)
                {
                    app.Use(async (context, next) =>
                    {
                        context.TraceIdentifier = string.Empty;
                        await next();
                    });
                }

                app.UseTracingMiddleware(options);

                app.Run((ctx) =>
                {
                    if (throwException)
                    {
                        throw new Exception("my app broke");
                    }

                    ctx.Response.StatusCode = StatusCodes.Status200OK;
                    return Task.CompletedTask;
                });
            }));

            return server.CreateClient();
        }
    }
}
