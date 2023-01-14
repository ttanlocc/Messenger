using System.Net;

namespace Messenger.Models
{
    public class Host
    {
        public string Protocol { get; set; }

        public string Name { get; set; }

        public int Port { get; set; }

        public int Count { get; set; }

        public int CountLimit { get; set; }

        public long Delay { get; set; } = 0;

        public IPAddress Address { get; set; } = null;

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;
            var info = obj as Host;
            if (info == null)
                return false;
            if (Port != info.Port)
                return false;
            if (Address == info.Address)
                return true;
            if (Address == null || info.Address == null)
                return false;
            return Address.Equals(info.Address);
        }

        public override int GetHashCode()
        {
            var add = Address;
            return add != null
                ? new IPEndPoint(add, Port).GetHashCode()
                : 0;
        }
    }
}
