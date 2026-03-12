using PayGateway.Domain.Entities;
using PayGateway.Domain.Services;
using Xunit;

namespace PayGateway.Tests.Unit;

public class PaymentStatusResolverTests
{
    [Fact]
    public void GetInitialStatus_Pix_ReturnsCompleted()
    {
        var status = PaymentStatusResolver.GetInitialStatus(PaymentMethod.Pix);
        Assert.Equal(PaymentStatus.Completed, status);
    }

    [Fact]
    public void GetInitialStatus_Card_ReturnsProcessing()
    {
        var status = PaymentStatusResolver.GetInitialStatus(PaymentMethod.Card);
        Assert.Equal(PaymentStatus.Processing, status);
    }
}
