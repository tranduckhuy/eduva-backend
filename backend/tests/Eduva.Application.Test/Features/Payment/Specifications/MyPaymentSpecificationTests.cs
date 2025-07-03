using Eduva.Application.Features.Payments.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.Payment.Specifications;

[TestFixture]
public class MyPaymentSpecificationTests
{
    private List<PaymentTransaction> _transactions = null!;
    private Guid _userId;

    #region MyPaymentSpecficationTests Setup

    [SetUp]
    public void Setup()
    {
        _userId = Guid.NewGuid();
        _transactions = new List<PaymentTransaction>
        {
            new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                PaymentPurpose = PaymentPurpose.SchoolSubscription,
                PaymentStatus = PaymentStatus.Paid,
                Amount = 100_000,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                PaymentPurpose = PaymentPurpose.CreditPackage,
                PaymentStatus = PaymentStatus.Pending,
                Amount = 200_000,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
            },
            new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                PaymentPurpose = PaymentPurpose.CreditPackage,
                PaymentStatus = PaymentStatus.Paid,
                Amount = 150_000,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };
    }

    #endregion

    #region MyPaymentSpecficationTests Tests

    [TestCase("unknown", "desc")]
    [TestCase("UNKNOWN", "asc")]
    public void Should_Use_Default_Sort_When_Unknown_Field(string sortBy, string sortDirection)
    {
        var param = new MyPaymentSpecParam
        {
            UserId = _userId,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var spec = new MyPaymentSpecification(param);
        var ordered = spec.OrderBy!(_transactions.AsQueryable().Where(spec.Criteria)).ToList();

        var expected = sortDirection.ToLower() == "desc"
            ? _transactions.Where(t => t.UserId == _userId).OrderByDescending(t => t.CreatedAt).ToList()
            : _transactions.Where(t => t.UserId == _userId).OrderBy(t => t.CreatedAt).ToList();

        Assert.That(ordered.Select(x => x.Id), Is.EqualTo(expected.Select(x => x.Id)));
    }

    [Test]
    public void Should_Filter_By_Last30Days()
    {
        var param = new MyPaymentSpecParam
        {
            UserId = _userId,
            DateFilter = DateFilter.Last30Days
        };

        var spec = new MyPaymentSpecification(param);
        var result = _transactions.AsQueryable().Where(spec.Criteria).ToList();

        Assert.That(result.All(t => t.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-30)), Is.True);
    }

    [Test]
    public void Selector_Should_Be_Null()
    {
        var param = new MyPaymentSpecParam { UserId = _userId };
        var spec = new MyPaymentSpecification(param);
        Assert.That(spec.Selector, Is.Null);
    }

    [Test]
    public void Should_Filter_By_UserId_And_PaymentPurpose_And_Status()
    {
        var param = new MyPaymentSpecParam
        {
            UserId = _userId,
            PaymentPurpose = PaymentPurpose.SchoolSubscription,
            PaymentStatus = PaymentStatus.Paid,
            DateFilter = DateFilter.All
        };

        var spec = new MyPaymentSpecification(param);
        var result = _transactions.AsQueryable().Where(spec.Criteria).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First().PaymentPurpose, Is.EqualTo(PaymentPurpose.SchoolSubscription));
    }

    [Test]
    public void Should_Filter_By_Last7Days()
    {
        var param = new MyPaymentSpecParam
        {
            UserId = _userId,
            DateFilter = DateFilter.Last7Days
        };

        var spec = new MyPaymentSpecification(param);
        var result = _transactions.AsQueryable().Where(spec.Criteria).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First().CreatedAt, Is.GreaterThan(DateTimeOffset.UtcNow.AddDays(-7)));
    }

    [Test]
    public void Should_Filter_By_Today()
    {
        var baseUtc = DateTimeOffset.UtcNow;
        var todayStart = baseUtc;
        var todayMiddle = todayStart.AddHours(12);

        var todayTransaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            PaymentPurpose = PaymentPurpose.CreditPackage,
            PaymentStatus = PaymentStatus.Paid,
            Amount = 300_000,
            CreatedAt = todayMiddle
        };

        _transactions.Add(todayTransaction);

        var param = new MyPaymentSpecParam
        {
            UserId = _userId,
            DateFilter = DateFilter.Today
        };

        var spec = new MyPaymentSpecification(param);
        var result = _transactions.AsQueryable().Where(spec.Criteria).ToList();

        Assert.That(result.Any(x => x.Id == todayTransaction.Id), Is.True);
    }

    [TestCase("createdat", "desc")]
    [TestCase("createdat", "asc")]
    [TestCase("amount", "desc")]
    [TestCase("amount", "asc")]
    public void Should_Order_By_Field(string sortBy, string sortDirection)
    {
        var param = new MyPaymentSpecParam
        {
            UserId = _userId,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var spec = new MyPaymentSpecification(param);
        var ordered = spec.OrderBy!(_transactions.AsQueryable().Where(spec.Criteria)).ToList();

        var expected = sortBy switch
        {
            "createdat" => sortDirection == "desc"
                ? _transactions.Where(t => t.UserId == _userId).OrderByDescending(t => t.CreatedAt).ToList()
                : _transactions.Where(t => t.UserId == _userId).OrderBy(t => t.CreatedAt).ToList(),

            "amount" => sortDirection == "desc"
                ? _transactions.Where(t => t.UserId == _userId).OrderByDescending(t => t.Amount).ToList()
                : _transactions.Where(t => t.UserId == _userId).OrderBy(t => t.Amount).ToList(),

            _ => throw new ArgumentException("Invalid sortBy")
        };

        Assert.That(ordered.Select(t => t.Id), Is.EqualTo(expected.Select(t => t.Id)));
    }

    [Test]
    public void Should_Apply_Skip_And_Take()
    {
        var param = new MyPaymentSpecParam
        {
            UserId = _userId,
            PageIndex = 2,
            PageSize = 1
        };

        var spec = new MyPaymentSpecification(param);
        var filtered = _transactions.AsQueryable().Where(spec.Criteria);
        var paginated = filtered.Skip(spec.Skip).Take(spec.Take).ToList();

        Assert.That(paginated, Has.Count.EqualTo(1));
    }

    #endregion

}