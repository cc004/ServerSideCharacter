using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.Social.Base;
using static ServerSideCharacter.ModContainer;

namespace ServerSideCharacter
{
    class SSCModule : CloudSocialModule
    {
        public override void Shutdown() { }
        public override bool Delete(string path) => false;
        public override bool HasFile(string path) => hasFile;
        public override IEnumerable<string> GetFiles() => new string[0];

        private readonly bool hasFile;

        public SSCModule(bool hasFile)
        {
            EnabledByDefault = true;
            this.hasFile = hasFile;
        }

        public override int GetFileSize(string path)
        {
            if (Main.netMode != 1) return 0;
            ModContainer instance = ModLoader.GetMod("ServerSideCharacter") as ModContainer;
            instance.FileSizeEvent.Reset();
            ModPacket request = instance.GetPacket();
            request.Write((byte)PacketType.FileSize);
            request.Write(path);
            request.Send();
            instance.FileSizeEvent.WaitOne();
            return instance.FileSize;
        }

        public override void Read(string path, byte[] buffer, int length)
        {
            if (Main.netMode != 1) return;
            instance.ReadFileEvent.Reset();
            ModPacket request = instance.GetPacket();
            request.Write((byte)PacketType.ReadFile);
            request.Write(path);
            request.Send();
            instance.ReadFileEvent.WaitOne();
            Buffer.BlockCopy(instance.Buffer, 0, buffer, 0, instance.Buffer.Length);
        }

        public override bool Write(string path, byte[] data, int length)
        {
            if (Main.netMode != 1) return false;
            ModPacket upload = instance.GetPacket();
            upload.Write((byte)PacketType.WriteFile);
            upload.Write(path);
            upload.Write(length);
            upload.Write(data, 0, length);
            upload.Send();
            return true;
        }
    }
}
