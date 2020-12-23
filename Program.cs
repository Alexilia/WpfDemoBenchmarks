using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace WpfDemoBenchmarks {
    [Config(typeof(Config))]
    [RPlotExporter]
    public class StartupDiargamDemo {
        class Config : ManualConfig {
            public Config() {
                AddJob(Job.Default
                    .WithStrategy(RunStrategy.Monitoring)
                    .WithIterationCount(15)
                    .WithToolchain(InProcessEmitToolchain.Instance));
            }
        }
        [Params("startup", "theme Office2019Black", "theme CobaltBlueOffice2019Colorful", "module")]
        public string BenchmarkName;

        bool WaitStartup { get; set; }
        Process Process { get; set; }

        [IterationSetup]
        public void SetupTheme() {
            WaitStartup = BenchmarkName != "startup";
            Process = new Process();
            Process.StartInfo.FileName = Environment.GetCommandLineArgs()[1];
            Process.StartInfo.WorkingDirectory = Path.GetDirectoryName(Process.StartInfo.FileName);
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.Arguments = "benchmark " + BenchmarkName;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.OutputDataReceived += Process_OutputDataReceived;
            Process.Start();
            Process.ProcessorAffinity = new IntPtr(3);
            Process.BeginOutputReadLine();

            while(WaitStartup)
                Thread.Sleep(1);
        }
        [IterationCleanup]
        public void CleanupChangeTheme() {
            if(!Process.HasExited)
                Process.Kill();
            else
                Process.Dispose();
            Process = null;
        }
        [Benchmark]
        public void RunDemo() {
            Process.WaitForExit();
        }
        void Process_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            if(string.IsNullOrEmpty(e.Data))
                return;
            if(BenchmarkName.StartsWith(e.Data))
                WaitStartup = false;
        }
    }

    class Program {
#if DEBUG
        static void Main(string[] args) {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly, new DebugInProcessConfig());
        }
#else
        static void Main(string[] args) {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
#endif
    }
}
