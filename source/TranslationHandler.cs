using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.ComponentModel;
using System.Globalization;

namespace TRTR
{

    class TranslationHandler
    {
        static Dictionary<string, FileStream> fileList = new Dictionary<string, FileStream>();
        static Dictionary<string, Type> procIntfList = new Dictionary<string, Type>();
        static List<TRTR.Processors.ITransProc> processors = new List<Processors.ITransProc>();

        static public void Init()
        {
            Type a = typeof(TRTR.Processors.SampleTransProc);
            procIntfList.Clear();
//            procIntfList.Add("MNU", typeof(TRTR.Processors.MenuFileIntf));
            procIntfList.Add("CINE", typeof(TRTR.Processors.CineFileIntf));
            //procIntfList.Add("FNTENC", typeof(TRTR.Processors.FontFileIntf));
//            procIntfList.Add("EXTSCH", typeof(TRTR.Processors.SubtitleFileIntf));
        }

        static void SetGameInfo()
        {
            processors.Clear();

            // close opened files
            foreach (FileStream str in fileList.Values)
                if (str != null)
                    str.Close();
            fileList.Clear();

            // add required processors
            foreach (string proc in TRGameInfo.Processors)
                if (procIntfList.ContainsKey(proc))
                {

                    Processors.ITransProc p = (Processors.ITransProc)(Activator.CreateInstance(procIntfList[proc]));
                    processors.Add(p);
                }

            // get file list
            foreach (TRTR.Processors.ITransProc proc in processors)
            {
                foreach (string fileName in proc.GetFileList())
                    fileList[fileName] = null;
            }
        }

        enum Operations { Undefined, Extract, CreateTranslation, CreateRestoration, Translate, Restore };

        class OperationArgs
        {
            public Operations op = Operations.Undefined;
            public Object e = null;

            public OperationArgs(Operations op, Object e)
            {
                this.op = op;
                this.e = e;
            }

            public OperationArgs(Operations op)
            {
                this.op = op;
                this.e = null;
            }
        }

        static void Extract() { Do(new OperationArgs(Operations.Extract)); } // extracts translatable data
        static void CreateTranslation() { Do(new OperationArgs(Operations.CreateTranslation)); } // creates translation xml
        static void CreateRestoration() { Do(new OperationArgs(Operations.CreateRestoration)); } // creates restoration xml
        static void Translate(bool simulated) { Do(new OperationArgs(Operations.Translate, simulated)); } // translates game
        static void Restore(bool simulated) { Do(new OperationArgs(Operations.Restore, simulated)); } // restores game

        static void Do(OperationArgs args)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;

            bw.DoWork += new DoWorkEventHandler(worker_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker.RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(Worker.ProgressChanged);

            Worker.WorkerStart(bw, null);
            
            bw.RunWorkerAsync(args);
        }
        static internal class Worker
        {
            static public DoWorkEventHandler WorkerStart;
            static public ProgressChangedEventHandler ProgressChanged;
            static public RunWorkerCompletedEventHandler RunWorkerCompleted;
            static public CultureInfo CurrentCulture;
        }
        static private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            OperationArgs args = (OperationArgs)(e.Argument);
            // try lock files for writing
            // 
            switch (args.op)
            {
        //       enum Operations { Undefined, Extract, CreateTranslation, CreateRestoration, Translate, Restore };
                case Operations.Extract:
                    break;
                case Operations.CreateTranslation:
                    break;
                case Operations.CreateRestoration:
                    break;
                case Operations.Translate:
                    break;
                case Operations.Restore:
                    break;
                default:
                    throw new Exception(Errors.InvalidParameter);
            }
        }
    }
}
