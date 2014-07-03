#TracingMiddleware

Enable tracing to log entries in the OWIN pipeline.  

OWIN Keys/Types can be included/ignored. 

Log format, type format, log action can be globally set whilst type format and log actions can be set individually for each type and key.

Enables granular OWIN pipeline logging.

##Default Usage

```
public void Configuration(IAppBuilder app)
{
  var defaultOptions = TracingMiddlewareOptions.Default;
  
   app
      .Use(TracingMiddleware.Tracing(defaultOptions))
      .UseNancy();
}
```

The above produces:

![Default Output](https://github.com/VQComms/TracingMiddleware/blob/master/defaulttracing.png)

##Custom Usage

For custom usage take a look at the demo app [here](https://github.com/VQComms/TracingMiddleware/blob/master/src/TracingMiddleware.Demo/Startup.cs)