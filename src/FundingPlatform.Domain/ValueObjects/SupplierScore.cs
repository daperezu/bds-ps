using FundingPlatform.Domain.Entities;

namespace FundingPlatform.Domain.ValueObjects;

public record SupplierScore(
    int Total,
    bool IsCompliantCCSS,
    bool IsCompliantHacienda,
    bool IsCompliantSICOP,
    bool HasElectronicInvoice,
    bool HasLowestPrice,
    bool IsRecommended,
    bool IsPreSelected)
{
    public static List<(int QuotationId, SupplierScore Score)> ComputeForItem(
        List<(Quotation Quotation, Supplier Supplier)> quotations)
    {
        if (quotations.Count == 0)
            return [];

        var minPrice = quotations.Min(q => q.Quotation.Price);

        var scored = quotations.Select(q =>
        {
            bool ccss = q.Supplier.IsCompliantCCSS;
            bool hacienda = q.Supplier.IsCompliantHacienda;
            bool sicop = q.Supplier.IsCompliantSICOP;
            bool eInvoice = q.Supplier.HasElectronicInvoice;
            bool lowestPrice = q.Quotation.Price == minPrice;

            int total = (ccss ? 1 : 0)
                      + (hacienda ? 1 : 0)
                      + (sicop ? 1 : 0)
                      + (eInvoice ? 1 : 0)
                      + (lowestPrice ? 1 : 0);

            return new { QuotationId = q.Quotation.Id, SupplierId = q.Supplier.Id, Total = total, CCSS = ccss, Hacienda = hacienda, SICOP = sicop, EInvoice = eInvoice, LowestPrice = lowestPrice };
        }).ToList();

        int maxScore = scored.Max(s => s.Total);

        // Pre-selected: highest score, tie-break by lowest supplier ID
        int preSelectedSupplierId = scored
            .Where(s => s.Total == maxScore)
            .OrderBy(s => s.SupplierId)
            .First()
            .SupplierId;

        return scored
            .Select(s => (
                s.QuotationId,
                new SupplierScore(
                    Total: s.Total,
                    IsCompliantCCSS: s.CCSS,
                    IsCompliantHacienda: s.Hacienda,
                    IsCompliantSICOP: s.SICOP,
                    HasElectronicInvoice: s.EInvoice,
                    HasLowestPrice: s.LowestPrice,
                    IsRecommended: s.Total == maxScore,
                    IsPreSelected: s.SupplierId == preSelectedSupplierId)))
            .OrderByDescending(s => s.Item2.Total)
            .ThenBy(s => s.QuotationId)
            .ToList();
    }
}
