using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Persistence.Seed;

internal static class CatalogSeedData
{
    private static readonly DateTime SeedCreatedAtUtc = new(2026, 05, 13, 0, 0, 0, DateTimeKind.Utc);

    internal static readonly Guid TechGearBrandId = Guid.Parse("a1f08f2b-9b5f-41a0-bc86-bb1131a6e101");
    internal static readonly Guid SoundCoreBrandId = Guid.Parse("a1f08f2b-9b5f-41a0-bc86-bb1131a6e102");
    internal static readonly Guid LegacyBrandId = Guid.Parse("a1f08f2b-9b5f-41a0-bc86-bb1131a6e103");

    internal static readonly Guid ElectronicsCategoryId = Guid.Parse("b2f08f2b-9b5f-41a0-bc86-bb1131a6e201");
    internal static readonly Guid PhonesCategoryId = Guid.Parse("b2f08f2b-9b5f-41a0-bc86-bb1131a6e202");
    internal static readonly Guid AccessoriesCategoryId = Guid.Parse("b2f08f2b-9b5f-41a0-bc86-bb1131a6e203");
    internal static readonly Guid ClearanceCategoryId = Guid.Parse("b2f08f2b-9b5f-41a0-bc86-bb1131a6e204");

    internal static readonly Guid WirelessMouseProductId = Guid.Parse("c3f08f2b-9b5f-41a0-bc86-bb1131a6e301");
    internal static readonly Guid HeadphonesProductId = Guid.Parse("c3f08f2b-9b5f-41a0-bc86-bb1131a6e302");
    internal static readonly Guid ArchivedKeyboardProductId = Guid.Parse("c3f08f2b-9b5f-41a0-bc86-bb1131a6e303");
    internal static readonly Guid LegacyChargerProductId = Guid.Parse("c3f08f2b-9b5f-41a0-bc86-bb1131a6e304");
    internal static readonly Guid ClearanceCableProductId = Guid.Parse("c3f08f2b-9b5f-41a0-bc86-bb1131a6e305");

    internal static void ApplyCatalogSeedData(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>().HasData(
            new
            {
                Id = TechGearBrandId,
                Name = "TechGear",
                Description = "Everyday consumer electronics and accessories.",
                IsActive = true,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = SoundCoreBrandId,
                Name = "SoundCore",
                Description = "Audio products for work and travel.",
                IsActive = true,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = LegacyBrandId,
                Name = "LegacyLine",
                Description = "Inactive supplier used for unavailable product scenarios.",
                IsActive = false,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            });

        modelBuilder.Entity<Category>().HasData(
            new
            {
                Id = ElectronicsCategoryId,
                Name = "Electronics",
                Description = "Electronic devices and peripherals.",
                ParentCategoryId = (Guid?)null,
                IsActive = true,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = PhonesCategoryId,
                Name = "Phones",
                Description = "Smartphones and mobile devices.",
                ParentCategoryId = (Guid?)ElectronicsCategoryId,
                IsActive = true,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = AccessoriesCategoryId,
                Name = "Accessories",
                Description = "Computer and mobile accessories.",
                ParentCategoryId = (Guid?)ElectronicsCategoryId,
                IsActive = true,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = ClearanceCategoryId,
                Name = "Clearance",
                Description = "Inactive category used for unavailable product scenarios.",
                ParentCategoryId = (Guid?)null,
                IsActive = false,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            });

        modelBuilder.Entity<Product>().HasData(
            new
            {
                Id = WirelessMouseProductId,
                Name = "Wireless Mouse",
                Description = "Compact wireless mouse with USB receiver.",
                Price = 799.90m,
                BrandId = TechGearBrandId,
                CategoryId = AccessoriesCategoryId,
                Status = ProductStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = HeadphonesProductId,
                Name = "Noise Cancelling Headphones",
                Description = "Over-ear Bluetooth headphones with active noise cancellation.",
                Price = 3499.00m,
                BrandId = SoundCoreBrandId,
                CategoryId = AccessoriesCategoryId,
                Status = ProductStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = ArchivedKeyboardProductId,
                Name = "Mechanical Keyboard",
                Description = "Archived product used to verify non-purchasable product status.",
                Price = 2199.00m,
                BrandId = TechGearBrandId,
                CategoryId = AccessoriesCategoryId,
                Status = ProductStatus.Archived,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = LegacyChargerProductId,
                Name = "Legacy Fast Charger",
                Description = "Active product under an inactive brand.",
                Price = 499.00m,
                BrandId = LegacyBrandId,
                CategoryId = AccessoriesCategoryId,
                Status = ProductStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = ClearanceCableProductId,
                Name = "Clearance USB-C Cable",
                Description = "Active product under an inactive category.",
                Price = 149.90m,
                BrandId = TechGearBrandId,
                CategoryId = ClearanceCategoryId,
                Status = ProductStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            });

        modelBuilder.Entity<ProductVariant>().HasData(
            new
            {
                Id = Guid.Parse("d4f08f2b-9b5f-41a0-bc86-bb1131a6e401"),
                ProductId = WirelessMouseProductId,
                Name = "Black",
                Sku = "WM-BLK",
                Status = VariantStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("d4f08f2b-9b5f-41a0-bc86-bb1131a6e402"),
                ProductId = WirelessMouseProductId,
                Name = "White",
                Sku = "WM-WHT",
                Status = VariantStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("d4f08f2b-9b5f-41a0-bc86-bb1131a6e403"),
                ProductId = HeadphonesProductId,
                Name = "Black",
                Sku = "NCH-BLK",
                Status = VariantStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("d4f08f2b-9b5f-41a0-bc86-bb1131a6e404"),
                ProductId = HeadphonesProductId,
                Name = "Silver",
                Sku = "NCH-SLV",
                Status = VariantStatus.Inactive,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("d4f08f2b-9b5f-41a0-bc86-bb1131a6e405"),
                ProductId = ArchivedKeyboardProductId,
                Name = "TR Layout",
                Sku = "MK-TR",
                Status = VariantStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("d4f08f2b-9b5f-41a0-bc86-bb1131a6e406"),
                ProductId = LegacyChargerProductId,
                Name = "65W",
                Sku = "LFC-65W",
                Status = VariantStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("d4f08f2b-9b5f-41a0-bc86-bb1131a6e407"),
                ProductId = ClearanceCableProductId,
                Name = "1m",
                Sku = "CUC-1M",
                Status = VariantStatus.Active,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            });

        modelBuilder.Entity<ProductImage>().HasData(
            new
            {
                Id = Guid.Parse("e5f08f2b-9b5f-41a0-bc86-bb1131a6e501"),
                ProductId = WirelessMouseProductId,
                ImageUrl = "https://cdn.marketplace.local/catalog/wireless-mouse-black.jpg",
                AltText = "Black wireless mouse",
                SortOrder = 0,
                IsPrimary = true,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("e5f08f2b-9b5f-41a0-bc86-bb1131a6e502"),
                ProductId = HeadphonesProductId,
                ImageUrl = "https://cdn.marketplace.local/catalog/noise-cancelling-headphones.jpg",
                AltText = "Noise cancelling headphones",
                SortOrder = 0,
                IsPrimary = true,
                CreatedAtUtc = SeedCreatedAtUtc,
                CreatedBy = "catalog-seed",
                IsDeleted = false
            });
    }
}
