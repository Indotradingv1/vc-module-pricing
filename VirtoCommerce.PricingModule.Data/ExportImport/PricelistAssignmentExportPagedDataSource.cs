using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.Domain.Pricing.Model.Search;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Data.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PricingModule.Data.ExportImport
{
    public class PricelistAssignmentExportPagedDataSource : ExportPagedDataSource<PricelistAssignmentExportDataQuery, PricelistAssignmentsSearchCriteria>
    {
        private readonly IPricingSearchService _searchService;
        private readonly IPricingService _pricingService;
        private readonly ICatalogService _catalogService;
        private readonly PricelistAssignmentExportDataQuery _dataQuery;

        public PricelistAssignmentExportPagedDataSource(
           IPricingSearchService searchService,
           IPricingService pricingService,
           ICatalogService catalogService,
           PricelistAssignmentExportDataQuery dataQuery) : base(dataQuery)
        {
            _searchService = searchService;
            _pricingService = pricingService;
            _catalogService = catalogService;
            _dataQuery = dataQuery;
        }

        protected override PricelistAssignmentsSearchCriteria BuildSearchCriteria(PricelistAssignmentExportDataQuery exportDataQuery)
        {
            var result = base.BuildSearchCriteria(exportDataQuery);

            result.PriceListIds = _dataQuery.PriceListIds;
            result.CatalogIds = _dataQuery.CatalogIds;
            result.Keyword = _dataQuery.Keyword;

            return result;
        }

        protected override ExportableSearchResult FetchData(PricelistAssignmentsSearchCriteria searchCriteria)
        {
            PricelistAssignment[] result;
            int totalCount;

            if (searchCriteria.ObjectIds.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                result = _pricingService.GetPricelistAssignmentsById(Enumerable.ToArray(searchCriteria.ObjectIds));
                totalCount = result.Length;
            }
            else
            {
                var pricelistAssignmentSearchResult = _searchService.SearchPricelistAssignments((PricelistAssignmentsSearchCriteria)searchCriteria);
                result = pricelistAssignmentSearchResult.Results.ToArray();
                totalCount = pricelistAssignmentSearchResult.TotalCount;
            }

            return new ExportableSearchResult()
            {
                Results = ToExportable(result).ToList(),
                TotalCount = totalCount,
            };
        }

        protected virtual IEnumerable<IExportable> ToExportable(IEnumerable<ICloneable> objects)
        {
            var models = objects.Cast<PricelistAssignment>();
            var viewableMap = models.ToDictionary(x => x, x => AbstractTypeFactory<ExportablePricelistAssignment>.TryCreateInstance().FromModel(x));

            FillAdditionalProperties(viewableMap);

            var modelIds = models.Select(x => x.Id).ToList();
            var result = viewableMap.Values.OrderBy(x => modelIds.IndexOf(x.Id));

            return result;
        }

        protected virtual void FillAdditionalProperties(Dictionary<PricelistAssignment, ExportablePricelistAssignment> viewableMap)
        {
            var models = viewableMap.Keys;
            var catalogIds = models.Select(x => x.CatalogId).Distinct().ToArray();
            var pricelistIds = models.Select(x => x.PricelistId).Distinct().ToArray();
            var catalogs = _catalogService.GetCatalogsList().Where(x => catalogIds.Contains(x.Id));
            var pricelists = _pricingService.GetPricelistsById(pricelistIds);

            foreach (var kvp in viewableMap)
            {
                var model = kvp.Key;
                var viewableEntity = kvp.Value;
                var catalog = catalogs.FirstOrDefault(x => x.Id == model.CatalogId);
                var pricelist = pricelists.FirstOrDefault(x => x.Id == model.PricelistId);

                viewableEntity.CatalogName = catalog?.Name;
                viewableEntity.PricelistName = pricelist?.Name;
            }
        }
    }
}
