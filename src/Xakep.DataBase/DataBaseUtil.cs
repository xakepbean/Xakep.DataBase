using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Xakep.DataBase
{
    /// <summary>
    /// 控制数据库初始化，启动，停止
    /// </summary>
    public class DataBaseUtil
    {
        private static readonly string pgsqlzipurl="https://raw.githubusercontent.com/xakepbean/Xakep.DataBase/master/src/Xakep.DataBase/zippgsql.zip";

        /// <summary>
        /// 初始化数据库，创建postgres账号，密码默认 postgres@123
        /// </summary>
        /// <param name="options"></param>
        /// <param name="StandardOutput"></param>
        private static void InitPgSql(DataBaseOptions options, Action<string> StandardOutput = null)
        {
            var binpath = Path.Combine(options.DataBaseSetupPath, "bin");

            string pwdfile = Path.Combine(binpath, "pwd.txt");
            System.IO.File.WriteAllText(pwdfile, "postgres@123");

            string strCmd = $"initdb -D {options.DataBasePath} -E UTF-8 --locale=chs -A md5 -U postgres --pwfile={pwdfile}";

            var vLine = RunInDirTimeoutPipeline(binpath, strCmd, Console.OutputEncoding, StandardOutput);
            if (System.IO.File.Exists(pwdfile))
                System.IO.File.Delete(pwdfile);
        }

        /// <summary>
        /// 在启动website时，启动数据库
        /// </summary>
        /// <param name="options"></param>
        /// <param name="StandardOutput"></param>
        /// <example>
        /// public static IWebHost BuildWebHost(string[] args) =>
        ///         WebHost.CreateDefaultBuilder(args)
        ///                .UseStartDataBase()
        ///                .UseStartup<Startup>()
        ///                .Build()
        ///                .UseStopDataBase();
        /// </example>
        public static void StartDataBase(DataBaseOptions options, Action<string> StandardOutput = null)
        {
            if (!Directory.Exists(options.DataBaseSetupPath))
            {
                StandardOutput?.Invoke("unzip database files ...");
                UnZipFile(options.DataBaseSetupPath, StandardOutput);
            }

            if (!Directory.Exists(options.DataBasePath))
            {
                StandardOutput?.Invoke("init database ...");
                InitPgSql(options, StandardOutput);
            }

            var pid = Path.Combine(options.DataBasePath, "postmaster.pid");
            if (File.Exists(pid))
            {
                var vpid = File.ReadAllLines(pid);
                if (vpid.Length > 0 && int.TryParse(vpid[0], out int spid))
                {
                    try
                    {
                        var postpro = Process.GetProcessById(spid);
                        if (postpro != null && postpro.ProcessName.Equals("postgres", StringComparison.OrdinalIgnoreCase))
                        {
                            StandardOutput?.Invoke("database runing ...");
                            return;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            StandardOutput?.Invoke("start database ...");
            string strCmd = $"pg_ctl -D {options.DataBasePath} -l logfile start";
            var binpath = Path.Combine(options.DataBaseSetupPath, "bin");
            var vLine = RunInDirTimeoutPipeline(binpath, strCmd, Console.OutputEncoding, StandardOutput);

            StandardOutput?.Invoke("database runing ...");
        }
        
        /// <summary>
        /// 在停止website时，停止数据库
        /// 注意只有正常使用ctrl+c的关闭程序才有效
        /// </summary>
        /// <param name="options"></param>
        /// <param name="StandardOutput"></param>
        /// <example>
        /// public static IWebHost BuildWebHost(string[] args) =>
        ///         WebHost.CreateDefaultBuilder(args)
        ///                .UseStartDataBase()
        ///                .UseStartup<Startup>()
        ///                .Build()
        ///                .UseStopDataBase();
        /// </example>
        public static void StopDataBase(DataBaseOptions options, Action<string> StandardOutput = null)
        {
            var pid = Path.Combine(options.DataBasePath, "postmaster.pid");
            if (File.Exists(pid))
            {
                StandardOutput?.Invoke("pgsql stoping ...");
                var binpath = Path.Combine(options.DataBaseSetupPath, "bin");
                string strCmd = $"pg_ctl -D {options.DataBasePath} stop";
                var vLine = RunInDirTimeoutPipeline(binpath, strCmd, Console.OutputEncoding, StandardOutput);
            }
            StandardOutput?.Invoke("database stop successfully");
        }

        /// <summary>
        /// 解压自带的数据库压缩包
        /// </summary>
        /// <param name="TargetDirectory"></param>
        private static void UnZipFile(string TargetDirectory, Action<string> StandardOutput = null)
        {
            var ExecAssembly = Assembly.GetExecutingAssembly();
            var pgsqlzip = $"{ExecAssembly.GetName().Name}.zippgsql.zip";
            var pgsqlfile = Path.Combine(AppContext.BaseDirectory, "zippgsql.zip");
            using (var PgStream = ExecAssembly.GetManifestResourceStream(pgsqlzip))
            {
                if (PgStream != null)
                {
                    StreamWriter sw = new StreamWriter(pgsqlfile);
                    PgStream.CopyTo(sw.BaseStream);
                    sw.Flush();
                    sw.Close();
                }
            }

            if (!File.Exists(pgsqlfile))
            {
                StandardOutput?.Invoke($"download {pgsqlzipurl}");
                StandardOutput?.Invoke("zippgsql.zip");
               DownLoadFile(pgsqlfile, StandardOutput);
            }

            if (File.Exists(pgsqlfile))
            {
                UnZip(pgsqlfile, TargetDirectory);
                File.Delete(pgsqlfile);
            }
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="cmdText"></param>
        /// <param name="standerd"></param>
        /// <param name="StandardOutput"></param>
        /// <returns></returns>
        private static List<string> RunInDirTimeoutPipeline(string dir, string cmdText, Encoding standerd, Action<string> StandardOutput = null)
        {
            dir = dir.TrimEnd('/', '\\');
            Process cmdPro = new Process();//创建进程对象  
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";//"powershell.exe";//cmd.exe";//设定需要执行的命令  // startInfo.FileName = "powershell.exe";//设定需要执行的命令  
            startInfo.Arguments = "";//“/C”表示执行完命令后马上退出  
            startInfo.UseShellExecute = false;//不使用系统外壳程序启动  
            startInfo.RedirectStandardInput = true;//重定向输入  
            startInfo.RedirectStandardOutput = true; //重定向输出  
            startInfo.RedirectStandardError = true;//重定向错误输出
            startInfo.CreateNoWindow = true;//不创建窗口  
            startInfo.WorkingDirectory = dir;
            startInfo.StandardOutputEncoding = standerd;//Encoding.UTF8;
            startInfo.StandardErrorEncoding = standerd;//Encoding.UTF8;
            cmdPro.StartInfo = startInfo;
            if (cmdPro.Start())//开始进程  
            {

                var vt = cmdPro.StandardOutput.ReadLine().Trim();
                vt = cmdPro.StandardOutput.ReadLine().Trim();
                cmdPro.StandardInput.WriteLine(cmdText);
                vt = cmdPro.StandardOutput.ReadLine().Trim();
                cmdPro.StandardInput.WriteLine("\n");
                vt = cmdPro.StandardOutput.ReadLine();
                List<string> outlog = new List<string>();
                if (vt != null)
                {
                    var PathStart = vt.Substring(0, vt.IndexOf('>') + 1);
                    do
                    {
                        String logm = cmdPro.StandardOutput.ReadLine().Trim();
                        if (logm == PathStart)
                            break;
                        if (!string.IsNullOrWhiteSpace(logm))
                        {
                            outlog.Add(logm);
                            StandardOutput?.Invoke(logm);
                        }
                    } while (true);
                }
                string stderr = string.Empty;
                if (outlog.Count == 0)
                {
                    do
                    {
                        String logm = cmdPro.StandardError.ReadLine().Trim();
                        if (!string.IsNullOrWhiteSpace(logm))
                        {
                            stderr += logm + Environment.NewLine;
                            StandardOutput?.Invoke(logm);
                        }
                    } while (cmdPro.StandardError.Peek() >= 0);
                    if (!string.IsNullOrWhiteSpace(stderr))
                    {
                        outlog.Add(stderr);
                        //   throw new Exception(stderr);
                    }
                }

                cmdPro.Close();
                cmdPro = null;
                return outlog;
            }
            return new List<string>();
        }

        /// <summary>
        /// 解压文件
        /// </summary>
        /// <param name="ZipFile"></param>
        /// <param name="TargetDirectory"></param>
        /// <param name="DecryptPassword"></param>
        /// <param name="OverWrite"></param>
        private static void UnZip(string ZipFile, string TargetDirectory, string DecryptPassword=null, bool OverWrite = true)
        {
            if (!Directory.Exists(TargetDirectory))
                Directory.CreateDirectory(TargetDirectory);

            if (!TargetDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                TargetDirectory = TargetDirectory + Path.DirectorySeparatorChar;

            using (ZipInputStream zipfiles = new ZipInputStream(File.OpenRead(ZipFile)))
            {
                if (!string.IsNullOrWhiteSpace(DecryptPassword))
                    zipfiles.Password = DecryptPassword;
                ZipEntry theEntry;

                while ((theEntry = zipfiles.GetNextEntry()) != null)
                {
                    string directoryName = "";
                    string pathToZip = "";
                    pathToZip = theEntry.Name;

                    var divToZip = System.IO.Path.GetDirectoryName(pathToZip);
                    if (pathToZip != "" && divToZip != "")
                        directoryName = divToZip + System.IO.Path.DirectorySeparatorChar;

                    string fileName = Path.GetFileName(pathToZip);

                    Directory.CreateDirectory(TargetDirectory + directoryName);

                    if (fileName != "")
                    {
                        if ((File.Exists(TargetDirectory + directoryName + fileName) && OverWrite) || (!File.Exists(TargetDirectory + directoryName + fileName)))
                        {
                            using (FileStream streamWriter = File.Create(TargetDirectory + directoryName + fileName))
                            {
                                if (theEntry.Size > 0)
                                {
                                    int size = 2048;
                                    byte[] data = new byte[2048];
                                    while (true)
                                    {
                                        size = zipfiles.Read(data, 0, data.Length);

                                        if (size > 0)
                                            streamWriter.Write(data, 0, size);
                                        else
                                            break;
                                    }
                                }
                                streamWriter.Close();
                            }
                        }
                    }
                }

                zipfiles.Close();
            }
        }

        /// <summary>
        /// -d start 启动数据库
        /// -d stop  停止数据库
        /// </summary>
        /// <param name="args"></param>
        /// <param name="StandardOutput"></param>
        /// <returns></returns>
        /// <example>
        /// public static void Main(string[] args)
        /// {
        ///    if (!DataBaseUtil.OnlyExecStartStop(args, p => Console.WriteLine(p)))
        ///        BuildWebHost(args).Run();
        /// }
        /// </example>
        public static bool OnlyExecStartStop(string[] args,Action<string> StandardOutput = null)
        {
            if (args.Length <2)
                return false;
            if (!args[0].Equals("-d", StringComparison.OrdinalIgnoreCase))
                return false;
            var vcmd = args[1].ToLower();
            switch (vcmd)
            {
                case "start":
                    StartDataBase(new DataBaseOptions(), StandardOutput);
                    break;
                case "stop":
                    StopDataBase(new DataBaseOptions(), StandardOutput);
                    break;
            }
            return vcmd == "start" || vcmd == "stop";
        }

        /// <summary>
        /// 从github上下载pgsql压缩安装包
        /// </summary>
        /// <param name="pgsqlfile"></param>
        /// <param name="StandardOutput"></param>
        private static void DownLoadFile(string pgsqlfile, Action<string> StandardOutput = null)
        {
            using (System.Net.WebClient web = new System.Net.WebClient())
            {
                string tempfile = Path.Combine(Path.GetDirectoryName(pgsqlfile), Path.GetFileNameWithoutExtension(pgsqlfile) +"temp"+Path.GetExtension(pgsqlfile));
                bool IsDownload = false;
                web.DownloadFileCompleted += delegate (object sender, System.ComponentModel.AsyncCompletedEventArgs e)
                 {
                     IsDownload = true;

                     StandardOutput?.Invoke("downloaded successfully");
                     File.Move(tempfile, pgsqlfile);
                 };
                web.DownloadProgressChanged += delegate (object sender, System.Net.DownloadProgressChangedEventArgs e)
                {
                    Console.Write($"\rdownloaded {ConvertByteToMb(e.BytesReceived)} of {ConvertByteToMb(e.TotalBytesToReceive)} mb. {e.ProgressPercentage} % complete...........................");
                };

                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
                {
                    IsDownload = true;
                };
                web.DownloadFileAsync(new Uri(pgsqlzipurl),
                    tempfile);
                while (!IsDownload)
                {
                    Thread.Sleep(10);
                }
                web.Dispose();
            }
        }

        private static string ConvertByteToMb(long Bytes)
        {
            return ((Bytes / 1024f) / 1024f).ToString("#0.##");
        }
    }

    /// <summary>
    /// 数据库安装选项
    /// </summary>
    public class DataBaseOptions
    {
        public DataBaseOptions()
        {
            DataBaseSetupPath = Path.Combine(AppContext.BaseDirectory, "database");
            DataBasePath = Path.Combine(AppContext.BaseDirectory, "database", "data");
        }

        /// <summary>
        /// 安装路径
        /// </summary>
        public string DataBaseSetupPath { get; set; }

        /// <summary>
        /// 数据库文件路径
        /// </summary>
        public string DataBasePath { get; set; }
    }
}
