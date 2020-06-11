/*==============================================================================
Copyright (c) 2016-2018 PTC Inc. All Rights Reserved.

Copyright (c) 2015 Qualcomm Connected Experiences, Inc. All Rights Reserved.

Vuforia is a trademark of PTC Inc., registered in the United States and other
countries.
 * ==============================================================================*/
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Vuforia;
using System;
using System.Collections;
using Dummiesman;
using UnityEngine.UI;


public class UDTEventHandler : MonoBehaviour, IUserDefinedTargetEventHandler
{
    #region PUBLIC_MEMBERS
    /// <summary>
    /// Can be set in the Unity inspector to reference an ImageTargetBehaviour
    /// that is instantiated for augmentations of new User-Defined Targets.
    /// </summary>
    public int flag = 0;
    public int rotCount = 0;
    public GameObject loadedObj;
    public Button closeButton;
    public ImageTargetBehaviour ImageTargetTemplate;
    // private ImageTargetBuilder.FrameQuality mFrameQuality = ImageTargetBuilder.FrameQuality.FRAME_QUALITY_NONE;

    public int LastTargetIndex
    {
        get { return (m_TargetCounter - 1) % MAX_TARGETS; }
    }
    #endregion PUBLIC_MEMBERS


    public static string Reverse( string s )
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse( charArray );
        return new string( charArray );
    }

    #region PRIVATE_MEMBERS
    const int MAX_TARGETS = 5;
    UserDefinedTargetBuildingBehaviour m_TargetBuildingBehaviour;
    ObjectTracker m_ObjectTracker;
    // public FrameQualityMeter m_FrameQualityMeter;

    // DataSet that newly defined targets are added to
    DataSet m_UDT_DataSet;

    // Currently observed frame quality
    ImageTargetBuilder.FrameQuality m_FrameQuality = ImageTargetBuilder.FrameQuality.FRAME_QUALITY_HIGH;

    // Counter used to name newly created targets
    int m_TargetCounter;
    #endregion //PRIVATE_MEMBERS


    #region MONOBEHAVIOUR_METHODS
    void Start()
    {
        m_TargetBuildingBehaviour = GetComponent<UserDefinedTargetBuildingBehaviour>();

        if (m_TargetBuildingBehaviour)
        {
            m_TargetBuildingBehaviour.RegisterEventHandler(this);
            Debug.Log("Registering User Defined Target event handler.");
        }

        // m_FrameQualityMeter = FindObjectOfType<FrameQualityMeter>();
        // Debug.Log("QUALMET " + m_FrameQualityMeter);
    }
    #endregion //MONOBEHAVIOUR_METHODS


    #region IUserDefinedTargetEventHandler Implementation
    /// <summary>
    /// Called when UserDefinedTargetBuildingBehaviour has been initialized successfully
    /// </summary>
    public void OnInitialized()
    {
        m_ObjectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        if (m_ObjectTracker != null)
        {
            // Create a new dataset
            m_UDT_DataSet = m_ObjectTracker.CreateDataSet();
            m_ObjectTracker.ActivateDataSet(m_UDT_DataSet);
        }
    }

    /// <summary>
    /// Updates the current frame quality
    /// </summary>
    public void OnFrameQualityChanged(ImageTargetBuilder.FrameQuality frameQuality)
    {
           
           m_FrameQuality = frameQuality;
    //     Debug.Log("Frame quality changed: " + frameQuality.ToString());
    //     m_FrameQuality = frameQuality;
        if (m_FrameQuality == ImageTargetBuilder.FrameQuality.FRAME_QUALITY_LOW)
        {
            Debug.Log("Low camera image quality");
        }
        // m_FrameQualityMeter = FindObjectOfType<FrameQualityMeter>();
        // Debug.Log("OnFrameQualityChanged: "+m_FrameQualityMeter);
        // Debug.Log("OnFrameQualityChanged: "+frameQuality);
        // m_FrameQualityMeter.SetQuality(frameQuality, true);
    }

    /// <summary>
    /// Takes a new trackable source and adds it to the dataset
    /// This gets called automatically as soon as you 'BuildNewTarget with UserDefinedTargetBuildingBehaviour
    /// </summary>
    public void OnNewTrackableSource(TrackableSource trackableSource)
    {
        m_TargetCounter++;

        // Deactivates the dataset first
        m_ObjectTracker.DeactivateDataSet(m_UDT_DataSet);

        // Destroy the oldest target if the dataset is full or the dataset
        // already contains five user-defined targets.
        if (m_UDT_DataSet.HasReachedTrackableLimit() || m_UDT_DataSet.GetTrackables().Count() >= MAX_TARGETS)
        {
            IEnumerable<Trackable> trackables = m_UDT_DataSet.GetTrackables();
            Trackable oldest = null;
            foreach (Trackable trackable in trackables)
            {
                if (oldest == null || trackable.ID < oldest.ID)
                    oldest = trackable;
            }

            if (oldest != null)
            {
                Debug.Log("Destroying oldest trackable in UDT dataset: " + oldest.Name);
                m_UDT_DataSet.Destroy(oldest, true);
            }
        }

        // Get predefined trackable and instantiate it
        ImageTargetBehaviour imageTargetCopy = Instantiate(ImageTargetTemplate);
        imageTargetCopy.gameObject.name = "UserDefinedTarget-" + m_TargetCounter;

        // Add the duplicated trackable to the data set and activate it
        m_UDT_DataSet.CreateTrackable(trackableSource, imageTargetCopy.gameObject);

        // Activate the dataset again
        m_ObjectTracker.ActivateDataSet(m_UDT_DataSet);

        // Make sure TargetBuildingBehaviour keeps scanning...
        m_TargetBuildingBehaviour.StartScanning();
    }
    #endregion IUserDefinedTargetEventHandler implementation


    #region PUBLIC_METHODS
    /// <summary>
    /// Instantiates a new user-defined target and is also responsible for dispatching callback to
    /// IUserDefinedTargetEventHandler::OnNewTrackableSource
    /// </summary>
    public void BuildNewTarget()
    {
        Debug.Log(m_FrameQuality);
        if (m_FrameQuality == ImageTargetBuilder.FrameQuality.FRAME_QUALITY_MEDIUM ||
            m_FrameQuality == ImageTargetBuilder.FrameQuality.FRAME_QUALITY_HIGH)
        {
            // create the name of the next target.
            // the TrackableName of the original, linked ImageTargetBehaviour is extended with a continuous number to ensure unique names
            string targetName = string.Format("{0}-{1}", ImageTargetTemplate.TrackableName, m_TargetCounter);
            // generate a new target:
            Vector3 trans = new Vector3(2000.0f, 2000.0f, 2000.0f);
            loadedObj = GameObject.Find("object");              //zip stronger than object -> contains mtl
            // closeButton = GameObject.Find("Close").GetComponent<Button>();
            m_TargetBuildingBehaviour.BuildNewTarget(targetName, ImageTargetTemplate.GetSize().x);
            // closeButton.gameObject.SetActive(true);
            int flag = 0;
            if(File.Exists("object.obj")){
                flag = 1;
                loadedObj = null;
                loadedObj = GameObject.Find("object");

                Destroy(loadedObj);

                string filePath = Directory.GetFiles("./Assets", "*.obj")[0];
                loadedObj = new OBJLoader().Load(filePath);

                loadedObj.transform.Rotate(0.0f, -180.0f, 360.0f, Space.World);
            }
            else if(Directory.Exists("./Assets/object")) {
            // if(Directory.Exists("./Assets/object")) {
                loadedObj = null;
                flag = 1;
                Debug.Log("I have a file available.");
                string filePath = Directory.GetFiles("./Assets/object", "*.obj")[0];
                filePath = filePath.Replace(@"\", "/");
                string mtlPath = Directory.GetFiles("./Assets/object", "*.mtl")[0];
                mtlPath = mtlPath.Replace(@"\", "/");
                loadedObj = GameObject.Find("object");
                Destroy(loadedObj);
                loadedObj = new OBJLoader().Load(filePath, mtlPath);
                loadedObj.transform.Rotate(0.0f, -180.0f, 360.0f, Space.World);

            }

            Vector3 pos = new Vector3(0.0f,-1254.0f,-840.0f);
            loadedObj.transform.position = pos;

            Debug.Log(loadedObj.transform.localScale);

            loadedObj.transform.Rotate(180.0f, 0.0f, 360.0f, Space.World);
            Vector3 scaleChange = new Vector3(10.0f, 10.0f, 10.0f);
            loadedObj.transform.localScale = scaleChange;
            if(flag == 0){
                loadedObj.transform.localScale += 4 * scaleChange;
                // if(rotCount == 0 ){
                //     loadedObj.transform.Rotate(-90.0f, 90.0f, 0.0f, Space.World);
                //     rotCount++;
                // }
            }

        }
        else
        {
            Debug.Log("Cannot build new target, due to poor camera image quality");
            StatusMessage.Instance.Display("Low camera image quality", true);
        }

    }

    #endregion //PUBLIC_METHODS
}