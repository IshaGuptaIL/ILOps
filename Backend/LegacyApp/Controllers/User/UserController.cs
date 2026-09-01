using DAL.Common.Login;
using DAL.Common.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.User
{
    /// <summary>
    /// Manages system user accounts, role definitions, and module-level permission assignments.
    /// Provides user lifecycle operations (CRUD), role management, and menu authorization control.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUser _user;

        public UserController(IUser user)
        {
            _user = user;
        }

        /// <summary>
        /// Retrieves a paginated list of all system users with their active status and roles.
        /// Used by the User Management dashboard table.
        /// </summary>
        [HttpGet("GetUsers")]
        public async Task<ApiResposne> GetUsers(int page, int pageSize)
        {
            return await _user.GetUsers(page, pageSize);
        }

        /// <summary>
        /// Retrieves specific user profile details by user identifier.
        /// Used to populate the edit user modal with current details.
        /// </summary>
        [HttpGet("GetUserById")]
        public async Task<ApiResposne> GetUserById(int id)
        {
            return await _user.GetUserById(id);
        }

        /// <summary>
        /// Creates a new user account with specified credentials and role assignment.
        /// Used by administrators to register new team members.
        /// </summary>
        [HttpPost("CreateUser")]
        public async Task<ApiResposne> CreateUser(UserModelBO model)
        {
            return await _user.CreateUser(model);
        }

        /// <summary>
        /// Updates an existing user's name, email, role, or active status.
        /// Saves updated user profile details in the database.
        /// </summary>
        [HttpPost("UpdateUser")]
        public async Task<ApiResposne> UpdateUser(UserModelBO model)
        {
            return await _user.UpdateUser(model);
        }

        /// <summary>
        /// Deletes or deactivates a user account by their user ID.
        /// Revokes application access for the specified user.
        /// </summary>
        [HttpDelete("DeleteUser")]
        public async Task<ApiResposne> DeleteUser(int id)
        {
            return await _user.DeleteUser(id);
        }

        /// <summary>
        /// Retrieves the list of available user roles for user creation and assignment.
        /// Populates role dropdowns on user management forms.
        /// </summary>
        [HttpGet("GetUserRoles")]
        public async Task<ApiResposne> GetUserRoles()
        {
            return await _user.GetUserRoles();
        }

        // ROLE PERMISSIONS
        /// <summary>
        /// Retrieves all configured roles in the system for permission management.
        /// Displays role selection on the permissions configuration matrix.
        /// </summary>
        [HttpGet("GetRoles")]
        public async Task<ApiResposne> GetRoles()
        {
            return await _user.GetRoles();
        }

        /// <summary>
        /// Retrieves all functional application menus and sub-menus.
        /// Used to build the navigation hierarchy and permissions matrix.
        /// </summary>
        [HttpGet("GetMenus")]
        public async Task<ApiResposne> GetMenus()
        {
            return await _user.GetMenus();
        }

        /// <summary>
        /// Retrieves menu view/edit permissions assigned to a specific role ID.
        /// Populates checked permissions on the role permissions screen.
        /// </summary>
        [HttpGet("GetRolePermissions")]
        public async Task<ApiResposne> GetRolePermissions(short roleId)
        {
            return await _user.GetRolePermissions(roleId);
        }

        /// <summary>
        /// Saves or updates authorized menu permissions for a specific role.
        /// Modifies access rights across the application's navigation structure.
        /// </summary>
        [HttpPost("SaveRolePermissions")]
        public async Task<ApiResposne> SavePermissions([FromBody] SaveRolePermissionBO model)
        {
            return await _user.SaveRolePermissions(model);
        }

        /// <summary>
        /// Retrieves the authorized menu structure for a logged-in user's role.
        /// Used by the frontend sidebar to render only permitted navigation routes.
        /// </summary>
        [HttpGet("GetUserMenuPermissions")]
        public async Task<ApiResposne> GetUserMenuPermissions(int? roleId)
        {
            return await _user.GetUserMenuPermissions(roleId);
        }

        /// <summary>
        /// Retrieves all active system roles eligible for assignment.
        /// Filters out deactivated roles for user creation workflows.
        /// </summary>
        [HttpGet("GetActiveRoles")]
        public async Task<ApiResposne> GetActiveRoles()
        {
            return await _user.GetActiveRoles();
        }

        // ROLE 
        /// <summary>
        /// Creates a new custom role with assigned name and description.
        /// Adds a new security group to the role permissions system.
        /// </summary>
        [HttpPost("AddUserRole")]
        public async Task<ApiResposne> Create(CreateRoleDto dto)
            => await _user.CreateRole(dto);

        /// <summary>
        /// Updates an existing role's title, description, or attributes.
        /// Used for maintaining security role definitions.
        /// </summary>
        [HttpPost("UpdateUserRole")]
        public async Task<ApiResposne> Update(UpdateRoleDto dto)
            => await _user.UpdateRole(dto);

        /// <summary>
        /// Toggles the active/inactive status of a specific security role.
        /// Deactivates or re-enables a role without deleting historical associations.
        /// </summary>
        [HttpPost("GetByIDUserRole")]
        public async Task<ApiResposne> Toggle(short id)
            => await _user.ToggleRole(id);
    }


}
