﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Compress.Support.Utils;

namespace Compress.SevenZip.Structure
{
    public enum InStreamSource
    {
        Unknown,
        FileStream,
        CompStreamOutput
    }


    public class InStreamSourceInfo
    {
        public InStreamSource InStreamSource = InStreamSource.Unknown;
        public ulong InStreamIndex;
    }

    public enum DecompressType
    {
        Unknown,
        Stored,
        LZMA,
        LZMA2,
        PPMd,
        BZip2,
        ZSTD,
        Deflate,

        BCJ,
        BCJ2,
        ARM64,
        ARMT,
        ARM,
        PPC,
        SPARC,
        IA64,
        Delta,
        Swap2,
        Swap4
    }


    public class Coder
    {
        public byte[] Method;
        public ulong NumInStreams;
        public ulong NumOutStreams;
        public byte[] Properties;

        /************Local Variables***********/
        public DecompressType DecoderType = DecompressType.Unknown;
        public bool OutputUsedInternally = false;
        public InStreamSourceInfo[] InputStreamsSourceInfo;
        public Stream DecoderStream;

        public void Read(BinaryReader br)
        {
            byte flags = br.ReadByte();
            int decompressionMethodIdSize = flags & 0xf;
            Method = br.ReadBytes(decompressionMethodIdSize);
            if ((flags & 0x10) != 0)
            {
                NumInStreams = br.ReadEncodedUInt64();
                NumOutStreams = br.ReadEncodedUInt64();
            }
            else
            {
                NumInStreams = 1;
                NumOutStreams = 1;
            }

            if ((flags & 0x20) != 0)
            {
                ulong propSize = br.ReadEncodedUInt64();
                Properties = br.ReadBytes((int)propSize);
            }

            if ((flags & 0x80) != 0)
            {
                throw new NotSupportedException("External flag");
            }

            if (Method.Length == 1 && Method[0] == 0)
                DecoderType = DecompressType.Stored;
            else if (Method.Length == 3 && Method[0] == 3 && Method[1] == 1 && Method[2] == 1)
                DecoderType = DecompressType.LZMA;
            else if (Method.Length == 1 && Method[0] == 33)
                DecoderType = DecompressType.LZMA2;
            else if (Method.Length == 3 && Method[0] == 3 && Method[1] == 4 && Method[2] == 1)
                DecoderType = DecompressType.PPMd;
            else if (Method.Length == 3 && Method[0] == 4 && Method[1] == 1 && Method[2] == 8)
                DecoderType = DecompressType.Deflate;
            else if (Method.Length == 3 && Method[0] == 4 && Method[1] == 2 && Method[2] == 2)
                DecoderType = DecompressType.BZip2;
            else if (Method.Length == 4 && Method[0] == 4 && Method[1] == 247 && Method[2] == 17 && Method[3] == 1)
                DecoderType = DecompressType.ZSTD;


            else if (Method.Length == 4 && Method[0] == 3 && Method[1] == 3 && Method[2] == 1 && Method[3] == 3)
                DecoderType = DecompressType.BCJ;
            else if (Method.Length == 4 && Method[0] == 3 && Method[1] == 3 && Method[2] == 1 && Method[3] == 27)
                DecoderType = DecompressType.BCJ2;
            //ARM64
            else if (Method.Length == 4 && Method[0] == 3 && Method[1] == 3 && Method[2] == 7 && Method[3] == 1)
                DecoderType = DecompressType.ARMT;
            else if (Method.Length == 4 && Method[0] == 3 && Method[1] == 3 && Method[2] == 5 && Method[3] == 1)
                DecoderType = DecompressType.ARM;
            else if (Method.Length == 4 && Method[0] == 3 && Method[1] == 3 && Method[2] == 2 && Method[3] == 5)
                DecoderType = DecompressType.PPC;
            else if (Method.Length == 4 && Method[0] == 3 && Method[1] == 3 && Method[2] == 8 && Method[3] == 5)
                DecoderType = DecompressType.SPARC;
            else if (Method.Length == 4 && Method[0] == 3 && Method[1] == 3 && Method[2] == 4 && Method[3] == 1)
                DecoderType = DecompressType.IA64;
            else if (Method.Length == 1 && Method[0] == 3)
                DecoderType = DecompressType.Delta;
            //Swap2
            //Swap4

            else
                Debug.WriteLine("Error");

            InputStreamsSourceInfo = new InStreamSourceInfo[NumInStreams];
            for (uint i = 0; i < NumInStreams; i++)
            {
                InputStreamsSourceInfo[i] = new InStreamSourceInfo();
            }
        }

        public void Write(BinaryWriter bw)
        {
            byte flags = (byte)Method.Length;
            if ((NumInStreams != 1) || (NumOutStreams != 1))
            {
                flags = (byte)(flags | 0x10);
            }

            if ((Properties != null) && (Properties.Length > 0))
            {
                flags = (byte)(flags | 0x20);
            }

            bw.Write(flags);

            bw.Write(Method);

            if ((NumInStreams != 1) || (NumOutStreams != 1))
            {
                bw.WriteEncodedUInt64(NumInStreams);
                bw.WriteEncodedUInt64(NumOutStreams);
            }

            if ((Properties != null) && (Properties.Length > 0))
            {
                bw.WriteEncodedUInt64((ulong)Properties.Length);
                bw.Write(Properties);
            }
        }



        public void Report(ref StringBuilder sb)
        {
            sb.AppendLine($"        Method[] = {Method.ToArrayString()}   : {DecoderType}");
            sb.AppendLine($"        NumInStreams = {NumInStreams}");
            sb.AppendLine($"        NumOutStreams = {NumOutStreams}");
            sb.AppendLine($"        Properties[] = {Properties.ToArrayString()}");
        }

    }
}