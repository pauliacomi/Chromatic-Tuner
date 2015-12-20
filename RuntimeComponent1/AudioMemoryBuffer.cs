using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Media;
using System.Runtime.InteropServices;


namespace AudioProcessing
{
    // Using the COM interface IMemoryBufferByteAccess allows us to access the underlying byte array in an AudioFrame
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public sealed class AudioMemoryBuffer
    {
        //Look into the data bit by bit
        unsafe public IList<float> ProcessFrameOutput(AudioFrame frame)
        {
            IList<float> points = new List<float>();

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                dataInFloat = (float*)dataInBytes;

                int dataInFloatLength = (int)buffer.Length / sizeof(float);

                for (int i = 0; i < dataInFloatLength; i++)
                {
                    points.Add(dataInFloat[i]);
                }

                return points;
            }
        }
    }
}
