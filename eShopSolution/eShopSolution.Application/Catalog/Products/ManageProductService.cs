using eShopSolution.Application.Common;
using eShopSolution.Data.EF;
using eShopSolution.Data.Entities;
using eShopSolution.Utilities.Exceptions;
using eShopSolution.ViewModels.Catalog.Products;
using eShopSolution.ViewModels.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace eShopSolution.Application.Catalog.Products
{
    public class ManageProductService : IManagedProductService
    {
        private readonly EShopDbContext _context;
        private readonly IStorageService _storageService;
        public ManageProductService(EShopDbContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        public Task<int> AddImage(int productId, List<IFormFile> files)
        {
            throw new NotImplementedException();
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
            //save image
            if(productCreateRequest.ThumbnailImage != null)
            {
                product.ProductImages = new List<ProductImage>()
                {
                    new ProductImage()
                    {
                        Caption = "Thumbnail image",
                        CreatedDate = DateTime.Now,
                        FileSize = productCreateRequest.ThumbnailImage.Length,
                        ImagePath = await this.SaveFile(productCreateRequest.ThumbnailImage),
                        IsDefault = true,
                        SortOrder = 1,
                    }
                };
            }
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
            var images = _context.ProductImages.Where(x => x.ProductId == productId).ToList();
            foreach(var image in images)
            {
                await _storageService.DeleteFileAsync(image.ImagePath);
            }
            _context.Remove(product);

            return await _context.SaveChangesAsync();
        }

        public async Task<List<ProductViewModel>> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResult<ProductViewModel>> GetAllPaging(GetManageProductPagingRequest getProductPagingRequest)
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

        public Task<List<ProductImageViewModel>> GetListImage(int productId)
        {
            throw new NotImplementedException();
        }

        public Task<int> RemoveImage(int imageId)
        {
            throw new NotImplementedException();
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
            if (productEditRequest.ThumbnailImage != null)
            {
                var thumbnailImage = await _context.ProductImages.FirstOrDefaultAsync(x => x.IsDefault == true && x.ProductId == productEditRequest.Id);
                if(thumbnailImage != null)
                {
                    thumbnailImage.FileSize = productEditRequest.ThumbnailImage.Length;
                    thumbnailImage.ImagePath = await this.SaveFile(productEditRequest.ThumbnailImage);
                    _context.ProductImages.Update(thumbnailImage);
                }
            }
            return await _context.SaveChangesAsync();
        }

        public Task<int> UpdateImage(int imageId, string caption, bool isDefault)
        {
            throw new NotImplementedException();
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

        private async Task<string> SaveFile(IFormFile file)
        {
            var originalFileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            await _storageService.SaveFileAsync(file.OpenReadStream(), fileName);
            return fileName;
        }
    }
}
