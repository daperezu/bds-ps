using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.ValueObjects;

namespace FundingPlatform.Tests.Unit.Domain;

[TestFixture]
public class SupplierScoreTests
{
    [Test]
    public void SingleQuotation_GetsLowestPricePoint()
    {
        var supplier = CreateSupplier(1, ccss: false, hacienda: false, sicop: false, eInvoice: false);
        var quotation = CreateQuotation(10, supplierId: 1, price: 100m);

        var results = SupplierScore.ComputeForItem([(quotation, supplier)]);

        Assert.That(results, Has.Count.EqualTo(1));
        var score = results[0].Score;
        Assert.That(score.Total, Is.EqualTo(1)); // only price point
        Assert.That(score.HasLowestPrice, Is.True);
        Assert.That(score.IsRecommended, Is.True);
        Assert.That(score.IsPreSelected, Is.True);
    }

    [Test]
    public void MultipleQuotations_VaryingCompliance_ScoresCorrectly()
    {
        var supplier1 = CreateSupplier(1, ccss: true, hacienda: true, sicop: true, eInvoice: true);
        var supplier2 = CreateSupplier(2, ccss: true, hacienda: false, sicop: false, eInvoice: false);
        var quotation1 = CreateQuotation(10, supplierId: 1, price: 1500m);
        var quotation2 = CreateQuotation(20, supplierId: 2, price: 500m);

        var results = SupplierScore.ComputeForItem([(quotation1, supplier1), (quotation2, supplier2)]);

        Assert.That(results, Has.Count.EqualTo(2));

        // Supplier 1: CCSS + Hacienda + SICOP + EInvoice = 4 (no price point)
        var score1 = results.First(r => r.QuotationId == 10).Score;
        Assert.That(score1.Total, Is.EqualTo(4));
        Assert.That(score1.HasLowestPrice, Is.False);

        // Supplier 2: CCSS + Price = 2
        var score2 = results.First(r => r.QuotationId == 20).Score;
        Assert.That(score2.Total, Is.EqualTo(2));
        Assert.That(score2.HasLowestPrice, Is.True);
    }

    [Test]
    public void PriceTieHandling_BothGetPricePoint()
    {
        var supplier1 = CreateSupplier(1, ccss: true, hacienda: false, sicop: false, eInvoice: false);
        var supplier2 = CreateSupplier(2, ccss: false, hacienda: true, sicop: false, eInvoice: false);
        var quotation1 = CreateQuotation(10, supplierId: 1, price: 1000m);
        var quotation2 = CreateQuotation(20, supplierId: 2, price: 1000m);

        var results = SupplierScore.ComputeForItem([(quotation1, supplier1), (quotation2, supplier2)]);

        var score1 = results.First(r => r.QuotationId == 10).Score;
        var score2 = results.First(r => r.QuotationId == 20).Score;

        Assert.That(score1.HasLowestPrice, Is.True);
        Assert.That(score2.HasLowestPrice, Is.True);
        Assert.That(score1.Total, Is.EqualTo(2)); // CCSS + price
        Assert.That(score2.Total, Is.EqualTo(2)); // Hacienda + price
    }

    [Test]
    public void AllIdenticalScores_AllRecommended()
    {
        var supplier1 = CreateSupplier(1, ccss: true, hacienda: false, sicop: false, eInvoice: false);
        var supplier2 = CreateSupplier(2, ccss: true, hacienda: false, sicop: false, eInvoice: false);
        var quotation1 = CreateQuotation(10, supplierId: 1, price: 500m);
        var quotation2 = CreateQuotation(20, supplierId: 2, price: 500m);

        var results = SupplierScore.ComputeForItem([(quotation1, supplier1), (quotation2, supplier2)]);

        Assert.That(results.All(r => r.Score.IsRecommended), Is.True);
        Assert.That(results.All(r => r.Score.Total == 2), Is.True); // CCSS + price
    }

    [Test]
    public void ZeroCompliance_OnlyPricePoints()
    {
        var supplier1 = CreateSupplier(1, ccss: false, hacienda: false, sicop: false, eInvoice: false);
        var supplier2 = CreateSupplier(2, ccss: false, hacienda: false, sicop: false, eInvoice: false);
        var quotation1 = CreateQuotation(10, supplierId: 1, price: 100m);
        var quotation2 = CreateQuotation(20, supplierId: 2, price: 200m);

        var results = SupplierScore.ComputeForItem([(quotation1, supplier1), (quotation2, supplier2)]);

        var score1 = results.First(r => r.QuotationId == 10).Score;
        var score2 = results.First(r => r.QuotationId == 20).Score;

        Assert.That(score1.Total, Is.EqualTo(1)); // only price
        Assert.That(score2.Total, Is.EqualTo(0)); // nothing
        Assert.That(score1.IsRecommended, Is.True);
        Assert.That(score2.IsRecommended, Is.False);
    }

    [Test]
    public void RecommendationFlag_OnlyHighestScorers()
    {
        var supplier1 = CreateSupplier(1, ccss: true, hacienda: true, sicop: true, eInvoice: true);
        var supplier2 = CreateSupplier(2, ccss: true, hacienda: false, sicop: false, eInvoice: false);
        var supplier3 = CreateSupplier(3, ccss: false, hacienda: false, sicop: false, eInvoice: false);
        var quotation1 = CreateQuotation(10, supplierId: 1, price: 500m);
        var quotation2 = CreateQuotation(20, supplierId: 2, price: 500m);
        var quotation3 = CreateQuotation(30, supplierId: 3, price: 500m);

        var results = SupplierScore.ComputeForItem([
            (quotation1, supplier1), (quotation2, supplier2), (quotation3, supplier3)]);

        var score1 = results.First(r => r.QuotationId == 10).Score;
        var score2 = results.First(r => r.QuotationId == 20).Score;
        var score3 = results.First(r => r.QuotationId == 30).Score;

        Assert.That(score1.Total, Is.EqualTo(5)); // all factors
        Assert.That(score1.IsRecommended, Is.True);
        Assert.That(score2.IsRecommended, Is.False);
        Assert.That(score3.IsRecommended, Is.False);
    }

    [Test]
    public void PreSelection_TieBreaksByLowestSupplierId()
    {
        var supplier5 = CreateSupplier(5, ccss: true, hacienda: true, sicop: false, eInvoice: false);
        var supplier3 = CreateSupplier(3, ccss: true, hacienda: true, sicop: false, eInvoice: false);
        var quotation1 = CreateQuotation(10, supplierId: 5, price: 1000m);
        var quotation2 = CreateQuotation(20, supplierId: 3, price: 1000m);

        var results = SupplierScore.ComputeForItem([(quotation1, supplier5), (quotation2, supplier3)]);

        var score5 = results.First(r => r.QuotationId == 10).Score;
        var score3 = results.First(r => r.QuotationId == 20).Score;

        // Both recommended (tied)
        Assert.That(score5.IsRecommended, Is.True);
        Assert.That(score3.IsRecommended, Is.True);

        // Supplier 3 (lower ID) is pre-selected
        Assert.That(score3.IsPreSelected, Is.True);
        Assert.That(score5.IsPreSelected, Is.False);
    }

    [Test]
    public void ResultsSortedByScoreDescending()
    {
        var supplier1 = CreateSupplier(1, ccss: true, hacienda: true, sicop: true, eInvoice: true);
        var supplier2 = CreateSupplier(2, ccss: false, hacienda: false, sicop: false, eInvoice: false);
        var quotation1 = CreateQuotation(10, supplierId: 1, price: 500m);
        var quotation2 = CreateQuotation(20, supplierId: 2, price: 1000m);

        var results = SupplierScore.ComputeForItem([(quotation2, supplier2), (quotation1, supplier1)]);

        // First result should be highest score
        Assert.That(results[0].Score.Total, Is.GreaterThanOrEqualTo(results[1].Score.Total));
        Assert.That(results[0].QuotationId, Is.EqualTo(10)); // supplier1 has score 5
    }

    [Test]
    public void EmptyList_ReturnsEmpty()
    {
        var results = SupplierScore.ComputeForItem([]);
        Assert.That(results, Is.Empty);
    }

    private static Supplier CreateSupplier(int id, bool ccss, bool hacienda, bool sicop, bool eInvoice)
    {
        var supplier = new Supplier(
            legalId: $"LEG-{id}",
            name: $"Supplier {id}",
            contactName: null,
            email: null,
            phone: null,
            location: null,
            hasElectronicInvoice: eInvoice,
            shippingDetails: null,
            warrantyInfo: null,
            isCompliantCCSS: ccss,
            isCompliantHacienda: hacienda,
            isCompliantSICOP: sicop);

        // Use reflection to set the Id since it's private set
        typeof(Supplier).GetProperty("Id")!.SetValue(supplier, id);

        return supplier;
    }

    private static Quotation CreateQuotation(int id, int supplierId, decimal price)
    {
        var quotation = new Quotation(supplierId, documentId: 1, price, DateOnly.FromDateTime(DateTime.Today.AddMonths(3)));

        // Use reflection to set the Id since it's private set
        typeof(Quotation).GetProperty("Id")!.SetValue(quotation, id);

        return quotation;
    }
}
