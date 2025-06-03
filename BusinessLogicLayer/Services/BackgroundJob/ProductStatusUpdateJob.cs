using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services.BackgroundJob
{
    public class ProductStatusUpdateJob : IJob
    {
        private readonly ILogger<ProductStatusUpdateJob> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ProductStatusUpdateJob(ILogger<ProductStatusUpdateJob> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {                
                await Task.Delay(10000);

                _logger.LogInformation("Product background job started at {Time}", DateTime.Now);

                // Out of stock products
                var ooStockProduct = await GetOutOfStockProductAsync();
                if (ooStockProduct.Any())
                {
                    await UpdateOutOfStockStatus(ooStockProduct);
                    _logger.LogInformation("Updated {Count} out of stock product to in stock status", ooStockProduct.Count);
                }
                else
                {
                    _logger.LogInformation("No out of stock products to update.");
                }

                // In stock products
                var iStockProduct = await GetInStockProductAsync();
                if (iStockProduct.Any())
                {
                    await UpdateInStockStatus(iStockProduct);
                    _logger.LogInformation("Updated {Count} in stock products to out of stock status", iStockProduct.Count);
                }
                else
                {
                    _logger.LogInformation("No in stock products to update.");
                }

                _logger.LogInformation("Product background job completed at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Product status.");
            }
        }

        private async Task<List<Product>> GetOutOfStockProductAsync()
        {
            return await _unitOfWork.ProductRepository.Queryable()
                                    .Where(p => p.Quantity == 0 && p.Status == Product.ProductStatus.InStock)
                                    .ToListAsync();
        }

        private async Task<List<Product>> GetInStockProductAsync()
        {
            return await _unitOfWork.ProductRepository.Queryable()
                                    .Where(p => p.Quantity != 0 && p.Status == Product.ProductStatus.OutOfStock)
                                    .ToListAsync();
        }

        private async Task UpdateOutOfStockStatus(List<Product> products)
        {
            foreach (var product in products)
            {
                product.Status = Product.ProductStatus.InStock;
            }

            await _unitOfWork.CommitAsync();
        }

        private async Task UpdateInStockStatus(List<Product> products)
        {
            foreach (var product in products)
            {
                product.Status = Product.ProductStatus.OutOfStock;
            }

            await _unitOfWork.CommitAsync();
        }
    }
}
