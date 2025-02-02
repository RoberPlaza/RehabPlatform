﻿using UnityEngine;
using System.Collections.Generic;

public enum CustomGestureType
{
    HandsUp
};

public class PoseManager : MonoBehaviour
{
    [SerializeField]
    private string[] modelPaths;

    [SerializeField]
    private JointMapping riggedAvatar;

    [SerializeField]
    private double modelError;

    private Quaternion[,] modelBuffer = new Quaternion[PoseModel.numberOfJoints, PoseModel.numberOfFrames];

    private nuitrack.GestureType targetNuitrackGesture = nuitrack.GestureType.GestureWaving;

    private CustomGestureType targetCustomGesture;

    private bool isNextGestureTrackedByNuitrack = true;

    private bool isCollectingData = false;

    private bool isMoreThanOneUser = false;

    private int frameIndex = 0;

    private float timestamp;

    private PoseModel[] gestureModels;

    public static Dictionary<nuitrack.GestureType, string> possibleNuitrackGestures = new Dictionary<nuitrack.GestureType, string>
    {
        { nuitrack.GestureType.GestureWaving, "Waving"},
        { nuitrack.GestureType.GestureSwipeLeft, "SwipeLeft" },
        { nuitrack.GestureType.GestureSwipeRight, "SwipeRight" }
    };

    public static Dictionary<CustomGestureType, string> possibleCustomGestures = new Dictionary<CustomGestureType, string>
    {
        { CustomGestureType.HandsUp, "HandsUp" }
    };

    public static Dictionary<CustomGestureType, PoseModel> gestureModel = new Dictionary<CustomGestureType, PoseModel>();

    public delegate void CorrectGesture();
    public static event CorrectGesture OnNewGoal;

    void Start()
    {
        Application.targetFrameRate = 30;
        Screen.SetResolution(1280, 720, true);

        NuitrackManager.GestureRecognizer.OnNewGesturesEvent += AttendGestures;

        gestureModels = new PoseModel[1];

        BetterStreamingAssets.Initialize();

        gestureModels[0] = new PoseModel();
        gestureModels[0].LoadModel(modelPaths[0]);

        gestureModel.Add(CustomGestureType.HandsUp,  gestureModels[0]);
    }

    // Update is called once per frame
    void Update()
    {

        if (isCollectingData)
        {
            Quaternion[] frameRotations = riggedAvatar.GetRotations();

            for (int jointIndex = 0; jointIndex < frameRotations.Length; jointIndex++)
            {
                modelBuffer[jointIndex, frameIndex] = frameRotations[jointIndex];
            }

            frameIndex++;
        }

        if (frameIndex >= PoseModel.numberOfFrames)
        {
            AfterDataCollection();
        }
    }

    private void OnStartDataCollection()
    {
        timestamp           =   Time.time;
        isCollectingData    =   true;
        frameIndex          =   0;
    }
    
    private void AfterDataCollection()
    {
        isNextGestureTrackedByNuitrack  =   true;
        isCollectingData                =   false;
        timestamp                       =   Time.time;
        frameIndex                      =   0;

        if (gestureModel[targetCustomGesture].Compare(new PoseModel(modelBuffer)) <= modelError)
        {
            OnNewGoal();
        }

        SelectNewGesture();
    }

    private void AttendGestures(nuitrack.GestureData gestures)
    {
        if (isNextGestureTrackedByNuitrack && !isMoreThanOneUser)
        {
            foreach (nuitrack.Gesture gesture in gestures.Gestures)
            {
                if (gesture.Type == targetNuitrackGesture)
                {
                    OnNewGoal();
                    SelectNewGesture();
                    break;
                }
            }
        }
    }

    public bool IsNextGestureNuitrack()
    {
        return isNextGestureTrackedByNuitrack;
    }

    public string GetNuitrackGestureStr()
    {
        return possibleNuitrackGestures[targetNuitrackGesture];
    }

    public string GetCustomGestureStr()
    {
        return possibleCustomGestures[targetCustomGesture];
    }

    public float GetTimestamp()
    {
        return timestamp;
    }

    private void SelectNewGesture()
    {
        isNextGestureTrackedByNuitrack = (Random.Range(1, 10) >= 2);

        if (isNextGestureTrackedByNuitrack)
        {
            List<nuitrack.GestureType> gestures = new List<nuitrack.GestureType>();
            foreach(var gesture in possibleNuitrackGestures)
            {
                gestures.Add(gesture.Key);
            }
            targetNuitrackGesture = gestures[Random.Range(0, gestures.Count)];
        }
        else
        {
            List<CustomGestureType> gestures = new List<CustomGestureType>();
            foreach(var gesture in possibleCustomGestures)
            {
                gestures.Add(gesture.Key);
            }
            targetCustomGesture = gestures[Random.Range(0, gestures.Count)];
            timestamp = Time.time;
            Invoke("OnStartDataCollection", 3f);
        }
    }

    public bool IsCollectingData()
    {
        return isCollectingData;
    }

    public int GetFrameIndex()
    {
        return frameIndex;
    }

}
