using System;
using System.Collections.Generic;

namespace ScadaApp.Models;

/// <summary>Параметры контроллера: тип, IP, rack, slot и т.д.</summary>
public class DeviceConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";

    /// <summary>Тип драйвера: "Siemens", "OpcUa".</summary>
    public string DriverType { get; set; } = "Siemens";

    public string IpAddress { get; set; } = "";
    public int Rack { get; set; } = 0;
    public int Slot { get; set; } = 1;
    public int Port { get; set; } = 102;

    /// <summary>Тип CPU: S71200, S71500 и др.</summary>
    public string CpuType { get; set; } = "S71500";

    public int PollingIntervalMs { get; set; } = 1000;
    public List<TagConfig> Tags { get; set; } = new();
}
