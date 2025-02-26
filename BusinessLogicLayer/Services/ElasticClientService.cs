using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using DataAccessObject.Models;
using Nest;

namespace BusinessLogicLayer.Services
{
    public class ElasticClientService : IElasticClientService
    {
        private readonly IElasticClient _elasticClient;
        private const string IndexName = "decorservice_index";

        public ElasticClientService(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task IndexDecorServiceAsync(DecorService decorService)
        {
            
            var response = await _elasticClient.IndexDocumentAsync(decorService);
            if (!response.IsValid)
            {
                throw new Exception($"Index DecorService failed: {response.OriginalException.Message}");
            }
        }

        public async Task DeleteDecorServiceAsync(int id)
        {
            var response = await _elasticClient.DeleteAsync<DecorService>(id);
            if (!response.IsValid)
            {
                throw new Exception($"Delete DecorService failed: {response.OriginalException.Message}");
            }
        }

        public async Task<List<DecorService>> SearchDecorServicesAsync(string keyword)
        {
            var searchResponse = await _elasticClient.SearchAsync<DecorService>(s => s
                .Index(IndexName)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f
                            .Field(ff => ff.Style)
                            .Field(ff => ff.Description)
                        )
                        .Query(keyword)
                        .Fuzziness(Fuzziness.Auto)
                    )
                )
                .Size(50)
            );

            if (!searchResponse.IsValid)
            {
                throw new Exception($"Search DecorService failed: {searchResponse.OriginalException.Message}");
            }

            return searchResponse.Documents.ToList();
        }
    }
}
