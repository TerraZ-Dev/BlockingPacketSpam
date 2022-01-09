using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Terraria;
using TerrariaApi.Server;

[ApiVersion(2, 1)]
public class BlockingSpamByUserPackets : TerrariaPlugin
{
    public override string Author => "Zoom L1";
    public override string Name => "BlockingSpamByUserPackets";
    public BlockingSpamByUserPackets(Main game) : base(game) { }

    public int[] Packets = new int[Main.player.Length];
    public bool[] BlockedPlayers = new bool[Main.player.Length];
    public DateTime[] WhenBlockedPlayer = new DateTime[Main.player.Length];

    public override void Initialize()
    {
        ServerApi.Hooks.GamePostUpdate.Register(this, OnUpdate);
        ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
    }

    public void OnGetData(GetDataEventArgs args)
    {
        if (Netplay.Clients[args.Msg.whoAmI].State != 10)
            return;
        if (args.MsgID == PacketTypes.LoadNetModule)
            return;
        if (BlockedPlayers[args.Msg.whoAmI])
        {
            args.Handled = true;
            return;
        }
        Packets[args.Msg.whoAmI]++;
    }

    DateTime lastUpdate = DateTime.Now;
    public void OnUpdate(EventArgs eargs)
    {
        if ((DateTime.Now - lastUpdate).TotalSeconds >= 1)
        {
            SecondUpdate();
            lastUpdate = DateTime.Now;
        }
    }
    public void SecondUpdate()
    {
        for (int i = 0; i < Main.player.Length; i++)
        {
            if (Main.player[i] == null || !Main.player[i].active)
                continue;

            if (BlockedPlayers[i])
            {
                if ((DateTime.Now - WhenBlockedPlayer[i]).TotalSeconds >= 10)
                {
                    BlockedPlayers[i] = false;
                    Packets[i] = 0;
                    WhenBlockedPlayer[i] = DateTime.MinValue;

                    Console.WriteLine("Player " + i + " unblocked.");
                }
            }
            else
            {
                if (Packets[i] >= 500)
                    NetMessage.SendData(2, i, -1, Terraria.Localization.NetworkText.FromLiteral("You shouldn't have betrayed us..."));
                else if (Packets[i] >= 120)
                {
                    BlockedPlayers[i] = true;
                    WhenBlockedPlayer[i] = DateTime.Now;

                    Console.WriteLine("Player "+i+" blocked.");
                }

                Packets[i] = 0;
            }
        }
    }

    public void OnLeave(LeaveEventArgs args)
    {
        Packets[args.Who] = 0;
        BlockedPlayers[args.Who] = false;
        WhenBlockedPlayer[args.Who] = DateTime.Now;
    }
}