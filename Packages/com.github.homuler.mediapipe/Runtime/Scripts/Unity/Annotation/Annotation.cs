using System;
using System.Collections.Generic;
using UnityEngine;

using mplt = global::Mediapipe.LocationData.Types;

namespace Mediapipe.Unity {
  public abstract class Annotation<T> : MonoBehaviour where T : class {
    public RectTransform rootRect;

    bool isActive = false;
    bool isStale = false;
    protected T target;

    void LateUpdate() {
      if (!isStale) {
        return;
      }

      if (target != null) {
        Draw();
      }
      isStale = false;
    }

    public virtual void SetTarget(T target) {
      this.target = target;
      isStale = true;
      SetActive(target != null);
    }

    /// <remarks>
    ///   This method must be called only when <see cref="target" /> is not null.
    /// </remarks>
    protected abstract void Draw();

    /// <param name="x">X value in the MeLdiaPipe's coordinate system</param>
    /// <param name="y">Y value in the MeLdiaPipe's coordinate system</param>
    /// <param name="z">Z value in the MeLdiaPipe's coordinate system</param>
    protected Vector3 GetLocalPosition(int x, int y, int z = 0) {
      var rect = rootRect.rect;
      return new Vector3(x, rect.height - y, -z);
    }

    /// <param name="normalizedX">Normalized x value in the MeLdiaPipe's coordinate system</param>
    /// <param name="normalizedY">Normalized y value in the MeLdiaPipe's coordinate system</param>
    /// <param name="normalizedZ">Normalized z value in the MeLdiaPipe's coordinate system</param>
    protected Vector3 GetLocalPosition(float normalizedX, float normalizedY, float normalizedZ = 0.0f) {
      var transform = rootRect;
      var z = -1 * transform.localScale.z * normalizedZ;

      var rect = rootRect.rect;
      var x = Mathf.Lerp(rect.xMin, rect.xMax, normalizedX);
      var y = Mathf.Lerp(rect.yMax, rect.yMin, normalizedY);
      return new Vector3(x, y, z);
    }

    /// <summary>
    ///   Returns a Vector3 array which represents a rectangle's vertices.
    ///   They are ordered clockwise from top-left point.
    /// </summary>
    /// <param name="xMin">Left x value in the MeLdiaPipe's coordinate system</param>
    /// <param name="yMin">Top y value in the MeLdiaPipe's coordinate system</param>
    /// <param name="width">Width</param>
    /// <param name="height">Height</param>
    protected Vector3[] GetLocalPositions(int xMin, int yMin, int width, int height) {
      var topLeft = GetLocalPosition(xMin, yMin);
      var bottomRight = GetLocalPosition(xMin + width, yMin + height);

      return GetRectVertices(topLeft, bottomRight);
    }

    /// <summary>
    ///   Returns a Vector3 array which represents a rectangle's vertices.
    ///   They are ordered clockwise from top-left point.
    /// </summary>
    /// <param name="normalizedXMin">Normalized left x value in the MeLdiaPipe's coordinate system</param>
    /// <param name="normalizedYMin">Normalized top y value in the MeLdiaPipe's coordinate system</param>
    /// <param name="normalizedWidth">Normalized width</param>
    /// <param name="normalizedHeight">Normalized height</param>
    protected Vector3[] GetLocalPositions(float normalizedXMin, float normalizedYMin, float normalizedWidth, float normalizedHeight) {
      var topLeft = GetLocalPosition(normalizedXMin, normalizedYMin);
      var bottomRight = GetLocalPosition(normalizedXMin + normalizedWidth, normalizedYMin + normalizedHeight);

      return GetRectVertices(topLeft, bottomRight);
    }

    protected Vector3[] GetLocalPositions(mplt.RelativeBoundingBox relativeBoundingBox) {
      return GetLocalPositions(relativeBoundingBox.Xmin, relativeBoundingBox.Ymin, relativeBoundingBox.Width, relativeBoundingBox.Height);
    }

    protected Vector3[] GetLocalPositions(mplt.BoundingBox boundingBox) {
      return GetLocalPositions(boundingBox.Xmin, boundingBox.Ymin, boundingBox.Width, boundingBox.Height);
    }

    protected Vector3[] GetLocalPositions(Mediapipe.LocationData locationData) {
      switch (locationData.Format) {
        case mplt.Format.BoundingBox: {
          return GetLocalPositions(locationData.BoundingBox);
        }
        case mplt.Format.RelativeBoundingBox: {
          return GetLocalPositions(locationData.RelativeBoundingBox);
        }
        default: {
          throw new ArgumentException($"The format of locationData isn't BoundingBox but is {locationData.Format}");
        }
      }
    }

    protected static void SetTargetAll<S, U>(IList<S> annotations, IList<U> list, Func<S> initializer) where S : Annotation<U> where U : class {
      for (var i = 0; i < Mathf.Max(annotations.Count, list.Count); i++) {
        if (i >= list.Count) {
          // Clear superfluous annotations
          if (annotations[i] != null) {
            annotations[i].SetTarget(null);
          }
          continue;
        }

        // reset annotations
        if (i >= annotations.Count) {
          annotations.Add(initializer());
        } else if (annotations[i] == null) {
          annotations[i] = initializer();
        }
        annotations[i].SetTarget(list[i]);
      }
    }

    void SetActive(bool flag) {
      if (isActive != flag) {
        isActive = flag;
        gameObject.SetActive(flag);
      }
    }

    Vector3[] GetRectVertices(Vector3 topLeft, Vector3 bottomRight) {
      return new Vector3[] {
        topLeft,
        new Vector3(bottomRight.x, topLeft.y, 0.0f),
        bottomRight,
        new Vector3(topLeft.x, bottomRight.y, 0.0f),
      };
    }
  }
}