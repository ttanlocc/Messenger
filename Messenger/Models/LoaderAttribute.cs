using System;

namespace Messenger.Models
{
    public class LoaderAttribute : Attribute
    {
        private readonly int _lev;

        private readonly LoaderFlags _tag;

        public int Level => _lev;

        public LoaderFlags Flag => _tag;

        public LoaderAttribute(int level, LoaderFlags flag)
        {
            _lev = level;
            _tag = flag;
        }
    }
}
