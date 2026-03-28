using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Management;

namespace QuickDock.Services;

public class SystemResourceService
{
    private readonly PerformanceCounter? _cpuCounter;
    private int _lastCpuPercent;

    public SystemResourceService()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue();
            _lastCpuPercent = 0;
        }
        catch
        {
            _cpuCounter = null;
        }
    }

    public SystemResourceInfo GetResourceInfo()
    {
        int cpuPercent = GetCpuUsage();
        int memPercent = GetMemoryUsage();
        int gpuPercent = GetGpuUsage();

        return new SystemResourceInfo
        {
            CpuPercent = cpuPercent,
            MemPercent = memPercent,
            GpuPercent = gpuPercent
        };
    }

    private int GetCpuUsage()
    {
        try
        {
            if (_cpuCounter != null)
            {
                _lastCpuPercent = (int)Math.Round(_cpuCounter.NextValue());
            }
            return _lastCpuPercent;
        }
        catch
        {
            return _lastCpuPercent;
        }
    }

    private int GetMemoryUsage()
    {
        try
        {
            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            var total = (long)computerInfo.TotalPhysicalMemory;
            var available = (long)computerInfo.AvailablePhysicalMemory;
            var used = total - available;
            return (int)Math.Round((double)used / total * 100);
        }
        catch
        {
            return 0;
        }
    }

    private int GetGpuUsage()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                var val = obj["LoadPercentage"];
                if (val != null && int.TryParse(val.ToString(), out int percent))
                {
                    return percent;
                }
            }
        }
        catch
        {
        }
        return 0;
    }
}

public class SystemResourceInfo
{
    public int CpuPercent { get; set; }
    public int MemPercent { get; set; }
    public int GpuPercent { get; set; }
}
