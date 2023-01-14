using Messenger.Models;
using Messenger.Modules;
using Mikodev.Logger;
using Mikodev.Network;

namespace Messenger.Controllers
{
    public class MessageController : LinkPacket
    {
        [Route("msg.text")]
        public void Text()
        {
            var txt = Data.As<string>();
            _ = HistoryModule.Insert(Source, Target, "text", txt);
        }

        [Route("msg.image")]
        public void Image()
        {
            var buf = Data.As<byte[]>();
            _ = HistoryModule.Insert(Source, Target, "image", buf);
        }

        [Route("msg.notice")]
        public void Notice()
        {
            var typ = Data["type"].As<string>();
            var par = Data["parameter"].As<string>();
            var str = typ == "share.file"
                ? $"File successfully received {par}"
                : typ == "share.dir"
                    ? $"Folder successfully received {par}"
                    : null;
            if (str == null)
                Log.Info($"Unknown notice type: {typ}, parameter: {par}");
            else
                _ = HistoryModule.Insert(Source, Target, "notice", str);
            return;
        }
    }
}
