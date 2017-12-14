using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    public class RiffChunker : IDisposable
    {
        /* #region Private Methods */
        private void ReadHeader()
        {
            //See here: http://soundfile.sapp.org/doc/WaveFormat/
            var firstBytes = new byte[36];
            this.FileStream.Read(firstBytes, 0, 36);
            this.FileStream.Seek(0, SeekOrigin.Begin);

            var riffHeader = ReadASCII(4);
            var chunkLen = ReadUInt32();
            var format = ReadASCII(4);

            var subChunk1Id = ReadASCII(4);
            var subChunk1Size = ReadUInt32();
            var audioFmt = ReadUInt16();
            var numChannels = ReadUInt16();
            var sampleRate = ReadUInt32();
            var byteRate = ReadUInt32();  //== SampleRate * NumChannels * BitsPerSample / 8
            var blockAlign = ReadUInt16();
            var bitsPerSample = ReadUInt16();

            var retval = new RiffHeader()
            {
                Bytes = firstBytes,
                RIFFHeader = riffHeader,
                ChunkLength = chunkLen,
                Format = format,
                AudioFormat = audioFmt,
                NumChannels = numChannels,
                SampleRate = sampleRate,
                ByteRate = byteRate,
                BlockAlign = blockAlign,
                BitsPerSample = bitsPerSample
            };
            this.RiffHeader = retval;
        }
        /* #endregion Private Methods */
        /* #region Public Sub-types */
        public class RiffChunk
        {
            /* #region Public Properties */
            public byte[] AllBytes { get; internal set; }
            public string ChunkId { get; set; }
            public UInt32 ChunkSize { get; set; }
            public long NumSamples { get; set; }
            public byte[] SubChunkDataBytes { get; set; }
            public byte[] SubChunkIdBytes { get; internal set; }
            public byte[] SubChunkSizeBytes { get; internal set; }
            /* #endregion Public Properties */
        }
        /* #endregion Public Sub-types */
        /* #region Public Properties */
        public FileStream FileStream { get; private set; }
        public long Position
        {
            get
            {
                return this.FileStream.Position;
            }
        }
        public RiffHeader RiffHeader { get; private set; }
        /* #endregion Public Properties */
        /* #region Public Constructors */
        public RiffChunker(string filePath)
        {
            this.FileStream = new FileInfo(filePath).OpenRead();
            this.ReadHeader();
        }
        /* #endregion Public Constructors */
        /* #region Public Methods */
        public RiffChunk Next()
        {
            if (this.FileStream.Position == this.FileStream.Length) return null;

            byte[] subchunkIdBytes = new byte[4];
            this.FileStream.Read(subchunkIdBytes, 0, (int)4);
            var subchunkId = Encoding.ASCII.GetString(subchunkIdBytes);

            byte[] subChunkSizeBytes = new byte[4];
            this.FileStream.Read(subChunkSizeBytes, 0, 4);
            var subChunkSize = BitConverter.ToUInt32(subChunkSizeBytes, 0);

            var numSamples = 8 * subChunkSize / (this.RiffHeader.NumChannels * this.RiffHeader.BitsPerSample);

            var arr = new byte[subChunkSize];
            this.FileStream.Read(arr, 0, (int)subChunkSize);

            var retval = new RiffChunk()
            {
                AllBytes = subchunkIdBytes.Concat(subChunkSizeBytes).Concat(arr).ToArray(),
                SubChunkIdBytes = subchunkIdBytes,
                SubChunkSizeBytes = subChunkSizeBytes,
                SubChunkDataBytes = arr,
                ChunkId = subchunkId,
                ChunkSize = subChunkSize,
                NumSamples = numSamples
            };
            return retval;

        }
        public string ReadASCII(long len)
        {
            byte[] arr = new byte[len];
            this.FileStream.Read(arr, 0, (int)len);
            var str = Encoding.ASCII.GetString(arr);
            return str;
        }
        public uint ReadUInt16()
        {
            byte[] arr = new byte[2];
            this.FileStream.Read(arr, 0, 2);
            return BitConverter.ToUInt16(arr, 0);
        }
        public uint ReadUInt32()
        {
            byte[] arr = new byte[4];
            this.FileStream.Read(arr, 0, 4);
            return BitConverter.ToUInt32(arr, 0);
        }
        /* #endregion Public Methods */
        /* #region Interface: 'System.IDisposable' Methods */
        public void Dispose()
        {
            this.FileStream.Dispose();
        }
        /* #endregion Interface: 'System.IDisposable' Methods */
    }
