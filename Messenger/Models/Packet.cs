using Messenger.Modules;
using System;

namespace Messenger.Models
{
    public class Packet
    {
        private readonly string _key;

        private readonly DateTime _timestamp;

        private readonly int _source;

        private readonly int _target;

        private readonly int _index;

        private readonly string _path;

        private readonly object _value;

        private string _image = null;

        private Profile _profile = null;

        public Packet(string key, DateTime datetime, int index, int source, int target, string path, object value)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _timestamp = datetime;

            _path = path ?? throw new ArgumentNullException(nameof(path));
            _value = value;

            _source = source;
            _target = target;
            _index = index;
        }

        public Packet(int index, int source, int target, string path, object value)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _value = value;

            _source = source;
            _target = target;
            _index = index;

            _key = Guid.NewGuid().ToString();
            _timestamp = DateTime.Now;
        }

        public string Key => _key;

        public int Index => _index;

        public DateTime DateTime => _timestamp;

        public int Target => _target;

        public int Source => _source;

        public string Path => _path;

        public object Object => _value;

        public Profile Profile
        {
            get
            {
                if (_profile == null)
                    _profile = ProfileModule.Query(_source, true);
                return _profile;
            }
        }

        public string MessageText
        {
            get
            {
                if (_value is string str && _path == "text")
                    return str;
                return null;
            }
        }

        public string MessageImage
        {
            get
            {
                if (_image == null && _value is string str && _path == "image")
                    _image = CacheModule.GetPath(str);
                return _image;
            }
        }

        public string MessageNotice
        {
            get
            {
                if (_value is string str && _path == "notice")
                    return str;
                return null;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Packet)} at {_timestamp:u}, form {_source} to {_target}, path: {_path}, value: {_value}";
        }
    }
}
