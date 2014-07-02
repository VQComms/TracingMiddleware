namespace TracingMiddleware
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading;

    public class TracingMiddlewareInterpreter : ITracingMiddlewareInterpreter
    {
        public TracingMiddlewareInterpreter()
        {
            Log = (key, value) => Console.WriteLine(key + ":" + value);
        }

        public void Interpret(string key, object value)
        {
            string interpretedvalue = null;

            TypeSwitch.On(value)
                .Case<string>(x => interpretedvalue = x)
                .Case<long>(x => interpretedvalue = x.ToString(CultureInfo.InvariantCulture))
                .Case<int>(x => interpretedvalue = x.ToString(CultureInfo.InvariantCulture))
                .Case<bool>(x => interpretedvalue = x.ToString())
                .Case<IDictionary<string, string[]>>(x =>
                {
                    foreach (var item in x)
                    {
                        interpretedvalue += item.Key + ":" + string.Join(",", item.Value);
                    }
                })
                .Case<CancellationToken>(
                    x =>
                        interpretedvalue =
                            "CancellationRequested:" + x.IsCancellationRequested + "CanBeCanceled:" + x.CanBeCanceled)
                .Case<Stream>(x =>
                {
                    if (!x.CanRead)
                    {
                        interpretedvalue = "Stream Unreadable";
                    }
                    else
                    {
                        var reader = new StreamReader(x);
                        var body = reader.ReadToEnd();
                        interpretedvalue = body;
                    }
                });

            Log(key, interpretedvalue);
        }

        public Action<string, string> Log { get; set; }
    }
}