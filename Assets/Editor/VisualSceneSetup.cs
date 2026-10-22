using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class VisualSceneSetup
{
    public static void Foundation()
    {
        Directory.CreateDirectory("Assets/Resources");Directory.CreateDirectory("Assets/Fonts");AssetDatabase.Refresh();
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ZenMaruGothic-Regular.ttf");
        var tmp=TMP_FontAsset.CreateFontAsset(font,48,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
        tmp.isMultiAtlasTexturesEnabled=true;tmp.name="Zen Maru Gothic Dynamic";AssetDatabase.CreateAsset(tmp,"Assets/Fonts/ZenMaruGothic Dynamic.asset");
        foreach(var atlas in tmp.atlasTextures){atlas.name="ZenMaru Atlas";AssetDatabase.AddObjectToAsset(atlas,tmp);}AssetDatabase.AddObjectToAsset(tmp.material,tmp);
        var theme=ScriptableObject.CreateInstance<UITheme>();theme.font=tmp;AssetDatabase.CreateAsset(theme,"Assets/Resources/UITheme.asset");
        var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Rounded panel (original)";
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(theme.cornerRadius-x,0,x-(63-theme.cornerRadius)),dy=Mathf.Max(theme.cornerRadius-y,0,y-(63-theme.cornerRadius));texture.SetPixel(x,y,new Color(1,1,1,dx*dx+dy*dy<=theme.cornerRadius*theme.cornerRadius?1:0));}texture.Apply();AssetDatabase.AddObjectToAsset(texture,theme);
        theme.roundedPanel=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Vector4.one*theme.cornerRadius);theme.roundedPanel.name="RoundedPanel";AssetDatabase.AddObjectToAsset(theme.roundedPanel,theme);EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();
    }
}
