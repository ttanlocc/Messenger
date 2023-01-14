using Messenger.Models;
using Messenger.Modules;
using Mikodev.Network;

namespace Messenger.Controllers
{
    public class ProfileController : LinkPacket
    {
        [Route("user.request")]
        public void Request()
        {
            PostModule.UserProfile(Source);
        }

        [Route("user.profile")]
        public void Profile()
        {
            var cid = Data["id"].As<int>();
            var pro = new Profile(cid)
            {
                Name = Data["name"].As<string>(),
                Text = Data["text"].As<string>(),
            };

            var buf = Data["image"].As<byte[]>();
            if (buf.Length > 0)
                pro.Image = CacheModule.SetBuffer(buf, true);
            ProfileModule.Insert(pro);
        }

        [Route("user.list")]
        public void List()
        {
            var lst = Data.As<int[]>();
            _ = ProfileModule.Remove(lst);
        }
    }
}
