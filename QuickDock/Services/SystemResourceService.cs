using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QuickDock.Services;

public class SystemResourceService : IDisposable
{
    private readonly PerformanceCounter? _cpuCounter;
    private readonly List<PerformanceCounter> _gpuCounters = new();

    public SystemResourceService()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue();
        }
        catch
        {
            _cpuCounter = null;
        }

        InitializeGpuCounters();
    }

    private void InitializeGpuCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames();

            foreach (var instance in instanceNames)
            {
                if (instance.Contains("engtype_"))
                {
                    try
                    {
                        var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        counter.NextValue();
                        _gpuCounters.Add(counter);
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }
    }

    public SystemResourceInfo GetResourceInfo()
    {
        return new SystemResourceInfo
        {
            CpuPercent = GetCpuUsage(),
            MemPercent = GetMemoryUsage(),
            GpuPercent = GetGpuUsage()
        };
    }

    private int GetCpuUsage()
    {
        try
        {
            if (_cpuCounter != null)
            {
                var value = (int)Math.Round(_cpuCounter.NextValue());
                return Math.Max(0, Math.Min(100, value));
            }
            return 0;
        }
        catch
        {
            return 0;
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
            if (_gpuCounters.Count == 0)
                return 0;

            float totalUsage = 0;
            int validCount = 0;

            foreach (var counter in _gpuCounters)
            {
                try
                {
                    float value = counter.NextValue();
                    if (value >= 0)
                    {
                        totalUsage += value;
                        validCount++;
                    }
                }
                catch
                {
                }
            }

            if (validCount > 0)
            {
                var avgUsage = totalUsage / validCount;
                return (int)Math.Round(Math.Min(100, avgUsage));
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        foreach (var counter in _gpuCounters)
        {
            counter.Dispose();
        }
        _gpuCounters.Clear();
    }
}

public class SystemResourceInfo
{
    public int CpuPercent { get; set; }
    public int MemPercent { get; set; }
    public int GpuPercent { get; set; }
}
