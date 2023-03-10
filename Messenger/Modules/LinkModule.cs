using Messenger.Extensions;
using Messenger.Models;
using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Messenger.Modules
{
    internal class LinkModule
    {
        private LinkModule() { }

        private static readonly LinkModule s_ins = new LinkModule();

        private LinkClient _client = null;

        private readonly object _locker = new object();

        public static int Id => s_ins._client?.Id ?? ProfileModule.Id;

        public static bool IsRunning => s_ins._client?.IsRunning ?? false;

        public static async Task<Task> Start(int id, IPEndPoint endpoint)
        {
            var clt = await LinkClient.Connect(id, endpoint, _RequestHandler);

            void _OnReceived(object sender, LinkEventArgs<LinkPacket> args) => RouteModule.Invoke(args.Object);

            void _OnDisposed(object sender, LinkEventArgs<Exception> args)
            {
                clt.Received -= _OnReceived;
                clt.Disposed -= _OnDisposed;

                lock (s_ins._locker)
                    s_ins._client = null;
                var obj = args.Object;
                if (obj == null || obj is OperationCanceledException)
                    return;
                Entrance.ShowError("Connection lost", obj);
            }

            clt.Received += _OnReceived;
            clt.Disposed += _OnDisposed;

            lock (s_ins._locker)
            {
                if (s_ins._client != null)
                {
                    clt.Dispose();
                    throw new InvalidOperationException();
                }
                s_ins._client = clt;
            }

            HistoryModule.Handled += _HistoryHandled;
            ShareModule.PendingList.ListChanged += _PendingListChanged;

            ProfileModule.SetId(id);

            PostModule.UserProfile(Links.Id);
            PostModule.UserRequest();
            PostModule.UserGroups();

            return clt.Start();
        }

        private static async Task _RequestHandler(Socket socket, LinkPacket packet)
        {
            var pth = packet.Path;
            if (pth == "share.directory" || pth == "share.file")
            {
                var src = packet.Source;
                var key = packet.Data.As<Guid>();
                await Share.Notify(src, key, socket);
            }
            else
            {
                Log.Info($"Path \"{pth}\" not supported.");
            }
        }

        [Loader(0, LoaderFlags.OnExit)]
        public static void Shutdown()
        {
            lock (s_ins._locker)
            {
                s_ins._client?.Dispose();
                s_ins._client = null;
            }

            ShareModule.Shutdown();
            ProfileModule.Clear();
            HistoryModule.Handled -= _HistoryHandled;
            ShareModule.PendingList.ListChanged -= _PendingListChanged;
        }

        public static void Enqueue(byte[] buffer) => s_ins._client?.Enqueue(buffer);

        public static List<IPEndPoint> GetEndPoints()
        {
            var lst = new List<IPEndPoint>();
            if (Extension.Lock(s_ins._locker, ref s_ins._client, out var clt) == false)
                return lst;
            lst.Add(clt.InnerEndPoint);
            lst.Add(clt.OuterEndPoint);
            var res = lst.Distinct().ToList();
            return res;
        }

        private static void _HistoryHandled(object sender, LinkEventArgs<Packet> e)
        {
            var hdl = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            if (e.Finish == false || Application.Current.MainWindow.IsActive == false)
                _ = NativeMethods.FlashWindow(hdl, true);
            return;
        }

        private static void _PendingListChanged(object sender, ListChangedEventArgs e)
        {
            if (sender == ShareModule.PendingList && e.ListChangedType == ListChangedType.ItemAdded)
            {
                if (Application.Current.MainWindow.IsActive == true)
                    return;
                var hdl = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                _ = NativeMethods.FlashWindow(hdl, true);
            }
        }
    }
}
