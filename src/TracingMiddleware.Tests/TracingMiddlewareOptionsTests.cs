namespace TracingMiddleware.Tests
{
    using System.Collections.Generic;
    using System.Reflection;
    using Xunit;

    public class TracingMiddlewareOptionsTests
    {
        [Fact]
        public void GetTrace_Returns_Default_Trace_If_Key_Not_Found()
        {
            //Given
            var list = new Dictionary<string,string>();
            var options = new TracingMiddlewareOptions((id, message) => list.Add(id,message));

            //When
            var trace = options.GetTrace("unknownkey");
            trace("requestid123", "this is a default trace hopefully");

            //Then
            Assert.True(list.ContainsKey("requestid123"));
        }

        [Fact]
        public void GetTrace_Returns_Defined_Trace_If_Key_Found()
        {
            //Given
            var list = new Dictionary<string, string>();
            var options = new TracingMiddlewareOptions().ForKey("owinkey", (id, message) => list.Add(id, message));

            //When
            var trace = options.GetTrace("owinkey");
            trace("requestid123", "this is a defined key trace hopefully");

            //Then
            Assert.True(list.ContainsKey("requestid123"));
        }

        [Fact]
        public void GetFormatter_Returns_Null_If_Key_In_Ignore_List()
        {
            //Given
            var options = new TracingMiddlewareOptions().Ignore("nastykey");

            //When
            var result = options.GetFormatter("nastykey", typeof (string));

            //Then
            Assert.Null(result);
        }

        [Fact]
        public void GetFormatter_Returns_Null_If_Type_In_Ignore_List()
        {
            //Given
            var options = TracingMiddlewareOptions.Default.Ignore<string>();

            //When
            var result = options.GetFormatter("key", typeof (string));

            //Then
            Assert.Null(result);
        }

        [Fact]
        public void GetFormatter_Returns_Null_If_Key_Predicate_Returns_True()
        {
            //Given
            var options = new TracingMiddlewareOptions().Ignore(key => key.StartsWith("nasty"));

            //When
            var result = options.GetFormatter("nastykey", typeof(string));

            //Then
            Assert.Null(result);
        }

        [Fact]
        public void GetFormatter_Returns_Default_Type_Formatter_If_None_Found()
        {
            //Given
            var options = new TracingMiddlewareOptions();

            //When
            var formatter = options.GetFormatter("key", typeof(string));
            var result = formatter("stringvalue");

            //Then
            Assert.Equal("stringvalue",result);
        }

        [Fact]
        public void GetFormatter_Returns_Specified_Formatter()
        {
            //Given
            var options = new TracingMiddlewareOptions().ForType<int>(i => "custom formatter " + i);

            //When
            var formatter = options.GetFormatter("key", typeof(int));
            var result = formatter(1);

            //Then
            Assert.Equal("custom formatter 1", result);
        }
    }
}
