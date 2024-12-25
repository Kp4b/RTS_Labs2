using System;
using System.Runtime.InteropServices;

public class TimeMeasurement
{
    [DllImport("kernel32.dll")]
    private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

    [DllImport("kernel32.dll")]
    private static extern bool QueryPerformanceFrequency(out long lpFrequency);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessTimes(IntPtr hProcess, out FILETIME lpCreationTime, out FILETIME lpExitTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("winmm.dll", EntryPoint = "timeGetTime")]
    private static extern uint TimeGetTime();

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    private long _frequency;
    private long _startCounter;
    private long _endCounter;
    private uint _startTimeGetTime;
    private uint _endTimeGetTime;

    public TimeMeasurement()
    {
        if (!QueryPerformanceFrequency(out _frequency))
        {
            throw new InvalidOperationException("High-performance counter not supported.");
        }
    }

    public void Start()
    {
        QueryPerformanceCounter(out _startCounter);
        _startTimeGetTime = TimeGetTime();
    }

    public void Stop()
    {
        QueryPerformanceCounter(out _endCounter);
        _endTimeGetTime = TimeGetTime();
    }

    public double GetElapsedTime()
    {
        return (double)(_endCounter - _startCounter) / _frequency;
    }

    public double GetElapsedTimeUsingTimeGetTime()
    {
        return (_endTimeGetTime - _startTimeGetTime) / 1000.0;
    }

    public TimeSpan GetProcessTime()
    {
        GetProcessTimes(GetCurrentProcess(), out _, out _, out FILETIME kernelTime, out FILETIME userTime);
        ulong kernel = ((ulong)kernelTime.dwHighDateTime << 32) | kernelTime.dwLowDateTime;
        ulong user = ((ulong)userTime.dwHighDateTime << 32) | userTime.dwLowDateTime;
        return TimeSpan.FromTicks((long)(kernel + user));
    }
}
