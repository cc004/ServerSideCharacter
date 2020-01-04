using System;
using Terraria;
using Terraria.Social.Base;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.IO;
using Terraria.Social;
using System.IO;
using System.Threading;

namespace ServerSideCharacter
{
    public class SSCPlayer : ModPlayer
    {
        TagCompound savable;

        private static readonly string sscfile = "ssc.data";
        private bool isLoaded = false;
        private int tick = 0;

        private void CheckNull()
        {
            if (savable == null)
            {
                try
                {
                    savable = TagIO.FromFile(Path.Combine(Main.PlayerPath, sscfile));
                }
                catch { }
                savable = savable ?? new TagCompound();
            }
        }

        public byte[] this[string filename]
        {
            get
            {
                CheckNull();
                return savable.Get<byte[]>(filename);
            }
            set
            {
                if (filename == "/achievements-steam.dat") return;
                CheckNull();
                savable[filename] = value;
                TagIO.ToFile(savable, Path.Combine(Main.PlayerPath, sscfile));
            }
        }

        public override void PreUpdate()
        {
            ++tick;
            if (tick == 60)
            {
                TrySave();
                tick = 0;
            }
        }
        public void TrySave()
        {
            if (!isLoaded) return;
            CloudSocialModule module = SocialAPI.Cloud;
            SocialAPI.Cloud = new SSCModule(false);
            Main.ServerSideCharacter = false;
            PlayerFileData.CreateAndSave(Main.LocalPlayer);
            SocialAPI.Cloud = module;
            Main.ServerSideCharacter = true;
        }

        private void OnEnterWorldInternal(object oplayer)
        {
            Player player = oplayer as Player;
            CloudSocialModule module = SocialAPI.Cloud;
            SocialAPI.Cloud = new SSCModule(false);
            string pathName = Main.GetPlayerPathFromName(player.name, true);
            SocialAPI.Cloud = new SSCModule(true);
            Player newPlayer = Player.GetFileData(pathName, true).Player;
            if (newPlayer.name == "")
            {
                newPlayer = new Player(true);
                PlayerHooks.SetupStartInventory(player, false);
                newPlayer.name = Main.LocalPlayer.name;
            }
            newPlayer.SpawnX = Main.LocalPlayer.SpawnX;
            newPlayer.SpawnY = Main.LocalPlayer.SpawnY;
            Main.player[Main.myPlayer] = newPlayer;
            Main.LocalPlayer.Spawn();
            SocialAPI.Cloud = module;
            Main.LocalPlayer.GetModPlayer<SSCPlayer>().isLoaded = true;
        }

        public override void OnEnterWorld(Player player)
        {
            if (Main.netMode != 1) return;
            Main.ServerSideCharacter = true;
            ThreadPool.QueueUserWorkItem(OnEnterWorldInternal, player);
        }

    }
}
