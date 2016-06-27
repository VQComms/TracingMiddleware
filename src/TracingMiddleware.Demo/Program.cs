namespace TracingMiddleware.Demo
{
    using Microsoft.AspNetCore.Hosting;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                 .UseContentRoot(Directory.GetCurrentDirectory())
                 .UseKestrel()
                 .UseStartup<Startup>()
                 .Build();

            host.Run();
        }
    }
}
