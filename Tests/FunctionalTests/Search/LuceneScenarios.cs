﻿namespace FunctionalTests.Search
{
    using System;
    using System.IO;
    using System.Linq;

    using VirtoCommerce.Foundation.Catalogs.Search;
    using VirtoCommerce.Foundation.Search;
    using VirtoCommerce.Foundation.Search.Schemas;
    using VirtoCommerce.Search.Providers.Lucene;

    using Xunit;

    public class LuceneScenarios
    {
        private string _LuceneStorageDir = "c:\\windows\\temp\\lucene";

        [Fact]
        public void Can_create_lucene_index()
        {
            var scope = "default";
            var queryBuilder = new LuceneSearchQueryBuilder();
            var conn = new SearchConnection(_LuceneStorageDir, scope);
            var provider = new LuceneSearchProvider(queryBuilder, conn);
            SearchHelper.CreateSampleIndex(provider, scope);
            Directory.Delete(_LuceneStorageDir, true);
        }

        [Fact]
        public void Can_find_item_lucene()
        {
            var scope = "default";
            var queryBuilder = new LuceneSearchQueryBuilder();
            var conn = new SearchConnection(_LuceneStorageDir, scope);
            var provider = new LuceneSearchProvider(queryBuilder, conn);


            if (Directory.Exists(_LuceneStorageDir))
            {
                Directory.Delete(_LuceneStorageDir, true);
            }

            SearchHelper.CreateSampleIndex(provider, scope);

            var criteria = new CatalogItemSearchCriteria
                               {
                                   SearchPhrase = "product",
                                   IsFuzzySearch = true,
                                   Catalog = "goods",
                                   RecordsToRetrieve = 10,
                                   StartingRecord = 0,
                                   Pricelists = new string[] { }
                               };


            var results = provider.Search(scope, criteria);

            Assert.True(results.DocCount == 1);

            Directory.Delete(_LuceneStorageDir, true);
        }

        [Fact]
        public void Can_get_item_facets_lucene()
        {
            var scope = "default";
            var queryBuilder = new LuceneSearchQueryBuilder();
            var conn = new SearchConnection(_LuceneStorageDir, scope);
            var provider = new LuceneSearchProvider(queryBuilder, conn);

            if (Directory.Exists(_LuceneStorageDir))
            {
                Directory.Delete(_LuceneStorageDir, true);
            }

            SearchHelper.CreateSampleIndex(provider, scope);

            var criteria = new CatalogItemSearchCriteria
                               {
                                   SearchPhrase = "",
                                   IsFuzzySearch = true,
                                   Catalog = "goods",
                                   RecordsToRetrieve = 10,
                                   StartingRecord = 0,
                                   Currency = "USD",
                                   Pricelists = new[] { "default" }
                               };

            var filter = new AttributeFilter { Key = "Color" };
            filter.Values = new[]
                                {
                                    new AttributeFilterValue { Id = "red", Value = "red" },
                                    new AttributeFilterValue { Id = "blue", Value = "blue" },
                                    new AttributeFilterValue { Id = "black", Value = "black" }
                                };

            var rangefilter = new RangeFilter { Key = "size" };
            rangefilter.Values = new[]
                                     {
                                         new RangeFilterValue { Id = "0_to_5", Lower = "0", Upper = "5" },
                                         new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "10" }
                                     };

            var priceRangefilter = new PriceRangeFilter { Currency = "usd" };
            priceRangefilter.Values = new[]
                                          {
                                              new RangeFilterValue { Id = "0_to_100", Lower = "0", Upper = "100" },
                                              new RangeFilterValue { Id = "100_to_700", Lower = "100", Upper = "700" }
                                          };

            criteria.Add(filter);
            criteria.Add(rangefilter);
            criteria.Add(priceRangefilter);

            var results = provider.Search(scope, criteria);

            Assert.True(results.DocCount == 4);

            var redCount = GetFacetCount(results, "Color", "red");
            Assert.True(redCount == 2);

            var priceCount = GetFacetCount(results, "Price", "0_to_100");
            Assert.True(priceCount == 2);

            var priceCount2 = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceCount2 == 2);

            var sizeCount = GetFacetCount(results, "size", "0_to_5");
            Assert.True(sizeCount == 2);

            var sizeCount2 = GetFacetCount(results, "size", "5_to_10");
            Assert.True(sizeCount2 == 1); // only 1 result because upper bound is not included

            Directory.Delete(_LuceneStorageDir, true);
        }


        private int GetFacetCount(ISearchResults results, string fieldName, string facetKey)
        {
            if (results.FacetGroups == null || results.FacetGroups.Length == 0)
            {
                return 0;
            }

            var group = (from fg in results.FacetGroups where fg.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase) select fg).SingleOrDefault();

            return @group == null ? 0 : (from facet in @group.Facets where facet.Key == facetKey select facet.Count).FirstOrDefault();
        }
    }
}
