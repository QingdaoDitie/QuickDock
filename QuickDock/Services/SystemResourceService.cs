using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace QuickDock.Services;

public class SystemResourceService : IDisposable
{
    private PerformanceCounter? _cpuCounter;
    private int _lastCpuPercent;
    private DateTime _lastCpuReadTime;
    private int _lastGpuPercent;
    private DateTime _lastGpuReadTime;
    private List<PerformanceCounter>? _gpuCounters;

    public SystemResourceService()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            _cpuCounter.NextValue();
            _lastCpuReadTime = DateTime.MinValue;
        }
        catch
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
                _lastCpuReadTime = DateTime.MinValue;
            }
            catch
            {
                _cpuCounter = null;
            }
        }

        InitializeGpuCounters();
    }

    private void InitializeGpuCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames();
            _gpuCounters = new List<PerformanceCounter>();

            foreach (var instance in instanceNames)
            {
                if (instance.Contains("engtype_3D"))
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
            _gpuCounters = null;
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
            if (_cpuCounter == null)
                return 0;

            var now = DateTime.Now;
            var elapsed = (now - _lastCpuReadTime).TotalMilliseconds;
            
            if (elapsed < 500)
            {
                return _lastCpuPercent;
            }

            float value = _cpuCounter.NextValue();
            _lastCpuPercent = (int)Math.Round(value);
            _lastCpuReadTime = now;
            
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
            var now = DateTime.Now;
            var elapsed = (now - _lastGpuReadTime).TotalMilliseconds;
            
            if (elapsed < 500)
            {
                return _lastGpuPercent;
            }

            if (_gpuCounters == null || _gpuCounters.Count == 0)
            {
                using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_VideoController");
                var results = searcher.Get();
                
                foreach (var obj in results)
                {
                    var load = obj["LoadPercentage"];
                    if (load != null && uint.TryParse(load.ToString(), out uint value))
                    {
                        _lastGpuPercent = (int)value;
                        break;
                    }
                }
            }
            else
            {
                float maxUsage = 0;

                foreach (var counter in _gpuCounters)
                {
                    try
                    {
                        float value = counter.NextValue();
                        if (value > maxUsage)
                        {
                            maxUsage = value;
                        }
                    }
                    catch
                    {
                    }
                }

                _lastGpuPercent = (int)Math.Round(maxUsage);
            }
            
            _lastGpuReadTime = now;
            return _lastGpuPercent;
        }
        catch
        {
            return _lastGpuPercent;
        }
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        if (_gpuCounters != null)
        {
            foreach (var counter in _gpuCounters)
            {
                counter.Dispose();
            }
            _gpuCounters.Clear();
        }
    }
}

public class SystemResourceInfo
{
    public int CpuPercent { get; set; }
    public int MemPercent { get; set; }
    public int GpuPercent { get; set; }
}
