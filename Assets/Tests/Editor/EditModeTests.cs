using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Fast checks that need no Play Mode: the single name definition and the title prefab.</summary>
public sealed class EditModeTests
{
    [Test]
    public void LogoLinesSpellTheDisplayName()=>Assert.AreEqual(GameInfo.DisplayName,GameInfo.LogoSmall+" "+string.Join(" ",GameInfo.LogoLarge));

    [Test]
    public void BuildSettingsReadmeAndCreditsMatchGameInfo()=>Assert.AreEqual("",GameInfoSync.Differences(),"run Tools > Game Info > Sync");

    [Test]
    public void RewardMissions()=>StringAssert.StartsWith("PASS",RewardsMissionValidation.Run());

    [Test]
    public void RewardTrophies()=>StringAssert.StartsWith("PASS",RewardProfileValidation.Run());

    [Test]
    public void BankruptSaveCannotBeContinued()
    {
        var data=new CampaignSaveData{cash="-12.5",rewards=new RewardSaveData{ended=true}};
        Assert.IsTrue(CampaignSaveFile.IsFinished(data),"settled with negative cash");
        data.cash="40";Assert.IsFalse(CampaignSaveFile.IsFinished(data),"positive cash continues");
        data.cash="-1";data.rewards.ended=false;Assert.IsFalse(CampaignSaveFile.IsFinished(data),"not settled yet");
        data.rewards=null;Assert.IsFalse(CampaignSaveFile.IsFinished(data),"saves from before the rewards");
    }

    [Test]
    public void SaveFolderDoesNotFollowTheDisplayName()=>Assert.AreEqual(GameInfo.SaveFolder,System.IO.Path.GetFileName(CampaignSaveFile.Directory));

    [Test]
    public void TitlePrefabHasEveryReference()
    {
        var view=AssetDatabase.LoadAssetAtPath<TitleView>(TitleViewBuilder.PrefabPath);
        Assert.IsNotNull(view,TitleViewBuilder.PrefabPath);
        Assert.AreEqual("",TitleValidation.MissingReferences(view));
    }


    [TestCase("abc", "120", "0", "120")]
    [TestCase("99999999999999999999999999999999999999999", "-1", "0", "0")]
    [TestCase(null, null, "0", "0")]
    public void InvalidRecordAmountsCannotBreakTitle(string daily,string total,string expectedDaily,string expectedTotal)
    {
        string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"pizza-record-test-"+System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        try
        {
            string save=System.IO.Path.Combine(folder,"campaign.json");
            var record=new BestRecords{bestDay=2,bestDelivered=7,bestDailyRevenue=daily,bestTotalRevenue=total};
            System.IO.File.WriteAllText(CampaignSaveFile.RecordsPath(save),JsonUtility.ToJson(record));
            string before=System.IO.File.ReadAllText(CampaignSaveFile.RecordsPath(save));
            var read=CampaignSaveFile.ReadRecords(save);
            Assert.AreEqual(expectedDaily,read.bestDailyRevenue);
            Assert.AreEqual(expectedTotal,read.bestTotalRevenue);
            Assert.DoesNotThrow(()=>CampaignSaveFile.RecordsSummary(read));
            Assert.AreEqual(before,System.IO.File.ReadAllText(CampaignSaveFile.RecordsPath(save)),"reading malformed records does not overwrite the file");
            Assert.AreEqual(2,read.bestDay,"valid fields are retained");
        }
        finally{System.IO.Directory.Delete(folder,true);}
    }

    [Test]
    public void ThemeSuppliesSharedShapesAsAssets()
    {
        var theme=AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UITheme.asset");
        Assert.IsNotNull(theme.roundSprite);Assert.IsNotNull(theme.circleSprite);Assert.IsNotNull(theme.triangleSprite);
        Assert.AreEqual(new Vector4(64,64,64,64),theme.roundSprite.border,"rounded sprite keeps its 9-slice border");
    }
}
