using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.Win32;
using System.Threading;
using System.Resources;
using System.IO;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace BitTcl
{
    [Serializable]
    public class TestCase : TclWorker
    {
        /// <summary>
        /// 执行脚本
        /// </summary>
        [Browsable(false)]
        public string TestScript
        {
            set
            {
                StreamWriter swTestScript = new StreamWriter(ExeDir + "\\TestCase.tcl");
                swTestScript.Write(value);
                swTestScript.Close();
            }
        }

        private DateTime testStartTime = new DateTime();
        public DateTime StartTime
        {
            get
            {
                return testStartTime;
            }
        }
        private DateTime testEndTime = new DateTime();
        public DateTime EndTime
        {
            get
            {
                return testEndTime;
            }
        }
        public int TestTime
        {
            get
            {
                return (int)((testEndTime - testStartTime).TotalSeconds);
            }
        }

        /// <summary>
        /// 构造测试用例对象
        /// </summary>
        /// <param name="script">Tcl script</param>
        public TestCase(string script)
        {
            TestScript = script;
        }
        /// <summary>
        /// 构造测试用例对象(run single tcl command)
        /// </summary>
        public TestCase()
        {
            TestScript = BitTcl.Properties.Resources.shell;
        }
        public TestCase(string script, string tcl_app):base(tcl_app)
        {
            TestScript = script;
        }

        /// <summary>
        /// 运行测试
        /// </summary>
        public void Run()
        {
            if (ProcessStatus == Status.Running)
            {
                return;
            }

            testStartTime = DateTime.Now;

            string argument = "\"" + ExeDir + "\\TestCase.tcl\"";
            if (!File.Exists(ExeDir + "\\TestCase.tcl"))
            {
                throw new FileNotFoundException("脚本未支持，敬请期待！");
            }

            ProcessStatus = Status.Running;

            runTclScriptAsync(argument);

            Worker.RunWorkerAsync();
        }
        /// <summary>
        /// 停止测试
        /// </summary>
        public void Stop()
        {
            if (ProcessStatus == Status.Running || ProcessStatus == Status.Init)
            {
                testEndTime = DateTime.Now;
                Cancel();
                ProcessStatus = Status.UserStop;
            }
            try
            {
                File.Delete(ExeDir + "\\TestCase.tcl");
            }
            catch { }
        }


        /// <summary>
        /// 程序结束操作
        /// </summary>
        public override void CleanWork()
        {
            testEndTime = DateTime.Now;
            //ProcessStatus = Status.Complete;
            try
            {
                File.Delete(ExeDir + "\\TestCase.tcl");
            }
            catch
            { 
            }
        }

        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="file">保存文件</param>
        /// <param name="rr">对象</param>
        public static void Serialize(string file, TestCase rr)
        {
            Stream stream = File.Open(file, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, rr);
            stream.Close();
        }
        public static void Serialize(string file, List<TestCase> rr)
        {
            Stream stream = File.Open(file, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, rr);
            stream.Close();

        }
        public static void Serialize(TestCase rr)
        {
            Serialize(rr.ExeDir + "\\default.tccfg", rr);
        }
        public static void Serialize(List<TestCase> rr)
        {
            if (rr.Count > 0)
            {
                Serialize(rr[0].ExeDir + "\\default.tclcfg", rr);
            }
        }

        /// <summary>
        /// 解序列化对象
        /// </summary>
        /// <param name="file">读取文件</param>
        /// <returns>对象</returns>
        public static TestCase Deserialize(string file)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            TestCase obj = (TestCase)formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }
        /// <summary>
        /// 解序列化对象列表
        /// </summary>
        /// <param name="file">读取文件</param>
        /// <returns>对象列表</returns>
        public static List<TestCase> DeserializeListObj(string file)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            List<TestCase> obj = (List<TestCase>)formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

    }

}
