using System.IO;
using System.Threading;
using Terraria.Social.Base;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.Social;

namespace ServerSideCharacter
{
    enum PacketType
    {
        ReadFile = 0,
        ReadFileCallback = 1,
        WriteFile = 2,
        FileSize = 3,
        FileSizeCallback = 4
    }

    public class ModContainer : Mod
    {
        internal static ModContainer instance;
        internal byte[] Buffer;
        internal int FileSize;
        internal AutoResetEvent ReadFileEvent = new AutoResetEvent(false);
        internal AutoResetEvent FileSizeEvent = new AutoResetEvent(false);

        public override void Load()
        {
            instance = this;
        }

        public override void Unload()
        {
            instance = null;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            PacketType type = (PacketType)reader.ReadByte();
            ModPacket packet;
            int size;
            byte[] raw;
            switch (type)
            {
                case PacketType.FileSize:
                    if (Main.netMode != 2) break;
                    packet = GetPacket();
                    packet.Write((byte)PacketType.FileSizeCallback);
                    packet.Write(Main.player[whoAmI].GetModPlayer<SSCPlayer>()[reader.ReadString()].Length);
                    packet.Send(whoAmI);
                    break;
                case PacketType.FileSizeCallback:
                    if (Main.netMode != 1) break;
                    FileSize = reader.ReadInt32();
                    FileSizeEvent.Set();
                    break;
                case PacketType.ReadFile:
                    if (Main.netMode != 2) break;
                    raw = Main.player[whoAmI].GetModPlayer<SSCPlayer>()[reader.ReadString()] ?? new byte[0];
                    packet = GetPacket(raw.Length + 10);
                    packet.Write((byte)PacketType.ReadFileCallback);
                    packet.Write(raw.Length);
                    packet.Write(raw, 0, raw.Length);
                    packet.Send(whoAmI);
                    break;
                case PacketType.ReadFileCallback:
                    if (Main.netMode != 1) break;
                    size = reader.ReadInt32();
                    Buffer = reader.ReadBytes(size);
                    ReadFileEvent.Set();
                    break;
                case PacketType.WriteFile:
                    if (Main.netMode != 2) break;
                    string path = reader.ReadString();
                    raw = reader.ReadBytes(reader.ReadInt32());
                    Main.player[whoAmI].GetModPlayer<SSCPlayer>()[path] = raw;
                    break;
            }
        }

        public override void PreSaveAndQuit()
        {
            if (Main.netMode != 1) return;
            Main.LocalPlayer.GetModPlayer<SSCPlayer>().TrySave();
        }

        public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
        {
            if (messageType == 0x07) //World info
            {
                long pos = reader.BaseStream.Position;
                reader.BaseStream.Position += 22L;
                string a = reader.ReadString();
                System.Console.WriteLine(a);
                reader.BaseStream.Position += 77L;
                byte b = reader.ReadByte();
                reader.BaseStream.Position -= 1L;
                reader.BaseStream.WriteByte((byte)(b | 0x40)); //switch on ssc
                reader.BaseStream.Position = pos;
            }
            return false;
        }

    }
}
