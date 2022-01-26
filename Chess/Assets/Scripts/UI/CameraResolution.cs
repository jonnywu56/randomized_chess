using UnityEngine;
using System.Collections.Generic;
 
// Forces camera aspect to be 16:9
// Thanks to quarag at https://forum.unity.com/threads/force-camera-aspect-ratio-16-9-in-viewport.385541/ for script
public class CameraResolution : MonoBehaviour
{
 
    // Variables for screen size
    private int screenSizeX = 0;
    private int screenSizeY = 0;
 
    // Forces camera to desired aspect
    private void RescaleCamera()
    {
        // If screen is already correct size, returns
        if (Screen.width == screenSizeX && Screen.height == screenSizeY) return;

        // Calculates necessary ratios
        float targetaspect = 16.0f / 9.0f;
        float windowaspect = (float) Screen.width / (float) Screen.height;
        float scaleheight = windowaspect / targetaspect;
        Camera camera = GetComponent<Camera>();
 
        // Adds boxes on top/bottom and rescales camera
        if (scaleheight < 1.0f)
        {
            Rect rect = camera.rect;
 
            rect.width = 1.0f;
            rect.height = scaleheight;
            rect.x = 0;
            rect.y = (1.0f - scaleheight) / 2.0f;
 
            camera.rect = rect;
        }
        // Adds boxes on left/right and rescales camera
        else 
        {
            float scalewidth = 1.0f / scaleheight;
 
            Rect rect = camera.rect;
 
            rect.width = scalewidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scalewidth) / 2.0f;
            rect.y = 0;
 
            camera.rect = rect;
        }
 
        // Update screen size variables
        screenSizeX = Screen.width;
        screenSizeY = Screen.height;
    }
 
    // Called before camera culls scene, sets background to black
    void OnPreCull()
    {
        // Creates variables for camera and background
        if (Application.isEditor) return;
        Rect wp = Camera.main.rect;
        Rect nr = new Rect(0, 0, 1, 1);
 
        // Sets background to black
        Camera.main.rect = nr;
        GL.Clear(true, true, Color.black);
       
        // Restores camera rect
        Camera.main.rect = wp;
 
    }
 
    // Calls RescaleCamera when camera is instantiated
    void Start () {
        RescaleCamera();
    }
   
    // Calls rescaleCamera once per frame
    void Update () {
        RescaleCamera();
    }
  
}