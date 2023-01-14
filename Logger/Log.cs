using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Logger
{
    public static class Log
    {
        private const int _MaxQueueLength = 256;

        internal static readonly string _prefix = $"[{nameof(Logger)}]";

        internal static readonly object s_filelocker = new object();

        internal static readonly Queue<string> s_queue = new Queue<string>();

        internal static TraceListener s_listener = null;

        internal static StreamWriter s_writer = null;

        internal static CancellationTokenSource s_cancel = null;

        internal static Task s_task = null;

        public static void Run(string path)
        {
            lock (s_filelocker)
            {
                if (s_listener is null)
                    _ = Trace.Listeners.Add(s_listener = new LogListener());

                s_writer?.Dispose();
                var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                s_writer = new StreamWriter(stream, Encoding.UTF8, 1024, false);

                if (s_cancel is null && s_task is null)
                {
                    var cancel = new CancellationTokenSource();
                    s_task = Task.Run(new Func<Task>(() => _Monitor(cancel.Token)));
                    s_cancel = cancel;
                }
            }
        }

        public static void Close()
        {
            lock (s_filelocker)
            {
                var tsk = s_task;
                var src = s_cancel;
                var wtr = s_writer;

                if (tsk is null || src is null || wtr is null)
                    return;

                src.Cancel();
                tsk.Wait();
                src.Dispose();
                wtr.Dispose();

                s_task = null;
                s_cancel = null;
                s_writer = null;
            }
        }

        private static async Task _Monitor(CancellationToken token)
        {
            while (true)
            {
                var arr = default(string[]);
                lock (s_queue)
                {
                    arr = s_queue.ToArray();
                    s_queue.Clear();
                }

                if (arr.Length < 1)
                {
                    if (token.IsCancellationRequested)
                        return;
                    await Task.Delay(4);
                    continue;
                }

                try
                {
                    lock (s_filelocker)
                    {
                        var wtr = s_writer;
                        foreach (var i in arr)
                            wtr.Write(i);
                        wtr.Flush();
                    }
                }
                catch (Exception ex)
                {
                    _InternalError(ex.ToString());
                }
            }
        }

        private static void _Enqueue(string msg)
        {
            lock (s_queue)
            {
                var len = s_queue.Count + 1;
                var sub = len - _MaxQueueLength;
                if (sub > 0)
                {
                    for (var i = 0; i < len; i++)
                        _ = s_queue.Dequeue();
                    _InternalError("Log queue full!");
                }
                s_queue.Enqueue(msg);
            }
        }

        public static void Error(Exception err, [CallerMemberName] string name = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            if (err == null)
                return;
            while (err is AggregateException a && a.InnerExceptions?.Count == 1 && a.InnerException is Exception val)
                err = val;
            Info(err.ToString(), name, file, line);
        }

        public static void Info(string message, [CallerMemberName] string name = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            if (message == null)
                return;
            var lbr = Environment.NewLine;

            var msg = $"[Time: {DateTime.Now:u}]" + lbr +
                $"[Document: {file}]" + lbr +
                $"[Linenumber: {line}]" + lbr +
                $"[Method: {name}]" + lbr +
                $"{message}" + lbr + lbr;

            Trace.Write(_prefix + Environment.NewLine + msg);
            _Enqueue(msg);
        }

        internal static void _InternalError(string msg)
        {
            Trace.WriteLine(_prefix + Environment.NewLine + msg);
        }

        internal static void _Trace(string txt)
        {
            if (string.IsNullOrEmpty(txt) || txt.StartsWith(_prefix))
                return;

            var lbr = Environment.NewLine;
            var msg = $"[Time: {DateTime.Now:u}]" + lbr +
                $"[Source: {nameof(Trace)}]" + lbr +
                $"{txt}" + lbr + lbr;
            _Enqueue(msg);
        }
    }
}
