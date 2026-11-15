using Bunit;
using FluentAssertions;
using IoT.Domain;
using IoT.Web.Components.Shared;

namespace IoT.Tests;

public sealed class SensorCardTests
{
    [Fact]
    public void SensorCard_ReflectsDeviceStatusInStatusBadge()
    {
        using var context=new BunitContext();
        var device=new SensorDevice(Guid.NewGuid(),"Boiler","Plant room",SensorType.Temperature,"°C",DeviceStatus.Online);
        var rendered=context.Render<SensorCard>(parameters=>parameters
            .Add(p=>p.Device,device)
            .Add(p=>p.Status,DeviceStatus.Critical));

        rendered.Find(".status-tag").ClassList.Should().Contain("critical");
        rendered.Find(".status-tag").TextContent.Should().Contain("Critical");
    }
}
