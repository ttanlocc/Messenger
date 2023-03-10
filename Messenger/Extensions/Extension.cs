using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Messenger.Extensions
{
    internal static class Extension
    {
        internal static readonly IReadOnlyList<string> s_units = new[] { string.Empty, "K", "M", "G", "T", "P", "E" };

        internal static bool ToHostEx(string str, out string host, out int port)
        {
            if (string.IsNullOrWhiteSpace(str))
                goto fail;
            var idx = str.LastIndexOf(':');
            if (idx < 0)
                goto fail;
            host = str.Substring(0, idx);
            if (string.IsNullOrWhiteSpace(host))
                goto fail;
            if (int.TryParse(str.Substring(idx + 1), out port) == false)
                goto fail;
            return true;

        fail:
            host = null;
            port = 0;
            return false;
        }

        internal static string ToUnitEx(long length)
        {
            if (ToUnitEx(length, out var len, out var pos))
                return $"{len:0.00} {pos}B";
            else return string.Empty;
        }

        internal static bool ToUnitEx(long length, out double len, out string pos)
        {
            if (length < 0)
                goto fail;
            var tmp = length;
            var idx = 0;
            while (idx < s_units.Count - 1)
            {
                if (tmp < 1024)
                    break;
                tmp >>= 10;
                idx++;
            }
            len = length / Math.Pow(1024, idx);
            pos = s_units[idx];
            return true;

        fail:
            len = 0;
            pos = string.Empty;
            return false;
        }

        internal static IPEndPoint ToEndPointEx(this string str)
        {
            if (str == null)
                throw new ArgumentNullException();
            var idx = str.LastIndexOf(':');
            var add = str.Substring(0, idx);
            var pot = str.Substring(idx + 1);
            return new IPEndPoint(IPAddress.Parse(add.Trim()), int.Parse(pot.Trim()));
        }

        internal static IEnumerable<T> FindAttribute<T>(Assembly assembly, Type attribute, Type basic, Func<Attribute, MethodInfo, Type, T> func) =>
            (basic == null
                ? assembly.GetTypes()
                : assembly.GetTypes().Where(t => t.IsSubclassOf(basic))
            ).Select(t => t.GetMethods()
                .Where(f => f.GetCustomAttributes(attribute).Any())
                .Select(f => func.Invoke(f.GetCustomAttributes(attribute).First(), f, t))
            ).SelectMany(i => i);

        internal static async Task ReceiveFileEx(this Socket socket, string path, long length, Action<long> slice, CancellationToken token)
        {
            if (length < 0)
                throw new ArgumentException("Receive file error!");
            var fst = new FileStream(path, FileMode.CreateNew, FileAccess.Write);

            try
            {
                while (length > 0)
                {
                    var sub = (int)Math.Min(length, Links.BufferLength);
                    var buf = await socket.ReceiveAsyncEx(sub);
                    await fst.WriteAsync(buf, 0, buf.Length, token);
                    length -= sub;
                    slice.Invoke(sub);
                }
                await fst.FlushAsync(token);
                fst.Dispose();
            }
            catch (Exception)
            {
                fst.Dispose();
                File.Delete(path);
                throw;
            }
        }

        internal static async Task SendFileEx(this Socket socket, string path, long length, Action<long> slice, CancellationToken token)
        {
            var fst = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            try
            {
                if (fst.Length != length)
                    throw new ArgumentException("File length not match!");
                var buf = new byte[Links.BufferLength];
                while (length > 0)
                {
                    var min = (int)Math.Min(length, Links.BufferLength);
                    var sub = await fst.ReadAsync(buf, 0, min, token);
                    await socket.SendAsyncEx(buf, 0, sub);
                    length -= sub;
                    slice.Invoke(sub);
                }
            }
            finally
            {
                fst.Dispose();
            }
        }

        internal static async Task SendDirectoryAsyncEx(this Socket socket, string path, Action<long> slice, CancellationToken token)
        {
            async Task _SendDir(DirectoryInfo subdir, IEnumerable<string> relative)
            {
                var cur = LinksHelper.Generator.Encode(new
                {
                    type = "dir",
                    path = relative,
                });
                await socket.SendAsyncExt(cur);

                foreach (var file in subdir.GetFiles())
                {
                    var len = file.Length;
                    var buf = LinksHelper.Generator.Encode(new
                    {
                        type = "file",
                        path = file.Name,
                        length = len,
                    });
                    await socket.SendAsyncExt(buf);
                    await socket.SendFileEx(file.FullName, len, slice, token);
                }

                foreach (var dir in subdir.GetDirectories())
                {
                    await _SendDir(dir, relative.Concat(new[] { dir.Name }));
                }
            }

            await _SendDir(new DirectoryInfo(path), Enumerable.Empty<string>());
            var end = LinksHelper.Generator.Encode(new { type = "end", });
            await socket.SendAsyncExt(end);
        }

        internal static async Task ReceiveDirectoryAsyncEx(this Socket _socket, string path, Action<long> slice, CancellationToken token)
        {
            var cur = path;

            while (true)
            {
                var buf = await _socket.ReceiveAsyncExt();
                var rea = new Token(LinksHelper.Generator, buf);
                var typ = rea["type"].As<string>();

                if (typ == "dir")
                {
                    var dir = rea["path"].As<string[]>();
                    cur = Path.Combine(new[] { path }.Concat(dir).ToArray());
                    _ = Directory.CreateDirectory(cur);
                }
                else if (typ == "file")
                {
                    var key = rea["path"].As<string>();
                    var len = rea["length"].As<long>();
                    var pth = Path.Combine(cur, key);
                    await _socket.ReceiveFileEx(pth, len, slice, token);
                }
                else
                {
                    if (typ == "end")
                        return;
                    throw new ApplicationException("Batch receive error!");
                }
            }
        }

        public static List<T> RemoveEx<T>(this IList<T> lst, Func<T, bool> fun)
        {
            var idx = 0;
            var res = new List<T>();
            while (idx < lst.Count)
            {
                var val = lst[idx];
                var con = fun.Invoke(val);
                if (con == true)
                {
                    res.Add(val);
                    lst.RemoveAt(idx);
                }
                else idx++;
            }
            return res;
        }

        public static bool Lock<TE, TR>(TE locker, ref TR location, out TR value) where TE : class where TR : class
        {
            lock (locker)
            {
                var val = location;
                if (val == null)
                {
                    value = null;
                    return false;
                }

                value = val;
                return true;
            }
        }

        public static TR Lock<TE, TR>(TE locker, Func<TR> func) where TE : class
        {
            lock (locker)
            {
                return func.Invoke();
            }
        }

        public static void InsertEx(this TextBox textbox, string text)
        {
            if (textbox == null)
                throw new ArgumentNullException(nameof(textbox));
            if (text == null)
                text = string.Empty;
            var txt = textbox.Text ?? string.Empty;
            var sta = textbox.SelectionStart;
            var len = textbox.SelectionLength;
            var bef = txt.Substring(0, sta);
            var aft = txt.Substring(sta + len);
            var val = string.Concat(bef, text, aft);
            textbox.Text = val;
            textbox.SelectionStart = sta + text.Length;
            textbox.SelectionLength = 0;
        }

        public static void ScrollIntoLastEx(this ListBox listbox)
        {
            if (listbox == null)
                throw new ArgumentNullException(nameof(listbox));
            var lst = listbox.Items;
            if (lst == null)
                return;
            var index = lst.Count - 1;
            if (index < 0)
                return;
            var itm = lst[index];
            listbox.ScrollIntoView(itm);
        }

        public static void AddLimitEx<T>(this IList<T> list, T value, int max)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));
            if (max < 1)
                throw new ArgumentOutOfRangeException(nameof(max));
            list.Add(value);
            var sub = list.Count - max;
            if (sub > 0)
                for (var i = 0; i < sub; i++)
                    list.RemoveAt(0);
            return;
        }

        public static int GetInvariantHashCode(string text)
        {
            if (text is null)
                return 0;
            var hash = 5381;
            foreach (var i in text)
                hash = (hash << 5) + hash + i;
            return hash;
        }
    }
}
