using UnityEngine;
using UnityEngine.UI;

public class TSDFSliceViewer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage sliceImage;
    [SerializeField] private ComputeShader sliceShader;
    [SerializeField] private RenderTexture tsdfVolume; // Assign your TSDF_Volume asset

    [Header("Settings")]
    [SerializeField, Range(0, 255)]
    private int sliceIndex = 128;

    [SerializeField, Range(0, 2)]
    private int axis = 1; // 0=X, 1=Y, 2=Z

    private RenderTexture _sliceTexture;

    // <<< FIX: Declare the 'kernel' variable here for the whole class to see.
    private int _kernel;

    void Start()
    {
        // Now we are just ASSIGNING a value to the class's _kernel variable,
        // not creating a new one that only exists in Start().
        _kernel = sliceShader.FindKernel("SliceKernel");

        _sliceTexture = new RenderTexture(tsdfVolume.width, tsdfVolume.height, 0, RenderTextureFormat.RHalf);
        _sliceTexture.enableRandomWrite = true;
        _sliceTexture.Create();

        sliceImage.texture = _sliceTexture;
    }

    void Update()
    {
        if (tsdfVolume == null || sliceShader == null) return;

        // For easy testing in the editor
        //if (Input.GetKeyDown(KeyCode.PageUp)) sliceIndex++;
        //if (Input.GetKeyDown(KeyCode.PageDown)) sliceIndex--;
        //sliceIndex = Mathf.Clamp(sliceIndex, 0, tsdfVolume.volumeDepth - 1);

        // Now the Update method can access the same _kernel variable that Start() used.
        sliceShader.SetTexture(_kernel, "_TSDFVolume", tsdfVolume);
        sliceShader.SetTexture(_kernel, "_SliceTexture", _sliceTexture);
        sliceShader.SetInt("_SliceIndex", sliceIndex);
        sliceShader.SetInt("_Axis", axis);
        sliceShader.Dispatch(_kernel, tsdfVolume.width / 8, tsdfVolume.height / 8, 1);
    }
}