using ExpenseApp.Data.Rows;
using ExpenseApp.DTOs;
using ExpenseApp.Enums;

namespace ExpenseApp.Services
{
    // Shared by ExpenseController and AdminController — both call stored
    // procedures that return a "forms" result set plus an "items" result
    // set, and both need those combined into the same response DTO shape.
    public static class ExpenseMapper
    {
        public static ExpenseFormResponseDto ToDto(FormRow form, IEnumerable<ItemRow> items)
        {
            var itemList = items.ToList();
            return new ExpenseFormResponseDto
            {
                Id = form.Id,
                EmployeeName = form.EmployeeName,
                Currency = form.Currency,
                Status = ((ExpenseStatus)form.Status).ToString(),
                TotalAmount = itemList.Sum(i => i.Amount),
                CreatedDate = form.CreatedDate,
                RejectionReason = form.RejectionReason,
                Items = itemList.Select(i => new ExpenseItemDto
                {
                    ExpenseDate = i.ExpenseDate,
                    Purpose = i.Purpose,
                    Category = i.Category,
                    Amount = i.Amount
                }).ToList()
            };
        }

        public static List<ExpenseFormResponseDto> ToDtoList(IEnumerable<FormRow> forms, IEnumerable<ItemRow> items)
        {
            var itemsByForm = items.GroupBy(i => i.ExpenseFormId).ToDictionary(g => g.Key, g => (IEnumerable<ItemRow>)g);
            return forms
                .Select(f => ToDto(f, itemsByForm.TryGetValue(f.Id, out var list) ? list : Enumerable.Empty<ItemRow>()))
                .ToList();
        }
    }
}
