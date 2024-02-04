using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;

/*

 RayLib CS audio callbacks have no way to track an instance (for multiple games for instance). 
 This class wraps the audio system and along with a trampoline dll, allows multiple instances to be tracked.

*/

internal class RayLibAudioHelper
{
    struct AudioShared
    {
        public nint audioBuffer;
        public int audioReadPos;
        public int audioWritePos;
        public bool audioEnabledWrite;
        public bool audioEnabledRead;
    }

    private unsafe delegate* unmanaged[Cdecl]<void*, void*, uint, void> audioCallback;
    private unsafe delegate* unmanaged[Cdecl]<void*, uint, void> audioCallbackTrampoline;
    private nint trampoline;
    private AudioStream audio;
    const int AudioBufferSize = 1024 * 1024;
    const int AudioBufferWritePosBeforeRead = 16 * 1024;

    private nint audioSharedData;

    public RayLibAudioHelper()
    {
        audioSharedData = Marshal.AllocHGlobal(Marshal.SizeOf<AudioShared>());
        unsafe
        {
            var audioShared = (AudioShared*)audioSharedData;
            audioShared->audioBuffer = (nint)Marshal.AllocHGlobal(AudioBufferSize); // TODO audio buffer size should be related to stream size probably
            audioShared->audioReadPos = 0;
            audioShared->audioWritePos = 0;
            audioShared->audioEnabledWrite = false;
            audioShared->audioEnabledRead = false;
        }
        audio = new AudioStream();
        var initialise = InterfaceTrampoline.GetInitialise();

        unsafe
        {
            audioCallback = &RayLibAudioCallback;
            trampoline = initialise(audioSharedData, 2, (nint)audioCallback);
            audioCallbackTrampoline = (delegate* unmanaged[Cdecl]<void*,uint, void>)trampoline;
        }

    }

    public void SwitchAudio(uint sampleRate, bool enable)
    {
        unsafe
        {
            var audioShared = (AudioShared*)audioSharedData;
            if (enable)
            {
                if (audioShared->audioEnabledWrite)
                {
                    return;
                }
                // If already enabled, do nothing?
                audio = Raylib.LoadAudioStream(sampleRate, 16, 2);
                unsafe
                {
                    Raylib.SetAudioStreamBufferSizeDefault((int)sampleRate * 2);
                    Raylib.SetAudioStreamCallback(audio, audioCallbackTrampoline);
                }
                audioShared->audioEnabledWrite = true;
                Raylib.PlayAudioStream(audio);
            }
            else
            {
                if (audioShared->audioEnabledWrite)
                {
                    Raylib.StopAudioStream(audio);
                    Raylib.UnloadAudioStream(audio);
                    audioShared->audioEnabledWrite = false;
                    audioShared->audioEnabledRead = false;
                    audioShared->audioReadPos = 0;
                    audioShared->audioWritePos = 0;
                }

            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    private unsafe static void RayLibAudioCallback(void* instance, void* ptr, uint size)
    {
        var audioShared = (AudioShared*)instance;
        if (audioShared->audioEnabledRead)
        {
            size*=4;
            if (audioShared->audioReadPos + size > AudioBufferSize)
            {
                var toCopy = AudioBufferSize - audioShared->audioReadPos;
                var remain = size - toCopy;

                var spanDest = new Span<byte>((byte*)ptr, (int)toCopy);
                var spanSrc = new Span<byte>((byte*)audioShared->audioBuffer + audioShared->audioReadPos, (int)toCopy);
                spanSrc.CopyTo(spanDest);
                audioShared->audioReadPos = 0;
                spanDest = new Span<byte>((byte*)ptr + (int)toCopy, (int)remain);
                spanSrc = new Span<byte>((byte*)audioShared->audioBuffer + audioShared->audioReadPos, (int)remain);
                spanSrc.CopyTo(spanDest);
                audioShared->audioReadPos += (int)remain;
            }
            else
            {
                var spanDest = new Span<byte>((byte*)ptr, (int)size);
                var spanSrc = new Span<byte>((byte*)audioShared->audioBuffer + audioShared->audioReadPos, (int)size);
                spanSrc.CopyTo(spanDest);
                audioShared->audioReadPos += (int)size;
            }
            IntPtr ptrP = new IntPtr(ptr);
        }
    }


    // * One frame is defined as a sample of left and right channels, interleaved.
    // * I.e. int16_t buf[4] = { l, r, l, r }; would be 2 frames.
    public void AudioSampleIn(IntPtr data, UIntPtr frames)
    {
        unsafe
        {
            var audioShared = (AudioShared*)audioSharedData;
            if (audioShared->audioEnabledWrite)
            {
                var bytes = ((int)frames) * 2 * 2;

                if (audioShared->audioWritePos + bytes > AudioBufferSize)
                {
                    var toCopy = AudioBufferSize - audioShared->audioWritePos;
                    var remain = bytes - toCopy;
                    var spanDest = new Span<byte>((byte*)audioShared->audioBuffer + audioShared->audioWritePos, (int)toCopy);
                    var spanSrc = new Span<byte>((byte*)data, (int)toCopy);
                    spanSrc.CopyTo(spanDest);
                    audioShared->audioWritePos = 0;
                    spanDest = new Span<byte>((byte*)audioShared->audioBuffer + audioShared->audioWritePos, (int)remain);
                    spanSrc = new Span<byte>((byte*)data + (int)toCopy, (int)remain);
                    spanSrc.CopyTo(spanDest);
                    audioShared->audioWritePos += (int)remain;
                }
                else
                {
                    var spanDest = new Span<byte>((byte*)audioShared->audioBuffer + audioShared->audioWritePos, (int)bytes);
                    var spanSrc = new Span<byte>((byte*)data, (int)bytes);
                    spanSrc.CopyTo(spanDest);
                    audioShared->audioWritePos += (int)bytes;
                }

                if (audioShared->audioWritePos > AudioBufferWritePosBeforeRead)
                {
                    audioShared->audioEnabledRead = true;
                }
            }
        }
    }

}