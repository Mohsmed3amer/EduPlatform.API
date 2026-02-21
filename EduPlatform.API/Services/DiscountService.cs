using EduPlatform.API.Data;
using EduPlatform.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Services
{
    public class DiscountService
    {
        private readonly ApplicationDbContext _context;

        public DiscountService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Discount> CreateDiscountAsync(Discount discount)
        {
            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();
            return discount;
        }

        public async Task<bool> ValidateDiscountAsync(string code, decimal amount, int? CourseId = null)
        {
            var discount = await _context.Discounts
                .Include(d => d.CourseDiscounts)
                .ThenInclude(cd => cd.Courses)
                .FirstOrDefaultAsync(d => d.Code == code && d.IsActive);

            if (discount == null) return false;

            var now = DateTime.UtcNow;
            if (discount.StartDate > now || discount.EndDate < now) return false;

            if (discount.MaxUses.HasValue && discount.UsedCount >= discount.MaxUses.Value) return false;

            if (discount.MinAmount.HasValue && amount < discount.MinAmount.Value) return false;

            if (CourseId.HasValue && discount.CourseDiscounts.Any())
            {
                var CourseExists = discount.CourseDiscounts.Any(cd => cd.CourseId == CourseId.Value);
                if (!CourseExists) return false;
            }

            return true;
        }

        public async Task ApplyDiscountAsync(int discountId)
        {
            var discount = await _context.Discounts.FindAsync(discountId);
            if (discount != null)
            {
                discount.UsedCount++;
                await _context.SaveChangesAsync();
            }
        }
    }
}