using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Mediapipe.Unity.FaceMesh {
  public class FaceMeshGraph : GraphRunner {
    public int maxNumFaces = 1;
    public UnityEvent<List<Detection>> OnFacesDetected = new UnityEvent<List<Detection>>();
    public UnityEvent<List<NormalizedRect>> OnFaceRectsDetected = new UnityEvent<List<NormalizedRect>>();
    public UnityEvent<List<NormalizedLandmarkList>> OnFaceLandmarksDetected = new UnityEvent<List<NormalizedLandmarkList>>();

    const string inputStreamName = "input_video";

    const string faceDetectionsStreamName = "face_detections";
    OutputStreamPoller<List<Detection>> faceDetectionsStreamPoller;
    DetectionVectorPacket faceDetectionsPacket;

    const string multiFaceLandmarksStreamName = "multi_face_landmarks";
    OutputStreamPoller<List<NormalizedLandmarkList>> multiFaceLandmarksStreamPoller;
    NormalizedLandmarkListVectorPacket multiFaceLandmarksPacket;

    const string faceRectsFromLandmarksStreamName = "face_rects_from_landmarks";
    OutputStreamPoller<List<NormalizedRect>> faceRectsFromLandmarksStreamPoller;
    NormalizedRectVectorPacket faceRectsFromLandmarksPacket;

    const string faceDetectionsPresenceStreamName = "face_detections_presence";
    OutputStreamPoller<bool> faceDetectionsPresenceStreamPoller;
    BoolPacket faceDetectionsPresencePacket;

    const string multiFaceLandmarksPresenceStreamName = "multi_face_landmarks_presence";
    OutputStreamPoller<bool> multiFacelandmarksPresenceStreamPoller;
    BoolPacket multiFaceLandmarksPresencePacket;

    public override Status StartRun(ImageSource imageSource) {
      faceDetectionsStreamPoller = calculatorGraph.AddOutputStreamPoller<List<Detection>>(faceDetectionsStreamName).Value();
      faceDetectionsPacket = new DetectionVectorPacket();

      multiFaceLandmarksStreamPoller = calculatorGraph.AddOutputStreamPoller<List<NormalizedLandmarkList>>(multiFaceLandmarksStreamName).Value();
      multiFaceLandmarksPacket = new NormalizedLandmarkListVectorPacket();

      faceRectsFromLandmarksStreamPoller = calculatorGraph.AddOutputStreamPoller<List<NormalizedRect>>(faceRectsFromLandmarksStreamName).Value();
      faceRectsFromLandmarksPacket = new NormalizedRectVectorPacket();

      faceDetectionsPresenceStreamPoller = calculatorGraph.AddOutputStreamPoller<bool>(faceDetectionsPresenceStreamName).Value();
      faceDetectionsPresencePacket = new BoolPacket();

      multiFacelandmarksPresenceStreamPoller = calculatorGraph.AddOutputStreamPoller<bool>(multiFaceLandmarksPresenceStreamName).Value();
      multiFaceLandmarksPresencePacket = new BoolPacket();

      return calculatorGraph.StartRun(BuildSidePacket(imageSource));
    }

    public Status StartRunAsync(ImageSource imageSource) {
      calculatorGraph.ObserveOutputStream(faceDetectionsStreamName, FaceDetectionsCallback, true).AssertOk();
      calculatorGraph.ObserveOutputStream(multiFaceLandmarksStreamName, FaceLandmarksCallback, true).AssertOk();
      calculatorGraph.ObserveOutputStream(faceRectsFromLandmarksStreamName, FaceRectsCallback, true).AssertOk();

      return calculatorGraph.StartRun(BuildSidePacket(imageSource));
    }

    public Status AddTextureFrameToInputStream(TextureFrame textureFrame) {
      return AddTextureFrameToInputStream(inputStreamName, textureFrame);
    }

    public FaceMeshValue FetchNextValue() {
      var isFaceDetectionsPresent = FetchNext(faceDetectionsPresenceStreamPoller, faceDetectionsPresencePacket, faceDetectionsPresenceStreamName);
      var faceDetections = isFaceDetectionsPresent ? FetchNextVector<Detection>(faceDetectionsStreamPoller, faceDetectionsPacket, faceDetectionsStreamName) : new List<Detection>();

      if (isFaceDetectionsPresent) {
        OnFacesDetected.Invoke(faceDetections);
      }

      var isMultiFaceLandmarksPresent = FetchNext(multiFacelandmarksPresenceStreamPoller, multiFaceLandmarksPresencePacket, multiFaceLandmarksPresenceStreamName);
      if (!isMultiFaceLandmarksPresent) {
        return new FaceMeshValue(faceDetections);
      }

      var multiFaceLandmarks = FetchNextVector<NormalizedLandmarkList>(multiFaceLandmarksStreamPoller, multiFaceLandmarksPacket, multiFaceLandmarksStreamName);
      var faceRectsFromLandmarks = FetchNextVector<NormalizedRect>(faceRectsFromLandmarksStreamPoller, faceRectsFromLandmarksPacket, faceRectsFromLandmarksStreamName);

      OnFaceLandmarksDetected.Invoke(multiFaceLandmarks);
      OnFaceRectsDetected.Invoke(faceRectsFromLandmarks);

      return new FaceMeshValue(faceDetections, multiFaceLandmarks, faceRectsFromLandmarks);
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    static IntPtr FaceDetectionsCallback(IntPtr graphPtr, IntPtr packetPtr){
      try {
        var isFound = TryGetGraphRunner(graphPtr, out var graphRunner);
        if (!isFound) {
          return Status.FailedPrecondition("Graph runner is not found").mpPtr;
        }
        using (var packet = new DetectionVectorPacket(packetPtr, false)) {
          var value = packet.IsEmpty() ? null : packet.Get();
          (graphRunner as FaceMeshGraph).OnFacesDetected.Invoke(value);
        }
        return Status.Ok().mpPtr;
      } catch (Exception e) {
        return Status.FailedPrecondition(e.ToString()).mpPtr;
      }
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    static IntPtr FaceLandmarksCallback(IntPtr graphPtr, IntPtr packetPtr){
      try {
        var isFound = TryGetGraphRunner(graphPtr, out var graphRunner);
        if (!isFound) {
          return Status.FailedPrecondition("Graph runner is not found").mpPtr;
        }
        using (var packet = new NormalizedLandmarkListVectorPacket(packetPtr, false)) {
          var value = packet.IsEmpty() ? null : packet.Get();
          (graphRunner as FaceMeshGraph).OnFaceLandmarksDetected.Invoke(value);
        }
        return Status.Ok().mpPtr;
      } catch (Exception e) {
        return Status.FailedPrecondition(e.ToString()).mpPtr;
      }
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    static IntPtr FaceRectsCallback(IntPtr graphPtr, IntPtr packetPtr){
      try {
        var isFound = TryGetGraphRunner(graphPtr, out var graphRunner);
        if (!isFound) {
          return Status.FailedPrecondition("Graph runner is not found").mpPtr;
        }
        using (var packet = new NormalizedRectVectorPacket(packetPtr, false)) {
          var value = packet.IsEmpty() ? null : packet.Get();
          (graphRunner as FaceMeshGraph).OnFaceRectsDetected.Invoke(value);
        }
        return Status.Ok().mpPtr;
      } catch (Exception e) {
        return Status.FailedPrecondition(e.ToString()).mpPtr;
      }
    }

    protected override void PrepareDependentAssets() {
      AssetLoader.PrepareAsset("face_detection_short_range.bytes");
      AssetLoader.PrepareAsset("face_landmark.bytes");
    }

    SidePacket BuildSidePacket(ImageSource imageSource) {
      var sidePacket = new SidePacket();
      sidePacket.Emplace("num_faces", new IntPacket(maxNumFaces));

      // Coordinate transformation from Unity to MediaPipe
      if (imageSource.isMirrored) {
        sidePacket.Emplace("input_rotation", new IntPacket(180));
        sidePacket.Emplace("input_vertically_flipped", new BoolPacket(false));
      } else {
        sidePacket.Emplace("input_rotation", new IntPacket(0));
        sidePacket.Emplace("input_vertically_flipped", new BoolPacket(true));
      }

      return sidePacket;
    }
  }
}
