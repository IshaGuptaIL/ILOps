using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DAL.Common.User
{
    public class UserBO
    {
    }
    public class MenuItemModel
    {
        public int Id { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-dot";
        public string? Controller { get; set; }
        public int ParentId { get; set; }
    }

    public class UserModelBO
    {
        [Key]
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

    public class RoleRequest
    {
        public string Name { get; set; } = string.Empty;
    }
    public class CreateRoleDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateRoleDto
    {
        public short Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class RoleUpdateRequest
    {
        public short Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class UserRoleBO
    {
        public short Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class RolePermissionSaveModel
    {
        public short RoleId { get; set; }
        public List<int> MenuIds { get; set; } = new();
    }

    public class RolePermissionBO
    {
        public int Id { get; set; }
        public short RoleId { get; set; }
        public int MenuId { get; set; }
        public DateTime CreatedDate { get; set; }

        public string RoleName { get; set; } = "";
        public string MenuName { get; set; } = "";
    }
    public class SaveRolePermissionBO
    {
        public short RoleId { get; set; }
        public List<int> SelectedMenus { get; set; }
    }

}
