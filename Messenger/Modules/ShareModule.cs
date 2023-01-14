using Messenger.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Messenger.Modules
{
    internal class ShareModule : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private const string _KeyPath = "share-save-path";

        private const string _Path = "Share";

        private bool _hasShare = false;

        private bool _hasReceiver = false;

        private bool _hasPending = false;

        private string _savepath = null;

        private readonly BindingList<Share> _shareList = new BindingList<Share>();

        private readonly BindingList<ShareReceiver> _receiverList = new BindingList<ShareReceiver>();

        private readonly BindingList<ShareReceiver> _pendingList = new BindingList<ShareReceiver>();

        public bool HasShare
        {
            get => _hasShare;
            set => OnPropertyChange(ref _hasShare, value);
        }

        public bool HasReceiver
        {
            get => _hasReceiver;
            set => OnPropertyChange(ref _hasReceiver, value);
        }

        public bool HasPending
        {
            get => _hasPending;
            set => OnPropertyChange(ref _hasPending, value);
        }

        #region PropertyChange

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangingEventHandler PropertyChanging;

        private void OnPropertyChange<T>(ref T source, T target, [CallerMemberName] string name = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
            if (Equals(source, target))
                return;
            source = target;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        private ShareModule()
        {
            _shareList.ListChanged += (s, e) => HasShare = _shareList.Count > 0;
            _receiverList.ListChanged += (s, e) => HasReceiver = _receiverList.Count > 0;
            _pendingList.ListChanged += (s, e) => HasPending = _pendingList.Count > 0;
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------

        private static readonly ShareModule s_ins = new ShareModule();

        public static string SavePath
        {
            get => s_ins._savepath;
            set
            {
                s_ins._savepath = value;
                EnvironmentModule.Update(_KeyPath, value);
            }
        }

        public static ShareModule Instance => s_ins;

        public static BindingList<Share> ShareList => s_ins._shareList;

        public static BindingList<ShareReceiver> ReceiverList => s_ins._receiverList;

        public static BindingList<ShareReceiver> PendingList => s_ins._pendingList;

        public static void Register(ShareReceiver receiver)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                s_ins._receiverList.Add(receiver);
                s_ins._pendingList.Add(receiver);
                receiver.PropertyChanged += _RemovePending;
            });
        }

        private static void _RemovePending(object sender, PropertyChangedEventArgs e)
        {
            var pro = e.PropertyName;
            if (pro != nameof(ShareReceiver.IsStarted) && pro != nameof(ShareReceiver.IsDisposed))
                return;
            var obj = s_ins._pendingList.FirstOrDefault(r => ReferenceEquals(r, sender));
            if (obj == null)
                return;
            _ = s_ins._pendingList.Remove(obj);
            obj.PropertyChanged -= _RemovePending;
        }

        public static void Shutdown()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var i in s_ins._shareList)
                    i.Dispose();
                foreach (var i in s_ins._receiverList)
                    i.Dispose();
            });
        }

        public static List<IFinal> Remove()
        {
            var lst = new List<IFinal>();
            void _Remove<T>(IList<T> list) where T : IFinal
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var val = list[i];
                    if (val.IsFinal == false)
                        continue;
                    lst.Add(val);
                    list.RemoveAt(i);
                    i--;
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var i in s_ins._shareList)
                    _Remove(i.WorkerList);
                _Remove(s_ins._shareList);
                _Remove(s_ins._receiverList);
            });
            return lst;
        }

        #region Other methods

        public static FileInfo AvailableFile(string name)
        {
            var dir = new DirectoryInfo(s_ins._savepath);
            if (dir.Exists == false)
                dir.Create();
            var inf = new FileInfo(Path.Combine(dir.FullName, name));
            if (inf.Exists == false)
                return inf;

            var pre = Path.GetFileNameWithoutExtension(name);
            var ext = Path.GetExtension(name);
            var str = $"{pre}@{DateTime.Now:yyyyMMdd-HHmmss-fff}{ext}";
            var res = new FileInfo(Path.Combine(dir.FullName, str));
            if (res.Exists)
                throw new IOException();
            return res;
        }

        public static DirectoryInfo AvailableDirectory(string name)
        {
            var dir = new DirectoryInfo(s_ins._savepath);
            if (dir.Exists == false)
                dir.Create();
            var pth = Path.Combine(dir.FullName, name);
            var inf = new DirectoryInfo(pth);
            if (inf.Exists == false)
                return inf;

            var str = $"{name}@{DateTime.Now:yyyyMMdd-HHmmss-fff}";
            var res = new DirectoryInfo(Path.Combine(dir.FullName, str));
            if (res.Exists)
                throw new IOException();
            return res;
        }

        [Loader(32, LoaderFlags.OnLoad)]
        public static void Load()
        {
            s_ins._savepath = EnvironmentModule.Query(_KeyPath, _Path);
        }

        #endregion
    }
}
