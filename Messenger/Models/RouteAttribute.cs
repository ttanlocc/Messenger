using System;

namespace Messenger.Models
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RouteAttribute : Attribute
    {
        private readonly string _pth = null;

        public string Path => _pth;

        public RouteAttribute(string path) => _pth = path;
    }
}
