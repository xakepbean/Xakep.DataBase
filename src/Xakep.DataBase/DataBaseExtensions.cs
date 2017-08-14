using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Xakep.DataBase
{
    public static class DataBaseExtensions
    {

        public static IWebHost UseStopDataBase(this IWebHost host)
        {
            var options = new DataBaseOptions() { DataBaseSetupPath = Path.Combine(AppContext.BaseDirectory, "database"), DataBasePath = Path.Combine(AppContext.BaseDirectory, "database", "data") };
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();
            applicationLifetime.ApplicationStopped.Register(obj =>
            {
                DataBaseUtil.StopDataBase((DataBaseOptions)obj, p => Console.WriteLine(p));
            }, options);
            return host;
        }

        public static IWebHostBuilder UseStartDataBase(this IWebHostBuilder hostBuilder)
        {
            
            var aa=hostBuilder.GetSetting("DataBaseSetupPath");

            var options = new DataBaseOptions() {
                DataBaseSetupPath = Path.Combine(AppContext.BaseDirectory, "database"),
                DataBasePath = Path.Combine(AppContext.BaseDirectory, "database", "data")
            };
            DataBaseUtil.StartDataBase(options, p => Console.WriteLine(p));
            
            return hostBuilder;
        }

    }
}
