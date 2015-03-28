using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeOffer;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;

namespace SteamBot
{
    public class ItemCollectingUserHandler : UserHandler
    {
        public ItemCollectingUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        public override void OnNewTradeOffer(TradeOffer offer)
        {

            if (IsAdmin || Bot.Manager.approvedIDs.Contains(OtherSID.ConvertToUInt64()))
            {
                string tradeid;
                if (offer.Accept(out tradeid))
                {
                    Log.Success("Accepted trade offer successfully : Trade ID: " + tradeid);
                    Bot.Manager.ReportTradeSuccess();
                }
            }
            else
            {
                //we don't know this user so we can decline
                if (offer.Decline())
                {
                    Log.Info("Declined trade offer : " + offer.TradeOfferId + " from untrusted user " + OtherSID.ConvertToUInt64());
                }
            }
        }

        public override void OnMessage(string message, EChatEntryType type) { }

        public override bool OnGroupAdd() { return false; }

        public override bool OnFriendAdd() { return (IsAdmin || Bot.Manager.approvedIDs.Contains(OtherSID.ConvertToUInt64())); }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() 
        {
            if (Bot.Manager.ConfigObject.AutoCraftWeapons)
            {
                Bot.AutoCraftAllWeapons();
            }
            if (Bot.Manager.ConfigObject.DeleteCrates)
            {
                Bot.DeleteCratesWithExclusions();
            }

            Bot.CombineAllMetal();
            Bot.ReportToManager();
        }

        public override bool OnTradeRequest() { return false; }

        public override void OnTradeError(string error) { }

        public override void OnTradeTimeout() { }

        public override void OnTradeSuccess() { }

        public override void OnTradeInit() { }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) { }

        public override void OnTradeAccept() { }
    }
}