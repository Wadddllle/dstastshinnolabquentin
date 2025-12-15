using UnityEngine;
using System.IO;

public class GridReplayer : MonoBehaviour
{
    public string fileName = "session_data.bin";

    // Structure to hold the data for one recorded frame
    public struct ReplayFrame
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
        public byte[] gridData; // The raw compressed grid bytes
    }

    // Structure to hold the header info
    public struct GridHeader
    {
        public int gridWidth;
        public int gridHeight;
        public float cellSize;
    }

    // List to store all frames after reading
    private System.Collections.Generic.List<ReplayFrame> _frames = new System.Collections.Generic.List<ReplayFrame>();
    private GridHeader _header;

    [ContextMenu("Read File")]
    public void ReadFile()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found at: {path}");
            return;
        }

        Debug.Log($"Attempting to read file: {path}");

        _frames.Clear(); // Clear previous data

        try
        {
            using (FileStream fs = File.Open(path, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // --- READ HEADER ---
                ReadHeader(reader);

                // --- READ FRAMES ---
                ReadFrames(reader);
            }
            Debug.Log($"File successfully read! Loaded {_frames.Count} frames.");
        }
        catch (EndOfStreamException)
        {
            Debug.LogWarning("Reached end of stream prematurely. File may be truncated.");
        }
        catch (IOException e)
        {
            Debug.LogError($"An error occurred during file reading: {e.Message}");
        }
    }

    private void ReadHeader(BinaryReader reader)
    {
        // Must read in the exact order and type as written!
        string magic = reader.ReadString(); // Reads length-prefixed string
        if (magic != "GRID")
        {
            Debug.LogError($"Invalid file format! Expected 'GRID' magic string, found '{magic}'.");
            // Throw an exception or stop reading here
            return;
        }

        int version = reader.ReadInt32(); // Version check (you can handle different versions here)
        if (version != 1)
        {
            Debug.LogWarning($"File version is {version}, but expected 1.");
        }

        // Store grid dimensions
        _header.gridWidth = reader.ReadInt32();
        _header.gridHeight = reader.ReadInt32();
        _header.cellSize = reader.ReadSingle(); // ReadSingle is for float

        Debug.Log($"Header: W:{_header.gridWidth}, H:{_header.gridHeight}, Cell:{_header.cellSize}");
    }

    private void ReadFrames(BinaryReader reader)
    {
        // Loop while there is data left in the stream
        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            ReplayFrame frame = new ReplayFrame();

            // 1. Read timestamp (float)
            frame.timestamp = reader.ReadSingle();

            // 2. Read position (3 floats)
            frame.position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            // 3. Read rotation (4 floats)
            frame.rotation = new Quaternion(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            // 4. Read the grid bytes (IMPORTANT: use ReadBytes to handle length prefix)
            // The BinaryWriter.Write(byte[]) method internally writes the length first.
            // BinaryReader.ReadBytes() takes a fixed length, it does *not* read the length prefix.
            // You must use ReadBytes(Int32) to read a specific number of bytes.
            // In your writer script, _writer.Write(gridBytes) is used.
            // **Correction/Assumption based on C# BinaryWriter behavior:**
            // If the original `sourceGrid.GetCompressedGridData()` returns a fixed size array, 
            // you should read that fixed size.
            // If the original array size is variable and the writer *was* intended to write 
            // the length prefix, it would typically use `_writer.Write(gridBytes.Length);` 
            // then `_writer.Write(gridBytes)`. 

            // Assuming `sourceGrid.GetCompressedGridData()` returns a fixed-size byte array
            // based on the grid dimensions. **You must know this fixed size.**
            // Let's calculate the expected size based on dimensions (e.g., if one byte per cell):
            int expectedGridByteSize = _header.gridWidth * _header.gridHeight;

            // If the size is calculated:
            frame.gridData = reader.ReadBytes(expectedGridByteSize);

            // If the original *writer* was using an explicit length:
            // int length = reader.ReadInt32();
            // frame.gridData = reader.ReadBytes(length);
            // Since your writer script doesn't explicitly write the length, the safest
            // assumption is that `GetCompressedGridData()` returns a fixed-size array
            // determined by the grid size, OR the writer uses a method that isn't `Write(byte[])`
            // that includes the length.

            // The line `_writer.Write(gridBytes);` in your writer script is the key.
            // In standard C#, `BinaryWriter.Write(byte[] buffer)` writes the contents of the array **without**
            // a length prefix. It's up to you to know the length. 
            // We'll proceed with the assumption that the `gridBytes` are of a fixed, known size
            // based on `_header.gridWidth` and `_header.gridHeight`.

            if (frame.gridData.Length != expectedGridByteSize)
            {
                Debug.LogError($"Frame data length mismatch! Expected {expectedGridByteSize} bytes, read {frame.gridData.Length}. Stopping read.");
                break;
            }

            _frames.Add(frame);
        }
    }
}