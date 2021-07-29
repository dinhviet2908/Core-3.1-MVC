﻿using eShopSolution.ViewModels.Catalog.ProductImages;
using eShopSolution.ViewModels.Catalog.Products;
using eShopSolution.ViewModels.Common;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eShopSolution.Application.Catalog.Products
{
    public interface IManageProductService
    {
        Task<int> Create(ProductCreateRequest productCreateRequest);

        Task<int> Update(ProductUpdateRequest productEditRequest);

        Task<int> Delete(int productId);

        Task<ProductViewModel> GetById(int productId, string languageId);

        Task<bool> UpdatePrice(int productId, decimal newPrice);

        Task<bool> UpdateStock(int productId, int addQuantity);

        Task AddViewCount(int productId);

        Task<List<ProductViewModel>> GetAll();

        Task<PagedResult<ProductViewModel>> GetAllPaging(GetManageProductPagingRequest getProductPagingRequest);

        Task<int> AddImage(int productId, ProductImageCreateRequest productImage);

        Task<int> RemoveImage(int imageId);

        Task<int> UpdateImage(int imageId, ProductImageUpdateRequest productImage);

        Task<List<ProductImageViewModel>> GetListImages(int productId);

        Task<ProductImageViewModel> GetImageById(int imageId);
    }
}