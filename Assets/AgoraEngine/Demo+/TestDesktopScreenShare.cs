using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

using agora_gaming_rtc;
using AgoraNative;

// this is an example of using Agora Unity SDK
// It demonstrates:
// How to enable video
// How to join/leave channel
// 
public class TestDesktopScreenShare : PlayerViewControllerBase
{

    Dropdown WindowOptionDropdown;

    protected override void SetupUI()
    {
        base.SetupUI();

        Dropdown dropdown = GameObject.Find("Dropdown").GetComponent<Dropdown>();
        if (dropdown != null)
        {

            WindowList list = AgoraNativeBridge.GetMacWindowList();
            if (list != null)
            {
                dropdown.options = list.windows.Select(w =>
                    new Dropdown.OptionData(w.kCGWindowOwnerName + "|" + w.kCGWindowNumber)).ToList();
            }
            WindowOptionDropdown = dropdown;
        }

        Button button = GameObject.Find("ShareWindowButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnShareWindowClick);
        }

        button = GameObject.Find("ShareDisplayButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ShareDisplayScreen);
        }

        button = GameObject.Find("StopShareButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => { mRtcEngine.StopScreenCapture(); });
        }

        GameObject quad = GameObject.Find("Quad");
        if (ReferenceEquals(quad, null))
        {
            Debug.Log("Error: failed to find Quad");
            return;
        }
        else
        {
            quad.AddComponent<VideoSurface>();
        }
    }

    #region MacOS ShareScreen --
    int displayID0or1 = 0;
    void ShareDisplayScreen()
    {
        ScreenCaptureParameters sparams = new ScreenCaptureParameters
        {
            captureMouseCursor = true,
            frameRate = 30
        };

        mRtcEngine.StopScreenCapture();

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        mRtcEngine.StartScreenCaptureByDisplayId(getDisplayId(displayID0or1), default(Rectangle), sparams);  // 
#endif
        displayID0or1 = 1 - displayID0or1;
    }

    uint getDisplayId(int k)
    {
        List<uint> ids = AgoraNativeBridge.GetMacDisplayIds();

        Debug.LogWarning("Size = " + ids.Count);
        if (k < ids.Count)
        {
            return ids[k];
        }
        return 0;
    }

    #endregion

    #region Windows Screen Screen ---

    void OnShareWindowClick()
    {
        char[] delimiterChars = { '|' };
        string option = WindowOptionDropdown.options[WindowOptionDropdown.value].text;
        if (string.IsNullOrEmpty(option))
        {
            return;
        }

        string wid = option.Split(delimiterChars, System.StringSplitOptions.RemoveEmptyEntries)[1];
        Debug.LogWarning(wid + " is chosen");
        mRtcEngine.StopScreenCapture();

        mRtcEngine.StartScreenCaptureByWindowId(int.Parse(wid), default(Rectangle), default(ScreenCaptureParameters));
    }
    #endregion
}
