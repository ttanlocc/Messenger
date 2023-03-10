using Messenger.Models;
using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Messenger.Modules
{
    internal class CacheModule
    {
        private const string _Directory = "Cache";

        private const string _ImageSuffix = ".png";

        private const string _KeyCache = "cache-path";

        private const int _LengthLimit = 4 * 1024 * 1024;

        private const int _PixelLimit = 384;

        private const float _Density = 96;

        private string _dir = _Directory;

        private static readonly CacheModule s_ins = new CacheModule();

        private CacheModule() { }

        [Loader(16, LoaderFlags.OnLoad)]
        public static void Load()
        {
            try
            {
                s_ins._dir = EnvironmentModule.Query(_KeyCache, _Directory);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public static string GetSHA256(byte[] buffer)
        {
            using (var sha = new SHA256Managed())
            {
                var buf = sha.ComputeHash(buffer);
                var str = buf.Aggregate(new StringBuilder(), (l, r) => l.AppendFormat("{0:x2}", r));
                return str.ToString();
            }
        }

        public static string GetPath(string sha)
        {
            var dir = new DirectoryInfo(s_ins._dir);
            var pth = Path.Combine(dir.FullName, sha + _ImageSuffix);
            var inf = new FileInfo(pth);

            try
            {
                if (inf.Exists == false)
                    return null;
                if (inf.Length < Links.BufferLengthLimit)
                    return inf.FullName;
                Log.Info("Cache file length overflow!");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return null;
        }

        public static string SetBuffer(byte[] buffer, bool fullPath, bool nothrow = true)
        {
            if (buffer.Length > Links.BufferLengthLimit)
            {
                Log.Info("Cache buffer length overflow!");
                return null;
            }

            var fst = default(FileStream);
            var sha = GetSHA256(buffer);
            var pth = default(string);

            try
            {
                var dir = new DirectoryInfo(s_ins._dir);
                if (dir.Exists == false)
                    dir.Create();
                pth = Path.Combine(dir.FullName, sha + _ImageSuffix);
                if (File.Exists(pth) == false)
                {
                    fst = new FileStream(pth, FileMode.CreateNew, FileAccess.Write);
                    fst.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                if (nothrow == false)
                    throw;
                Log.Error(ex);
                return null;
            }
            finally
            {
                fst?.Dispose();
            }
            return fullPath ? pth : sha;
        }

        public static byte[] ImageSquare(string filepath)
        {
            var inf = new FileInfo(filepath);
            if (inf.Length > _LengthLimit)
                throw new IOException("File too big!");
            var bmp = new Bitmap(filepath);
            var src = new Rectangle();
            if (bmp.Width > bmp.Height)
                src = new Rectangle((bmp.Width - bmp.Height) / 2, 0, bmp.Height, bmp.Height);
            else
                src = new Rectangle(0, (bmp.Height - bmp.Width) / 2, bmp.Width, bmp.Width);
            var len = bmp.Width > bmp.Height ? bmp.Height : bmp.Width;
            var div = 1;
            for (div = 1; len / div > _PixelLimit; div++) ;
            var dst = new Rectangle(0, 0, len / div, len / div);
            return _LoadImage(bmp, src, dst, ImageFormat.Jpeg);
        }

        public static byte[] ImageZoom(string filepath)
        {
            var inf = new FileInfo(filepath);
            if (inf.Length > _LengthLimit)
                throw new IOException("File too big!");
            var bmp = new Bitmap(filepath);
            var len = bmp.Size;
            var div = 1;
            for (div = 1; len.Width / div > _PixelLimit || len.Height / div > _PixelLimit; div++) ;

            var src = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var dst = new Rectangle(0, 0, len.Width / div, len.Height / div);

            return _LoadImage(bmp, src, dst, ImageFormat.Png);
        }

        private static byte[] _LoadImage(Bitmap bmp, Rectangle src, Rectangle dst, ImageFormat format)
        {
            var img = new Bitmap(dst.Right, dst.Bottom);
            var gra = Graphics.FromImage(img);
            var mst = new MemoryStream();
            var buf = default(byte[]);

            try
            {
                img.SetResolution(_Density, _Density);
                if (format != ImageFormat.Png)
                    gra.Clear(Color.Black);
                gra.DrawImage(bmp, dst, src, GraphicsUnit.Pixel);
                img.Save(mst, format);
                buf = mst.ToArray();
            }
            finally
            {
                mst?.Dispose();
                gra?.Dispose();
                bmp?.Dispose();
                img?.Dispose();
            }
            return buf;
        }
    }
}
