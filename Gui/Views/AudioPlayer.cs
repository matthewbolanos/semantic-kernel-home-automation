using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;

public static class AudioPlayer
{
    public static void PlaySound(System.BinaryData binaryData)
    {
        // Initialize the OpenAL audio context
        var device = ALC.OpenDevice(null); // null for the default device
        var context = ALC.CreateContext(device, (int[])null);
        ALC.MakeContextCurrent(context);

        // Generate a buffer and source
        int buffer = AL.GenBuffer();
        int source = AL.GenSource();

        byte[] soundData = binaryData.ToArray();

        ALFormat format = ALFormat.Mono16;
        int sampleRate = 22050;

        // Pin the soundData array in memory
        GCHandle handle = GCHandle.Alloc(soundData, GCHandleType.Pinned);
        try
        {
            IntPtr pointer = handle.AddrOfPinnedObject();
            AL.BufferData(buffer, format, pointer, soundData.Length, sampleRate);
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free(); // Make sure to free the handle to avoid memory leaks
            }
        }

        // Bind the buffer with the source and play it
        AL.Source(source, ALSourcei.Buffer, buffer);
        AL.SourcePlay(source);

        // Simple playback loop
        AL.GetSource(source, ALGetSourcei.SourceState, out int state);

        // Loop until the sound has finished playing
        while (state == (int)ALSourceState.Playing)
        {
            System.Threading.Thread.Sleep(100); // Sleep to prevent a tight loop, adjust as needed
            AL.GetSource(source, ALGetSourcei.SourceState, out state);
        }

        // Cleanup
        AL.DeleteSource(source);
        AL.DeleteBuffer(buffer);
        ALC.DestroyContext(context);
        ALC.CloseDevice(device);
    }
}
