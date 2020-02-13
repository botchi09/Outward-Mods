using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer_2
{
    public class MerchantHolder
    {
        public string Name;
        public string UID;

        public DroptableHolder DropTable;

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

            string dir = Folders.Merchants;
            string saveName = SceneManager.Instance.GetCurrentLocation(merchant.transform.position) + " - " + merchantHolder.Name + " (" + merchantHolder.UID + ")";

            Dataminer.SerializeXML(dir, saveName, merchantHolder, typeof(MerchantHolder));

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
