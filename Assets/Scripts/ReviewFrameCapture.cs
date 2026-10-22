#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Review-only capture after overlay UI finishes rendering; never changes Canvas/camera modes.</summary>
public sealed class ReviewFrameCapture : MonoBehaviour
{
    public static void Begin(string path)
    {
        var go=new GameObject("ReviewFrameCapture"){hideFlags=HideFlags.DontSave};
        go.AddComponent<ReviewFrameCapture>().StartCoroutine(Capture(go,path));
    }
    private static IEnumerator Capture(GameObject owner,string path)
    {
        yield return new WaitForEndOfFrame();
        var texture=ScreenCapture.CaptureScreenshotAsTexture();
        try{Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally{Destroy(texture);Destroy(owner);}
    }
}
#endif
