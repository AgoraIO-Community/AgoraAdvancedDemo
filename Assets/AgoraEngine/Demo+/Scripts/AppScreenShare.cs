using UnityEngine;
using agora_gaming_rtc;
using System.Collections;
using UnityEngine.UI;

/// <summary>
///   This script shows how to do Screen Sharing the Unity Application screen.
///   Two important APIs are center of this feature:
///     1. SetExternalVideoSource - sets up the stream to use user defined data
///     2. PushVideoFrame - sends the raw data to the receipants. 
///   Note:  VIDEO_PIXEL_BGRA is the current Pixel format that the API supports
///     So the receipant side should instantiate Textures with BGRA32 format to
///   decode the color properly.
/// </summary>
public class TestAppScreenShare : PlayerViewControllerBase
{
    int TotalUserJoined = 0;
    Texture2D mTexture;
    Rect mRect;
    bool running = false;
    int timestamp = 0;
    MonoBehaviour monoProxy;

    protected override void PrepareToJoin()
    {
        base.PrepareToJoin();
        EnableShareScreen();
    }

    protected override void SetupUI()
    {
        base.SetupUI();
        monoProxy = GameObject.Find("Canvas").GetComponent<MonoBehaviour>();

        Button button = GameObject.Find("StopButton").GetComponent<Button>();
        button.onClick.AddListener(() => { DisableShareScreen(); });
    }

    protected override void OnUserJoined(uint uid, int elapsed)
    {
        base.OnUserJoined(uid, elapsed);
        TotalUserJoined++;
        if (TotalUserJoined == 1 && running == false)
        {
            // CreateController();
            // Create a rectangle width and height of the screen
            mRect = new Rect(0, 0, Screen.width, Screen.height);
            // Create a texture the size of the rectangle you just created
            mTexture = new Texture2D((int)mRect.width, (int)mRect.height, TextureFormat.RGBA32, false);
            // get the rtc engine instance, assume it has been created before this script starts
            running = true;
            monoProxy.StartCoroutine(shareScreen());
        }
    }

    protected override void OnUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        base.OnUserOffline(uid, reason);

        GameObject gameObject = GameObject.Find(uid.ToString());
        if (gameObject != null)
        {
            GameObject.Destroy(gameObject);
            TotalUserJoined--;
            if (TotalUserJoined == 0)
            {
                StopSharing();
            }
        }
    }

    void EnableShareScreen()
    {
        // enable video
        // mRtcEngine.EnableVideo();
        // allow camera output callback
        //mRtcEngine.EnableVideoObserver();

        // Very Important to make this app work
        mRtcEngine.SetExternalVideoSource(true, false);
    }

    void DisableShareScreen()
    {
        StopSharing();
        Debug.Log("ScreenShare Deactivated");
        monoProxy.StartCoroutine(coResetChannel());
    }

    IEnumerator coResetChannel()
    {
        mRtcEngine.DisableVideo();
        mRtcEngine.DisableVideoObserver();
        mRtcEngine.LeaveChannel();
        yield return new WaitForSeconds(0.5f);


        mRtcEngine.EnableVideo();
        mRtcEngine.EnableLocalVideo(true);
        // Enables the video observer.
        mRtcEngine.EnableVideoObserver();

        yield return new WaitForSeconds(0.5f);

        int i = mRtcEngine.SetExternalVideoSource(false, false);
        Debug.LogWarning("SetExternalVideoSource i = " + i);
        mRtcEngine.JoinChannel(mChannel, null, 0);
    }

    IEnumerator shareScreen()
    {
        while (running)
        {
            yield return new WaitForEndOfFrame();
            //Read the Pixels inside the Rectangle
            mTexture.ReadPixels(mRect, 0, 0);
            //Apply the Pixels read from the rectangle to the texture
            mTexture.Apply();
            // Get the Raw Texture data from the the from the texture and apply it to an array of bytes
            byte[] bytes = mTexture.GetRawTextureData();
            // int size = Marshal.SizeOf(bytes[0]) * bytes.Length;
            // Check to see if there is an engine instance already created
            //if the engine is present
            if (mRtcEngine != null)
            {
                //Create a new external video frame
                ExternalVideoFrame externalVideoFrame = new ExternalVideoFrame();
                //Set the buffer type of the video frame
                externalVideoFrame.type = ExternalVideoFrame.VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA;
                // Set the video pixel format
                //externalVideoFrame.format = ExternalVideoFrame.VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_BGRA;  // V.2.9.x
                externalVideoFrame.format = ExternalVideoFrame.VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA;  // V.3.x.x
                //apply raw data you are pulling from the rectangle you created earlier to the video frame
                externalVideoFrame.buffer = bytes;
                //Set the width of the video frame (in pixels)
                externalVideoFrame.stride = (int)mRect.width;
                //Set the height of the video frame
                externalVideoFrame.height = (int)mRect.height;
                //Remove pixels from the sides of the frame
                externalVideoFrame.cropLeft = 10;
                externalVideoFrame.cropTop = 10;
                externalVideoFrame.cropRight = 10;
                externalVideoFrame.cropBottom = 10;
                //Rotate the video frame (0, 90, 180, or 270)
                externalVideoFrame.rotation = 180;
                externalVideoFrame.timestamp = timestamp++;
                //Push the external video frame with the frame we just created
                mRtcEngine.PushVideoFrame(externalVideoFrame);
            }
        }
    }

    void StopSharing()
    {
        running = false;
    }
}
