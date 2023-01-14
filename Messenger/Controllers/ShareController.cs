using Messenger.Models;
using Messenger.Modules;
using Mikodev.Network;

namespace Messenger.Controllers
{
    public class ShareController : LinkPacket
    {
        [Route("share.info")]
        public void Take()
        {
            var rec = new ShareReceiver(Source, Data);
            ShareModule.Register(rec);
            _ = HistoryModule.Insert(Source, Target, "share", rec);
        }
    }
}
