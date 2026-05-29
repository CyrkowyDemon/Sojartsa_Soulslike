using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

// Credits:

// Creating TGA Image files  Written by Paul Bourke
// http://local.wasp.uwa.edu.au/~pbourke/dataformats/tga/

// CxImage  By Davide Pizzolato.
// http://www.codeproject.com/bitmap/cximage.asp

namespace png32totga {
    public static class Program {
        public static void Main(string args, Texture2D picin) {
            if (args == "") {
                //Debug.Log("png32totga <input.png> <output.tga>");
                return;
            }
            using (FileStream fs = File.Create(args)) {
                BinaryWriter wr = new BinaryWriter(fs);
                wr.Write((byte)0); // IdLength
                wr.Write((byte)0); // CmapType
                wr.Write((byte)10); // ImageType ... TGA_RLERGB 10

                wr.Write((UInt16)0); // CmapIndex
                wr.Write((UInt16)0); // CmapLength
                wr.Write((byte)0); // CmapEntrySize

                wr.Write((UInt16)0); // X_Origin
                wr.Write((UInt16)0); // Y_Origin
                wr.Write(Convert.ToUInt16(picin.width)); // ImageWidth
                wr.Write(Convert.ToUInt16(picin.height)); // ImageHeight
                wr.Write((byte)32); // PixelDepth
                wr.Write((byte)0); // ImagDesc

                int cx = picin.width;
                int cy = picin.height;
                int maxpos = picin.width * picin.height;
                PacketRaw pktraw = new PacketRaw();
                for (int pos = 0; pos < maxpos; ) {
					UnityEngine.Color32 clr = (UnityEngine.Color32)picin.GetPixel(pos % cx, (pos / cx));
                    int t = 1;
                    for (; t < 128 && t + pos < maxpos && (clr.r == picin.GetPixel((pos + t) % cx, (pos + t) / cx).r && clr.g == picin.GetPixel((pos + t) % cx, (pos + t) / cx).g && clr.b == picin.GetPixel((pos + t) % cx, (pos + t) / cx).b && clr.a == picin.GetPixel((pos + t) % cx, (pos + t) / cx).a); t++) ;
                    if (1 < t) {
                        // RLE
                        Ut.WritePacket(wr, pktraw);
                        pktraw = new PacketRaw();
                        Ut.WritePacket(wr, new PacketRLE(clr, (byte)t));
                        pos += t;
                    }
                    else {
                        // Raw
                        if (pktraw.pixels.Count == 128) {
                            Ut.WritePacket(wr, pktraw);
                            pktraw = new PacketRaw();
                        }
                        pktraw.pixels.Add(clr);
                        pos += t;
                    }
                }
                Ut.WritePacket(wr, pktraw);
            }
        }
    }
    class Ut {
        public static void Write32(BinaryWriter fs, UnityEngine.Color32 clr) {
            fs.Write((byte)clr.b);
            fs.Write((byte)clr.g);
            fs.Write((byte)clr.r);
            fs.Write((byte)clr.a);
        }
        public static void WritePacket(BinaryWriter wr, PacketRaw o) {
            if (o.pixels.Count != 0) {
                if (o.pixels.Count > 128) throw new ArgumentOutOfRangeException("o");
                wr.Write((byte)(o.pixels.Count - 1));
                foreach (UnityEngine.Color32 clr in o.pixels) Ut.Write32(wr, clr);
            }
        }
        public static void WritePacket(BinaryWriter wr, PacketRLE o) {
            if (o.cnt != 0) {
                if (o.cnt > 128) throw new ArgumentOutOfRangeException("o");
                wr.Write((byte)(128 | (o.cnt - 1)));
                Ut.Write32(wr, o.clr);
            }
        }
    }

    class PacketRaw {
        public List<UnityEngine.Color32> pixels = new List<UnityEngine.Color32>();
    }
    class PacketRLE {
        public UnityEngine.Color32 clr;
        public byte cnt;

        public PacketRLE(UnityEngine.Color32 clr, byte cnt) {
            this.clr = clr; this.cnt = cnt;
        }
    }
}