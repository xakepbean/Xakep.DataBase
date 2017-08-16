using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Xakep.DataBase
{
    /// <summary>
    /// 数据库扩展
    /// </summary>
    public static class DataBaseExtensions
    {
        /// <summary>
        /// 停止数据库
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IWebHost UseStopDataBase(this IWebHost host)
        {
            var options = new DataBaseOptions();
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();
            applicationLifetime.ApplicationStopped.Register(obj =>
            {
                DataBaseUtil.StopDataBase((DataBaseOptions)obj, p => Console.WriteLine(p));
            }, options);
            return host;
        }

        /// <summary>
        /// 启动数据库，如没安装，自动安装，如没初始化，自动初始化
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseStartDataBase(this IWebHostBuilder hostBuilder)
        {
            DataBaseUtil.StartDataBase(new DataBaseOptions(), p => Console.WriteLine(p));
            return hostBuilder;
        }
    }
}
