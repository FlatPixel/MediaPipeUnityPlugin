using Mediapipe;
using System;
using Unity.Collections;
using UnityEngine;

public class TextureFrame {
  private readonly Texture2D texture;
  private IntPtr nativeTexturePtr = IntPtr.Zero;

  public int width { get { return texture.width; } }
  public int height { get { return texture.height; } }

  public readonly GlTextureBuffer.DeletionCallback OnRelease;

  public TextureFrame(int width, int height, TextureFormat format, GlTextureBuffer.DeletionCallback OnRelease) {
    texture = new Texture2D(width, height, format, false);
    this.OnRelease = OnRelease;
  }

  public TextureFrame(int width, int height, GlTextureBuffer.DeletionCallback OnRelease) :
      this(width, height, TextureFormat.RGBA32, OnRelease) {}

  public void CopyTexture(Texture dst) {
    Graphics.CopyTexture(texture, dst);
  }

  public void CopyTextureFrom(WebCamTexture src) {
    Graphics.CopyTexture(src, texture);
    nativeTexturePtr = IntPtr.Zero;
  }

  public Color32[] GetPixels32() {
    return texture.GetPixels32();
  }

  public NativeArray<T> GetRawTextureData<T>() where T : struct {
    return texture.GetRawTextureData<T>();
  }

  public IntPtr GetNativeTexturePtr() {
    if (nativeTexturePtr == IntPtr.Zero) {
      nativeTexturePtr = texture.GetNativeTexturePtr();
    }
    return nativeTexturePtr;
  }

  public GpuBufferFormat gpuBufferformat {
    get {
      return GpuBufferFormat.kBGRA32;
    }
  }

  public void Release() {
    OnRelease((UInt64)GetNativeTexturePtr(), IntPtr.Zero);
  }
}
