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
    public void SaveFolderDoesNotFollowTheDisplayName()=>Assert.AreEqual(GameInfo.SaveFolder,System.IO.Path.GetFileName(CampaignSaveFile.Directory));

    [Test]
    public void TitlePrefabHasEveryReference()
    {
        var view=AssetDatabase.LoadAssetAtPath<TitleView>(TitleViewBuilder.PrefabPath);
        Assert.IsNotNull(view,TitleViewBuilder.PrefabPath);
        Assert.AreEqual("",TitleValidation.MissingReferences(view));
    }

    [Test]
    public void ThemeSuppliesSharedShapesAsAssets()
    {
        var theme=AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UITheme.asset");
        Assert.IsNotNull(theme.roundSprite);Assert.IsNotNull(theme.circleSprite);Assert.IsNotNull(theme.triangleSprite);
        Assert.AreEqual(new Vector4(64,64,64,64),theme.roundSprite.border,"rounded sprite keeps its 9-slice border");
    }
}
