using eShopSolution.Application.Catalog.Products.Dtos;
using eShopSolution.Application.Catalog.Products.Dtos.Manage;
using eShopSolution.Application.Dtos;
using eShopSolution.Data.EF;
using eShopSolution.Data.Entities;
using eShopSolution.Utilities.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShopSolution.Application.Catalog.Products
{
    public class ManageProductService : IManagedProductService
    {
        private readonly EShopDbContext _context;
        public ManageProductService(EShopDbContext context)
        {
            _context = context;
        }

        public async Task AddViewCount(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            product.ViewCount += 1;
            await _context.SaveChangesAsync();
        }

        public async Task<int> Create(ProductCreateRequest productCreateRequest)
        {
            var product = new Product()
            {
                Price = productCreateRequest.Price,
                OriginalPrice = productCreateRequest.OriginalPrice,
                Stock = productCreateRequest.Stock,
                ViewCount = 0,
                CreatedDate = DateTime.Now,
                ProductTranslations = new List<ProductTranslation>()
                {
                    new ProductTranslation()
                    {
                        Name = productCreateRequest.Name,
                        Description = productCreateRequest.Description,
                        Details = productCreateRequest.Details,
                        SeoDescription = productCreateRequest.SeoDescription,
                        SeoAlias = productCreateRequest.SeoAlias,
                        SeoTitle = productCreateRequest.SeoTitle,
                        LanguageId = productCreateRequest.LanguageId
                    }
                }
            };
            _context.Products.Add(product);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> Delete(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new EShopException($"Product not found with id: {productId}");
            }
            _context.Remove(product);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<ProductViewModel>> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResult<ProductViewModel>> GetAllPaging(GetProductPagingRequest getProductPagingRequest)
        {
            var query = from p in _context.Products
                        join pt in _context.ProductTranslations on p.Id equals pt.ProductId
                        join pic in _context.ProductInCategories on p.Id equals pic.ProductId
                        join c in _context.Categories on pic.CategoryId equals c.Id
                        select new { p, pt, pic };
            if (!string.IsNullOrEmpty(getProductPagingRequest.Keyword))
            {
                query = query.Where(x => x.pt.Name.Contains(getProductPagingRequest.Keyword));
            }
            if (getProductPagingRequest.CategoryIds.Count > 0)
            {
                query = query.Where(x => getProductPagingRequest.CategoryIds.Contains(x.pic.CategoryId));
            }
            int totalRow = await query.CountAsync();
            var data = await query.Skip((getProductPagingRequest.PageIndex - 1) * getProductPagingRequest.PageSize).Take(getProductPagingRequest.PageSize).Select(x => new ProductViewModel() {
                Id = x.p.Id,
                Name = x.pt.Name,
                CreatedDate = x.p.CreatedDate,
                Description = x.pt.Description,
                Details = x.pt.Details,
                LanguageId = x.pt.LanguageId,
                Price = x.p.Price,
                OriginalPrice = x.p.OriginalPrice,
                SeoAlias = x.pt.SeoAlias,
                SeoDescription = x.pt.SeoDescription,
                SeoTitle = x.pt.SeoTitle,
                Stock = x.p.Stock,
                ViewCount = x.p.ViewCount
            }).ToListAsync();
            var pagedResult = new PagedResult<ProductViewModel>()
            {
                TotalRecord = totalRow,
                Items = data
            };
            return pagedResult;
        }

        public async Task<int> Update(ProductUpdateRequest productEditRequest)
        {
            var product = await _context.Products.FindAsync(productEditRequest.Id);
            var productTrans = await _context.ProductTranslations.FirstOrDefaultAsync(x => x.ProductId == productEditRequest.Id && x.LanguageId == productEditRequest.LanguageId);
            if (product == null || productTrans == null)
            {
                throw new EShopException($"Product not found with id: {productEditRequest.Id}");
            }
            productTrans.Name = productEditRequest.Name;
            productTrans.SeoAlias = productEditRequest.SeoAlias;
            productTrans.SeoDescription = productEditRequest.SeoDescription;
            productTrans.SeoTitle = productEditRequest.SeoTitle;
            productTrans.Description = productEditRequest.Description;
            productTrans.Details = productEditRequest.Details;
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdatePrice(int productId, decimal newPrice)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new EShopException($"Product not found with id: {productId}");
            }
            product.Price = newPrice;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateStock(int productId, int addQuantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new EShopException($"Product not found with id: {productId}");
            }
            product.Stock += addQuantity;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
