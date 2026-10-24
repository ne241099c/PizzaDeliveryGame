using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The opening (a new shop opens on borrowed money) and the ending (the loan is repaid), drawn with the city,
/// the shop, pizzas and numbers only — no people. Coordinates are the 1920×1080 stage; the caption band covers y 880+.</summary>
public static class StoryScenes
{
    private static UITheme T=>UiKit.T;
    private static readonly Color Night=Hex("#1B2440"),NightLow=Hex("#3B3F6B"),Dusk=Hex("#2B2F55"),DuskLow=Hex("#E9A06B"),
        Morning=Hex("#8FD0F0"),MorningLow=Hex("#FFE3A8"),Building=Hex("#141A2E"),BuildingDay=Hex("#9C8A78");
    private const float Ground=880;

    public static List<StoryScene> Opening(GameMode mode,decimal loan,int days)=>new()
    {
        new StoryScene{Caption="ある街のはずれに、小さなピザ屋が開きました。",Build=(s,a)=>
        {
            Sky(s,Night,NightLow);Stars(s,70,1,a);Moon(s,1500,120);
            var windows=City(s,3,.5f,Building);
            a.Add(t=>{for(int i=0;i<windows.Count;i++)windows[i].enabled=t>.3f+i*.03f;});
        }},
        new StoryScene{Caption="窯も、配達の自転車も、借りたお金でそろえたものです。",Build=(s,a)=>
        {
            Sky(s,Dusk,DuskLow);GroundStrip(s,Hex("#6B5A4A"));
            var shop=Shop(s,1060,Ground-120,560,a);
            var bike=UiKit.Icon(s,"Bike","fac_bike",0,640,220,Color.white);Centered(bike.rectTransform);
            a.Add(t=>
            {
                shop.localScale=Vector3.one*Overshoot(t,.45f);
                bike.rectTransform.anchoredPosition=new Vector2(Mathf.Lerp(-200,560,Ease(t-.9f,.8f)),-670);
            });
        }},
        new StoryScene{Caption=mode==GameMode.Normal?$"借りたのは {UiKit.Yen(loan)}。{days}日目の精算までに、ぜんぶ返しきろう。":$"借りたのは {UiKit.Yen(loan)}。期限はありません。自分のペースで返していこう。",Build=(s,a)=>
        {
            Sky(s,T.paper,T.paperDeep);Coins(s,a,22);
            var card=UiKit.Card(s,"LoanCard",560,240,800,400,T.paper,28,10,6,T.berry);Centered(card);
            UiKit.Label(card,"Name","借金",0,40,800,70,T.headingSize*1.4f,T.line,FontRole.Display,TextAlignmentOptions.Center);
            var amount=UiKit.Label(card,"Amount","",0,120,800,160,140,T.berry,FontRole.Display,TextAlignmentOptions.Center);
            var due=UiKit.Badge(card,"Due",mode==GameMode.Normal?$"返す期限：{days}日目の精算まで":"返す期限：なし",0,300,T.line,Color.white,64,T.subheadingSize);
            due.anchoredPosition=new Vector2((800-due.sizeDelta.x)/2,-300);
            a.Add(t=>
            {
                card.localScale=Vector3.one*Overshoot(t,.45f);
                amount.text=UiKit.Yen(loan*(decimal)Ease(t-.4f,1.2f));
                due.gameObject.SetActive(t>1.8f);due.localScale=Vector3.one*Overshoot(t-1.8f,.3f);
            });
        }},
        new StoryScene{Caption="さあ、開店です！",Build=(s,a)=>
        {
            var bands=Sky(s,Night,NightLow);
            var sun=UiKit.Icon(s,"Sun","icon_sun",860,900,200,T.cheese);
            var buildings=new List<Image>();var windows=City(s,3,.5f,Building,null,buildings);
            var sign=Title(s,"開店！",T.tomato);
            var pizzas=Orbit(s,6);
            a.Add(t=>
            {
                float k=Ease(t,2.2f);Recolor(bands,Color.Lerp(Night,Morning,k),Color.Lerp(NightLow,MorningLow,k));
                sun.rectTransform.anchoredPosition=new Vector2(860,-Mathf.Lerp(900,300,k));
                foreach(var w in windows)w.enabled=k<.6f;
                foreach(var b in buildings)b.color=Color.Lerp(Building,BuildingDay,k);
                sign.localScale=Vector3.one*Overshoot(t-1.8f,.4f);
                for(int i=0;i<pizzas.Count;i++)
                {
                    float angle=t*.9f+i*Mathf.PI*2/pizzas.Count;
                    pizzas[i].anchoredPosition=new Vector2(960+Mathf.Cos(angle)*440-60,-(420+Mathf.Sin(angle)*190)-60);
                    pizzas[i].localScale=Vector3.one*Ease(t-2,.5f);
                }
            });
        }},
    };

    public static List<StoryScene> Ending(decimal borrowed,int days,int delivered,string grade,int gradeLevel,decimal revenue)=>new()
    {
        new StoryScene{Caption="借りたお金を、ぜんぶ返しきりました！",Build=(s,a)=>
        {
            Sky(s,T.paper,T.paperDeep);
            var card=UiKit.Card(s,"LoanCard",560,240,800,400,T.paper,28,10,6,T.basil);Centered(card);
            UiKit.Label(card,"Name","借金",0,40,800,70,T.headingSize*1.4f,T.line,FontRole.Display,TextAlignmentOptions.Center);
            var amount=UiKit.Label(card,"Amount","",0,120,800,160,140,T.berry,FontRole.Display,TextAlignmentOptions.Center);
            var stamp=UiKit.Card(s,"Stamp",1180,500,340,170,new Color(1,1,1,.9f),20,0,8,T.tomato);
            UiKit.Label(stamp,"Text","完済",0,0,340,170,96,T.tomato,FontRole.Display,TextAlignmentOptions.Center);Centered(stamp);
            a.Add(t=>
            {
                card.localScale=Vector3.one*Overshoot(t,.45f);
                float k=Ease(t-.5f,1.1f);amount.text=UiKit.Yen(borrowed*(decimal)(1-k));amount.color=k>=1?T.basil:T.berry;
                float hit=Mathf.Clamp01((t-1.8f)/.18f);stamp.gameObject.SetActive(t>1.8f);
                stamp.localScale=Vector3.one*Mathf.Lerp(2.8f,1,hit*hit);stamp.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-35,-14,hit));
            });
        }},
        new StoryScene{Caption=$"{days}日間で、{delivered:#,0}枚のピザを届けました。",Build=(s,a)=>
        {
            Sky(s,Night,NightLow);Stars(s,50,7,a);
            var roofs=new List<Vector2>();var windows=City(s,11,.75f,Building,roofs);
            int pins=Mathf.Min(roofs.Count,delivered);
            var drops=new List<RectTransform>();
            for(int i=0;i<pins;i++){var p=UiKit.Icon(s,"Pin"+i,"pin_drop",0,0,64,T.tomato).rectTransform;Centered(p);drops.Add(p);}
            var count=UiKit.Label(s,"Count","",0,120,1920,160,150,T.cheese,FontRole.Display,TextAlignmentOptions.Center);
            a.Add(t=>
            {
                for(int i=0;i<drops.Count;i++)
                {
                    float k=Ease(t-.4f-i*.12f,.35f);var roof=roofs[i];
                    drops[i].gameObject.SetActive(k>0);drops[i].anchoredPosition=new Vector2(roof.x,-Mathf.Lerp(roof.y-500,roof.y-40,k*k));
                }
                count.text=$"{Mathf.RoundToInt(delivered*Ease(t-.3f,1.6f)):#,0}枚";
            });
        }},
        new StoryScene{Caption=$"小さな店は「{grade}」になりました。累計売上 {UiKit.Yen(revenue)}。",Build=(s,a)=>
        {
            Sky(s,Dusk,DuskLow);GroundStrip(s,Hex("#6B5A4A"));
            var shop=Shop(s,640,Ground-120,560,a);
            var badge=UiKit.Badge(s,"Grade",grade,0,120,T.cheese,T.line,96,T.headingSize*1.4f);
            badge.anchoredPosition=new Vector2(1400-badge.sizeDelta.x/2,-300);
            var stars=new List<RectTransform>();
            for(int i=0;i<4;i++){var st=UiKit.Icon(s,"Star"+i,i<gradeLevel?"icon_star_fill":"icon_star",1400-2*110+i*110,440,96,i<gradeLevel?T.cheese:new Color(1,1,1,.5f)).rectTransform;Centered(st);stars.Add(st);}
            a.Add(t=>
            {
                shop.localScale=Vector3.one*Mathf.Lerp(.4f,1,Ease(t-.2f,1.2f));
                badge.localScale=Vector3.one*Overshoot(t-1.2f,.35f);
                for(int i=0;i<stars.Count;i++)stars[i].localScale=Vector3.one*Overshoot(t-1.5f-i*.2f,.3f);
            });
        }},
        new StoryScene{Caption="街は今日も、ピザを待っています。　あそんでくれて、ありがとう！",Build=(s,a)=>
        {
            Sky(s,Night,NightLow);Stars(s,60,13,a);City(s,5,.6f,Building);
            Fireworks(s,a);Coins(s,a,16,"pizza");
            var end=Title(s,"おしまい",T.cheese);
            a.Add(t=>end.localScale=Vector3.one*Overshoot(t-.6f,.5f));
        }},
    };

    // ---- drawing helpers ----
    private static List<Image> Sky(RectTransform s,Color top,Color bottom)
    {
        var bands=new List<Image>();
        for(int i=0;i<12;i++)bands.Add(UiKit.Fill(UiKit.Box(s,"Sky"+i,0,i*90,1920,92),Color.Lerp(top,bottom,i/11f),0));
        return bands;
    }
    private static void Recolor(List<Image> bands,Color top,Color bottom){for(int i=0;i<bands.Count;i++)bands[i].color=Color.Lerp(top,bottom,i/11f);}
    private static void GroundStrip(RectTransform s,Color c)=>UiKit.Fill(UiKit.Box(s,"Ground",0,Ground-120,1920,120),c,0);
    private static void Stars(RectTransform s,int n,int seed,List<Action<float>> a)
    {
        var rng=new System.Random(seed);var stars=new List<(Image image,float phase)>();
        for(int i=0;i<n;i++)
        {
            float size=rng.Next(4,10);
            stars.Add((UiKit.Fill(UiKit.Box(s,"Star"+i,rng.Next(0,1920),rng.Next(0,520),size,size),Color.white,size/2),(float)rng.NextDouble()*6));
        }
        a.Add(t=>{foreach(var st in stars)st.image.color=new Color(1,1,1,.45f+.55f*Mathf.Abs(Mathf.Sin(t*1.7f+st.phase)));});
    }
    private static void Moon(RectTransform s,float x,float y)=>UiKit.Fill(UiKit.Box(s,"Moon",x,y,130,130),T.cheeseLight,65);
    /// <summary>A row of buildings standing on the ground line, with lit windows (returned so scenes can switch them).</summary>
    private static List<Image> City(RectTransform s,int seed,float lit,Color body,List<Vector2> roofs=null,List<Image> faces=null)
    {
        var rng=new System.Random(seed);var windows=new List<Image>();float x=-20;int b=0;
        while(x<1920)
        {
            float w=rng.Next(110,190),h=rng.Next(170,430);
            var building=UiKit.Box(s,"Building"+b,x,Ground-h,w,h);faces?.Add(UiKit.Fill(building,body,6));
            if(rng.NextDouble()<.35)faces?.Add(UiKit.Fill(UiKit.Box(building,"Roof",w*.2f,-18,w*.6f,22),body,4));
            int cols=Mathf.Max(2,(int)(w/48)),rows=Mathf.Max(2,(int)(h/62));
            for(int r=0;r<rows;r++)for(int c=0;c<cols;c++)
            {
                if(rng.NextDouble()>lit)continue;
                var win=UiKit.Fill(UiKit.Box(building,$"W{r}_{c}",14+c*(w-28)/cols,22+r*56,22,28),T.cheese,3);windows.Add(win);
            }
            roofs?.Add(new Vector2(x+w/2,Ground-h));
            x+=w+rng.Next(4,26);b++;
        }
        // Light the windows in a scattered order, not row by row.
        for(int i=windows.Count-1;i>0;i--){int j=rng.Next(i+1);(windows[i],windows[j])=(windows[j],windows[i]);}
        if(roofs!=null)for(int i=roofs.Count-1;i>0;i--){int j=rng.Next(i+1);(roofs[i],roofs[j])=(roofs[j],roofs[i]);}
        return windows;
    }
    private static readonly string[] Toppings={"pizza_basil","pizza_pepperoni","pizza_shrimp","pizza_squid"};
    /// <summary>A whole pizza from the kitchen layers: crust, sauce, cheese and one topping.</summary>
    private static RectTransform Pizza(RectTransform s,string name,float size,int topping)
    {
        var r=UiKit.Box(s,name,0,0,size,size);
        foreach(var layer in new[]{"pizza_base","pizza_sauce","pizza_cheese",Toppings[topping%Toppings.Length]})
            UiKit.Icon(r,layer,layer,0,0,size,Color.white);
        return r;
    }
    /// <summary>The pizza shop: a striped awning, a ピザ sign, a warm window with a pizza and a door. Pivot at the bottom middle.</summary>
    private static RectTransform Shop(RectTransform s,float cx,float bottom,float w,List<Action<float>> a)
    {
        float h=w*.62f,signH=w*.16f,awning=w*.16f,total=h+awning+signH;
        var shop=UiKit.Box(s,"Shop",cx-w/2,bottom-total,w,total);
        shop.pivot=new Vector2(.5f,0);shop.anchoredPosition=new Vector2(cx,-bottom);
        var sign=UiKit.Card(shop,"Sign",w*.2f,0,w*.6f,signH,T.tomato,14,0,5,T.line);
        UiKit.Label(sign,"Text","ピザ",0,0,w*.6f,signH,signH*.62f,Color.white,FontRole.Display,TextAlignmentOptions.Center);
        UiKit.Card(shop,"Body",w*.04f,signH+awning*.5f,w*.92f,h+awning*.5f,T.paper,10,0,6,T.line);
        int stripes=8;float sw=(w+20)/stripes;
        for(int i=0;i<stripes;i++)
        {
            var c=i%2==0?T.tomato:Color.white;
            UiKit.Fill(UiKit.Box(shop,"Stripe"+i,-10+i*sw,signH+4,sw+1,awning-sw/2),c,0);
            UiKit.Fill(UiKit.Box(shop,"Scallop"+i,-10+i*sw,signH+awning-sw*.75f,sw,sw*.6f),c,sw*.3f);
        }
        var window=UiKit.Card(shop,"Window",w*.12f,signH+awning+h*.14f,w*.4f,h*.5f,T.cheeseLight,10,0,6,T.line);
        var glow=UiKit.Fill(UiKit.Box(window,"Glow",6,6,w*.4f-12,h*.5f-12),new Color(T.cheese.r,T.cheese.g,T.cheese.b,.5f),6);
        var pie=Pizza(window,"Pizza",h*.34f,1);Centered(pie);pie.anchoredPosition=new Vector2(w*.2f,-h*.25f);
        UiKit.Card(shop,"Door",w*.62f,signH+awning+h*.2f,w*.22f,h*.8f-6,Hex("#C98B5E"),8,0,6,T.line);
        a.Add(t=>{var c=glow.color;c.a=.35f+.2f*Mathf.Sin(t*5);glow.color=c;pie.localRotation=Quaternion.Euler(0,0,-t*20);});
        return shop;
    }
    private static RectTransform Title(RectTransform s,string text,Color color)
    {
        var card=UiKit.Card(s,"Title_"+text,560,330,800,220,T.paper,40,12,8,color);Centered(card);
        UiKit.Label(card,"Text",text,0,0,800,220,130,color,FontRole.Display,TextAlignmentOptions.Center);
        card.localScale=Vector3.zero;return card;
    }
    private static List<RectTransform> Orbit(RectTransform s,int n)
    {
        var list=new List<RectTransform>();
        for(int i=0;i<n;i++){var r=Pizza(s,"Pizza"+i,120,i);r.localScale=Vector3.zero;list.Add(r);}
        return list;
    }
    /// <summary>Icons falling and turning from the top (coins for the loan, pizzas at the end).</summary>
    private static void Coins(RectTransform s,List<Action<float>> a,int n,string sprite="icon_coin")
    {
        var rng=new System.Random(n);var items=new List<(RectTransform r,float x,float speed,float delay,float spin)>();
        for(int i=0;i<n;i++)
        {
            int size=rng.Next(56,96);
            var r=sprite=="pizza"?Pizza(s,"Fall"+i,size+20,i):UiKit.Icon(s,"Fall"+i,sprite,0,0,size,T.cheese).rectTransform;Centered(r);
            items.Add((r,rng.Next(40,1880),rng.Next(220,420),(float)rng.NextDouble()*3,rng.Next(-160,160)));
        }
        a.Add(t=>{foreach(var it in items){float y=((t-it.delay)*it.speed)%1000-80;it.r.gameObject.SetActive(t>it.delay);it.r.anchoredPosition=new Vector2(it.x,-y);it.r.localRotation=Quaternion.Euler(0,0,t*it.spin);}});
    }
    private static void Fireworks(RectTransform s,List<Action<float>> a)
    {
        Color[] colors={T.cheese,T.tomato,T.basil,T.info,Color.white};
        var bursts=new List<(Vector2 at,float start,List<Image> sparks)>();var rng=new System.Random(42);
        for(int b=0;b<6;b++)
        {
            var sparks=new List<Image>();var c=colors[b%colors.Length];
            for(int i=0;i<18;i++)sparks.Add(UiKit.Fill(UiKit.Box(s,$"Spark{b}_{i}",0,0,14,14),c,7));
            bursts.Add((new Vector2(rng.Next(260,1660),rng.Next(140,420)),b*.55f,sparks));
        }
        a.Add(t=>
        {
            foreach(var burst in bursts)
            {
                float k=((t-burst.start)%3.3f)/1.4f;bool on=t>=burst.start && k<=1;
                for(int i=0;i<burst.sparks.Count;i++)
                {
                    var sp=burst.sparks[i];sp.gameObject.SetActive(on);if(!on)continue;
                    float angle=i*Mathf.PI*2/burst.sparks.Count,radius=Mathf.Sqrt(k)*190;
                    sp.rectTransform.anchoredPosition=new Vector2(burst.at.x+Mathf.Cos(angle)*radius,-(burst.at.y+Mathf.Sin(angle)*radius+k*k*60));
                    var col=sp.color;col.a=1-k;sp.color=col;
                }
            }
        });
    }
    private static void Centered(RectTransform r){var size=r.sizeDelta;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition+=new Vector2(size.x/2,-size.y/2);}
    private static float Ease(float t,float duration){float k=Mathf.Clamp01(t/duration);return 1-(1-k)*(1-k);}
    private static float Overshoot(float t,float duration){float k=Mathf.Clamp01(t/duration);return k<=0?0:k+.25f*Mathf.Sin(k*Mathf.PI)*(1-k*.3f);}
    private static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
}
