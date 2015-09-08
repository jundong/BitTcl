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
    public class TclWorker
    {
        [NonSerialized]
        private BackgroundWorker worker;
        /// <summary>
        /// Background worker for tcl input and output control
        /// </summary>
        [Browsable(false)]
        public BackgroundWorker Worker
        {
            get
            {
                return worker;
            }
        }

        /// <summary>
        /// Ctor with reading Register to load Tcl information
        /// </summary>
        public TclWorker()
        {
            InitTcl();
            InitWorker();
        }
        /// <summary>
        /// Ctor with configure of Tcl information
        /// </summary>
        /// <param name="tcl_app">~\tcl8.4\bin\tclsh84.exe</param>
        /// <param name="exe_dir">application output path</param>
        public TclWorker(string tcl_app, string exe_dir)
        {
            tclApp = tcl_app;
            exeDir = exe_dir;
            InitWorker();
        }
        public TclWorker(string tcl_app)
        {
            tclApp = tcl_app;
            exeDir = System.IO.Directory.GetCurrentDirectory();
            InitWorker();
        }

        /// <summary>
        /// 刷新Tcl环境参数
        /// </summary>
        public void Refresh()
        {
            InitTcl();
            InitWorker();
        }
        /// <summary>
        /// 等待Tcl返回值
        /// </summary>
        public void WaitForTcl()
        {
            int output_count = 0;
            output_count = m_TclOutput.Count + m_TclErr.Count;
            while (m_TclOutput.Count + m_TclErr.Count == output_count)
            {
                try
                {
                    Thread.Sleep(1000);
                }
                catch
                {
                    return;
                }
            }
        }
        public int WaitForTcl(string tclString)
        {
             //wait from Tcl Shell
            int ackIndex = m_TclOutput.LastIndexOf(tclString);

            while (m_TclOutput.LastIndexOf(tclString) == ackIndex)
            {

                try
                {
                    Thread.Sleep(1000);
                }
                catch
                {
                    return -1;
                }
            }
            return m_TclOutput.LastIndexOf(tclString);
        }
        public int WaitForTcl(string tclString, int timeoutInSecond)
        {
            //wait from Tcl Shell
            int ackIndex = m_TclOutput.LastIndexOf(tclString);

            while (m_TclOutput.LastIndexOf(tclString) == ackIndex)
            {

                try
                {
                    Thread.Sleep(1000);
                    timeoutInSecond--;
                    if (timeoutInSecond <= 0)
                    {
                        return -1;
                    }
                }
                catch
                {
                    return -1;
                }
            }
            return m_TclOutput.LastIndexOf(tclString);
        }

        /// <summary>
        /// 等待用例执行完毕
        /// </summary>
        public void WaitEnd()
        {
            while (true)
            {
                if (m_TclProcess == null || m_TclProcess.HasExited)
                {
                    break;
                }
                if (status == Status.Running)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                else
                {
                    break;
                }
            }
        }
        /// <summary>
        /// 等待用例执行完毕
        /// </summary>
        /// <param name="timeoutInSecond">超时时间</param>
        public void WaitEnd(int timeoutInSecond)
        {
            while (true)
            {
                if (status == Status.Running)
                {
                    Thread.Sleep(1000);
                    timeoutInSecond--;
                    if (timeoutInSecond == 0)
                    {
                        break;
                    }
                    continue;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Tcl environment init
        /// </summary>
        private void InitTcl()
        {

            exeDir = System.IO.Directory.GetCurrentDirectory();
            tclRoot = exeDir + "\\tcl";
            tclApp = exeDir + "\\tcl\\bin\\tclsh.exe";
 
        }
        /// <summary>
        /// Register worker event
        /// </summary>
        private void InitWorker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        /// <summary>
        /// The work to do.
        /// Generally tcl input is defined in this method.
        /// </summary>
        public virtual void DoWork() { }
        /// <summary>
        /// The clean job after work
        /// </summary>
        public virtual void CleanWork() { }

        /// <summary>
        /// worker.RunWorkerCompleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (m_TclErr.Count > 0)
            {
                status = Status.ErrStop;
            }
            else
            {
                status = Status.Complete;
            }
            //progress = maxProgress;
            CleanWork();
        }
        /// <summary>
        /// worker.ProgressChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) 
        {
            progress = e.ProgressPercentage;
        }
        /// <summary>
        /// worker.DoWork
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void worker_DoWork(object sender, DoWorkEventArgs e) 
        {
            //wait ACK from Tcl Shell
            int ackIndex = m_TclOutput.LastIndexOf("ACK");
            int nakIndex = m_TclOutput.LastIndexOf("NAK");

            DoWork();

            int reportProgress = 0;
            progress = 0;
            //int tempIndex;
            while (m_TclOutput.LastIndexOf("ACK") == ackIndex && 
                m_TclOutput.LastIndexOf("NAK") == nakIndex)
            {
                //tempIndex = m_TclOutput.LastIndexOf("ACK");
                if (m_TclErr.Count > 0)
                {
                    status = Status.ErrStop;
                    progress = maxProgress;
                    return;
                }
                if (m_TclOutput.Count > 0)
                {
                    List<string> tempOutput = new List<string>();
                    tempOutput.AddRange(m_TclOutput);
                    foreach (string progStr in tempOutput)
                    {
                        int.TryParse(progStr, out progress);
                        if (progress > reportProgress)
                        {
                            reportProgress = progress;
                        }
                    }
                    try
                    {
                        worker.ReportProgress(reportProgress);
                    }
                    catch
                    {
                        return;
                    }
                    finally
                    {
                        tempOutput.Clear();
                    }
                }
                try
                {
                    Thread.Sleep(3000);
                }
                catch
                {
                    return;
                }
            }

            if (m_TclOutput.LastIndexOf("ACK") != ackIndex)
            {
                reportProgress = maxProgress;
                status = Status.Complete;
            }

            if(m_TclOutput.LastIndexOf("NAK") != nakIndex)
            {
                status = Status.ErrStop;
                errProgress = reportProgress;
                reportProgress = maxProgress;
                m_TclErr.Add(m_TclOutput[m_TclOutput.Count - 1]);
            }
                
            try
            {
                worker.ReportProgress(reportProgress);
            }
            catch
            {
                return;
            }

        }

        # region Tcl param setting
        private string tclRoot = "";
        [Browsable(false)]
        public string TclRoot
        {
            get
            {
                return tclRoot;
            }
        }

        private string tclApp = "";
        /// <summary>
        /// Tclsh84.exe所在路径
        /// </summary>
        [Browsable(false)]
        public string TclApp
        {
            get { return tclApp; }
        }

        private string exeDir = "";
        /// <summary>
        /// 运行当前目录
        /// </summary>
        [Browsable(false)]
        public string ExeDir
        {
            get { return exeDir; }
            set { exeDir = value; }
        }

        private StreamWriter errLog = null;
        /// <summary>
        /// 运行当前目录
        /// </summary>
        [Browsable(false)]
        public StreamWriter ErrLog
        {
            get {
                if (errLog == null)
                {
                    errLog = new StreamWriter(exeDir + "\\" + DateTime.Now.Ticks + "_err.txt", true);
                }
                return errLog; 
            }
        }

        private StreamWriter outputLog = null;
        /// <summary>
        /// 运行当前目录
        /// </summary>
        [Browsable(false)]
        public StreamWriter OutputLog
        {
            get {
                if (outputLog == null)
                {
                    outputLog = new StreamWriter(exeDir + "\\" + DateTime.Now.Ticks + "_log.txt", true);
                }
                return outputLog; 
            }
        }
        #endregion

        # region Tcl Process setting
        private List<string> m_TclOutput = new List<string>();
        private List<string> m_TclErr = new List<string>();
        [NonSerialized]
        private Process m_TclProcess;
        private int max_list_len = 100;

        private List<string> bg_output = new List<string>();
        /// <summary>
        /// 输出备份
        /// </summary>
        [Browsable(false)]
        public List<string> BG_Output
        {
            get
            {
                return bg_output;
            }

        }

        /// <summary>
        /// 是否备份输出
        /// </summary>
        [Browsable(false)]
        public bool BG_Output_Exist
        {
            get
            {
                if (bg_output.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 获取指定备份文件内容
        /// </summary>
        /// <param name="index">备份文件数组下标</param>
        /// <returns></returns>
        public List<string> PopOutputFromBG(int index)
        {
            string popFile = bg_output[index];
            List<string> bg_output_content = new List<string>();
            if (File.Exists(popFile))
            {
                StreamReader sr = new StreamReader(popFile);
                while (!sr.EndOfStream)
                {
                    string output = sr.ReadLine();
                    bg_output_content.Add(output);
                }
            }
            return bg_output_content;
        }
        #endregion

        /// <summary>
        /// 将Tcl数组转换为DotNet数组
        /// </summary>
        /// <param name="tclStr"></param>
        /// <returns></returns>
        private List<string> TclString2ListString(string tclStr)
        {
            tclStr = tclStr.Replace('{', '_');
            tclStr = tclStr.Replace('}', '_');
            string[] tclList = tclStr.Split('_');
            List<string> newStringList = new List<string>();
            for (int index = 0; index < tclList.Length; index++)
            {
                if (tclList[index] == " ")
                {
                    continue;
                }
                if (tclList[index] == "")
                {
                    continue;
                }
                newStringList.Add(tclList[index]);
            }
            return newStringList;
        }

        /// <summary>
        /// 运行Tcl脚本
        /// </summary>
        /// <param name="argument"> argument = "\"" + exeDir + "\\x.tcl\" + args + "\"" </param>
        protected void runTclScript(string argument)
        {
            m_TclErr.Clear();
            m_TclOutput.Clear();

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = tclApp;
            info.RedirectStandardOutput = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.Arguments = argument;

            m_TclProcess = new Process();
            m_TclProcess.StartInfo = info;
            //m_TclProcess.EnableRaisingEvents = true;
            m_TclProcess.OutputDataReceived += new DataReceivedEventHandler(m_TclProcess_OutputDataReceived);
            m_TclProcess.ErrorDataReceived += new DataReceivedEventHandler(m_TclProcess_ErrorDataReceived);
            m_TclProcess.Exited += new EventHandler(transmit_Exited);


            m_TclProcess.Start();
            m_TclProcess.BeginOutputReadLine();
            m_TclProcess.BeginErrorReadLine();
            m_TclProcess.WaitForExit();
            m_TclProcess.Close();
        }
        /// <summary>
        /// 异步运行脚本
        /// </summary>
        /// <param name="argument"> argument = "\"" + exeDir + "\\x.tcl\" + args + "\"" </param>
        protected void runTclScriptAsync(string argument)
        {
            m_TclErr.Clear();
            m_TclOutput.Clear();

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = tclApp;
            info.RedirectStandardOutput = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.Arguments = argument;

            m_TclProcess = new Process();
            m_TclProcess.StartInfo = info;
            //m_TclProcess.EnableRaisingEvents = true;
            m_TclProcess.OutputDataReceived += new DataReceivedEventHandler(m_TclProcess_OutputDataReceived);
            m_TclProcess.ErrorDataReceived += new DataReceivedEventHandler(m_TclProcess_ErrorDataReceived);
            m_TclProcess.Exited += new EventHandler(transmit_Exited);

            m_TclProcess.Start();
            m_TclProcess.BeginOutputReadLine();
            m_TclProcess.BeginErrorReadLine();
        }

        #region Tcl Process Event Handler
        void transmit_Exited(object sender, EventArgs e)
        {
            m_TclProcess.CancelErrorRead();
            m_TclProcess.CancelOutputRead();
            //m_TclProcess.Kill();
            m_TclProcess.Close();
            m_TclProcess = null;

        }

        void m_TclProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                m_TclErr.Add(e.Data);
                ErrLog.WriteLine(e.Data);
            }
        }

        void m_TclProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                m_TclOutput.Add(e.Data);
                OutputLog.WriteLine(e.Data);
                if (m_TclOutput.Count > max_list_len)
                {
                    m_TclOutput.Clear();
                }
            }
        }
        #endregion

        /// <summary>
        /// TCL输出列表
        /// </summary>
        [Browsable(false)]
        public List<string> TclOutput
        {
            get
            {
                return m_TclOutput;
            }
        }

        /// <summary>
        /// TCL 输出表中的最后一项
        /// </summary>
        [Browsable(false)]
        public string TclResult
        {
            get
            {
                if (m_TclOutput.Count > 0)
                {
                    return m_TclOutput[m_TclOutput.Count - 1];
                }
                return "";
            }
        }

        /// <summary>
        /// TCL错误输出列表
        /// </summary>
        [Browsable(false)]
        public List<string> TclErr
        {
            get
            {
                return m_TclErr;
            }
        }

        private int maxProgress = 10;
        /// <summary>
        /// 最大进度
        /// </summary>
        [Browsable(false)]
        public int MaxProgress
        {
            get
            {
                return maxProgress;
            }
        }

        private int progress = 0;
        /// <summary>
        /// 运行进度
        /// </summary>
        [Browsable(false)]
        public int Progress
        {
            get
            {
                return progress;
            }
            set
            {
                progress = value;
            }
        }

        private int errProgress = 0;
        /// <summary>
        /// 若以失败结束，结束时的进度
        /// </summary>
        [Browsable(false)]
        public int ErrProgress
        {
            get
            {
                return errProgress;
            }
            set
            {
                errProgress = value;
            }
        }

        /// <summary>
        /// 运行状态
        /// </summary>
        public enum Status
        {
            Running = 0,
            ErrStop,
            UserStop,
            Complete,
            Init
        }

        private Status status = Status.Init;
        /// <summary>
        /// 运行状态
        /// </summary>
        [Browsable(false)]
        public Status ProcessStatus
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
            }
        }

        /// <summary>
        /// 取消运行
        /// </summary>
        public void Cancel()
        {
            try
            {
                m_TclProcess.Kill();
             
            }
            catch 
            {
                m_TclProcess = new Process();
            }
            try
            {
                worker.CancelAsync();
                worker = new BackgroundWorker();
                worker.RunWorkerCompleted +=
                    new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
                worker.DoWork +=
                    new DoWorkEventHandler(worker_DoWork);
                worker.ProgressChanged +=
                    new ProgressChangedEventHandler(worker_ProgressChanged);
                worker.WorkerSupportsCancellation = true;
                worker.WorkerReportsProgress = true;

                if (ErrLog.BaseStream != null)
                {
                    ErrLog.Close();
                }
                if (OutputLog.BaseStream != null)
                {
                    OutputLog.Close();
                }
            }
            catch { }
        }

        /// <summary>
        /// 向TCL输出命令
        /// </summary>
        /// <param name="args">字符串</param>
        public void WriteToTcl(string args)
        {
            m_TclProcess.StandardInput.WriteLine(args);
        }
        /// <summary>
        /// 向TCL输出命令
        /// </summary>
        /// <param name="argsList">字符串列表</param>
        public void WriteToTcl(List<string> argsList)
        {
            string args = "";
            foreach (string element in argsList)
            {
                args += element + " ";
            }
            m_TclProcess.StandardInput.WriteLine(args);
        }

        /// <summary>
        /// 重设状态/进度/输出
        /// </summary>
        public void Reset()
        {
            status = Status.Init;
            progress = 0;
            m_TclOutput.Clear();
            m_TclErr.Clear();
        }

    }
}
