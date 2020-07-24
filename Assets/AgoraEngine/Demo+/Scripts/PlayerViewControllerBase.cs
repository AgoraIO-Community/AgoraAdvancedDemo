using System;
using UnityEngine;
using UnityEngine.UI;
using agora_gaming_rtc;
using agora_utilities;

public class PlayerViewControllerBase : IVideoChatClient
{
    public event Action OnViewControllerFinish;
    protected IRtcEngine mRtcEngine;

    protected const string SelfVideoName = "myImage";
    protected const string MainVideoName = "mainImage";
    protected string mChannel;

    /// <summary>
    ///   Where to display the video stream for remote user.  See
    /// the option SelfVideoName or MainVideoName.  Derived class to override.
    /// </summary>
    protected virtual string RemoteStreamTargetImage
    {
        get
        {
            return SelfVideoName;
        }
    }

    protected bool remoteUserJoined = false;

    public PlayerViewControllerBase()
    {
        // Constructor, nothing to do for base
    }

    /// <summary>
    ///   Join a RTC channel
    /// </summary>
    /// <param name="channel"></param>
    public void Join(string channel)
    {
        Debug.Log("calling join (channel = " + channel + ")");

        if (mRtcEngine == null)
            return;

        mChannel = channel;

        // set callbacks (optional)
        mRtcEngine.OnJoinChannelSuccess = OnJoinChannelSuccess;
        mRtcEngine.OnUserJoined = OnUserJoined;
        mRtcEngine.OnUserOffline = OnUserOffline;

        // Calling virtual setup function
        PrepareToJoin();

        // join channel
        mRtcEngine.JoinChannel(channel, null, 0);

        Debug.Log("initializeEngine done");
    }

    /// <summary>
    ///    Preparing video/audio/channel related characteric set up
    /// </summary>
    protected virtual void PrepareToJoin()
    {
        // enable video
        mRtcEngine.EnableVideo();
        // allow camera output callback
        mRtcEngine.EnableVideoObserver();
    }

    /// <summary>
    ///   Leave a RTC channel
    /// </summary>
    public virtual void Leave()
    {
        Debug.Log("calling leave");

        if (mRtcEngine == null)
            return;

        // leave channel
        mRtcEngine.LeaveChannel();
        // deregister video frame observers in native-c code
        mRtcEngine.DisableVideoObserver();
    }

    protected bool MicMuted { get; set; }

    protected virtual void SetupUI()
    {
        Button button = GameObject.Find("LeaveButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnLeaveButtonClicked);
        }

        Button mutton = GameObject.Find("MuteButton").GetComponent<Button>();
        if (mutton != null)
        {
            mutton.onClick.AddListener(() =>
            {
                MicMuted = !MicMuted;
                mRtcEngine.EnableLocalAudio(!MicMuted);
                Text text = mutton.GetComponentInChildren<Text>();
                text.text = MicMuted ? "Unmute" : "Mute";
            });
        }
    }

    protected void OnLeaveButtonClicked()
    {
        Leave();
        UnloadEngine();

        if (OnViewControllerFinish != null)
        {
            OnViewControllerFinish();
        }
    }

    /// <summary>
    ///   Load the Agora RTC engine with given AppID
    /// </summary>
    /// <param name="appId">Get the APP ID from Agora account</param>
    public void LoadEngine(string appId)
    {
        // init engine
        mRtcEngine = IRtcEngine.GetEngine(appId);

        mRtcEngine.OnError = (code, msg) =>
        {
            Debug.LogErrorFormat("RTC Error:{0}, msg:{1}", code, IRtcEngine.GetErrorDescription(code));
        };

        mRtcEngine.OnWarning = (code, msg) =>
        {
            Debug.LogWarningFormat("RTC Warning:{0}, msg:{1}", code, IRtcEngine.GetErrorDescription(code));
        };

        // enable log
        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);
    }

    // unload agora engine
    public virtual void UnloadEngine()
    {
        Debug.Log("calling unloadEngine");

        // delete
        if (mRtcEngine != null)
        {
            IRtcEngine.Destroy();  // Place this call in ApplicationQuit
            mRtcEngine = null;
        }
    }

    /// <summary>
    ///   Enable/Disable video
    /// </summary>
    /// <param name="pauseVideo"></param>
    public void EnableVideo(bool pauseVideo)
    {
        if (mRtcEngine != null)
        {
            if (!pauseVideo)
            {
                mRtcEngine.EnableVideo();
            }
            else
            {
                mRtcEngine.DisableVideo();
            }
        }
    }

    public virtual void OnSceneLoaded()
    {
        SetupUI();
    }

    // implement engine callbacks
    protected virtual void OnJoinChannelSuccess(string channelName, uint uid, int elapsed)
    {
        Debug.Log("JoinChannelSuccessHandler: uid = " + uid);
    }

    // When a remote user joined, this delegate will be called. Typically
    // create a GameObject to render video on it
    protected virtual void OnUserJoined(uint uid, int elapsed)
    {
        Debug.Log("onUserJoined: uid = " + uid + " elapsed = " + elapsed);

        // find a game object to render video stream from 'uid'
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            return; // reuse
        }

        // create a GameObject and assign to this new user
        VideoSurface videoSurface = makeImageSurface(uid.ToString());
        if (!ReferenceEquals(videoSurface, null))
        {
            // configure videoSurface
            videoSurface.SetForUser(uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
            videoSurface.SetGameFps(30);
        }
    }

    // When remote user is offline, this delegate will be called. Typically
    // delete the GameObject for this user
    protected virtual void OnUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        // remove video stream
        Debug.Log("onUserOffline: uid = " + uid + " reason = " + reason);
    }

    private const float Offset = 100;
    public VideoSurface makeImageSurface(string goName)
    {
        GameObject go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName;

        // to be renderered onto
        go.AddComponent<RawImage>();

        // make the object draggable
        go.AddComponent<UIElementDragger>();
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            go.transform.SetParent(canvas.transform);
        }
        // set up transform
        go.transform.Rotate(0f, 0.0f, 180.0f);
        float xPos = UnityEngine.Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
        float yPos = UnityEngine.Random.Range(Offset, Screen.height / 2f - Offset);
        go.transform.localPosition = new Vector3(xPos, yPos, 0f);
        go.transform.localScale = new Vector3(1.5f, 2f, 1f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }
}
