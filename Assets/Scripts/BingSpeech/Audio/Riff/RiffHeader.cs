using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    public class RiffHeader
    {
        public string RIFFHeader { get; internal set; }
        public uint ChunkLength { get; internal set; }
        public string Format { get; internal set; }
        public uint AudioFormat { get; internal set; }
        public uint NumChannels { get; internal set; }
        public uint SampleRate { get; internal set; }
        public uint ByteRate { get; internal set; }
        public uint BlockAlign { get; internal set; }
        public uint BitsPerSample { get; internal set; }
        public byte[] Bytes { get; internal set; }
    }
