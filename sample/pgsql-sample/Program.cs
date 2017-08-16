using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xakep.DataBase;


namespace pgsql_sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (!DataBaseUtil.OnlyExecStartStop(args, p => Console.WriteLine(p)))
                BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartDataBase()
                .UseStartup<Startup>()
                .Build()
                .UseStopDataBase();
    }
}
