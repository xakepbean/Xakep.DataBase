using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Xakep.DataBase
{
    public class DataBaseUtil
    {
        //private static void InitDataBase()
        //{
        //    using (var connection = new NpgsqlConnection("host=localhost;user id=postgres;password=postgres@123;database=postgres"))
        //    {
        //        var vExists = connection.ExecuteScalar<int>("select count(*) from pg_database where datname='databackup'") > 0;
        //        if (!vExists)
        //        {
        //            Console.WriteLine("init database ...");
        //            connection.Execute("create database databackup");
        //        }
        //    }
        //}

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

        public static void StartDataBase(DataBaseOptions options, Action<string> StandardOutput = null)
        {
            //string strPgDir = Path.Combine(AppContext.BaseDirectory, "pgsql");
            if (!Directory.Exists(options.DataBaseSetupPath))
            {
                Console.WriteLine("unzip database files ...");
                UnZipFile(options.DataBaseSetupPath);
            }
            //string strPgData = Path.Combine(AppContext.BaseDirectory, "pgsql", "pg_data");
            if (!Directory.Exists(options.DataBasePath))
            {
                Console.WriteLine("init database ...");
                InitPgSql(options, p => Console.WriteLine(p));
            }


            var binpath = Path.Combine(options.DataBaseSetupPath, "bin");
            var pid = Path.Combine(options.DataBasePath, "postmaster.pid");
            if (!File.Exists(pid))
            {
                StandardOutput?.Invoke("start database ...");
                string strCmd = $"pg_ctl -D {options.DataBasePath} -l logfile start";
                var vLine = RunInDirTimeoutPipeline(binpath, strCmd, Console.OutputEncoding, StandardOutput);
            }
            StandardOutput?.Invoke("database runing ...");
        }

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

        private static void UnZipFile(string TargetDirectory)
        {
            var ExecAssembly = Assembly.GetExecutingAssembly();
            var pgsqlzip = $"{ExecAssembly.GetName().Name}.zippgsql.zip";
            var pgsqlfile = Path.Combine(AppContext.BaseDirectory, "pgsql.zip");
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
            if (File.Exists(pgsqlfile))
            {
                UnZip(pgsqlfile, TargetDirectory);
                File.Delete(pgsqlfile);
            }
        }

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

                    if (pathToZip != "")
                        directoryName = Path.GetDirectoryName(pathToZip) + Path.DirectorySeparatorChar;

                    string fileName = Path.GetFileName(pathToZip);

                    Directory.CreateDirectory(TargetDirectory + directoryName);

                    if (fileName != "")
                    {
                        if ((File.Exists(TargetDirectory + directoryName + fileName) && OverWrite) || (!File.Exists(TargetDirectory + directoryName + fileName)))
                        {
                            using (FileStream streamWriter = File.Create(TargetDirectory + directoryName + fileName))
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
                                streamWriter.Close();
                            }
                        }
                    }
                }

                zipfiles.Close();
            }
        }
    }

    public class DataBaseOptions
    {
        public string DataBaseSetupPath { get; set; }
        public string DataBasePath { get; set; }
    }
}
