using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Esc menu, help and the return-to-title confirmation as modals (設計書_画面デザイン 4-6): the screen behind stays
/// visible under a dark veil, with one sticker card in the middle. The scene-authored panels keep their CanvasGroups
/// (open/closed state and existing buttons); they are moved under the shop UI so the legacy theme pass leaves them alone.</summary>
public static class ShopModals
{
    private static UITheme T=>UiKit.T;
    public sealed class Modal {public RectTransform Card;public TextMeshProUGUI Title,Body;public readonly List<StickerButton> Buttons=new();}

    public static Modal Build(CanvasGroup panel,Transform shopRoot,float width,float height,string title)
    {
        var root=(RectTransform)panel.transform;
        if(shopRoot!=null){root.SetParent(shopRoot,false);root.SetAsLastSibling();}
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        foreach(Transform child in root)child.gameObject.SetActive(false); // the old layout stays for its button references
        var own=root.GetComponent<Image>();if(own!=null){own.sprite=null;own.color=new Color(T.line.r,T.line.g,T.line.b,.55f);own.raycastTarget=true;}
        var panelTheme=root.GetComponent<ThemedPanel>();if(panelTheme!=null)UnityEngine.Object.Destroy(panelTheme);
        var modal=new Modal();
        modal.Card=UiKit.Card(root,"ModalCard",(1920-width)/2,(1080-height)/2,width,height,T.paper,24,8,3);
        modal.Title=UiKit.Label(modal.Card,"ModalTitle",title,40,28,width-80,56,T.headingSize,T.line,FontRole.Display);
        return modal;
    }
    public static TextMeshProUGUI Body(Modal modal,float y,float height,string text,float size=20)
    {
        modal.Body=UiKit.Label(modal.Card,"ModalBody",text,40,y,modal.Card.sizeDelta.x-80,height,size,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        return modal.Body;
    }
    public static StickerButton Button(Modal modal,string name,string label,StickerKind kind,float x,float y,float w,float h,Action onClick)
    {
        var b=UiKit.Button(modal.Card,name,label,kind,x,y,w,h,kind==StickerKind.Primary?28:24,()=>onClick(),FontRole.Display);modal.Buttons.Add(b);return b;
    }
}
