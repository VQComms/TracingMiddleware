# TracingMiddleware

Enable tracing to log entries in the ASP.Net Core pipeline.  

Keys/Types can be included/ignored. 

Log format, type format, log action can be globally set whilst type format and log actions can be set individually for each type and key.

Enables granular pipeline logging.

## Default Usage

```
public void Configuration(IApplicationBuilder app)
{
  var defaultOptions = TracingMiddlewareOptions.Default;
  
   app.UseTracingMiddleware(defaultOptions);
   app.UseOwin(x => x.UseNancy());
}
```

The above produces:

![Default Output](https://raw.githubusercontent.com/VQComms/TracingMiddleware/master/defaulttracing.png)

## Custom Usage

For custom usage take a look at the demo app [here](https://github.com/VQComms/TracingMiddleware/blob/master/src/TracingMiddleware.Demo/Startup.cs)
