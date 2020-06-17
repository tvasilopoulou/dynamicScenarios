#if UNITY_EDITOR
    using UnityEditor;
#endif

using UnityEngine;
using System;
using System.Collections;
using Vuforia;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;
using System.IO;
using System.IO.Compression;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Threading;
using System.Net;
using Image = Vuforia.Image;
using Dummiesman;
using System.Text.RegularExpressions;



public class CameraScript : MonoBehaviour
{
    public int loopCount;
    //public RawImage rawImage;
    public UnityEngine.UI.Image image;
    public Plane pl;
    //public GameObject uDTManager;
    public Canvas canvas;
    public Material init_material;
    public Material project_material;
    //public UnityEngine.UI.Text textinfo;
    // public Button fullscreen_button;
    // public Button exit_fullscreen_button;
    public Button closeButton;
    // public Button closeButton;
    public Button play_button;
    public Button pause_button;
    public Button scan_area;
    public GameObject ProcessingMsg;
    public MeshRenderer frame;

    private BarCodeManager barCodeManager;
    private Stream stream;
    // public Sprite fullscreen;
    // public Sprite exitFullscreen;

    // public GameObject plane;

    private float step = 0.1f;
    private float limit_zoom = 2.0f;
    private float zoom_in = 1, zoom_out = 1;
    public Slider zoom;
    public float zoomPosition;

    public GameObject loadedObj;
    public MemoryStream ms;
    public bool draw;
    public bool zipBuild;
    public bool plainBuild;
    public bool imgBuild;
    public bool vidBuild;

    // public bool buttonFlag;
    private int rotCount;
    private Coroutine any_coroutine;

    //private data members
    private bool cameraInitialized;
    private IBarcodeReader barCodeReader;
    private Texture2D texture2D;
    private CameraDevice cameraInstance;
    private Result currentQRScannedData;
    private long counter;
    private PIXEL_FORMAT mPixelFormat;
    QRCodeReader reader;
    CameraDevice.VideoModeData videoModeData;
    private ResultPoint[] point;
    //private Vector3 world_point = new Vector3();
    //private Camera cam;
    private bool decoding;
    //public VideoClip videoToPlay;
    //private VideoPlayer videoPlayer;
    private VideoSource videoSource;
    private AudioSource audioSource;
    //https://answers.unity.com/questions/300864/how-to-stop-a-co-routine-in-c-instantly.html
    private Coroutine my_coroutine;

    public RawImage Video;
    public VideoPlayer videoPlayer;

    private bool downloading = false;
    private float shrink_top = 0.25f;
    private float shrink_left = 0.25f;
    private float shrink_height = 0.5f;
    private float shrink_width = 0.5f;

    void Start()
    {
        closeButton.gameObject.SetActive(false);
        // fullscreen_button.gameObject.SetActive(false);
        // exit_fullscreen_button.gameObject.SetActive(false);
        play_button.gameObject.SetActive(false);
        pause_button.gameObject.SetActive(false);
        scan_area.gameObject.SetActive(true);
        ProcessingMsg.SetActive(false);
        zoom.gameObject.SetActive(false);
        zoomPosition = zoom.gameObject.GetComponent<Slider>().value;

        rotCount = 0;
        loopCount = 0;
        draw = false;
        zipBuild = true;
        imgBuild = true;
        vidBuild = true;
        plainBuild = true;
        var vuforia = VuforiaARController.Instance;
        vuforia.RegisterVuforiaStartedCallback(OnVuforiaStarted);
        vuforia.RegisterOnPauseCallback(OnPaused);
        vuforia.RegisterTrackablesUpdatedCallback(OnTrackablesUpdated);
        //barCodeManager = new BarCodeManager(OnResultFound);

        zoom_in = zoom_out = 1;
        
        closeButton.onClick.AddListener(delegate
        {
            this.close();
        });


        play_button.onClick.AddListener(delegate
        {

            this.PlayVideo();
        });

        pause_button.onClick.AddListener(delegate
        {
            this.PauseVideo();
        });

        zoom.minValue = 1; zoom.maxValue = zoom.minValue + limit_zoom;
        zoom.onValueChanged.AddListener(delegate
        {
            image.GetComponent<RectTransform>().localScale = new Vector3(zoom.value, zoom.value, 1);
        });
    }

    private void OnVuforiaStarted()
    {
        Debug.Log("OnVuforiaStarted");
        cameraInstance = CameraDevice.Instance;
        cameraInstance.SetFrameFormat(PIXEL_FORMAT.GRAYSCALE, true);
        mPixelFormat = PIXEL_FORMAT.GRAYSCALE;
        Debug.Log("PIXEL FORMAT is " + mPixelFormat);
        cameraInstance.GetVideoMode(CameraDevice.CameraDeviceMode.MODE_DEFAULT);
        cameraInstance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
        ConfigureBarcodeScanner();
        cameraInitialized = true;
        Debug.Log("Screen width" + Screen.width * shrink_width);
        Debug.Log("Screen height" + Screen.height * shrink_height);
    }

    private void ConfigureBarcodeScanner()
    {
        
        reader = new QRCodeReader();
        barCodeReader = new BarcodeReader(reader, null, (src)=> {return new HybridBinarizer(src); });
        barCodeReader.ResultFound += (obj) => {
        Debug.Log("OnResultScanned");

            if (obj != null)
			{
                string url = obj.Text;
                
                if(url.Contains("dropbox") || url.Contains("drive")){
                    url = url.Split('?')[0];                    //MAYBE MAKE THIS ?dl=
                    url = url.Replace("://www.", "://dl.");
                }
                // else if(url.Contains("iframe"))
                //     url = url.Replace("http://", "");
                Debug.Log("First URL:" + url);
				StartCoroutine(LoadFromWeb(url));
			}
            
            //VERY IMPORTANT NOTES
            //https://docs.unity3d.com/ScriptReference/Camera.ScreenToWorldPoint.html
            //https://stackoverflow.com/questions/50073719/using-zxing-in-unity-to-locate-qrcodes-position-pattern
            point = obj.ResultPoints;
            Debug.Log("qr code position : " + point[0].X + " and " + point[1].Y);
            //world_point = Camera.main.ScreenToWorldPoint(new Vector3(point[0].X, point[1].Y, Camera.main.nearClipPlane));
        };
        barCodeReader.Options = new DecodingOptions();
        //barCodeReader.Options.TryHarder = true;
    }

    

    private void projectImage(Sprite s)
    {
        image.sprite = s;
        image.material = project_material;
        closeButton.gameObject.SetActive(true);
        // fullscreen_button.gameObject.SetActive(true);
        // exit_fullscreen_button.gameObject.SetActive(true);
    }


    public static string Reverse( string s )
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse( charArray );
        return new string( charArray );
    }

    public IEnumerator Fetch( string url ) {
        texture2D = new Texture2D(1, 1, TextureFormat.RGB24, true);
         while(true) {
             Debug.Log("loading... "+Time.realtimeSinceStartup);
             WWWForm form = new WWWForm();
             WWW www = new WWW(url);
             yield return www;
             if(!string.IsNullOrEmpty(www.error))
                 throw new UnityException(www.error);
             www.LoadImageIntoTexture(texture2D);
         }
     }

    public void OnGUI() {
        if(draw == true)
            GUI.DrawTexture(new Rect(160, 10, 330, 300), texture2D);
     }


    public void GetVideo(string url){
        texture2D = new Texture2D(2, 2); 
        // create HTTP request
        HttpWebRequest req = (HttpWebRequest) WebRequest.Create( url );
        //Optional (if authorization is Digest)
        req.ProtocolVersion = HttpVersion.Version10;
        draw = true;
        req.Timeout = 5000;
        req.Credentials = new NetworkCredential("username", "password");
        // get response
        WebResponse resp = req.GetResponse();
        
        // get response stream
        stream = resp.GetResponseStream();
        frame.material.color = Color.white;
        StartCoroutine (GetFrame ());
    }
    IEnumerator GetFrame (){
        Byte [] JpegData = new Byte[65536];

        while(true) {
            int bytesToRead = FindLength(stream);
            if (bytesToRead == -1) {
                print("End of stream");
                yield break;
            }

            int leftToRead=bytesToRead;

            while (leftToRead > 0) {
                leftToRead -= stream.Read (JpegData, bytesToRead - leftToRead, leftToRead);
                yield return null;
            }

            ms = new MemoryStream(JpegData, 0, bytesToRead, false, true);

            texture2D.LoadImage (ms.GetBuffer ());
            frame.material.mainTexture = texture2D;
            frame.material.color = Color.white;
            stream.ReadByte(); // CR after bytes
            stream.ReadByte(); // LF after bytes
            ms.Close();
        }
    }

    int FindLength(Stream stream)  {
        int b;
        string line="";
        int result=-1;
        bool atEOL=false;

        while ((b=stream.ReadByte())!=-1) {
            if (b==10) continue; // ignore LF char
            if (b==13) { // CR
                if (atEOL) {  // two blank lines means end of header
                    stream.ReadByte(); // eat last LF
                    return result;
                }
                if (line.StartsWith("Content-Length:")) {
                    result=Convert.ToInt32(line.Substring("Content-Length:".Length).Trim());
                } else {
                    line="";
                }
                atEOL=true;
            } else {
                atEOL=false;
                line+=(char)b;
            }
        }
        return -1;
    }
    
    void OnTrackablesUpdated()
    {
        CameraDevice.Instance.SetFlashTorchMode( true );
        if (cameraInitialized && !downloading)
        {
            Vuforia.Image cameraFrame = null;
            try
            {   if(counter % 10 == 0) 
                   cameraFrame = CameraDevice.Instance.GetCameraImage(PIXEL_FORMAT.GRAYSCALE);
                counter++;
            }
            catch (Exception e) { Debug.LogError(e.Message); }
            if (cameraFrame == null)
            {
                //Debug.Log("CameraFeed is null");
                return;
            }
            else if ( cameraFrame.BufferWidth > 0 && cameraFrame.BufferHeight > 0)
            {
                Debug.Log(" Decoding Frame ");
                RGBLuminanceSource src = new RGBLuminanceSource(cameraFrame.Pixels, cameraFrame.BufferWidth, cameraFrame.BufferHeight, RGBLuminanceSource.BitmapFormat.Gray8);
                RGBLuminanceSource croppedSrc = (RGBLuminanceSource)src.crop(Convert.ToInt32(cameraFrame.BufferWidth * 0.25),
                Convert.ToInt32(cameraFrame.BufferHeight * 0.25), Convert.ToInt32(cameraFrame.BufferWidth * 0.5), Convert.ToInt32(cameraFrame.BufferHeight * 0.5));

                //barCodeReader.Decode(src);
                barCodeReader.Decode(croppedSrc);
                //or
                // barCodeReader.Decode(cameraFrame.Pixels, cameraFrame.BufferWidth, cameraFrame.BufferHeight, RGBLuminanceSource.BitmapFormat.BGR32);
            }
        }
        if (Int64.MaxValue - counter < 1000)
        {
            Debug.Log("Resetting counter");
            counter = 0;
        }
    }
    IEnumerator LoadFromWeb(string url)
    {
        string urlBackup = "";
        var gObj = FindObjectsOfType(typeof(GameObject));
        ProcessingMsg.SetActive(true);
        downloading = true;
        cameraInitialized = false;
        scan_area.gameObject.SetActive(false);
        Dictionary<string, string> content_info = null;
        Debug.Log("Fetching data from URL decoded...");
        if(url.Contains("youtube")){
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C youtube-dl.exe " + url;
            process.StartInfo = startInfo;
            process.Start();
            //https://github.com/ytdl-org/youtube-dl

            // string content = url;
            // File.WriteAllText("newIFrame.html", content);
            // url = "https://this-page-intentionally-left-blank.org/";
            // Debug.Log("ALLDONE");
        }
        else if(isIP(url)){
            Debug.Log("Indeed ip");
            urlBackup = String.Copy(url);
            url = "https://this-page-intentionally-left-blank.org/";
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            Debug.Log(webRequest);
            if (webRequest.isNetworkError)
                Debug.Log("A network error happened");
            else {
                content_info = webRequest.GetResponseHeaders();
                foreach (KeyValuePair<string, string> pair in content_info)
                {
                    Debug.Log("key : " + pair.Key + " and Value : " + pair.Value);
                }
            }
        }

        else{
            if(url.Contains("drive")){
                string ext = Reverse(url);
                int extension = ext.IndexOf("/");
                ext = ext.Substring(extension , ext.Length-extension);                              //reverse string to get its extension
                extension = ext.IndexOf("/");
                ext = ext.Split('/')[1];
                ext = ext.Split('/')[0];

                ext = Reverse(ext);
                url = "https://drive.google.com/uc?export=download&id=" + ext;
                // using (var client = new WebClient()){
                //     client.DownloadFile(url, "./Assets/" + ext);           //from qr-url to specified obj path
                // }
            }
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            Debug.Log(webRequest);
            if (webRequest.isNetworkError)
                Debug.Log("A network error happened");
            else {
                content_info = webRequest.GetResponseHeaders();
                foreach (KeyValuePair<string, string> pair in content_info)
                {
                    Debug.Log("key : " + pair.Key + " and Value : " + pair.Value);
                }
            }
        }
        Debug.Log("2OK");
        DownloadHandlerTexture texDl = new DownloadHandlerTexture(true);

        UnityWebRequest wr = new UnityWebRequest(url);
        wr.downloadHandler = texDl;
        yield return wr.SendWebRequest();
        Debug.Log("3OK " +wr);
        //checking if content is video or image
        bool isYoutube = url.Contains("youtube");           //ROUTINE TO HANDLE YOUTUBE VIDEOS

        if(isYoutube){
            Debug.Log("Yvideo is here");
            string[] files = System.IO.Directory.GetFiles("./", "*.mp4");
            //Play_Video(url);
            Debug.Log(files[0]);
            closeButton.gameObject.SetActive(true); //if video takes to much time cancelation is available
            any_coroutine = StartCoroutine(VideoRoutine(files[0], VideoPrepareCompleted, VideoEndReached));
        }


        if (!(wr.isNetworkError || wr.isHttpError) && !isYoutube)
        {
            Debug.Log("DownloadedImage");

            //Types of Digital Image Files: TIFF, JPEG, GIF, PNG
            bool isImage = content_info.ContainsValue("image/*") || 
                    url.Contains(".jpg") || url.Contains(".jpeg") | url.Contains(".tiff") || url.Contains(".gif") || url.Contains(".png");
            Debug.Log("content_info " + content_info);
            bool isVideo = content_info.ContainsValue("video/mp4");
            bool is3DObj = url.Contains(".obj");  //SHOULD PROBABLY CHANGE
            bool isZip = url.Contains(".zip");  //SHOULD PROBABLY CHANGE
            bool isIPCamera = isIP(url);
            // Debug.Log(content_info);
            Debug.Log("isImage = " + isImage);
            Debug.Log("isVideo = " + isVideo);
            Debug.Log("is3DObj = " + is3DObj);
            Debug.Log("isZip = " + isZip);
            Debug.Log("isIPCamera = " + isIPCamera);
            Debug.Log("content_info " + isImage);
            ProcessingMsg.SetActive(false);
            
        //GOOD PART
            if (content_info.ContainsValue("image/tiff") || content_info.ContainsValue("image/jpeg") || content_info.ContainsValue("image/jpg")
                || content_info.ContainsValue("image/gif") || content_info.ContainsValue("image/png") || isImage == true)
            {
                Debug.Log("we have an image!");
                //the user see the object. no scanning needed
                
                if (imgBuild == true){
                
                var Plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Plane.transform.localScale = new Vector3(100.0f, 100.0f, 100.0f);
                Plane.transform.Rotate(0.0f, 0.0f, -180.0f, Space.World);


                cameraInitialized = false;
                Debug.Log(texDl.texture.width);
                Sprite s =  Sprite.Create(texDl.texture, new Rect(0, 0, texDl.texture.width, texDl.texture.height), new Vector2(0, 0));
                texture2D = texDl.texture;
                
                // image.GetComponent<RectTransform>().sizeDelta = new Vector2(texDl.texture.width, texDl.texture.height);
                // image.sprite = s;
                // Debug.Log("sprite: " + s);
                // image.material = project_material;
                // Vector3 trans = new Vector3(2000.0f, 2000.0f, 2000.0f);
                // image.transform.position = trans;

                GameObject plane = GameObject.Find("Plane");
                Debug.Log("project material is: " + project_material);
                plane.GetComponent<Renderer>().material = project_material;
                plane.GetComponent<Renderer>().material.mainTexture = texture2D;
                // Graphics graph = plane as Graphics;

                plane.name = "object";
                }
                Debug.Log( "zoomslider : " + zoom.GetComponent<Slider>().value);
                Button gameObj = GameObject.Find("BuildButton").GetComponent<Button>();
                if (imgBuild == true){
                    gameObj.onClick.Invoke();
                    imgBuild = false;
                } 
                // plane.GetComponent<MeshRenderer>()=image;
                closeButton.gameObject.SetActive(true);
                zoom.gameObject.SetActive(true);
                // GameObject temp = 
                Debug.Log(zoom.GetComponent<Slider>().targetGraphic);

                //fullscreen_button.gameObject.SetActive(true);
                //exit_fullscreen_button.gameObject.SetActive(true);
                ProcessingMsg.SetActive(false);
            }
            else if(isZip){
                Debug.Log("Zip is here");
                closeButton.gameObject.SetActive(true);

                string ext = Reverse(url);
                int extension = ext.IndexOf(".");
                ext = ext.Substring(0, extension);                              //reverse string to get its extension
                ext = Reverse(ext);
                Debug.Log("extension is "+ ext);
                if(!File.Exists("./Assets/object.zip")){
                    using (var client = new WebClient()){
                        client.DownloadFile(url, "./Assets/object.zip");           //from qr-url to specified obj path
                    }
                    if(!File.Exists("./Assets/object")) {                           //if no zips chached
                        ZipFile.ExtractToDirectory("./Assets/object.zip", "./Assets/object", true);
                        Debug.Log("Extraction OK");
                        // if(File.Exists("./Assets/object.zip"))                      //delete zipfile
                        File.Delete("./Assets/object.zip");
                        File.Delete("./Assets/object.zip.meta");
                        Debug.Log("Deletion done");
                        string objFile = Directory.GetFiles("./Assets/object", "*.obj")[0];
                        string mtlFile = Directory.GetFiles("./Assets/object", "*.mtl")[0];
                        if(!File.Exists("./Assets/object/object.obj")) System.IO.File.Move(objFile, "./Assets/object/object.obj");
                        if(!File.Exists("./Assets/object/object.mtl")) System.IO.File.Move(mtlFile, "./Assets/object/object.mtl");
                        string filePath = "";
                        string mtlPath = "";
                        int objFiles = Directory.GetFiles("./Assets/object", "*.obj").Length;
                        int mtlFiles = Directory.GetFiles("./Assets/object", "*.mtl").Length;
                        if(objFiles == 1 && mtlFiles == 1){
                            filePath = Directory.GetFiles("./Assets/object", "*.obj")[0];           //create object to load
                            mtlPath = Directory.GetFiles("./Assets/object", "*.mtl")[0];
                            Debug.Log("Proceed");
                            Destroy(loadedObj);
                            // loadedObj = null;
                            // gObj = FindObjectsOfType(typeof(GameObject));
                            for(int i = 0; i<gObj.Length; i++){
                                if(gObj[i].name.Contains("object")){
                                    Debug.Log(gObj[i] + "  : " + i);
                                    Destroy(gObj[i]);
                                }
                            }
                            if (zipBuild == true) loadedObj = new OBJLoader().Load(filePath, mtlPath);             //GameObject var

                            Vector3 pos = new Vector3(450.0f,140.0f,0.0f);
                            loadedObj.transform.position += pos;
                            loadedObj.transform.Rotate(180.0f, 0.0f, 180.0f, Space.Self);
                            Vector3 scaleChange = new Vector3(3.0f, 1.0f, 1.0f);
                            loadedObj.transform.localScale += scaleChange;
                        }
                    }
                    // ProcessingMsg.SetActive(false);
                    var gameObjects = FindObjectsOfType(typeof(GameObject));
                    // for(int i = 0; i<gameObjects.Length; i++){
                    //     if(gameObjects[i].name.Contains("BuildButton")){
                    //         Debug.Log(gameObjects[i] + "  : " + i);
                    //         // Destroy(gameObjects[i]);
                    //         gameObjects[i].onClick.Invoke();
                    //     }
                    // }
                    Button gameObj = GameObject.Find("BuildButton").GetComponent<Button>();
                    if (zipBuild == true){
                        gameObj.onClick.Invoke();
                        zipBuild = false;
                    } 
                }
            }
            else if(is3DObj)
            {
                Debug.Log("3d object is here");
                closeButton.gameObject.SetActive(true);

                string ext = Reverse(url);
                int extension = ext.IndexOf(".");
                ext = ext.Substring(0, extension);
                ext = Reverse(ext);
                Debug.Log("extension is "+ ext);
                if(ext == "obj"){
                    using (var client = new WebClient()){
                        client.DownloadFile(url, "./Assets/object.obj");           //from qr-url to specified obj path
                    }
                }
                else if(ext == "fbx" || ext == "FBX"){
                    using (var client = new WebClient()){
                        client.DownloadFile(url, "./Assets/object.fbx");           //from qr-url to specified obj path
                    }
                }
                

                // string mtl = Reverse(url);
                // int extension = mtl.IndexOf(".");
                // mtl = mtl.Substring(0, extension);
                // mtl = Reverse(mtl);
                // mtl = mtl + "object.mtl";


                //MTL FILE -> add prompt
                // Debug.Log(mtl);
                // using (var client = new WebClient()){
                //     client.DownloadFile(mtl, "object.mtl");
                // }  //doesn't work -> different url route in dropbox

                //name from url!!!
                // string modelName = Reverse(url);
                // int found = modelName.IndexOf("/");
                // modelName = modelName.Substring(0, found);
                // modelName = Reverse(modelName);
                // Debug.Log("object is: " + modelName);       //returns that 122..object... string name -> in case needed


                string filePath = "";
                if(ext == "obj"){
                    filePath = @"./Assets/object.obj";           //save under name object
                    Debug.Log("filepath: " + filePath);
                    #if UNITY_EDITOR
                        AssetDatabase.ImportAsset(filePath);
                    #endif
                }
                // else if(ext == "fbx" || ext == "FBX"){
                //     filePath = @"./Assets/object.fbx";
                //     Debug.Log("filepath: " + filePath);
                //     #if UNITY_EDITOR
                //         AssetDatabase.ImportAsset(filePath);
                //     #endif
                // }

                Debug.Log("object is here " + filePath);
                if (!File.Exists(filePath))
                {
                    Debug.LogError("Please set FilePath in ObjFromFile.cs to a valid path.");
                }
                // string mtlPath = @"./Assets/object.mtl";                               //add prompt to scan materials or use defaults
                // Destroy(loadedObj);
                // loadedObj = null;
                // gObj = FindObjectsOfType(typeof(GameObject));
                for(int i = 0; i<gObj.Length; i++){
                    if(gObj[i].name.Contains("object") && plainBuild == true){
                        Debug.Log(gObj[i] + "  : " + i);
                        Destroy(gObj[i]);
                    }
                }
                // if (plainBuild == false)
                loadedObj = new OBJLoader().Load(filePath);             //GameObject var
                Vector3 pos = new Vector3(450.0f,140.0f,0.0f);
                loadedObj.transform.position += pos;


                loadedObj.transform.Rotate(180.0f, 0.0f, 180.0f, Space.World);
                Vector3 scaleChange = new Vector3(3.0f, 1.0f, 1.0f);
                loadedObj.transform.localScale += scaleChange;

                Button gameObj = GameObject.Find("BuildButton").GetComponent<Button>();
                Debug.Log("hello4");

                if (plainBuild == true){
                    gameObj.onClick.Invoke();
                    plainBuild = false;
                } 

            }


            else if(content_info.ContainsValue("video/mp4"))
            {
                Debug.Log("video is here");
                //Play_Video(url);
                closeButton.gameObject.SetActive(true); //if video takes to much time cancelation is available
                any_coroutine = StartCoroutine(VideoRoutine(url, VideoPrepareCompleted, VideoEndReached));
            }

            else if (url.Contains("https://this-page-intentionally-left-blank.org/")){
                closeButton.gameObject.SetActive(true);

                url = "";
                url = String.Copy(urlBackup);
                Debug.Log("I am an ip: " + url);
                GetVideo(url);

            }

            else{
                    //do nothing
            }

            //-------------------------------------------------
            // QRCode detected.
            Debug.Log(url);
            //Application.OpenURL(data.Text);      // our function to call and pass url as text
            //textinfo.text = url;
            cameraInitialized = true;
            downloading = false;
            ///////////////
        }
    }


    public void onImageZoomed(){
        var zoomPlane = GameObject.Find("object");
        if(zoomPlane!=null){
            if(zoom.gameObject.GetComponent<Slider>().value > zoomPosition){
                Debug.Log("zoomin");
                zoomPlane.transform.localScale += new Vector3(10.0f, 10.0f, 10.0f);
                zoomPosition = zoom.gameObject.GetComponent<Slider>().value;
            }
            else if (zoom.gameObject.GetComponent<Slider>().value < zoomPosition){
                Debug.Log("zoomout");
                zoomPlane.transform.localScale -= new Vector3(20.0f, 20.0f, 20.0f);
                zoomPosition = zoom.gameObject.GetComponent<Slider>().value;
            }
        }
    }



    public void VideoPrepareCompleted(VideoPlayer videoPlayer)
    {
        Debug.Log("In VideoPrepareCompleted");
        ProcessingMsg.SetActive(false);
        play_button.gameObject.SetActive(true);
        pause_button.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
        Video.material = project_material;
        Debug.Log("Done Preparing Video");
        videoPlayer.Play();
        videoPlayer.gameObject.GetComponent<AudioSource>().Play();// audioSource.Play();
        videoPlayer.gameObject.GetComponent<VideoPlayer>().Play();// audioSource.Play();
        Debug.Log("Done Playing Video");
    }

    public void VideoEndReached(VideoPlayer videoPlayer)
    {
        videoPlayer.loopPointReached += VideoPrepareCompleted;
        Debug.Log("Done Playing Video");
    }

    private void PlayVideo()
    {
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }
    private void PauseVideo()
    {
        if (!videoPlayer.isPaused)
        {
            videoPlayer.Pause();
        }
    }

    public IEnumerator VideoRoutine(string url, VideoPlayer.EventHandler VideoPrepareCompleted, VideoPlayer.EventHandler VideoEndReached)
    {
        /// <summary>set the trackable handler handle the videoplayer</summary>
        /// <summary>wait until components have been created</summary>
        /// <summary>callbacks for onprepared and loop</summary>
        /// 

        //GameObject Quad4Video = GameObject.Find("Quad4Video");
        //VideoPlayer videoPlayer = videoImage.transform.Find("Video")GetComponent.GetComponent<VideoPlayer>();

        //to wait for the video is a good tecnhique but may halt the app when internet connectivity is bad
        //MUST RE-CHECK THIS HERE
        videoPlayer.prepareCompleted += VideoPrepareCompleted;
        videoPlayer.loopPointReached += VideoEndReached;

        Debug.LogWarning(url);
                

        //processing ended here 
        //Status.SetActive(false);

        //just some silly code
        yield return new WaitUntil(predicate: () =>
        {
            //Debug.Log("Checking video UDT creation");
            //Thread.Sleep(1);
            return videoPlayer!=null;
        });

        AudioSource audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
        //Disable Play on Awake for both Video and Audio
        videoPlayer.playOnAwake = false;
        audioSource.playOnAwake = false;
        videoPlayer.skipOnDrop = true;
        Debug.Log("vidBuild: " + vidBuild + loopCount);
        if(vidBuild == true){
            var Plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Plane.transform.localScale = new Vector3(100.0f, 100.0f, 100.0f);
            Plane.transform.Rotate(0.0f, 0.0f, -180.0f, Space.World);

            GameObject plane = GameObject.Find("Plane");
            plane.GetComponent<Renderer>().material = project_material;
            Debug.Log("targettext: " + videoPlayer.targetTexture);
            Debug.Log("targetmat: " + plane.GetComponent<Renderer>().material);
            plane.GetComponent<Renderer>().material.mainTexture = videoPlayer.targetTexture;
            plane.name = "object";
        }
        
        GameObject objPlane = GameObject.Find("object");
        videoPlayer = objPlane.AddComponent<VideoPlayer>();
        // vidPl = videoPlayer;
        // objPlane.transform.SetParent(videoPlayer.transform);
        // Video clip from Url
        //https://answers.unity.com/questions/1370621/using-videoplayer-to-stream-a-video-from-a-website.html
        //videoPlayer.source = VideoSource.Url;
        //videoPlayer.url = "https://dl.dropbox.com/s/d4f4v2df1lhtz5q/DEMO%201%2027-36%281%29%281%29.mp4";
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        //Set Audio Output to AudioSource
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        Debug.Log("Audiosource fixed");
        //Assign the Audio from Video to AudioSource to be played
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        //renderVideoOptions(videoPlayer);
        videoPlayer.Prepare();
        // var renderer = GetComponent<Renderer>();
        // renderer.material.mainTexture = videoPlayer.targetTexture;
        pause_button.gameObject.SetActive(true);
        play_button.gameObject.SetActive(true);
        
        Button gameObj = GameObject.Find("BuildButton").GetComponent<Button>();
        if (vidBuild == true){
            gameObj.onClick.Invoke();
            vidBuild = false;
        } 
        
    }

    
    private void close()
    {
        image.material = init_material;
        Video.material = init_material;


        zoom.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        //fullscreen_button.gameObject.SetActive(false);
        play_button.gameObject.SetActive(false);
        pause_button.gameObject.SetActive(false);
        //exit_fullscreen_button.gameObject.SetActive(false);
        image.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 480);
        image.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        //image.uvRect = new Rect(0, 0, 100, 100);
        scan_area.gameObject.SetActive(true);
        //when an object is present dont scan until close button is pressed
        cameraInitialized = true;

        VideoPlayer vidPl=null;
        if(GameObject.Find("object").GetComponent<VideoPlayer>()!=null)
            vidPl = GameObject.Find("object").GetComponent<VideoPlayer>();
        Destroy(loadedObj);
        loadedObj = GameObject.Find("object");
        Destroy(loadedObj);
        Destroy(GameObject.Find("object"));
        if(File.Exists("./Assets/object.obj")){
            File.Delete("./Assets/object.obj");
            File.Delete("./Assets/object.obj.meta");
        }
        if(File.Exists(@"./Assets/object.zip")){
            File.Delete("./Assets/object.zip");
        }
        if(Directory.Exists("./Assets/object"))                      //delete object directory
            Directory.Delete("./Assets/object", true);
        // videoPlayer = vidPl;
        loopCount += 1;
        draw = false;
        zipBuild = true;
        imgBuild = true;
        vidBuild = true;
        plainBuild = true;

        if(vidBuild==false) vidBuild = true;
        var imageTarget = GameObject.Find("ImageTargetBehaviour");
        // videoPlayer = vidPl.GetComponent<VideoPlayer>();
        videoPlayer = GameObject.Find("Video Player").GetComponent<VideoPlayer>();
        StopCoroutine(GetFrame());
        ProcessingMsg.gameObject.SetActive(false);
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
            StopCoroutine(any_coroutine);
        }

    }




    void OnPaused(bool paused)
    {
        Debug.Log("OnPaused");
        if (paused) cameraInitialized = false;
        if (!paused) // resumed
        {
            // Set again autofocus mode when app is resumed
            cameraInitialized = true;
            cameraInstance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
        }
    }



    
    //unused
    private RGBLuminanceSource CreateRGBLuminanceSource(Color32[] bytes, int width, int height)
    {
        Debug.Log("CreateLuminanceSource");
        Color32LuminanceSource src = new Color32LuminanceSource(bytes, 640, 480);

        RGBLuminanceSource rGBLuminanceSource = (RGBLuminanceSource)src.crop(Convert.ToInt32(640 * 0.25), Convert.ToInt32(480 * 0.25),
                                                             Convert.ToInt32(640 * 0.5), Convert.ToInt32(480 * 0.5));
        return rGBLuminanceSource;

    }

    

    private bool isIP( string url ){ 
        var checkURL = url.Replace("http://", "");
        checkURL = checkURL.Replace("https://", "");
        if(checkURL.Contains("mjpg") || checkURL.Contains("mpg")) return true;
        checkURL = checkURL.Split('/')[0];
        checkURL = checkURL.Split(':')[0];
        Debug.Log("is ip? " + checkURL);
        var match = Regex.Match(checkURL, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
        if(match.Success) return true;
        else return false;
    }

    //never used
    private LuminanceSource CreateLuminanceSource(Color32[] bytes, int width, int height)
    {
        Debug.Log("CreateLuminanceSource");
        Color32LuminanceSource src = new Color32LuminanceSource(bytes, 640, 480);

        RGBLuminanceSource rGBLuminanceSource = (RGBLuminanceSource)src.crop(Convert.ToInt32(640 * 0.25), Convert.ToInt32(480 * 0.25),
                                                             Convert.ToInt32(640 * 0.5), Convert.ToInt32(480 * 0.5));
        return rGBLuminanceSource;

    }

    void OnApplicationQuit()
    {
        Debug.Log("I'm out of here");
        if(Directory.Exists("./Assets/object"))                      //delete object directory
            Directory.Delete("./Assets/object", true);
    }

}
