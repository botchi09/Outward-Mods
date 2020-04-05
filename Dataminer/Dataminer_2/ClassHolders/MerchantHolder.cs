using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer
{
    public class MerchantHolder
    {
        public string Name;
        public string UID;

        public DroptableHolder DropTable;

        public List<float> BuyModifiers = new List<float>();
        public List<float> SellModifiers = new List<float>();

        public static MerchantHolder ParseMerchant(Merchant merchant)
        {
            var merchantHolder = new MerchantHolder
            {
                Name = merchant.ShopName,
                UID = merchant.HolderUID
            };

            if (At.GetValue(typeof(Merchant), merchant, "m_dropableInventory") is Dropable dropper)
            {
                merchantHolder.DropTable = DroptableHolder.ParseDropTable(dropper, merchant);
            }

            foreach (PriceModifier priceMod in merchant.GetComponentsInChildren<PriceModifier>())
            {
                if (priceMod.BuyMultiplierAdded != 0f || priceMod.SellMultiplierAdded != 0f)
                {
                    Debug.Log("Merchant " + merchantHolder.Name + " has buy or sell mods! Buy: " + priceMod.BuyMultiplierAdded + ", Sell: " + priceMod.SellMultiplierAdded);
                    merchantHolder.BuyModifiers.Add(priceMod.BuyMultiplierAdded);
                    merchantHolder.SellModifiers.Add(priceMod.SellMultiplierAdded);
                }
            }

            string dir = Folders.Merchants;
            string saveName = SceneManager.Instance.GetCurrentLocation(merchant.transform.position) + " - " + merchantHolder.Name + " (" + merchantHolder.UID + ")";

            Dataminer.SerializeXML(dir, saveName, merchantHolder, typeof(MerchantHolder));

            ListManager.Merchants.Add(saveName, merchantHolder);

            return merchantHolder;
        }

        public static void ParseAllMerchants()
        {
            foreach (Merchant m in Resources.FindObjectsOfTypeAll<Merchant>().Where(x => x.gameObject.scene != null && x.ShopName != "Merchant"))
            {
                var merchantHolder = ParseMerchant(m);

                var summary = ListManager.SceneSummaries[ListManager.GetSceneSummaryKey(m.transform.position)];
                summary.Merchants.Add(merchantHolder.Name + " (" + merchantHolder.UID + ")");
            }
        }
    }
}
