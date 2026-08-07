using ExpenseApp.Enums;

namespace ExpenseApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }

        // Employee ka manager kaun hai (self-referencing FK)
        public int? ManagerId { get; set; }
        public User? Manager { get; set; }

        // Navigation: is manager ke neeche kaun kaun employees hain
        public ICollection<User> Employees { get; set; } = new List<User>();

        // Navigation: is user ke expense forms
        public ICollection<ExpenseForm> ExpenseForms { get; set; } = new List<ExpenseForm>();
    }
}