using IoT.Application;
using IoT.Domain;
using IoT.Infrastructure;

namespace IoT.Tests;

public sealed class TelemetryTests
{
    [Fact]
    public void CircularBuffer_StaysBounded_AndReturnsOldestToNewest()
    {
        var buffer=new CircularBuffer<int>(3);
        for(var i=1;i<=5;i++) buffer.Add(i);
        Assert.Equal(3,buffer.Count);
        Assert.Equal(new[]{3,4,5},buffer.Snapshot());
    }
    [Theory]
    [InlineData(96,DeviceStatus.Critical)]
    [InlineData(90,DeviceStatus.Warning)]
    [InlineData(70,null)]
    public void AlertEvaluator_AppliesCriticalBeforeWarning(double value, DeviceStatus? expected)
    {
        var device=new SensorDevice(Guid.NewGuid(),"Test","Lab",SensorType.Temperature,"°C",DeviceStatus.Online);
        var reading=new SensorReading(device.Id,DateTimeOffset.UtcNow,value,DeviceStatus.Online);
        var alert=new AlertEvaluator().Evaluate(device,reading,new(device.Id,60,82,45,95));
        Assert.Equal(expected,alert?.Severity);
    }
    [Fact]
    public void DomainValidation_RejectsNonFiniteTelemetry()
    {
        var result=DomainValidation.Validate(new(Guid.NewGuid(),DateTimeOffset.UtcNow,double.NaN,DeviceStatus.Online));
        Assert.False(result.IsSuccess);
    }
}
