using System.IO;
using System.IO.Compression;
// ReSharper disable IdentifierTypo


namespace BiliBiliDanmakuCrawler
{
    public static class EncodingUtil
    {
        public static byte[] DecompressDeflateBytes(this byte[] data)
        {
            var ms = new MemoryStream(data)
            {
                Position = 0
            };
            var outMs = new MemoryStream();
            using (var deflateStream = new DeflateStream(ms, CompressionMode.Decompress, true))
            {
                var buffer = new byte[1024];
                int length;
                while ((length = deflateStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outMs.Write(buffer, 0, length);
                }
            }

            return outMs.ToArray();
        }

        public static byte[] DecompressGzipBytes(this byte[] data)
        {
            var ms = new MemoryStream(data);
            var outBuffer = new MemoryStream();
            using (var gzipStream = new GZipStream(ms, CompressionMode.Decompress))
            {

                var buffer = new byte[1024];
                int length;
                while ((length = gzipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outBuffer.Write(buffer, 0, length);
                }
            }

            return outBuffer.ToArray();
        }
    }
}