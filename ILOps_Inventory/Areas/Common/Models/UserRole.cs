using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ILOps_Inventory.Areas.Common.Models
{
    public class UserRole
    {
        public short Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }


    public class RolePermission
    {
        public int Id { get; set; }
        public short RoleId { get; set; }
        public int MenuId { get; set; }
        public DateTime CreatedDate { get; set; }

        public string RoleName { get; set; } = "";
        public string MenuName { get; set; } = "";
    }


    public class Menu
    {
        public short MenuId { get; set; }
        public string MenuName { get; set; } = "";
        public short? ParentMenuId { get; set; }
        public string Controller { get; set; }
    }


    public class UserModel
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        public string Password { get; set; } = "";

        [Required]
        public short UserRoleId { get; set; } 

        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        [Phone(ErrorMessage = "Invalid phone number"), MaxLength(20)]
        public string? ContactNumber { get; set; } = "";

        [MaxLength(200)]
        public string? Address { get; set; } = "";

        [MaxLength(100)]
        public string? State { get; set; } = "";

    

        [MaxLength(20)]
        public string? ZipCode { get; set; } = "";

        [MaxLength(100)]
        public string? Country { get; set; } = "";

        [MaxLength(100)]
        public string? City { get; set; } = "";

        [NotMapped]
        public string? RoleName { get; set; }
    }

    public class UserIndexViewModel  
    {
        public List<UserModel> Users { get; set; } = new();
        public List<UserRole> Roles { get; set; } = new(); // ✅ Corrected
        public int TotalUsers { get; set; } = 0;     // ✅ New
        public int CurrentPage { get; set; } = 1;    // ✅ New
        public int PageSize { get; set; } = 10;      // ✅ New
        public int TotalPages { get; set; } = 1;
    }
}
