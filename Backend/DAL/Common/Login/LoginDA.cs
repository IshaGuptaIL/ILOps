using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Common.Login
{
    public class LoginDA : ILogin
    {
        public readonly AppDBContext _dbContext;

        public LoginDA(AppDBContext context)
        {
            _dbContext = context;
        }

        #region Login

        /// <summary>
        /// Authenticates user using email and password
        /// </summary>
        public async Task<ApiResposne> Login(LoginBO login)
        {
            var response = new ApiResposne();

            try
            {
                var user = await _dbContext.usermaster
                    .FirstOrDefaultAsync(x =>
                        x.Email == login.Email &&
                        x.Password == login.Password &&
                        x.IsActive);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "Invalid email or password";
                    return response;
                }

                response.Success = true;
                response.Message = "Login successful";
                response.Result = new
                {
                    UserId = (short)user.Id,
                    Email = user.Email,
                    UserName = user.Email,
                    UserRoleId=user.UserRoleId
                };
            }
            catch
            {
                response.Success = false;
                response.Message = "Something went wrong during login";
            }

            return response;
        }

        #endregion

        // ================= USER ROLE MODULE START =================

        /// <summary>
        /// Get all user roles
        /// </summary>
        public async Task<ApiResposne> GetRoles()
        {
            var response = new ApiResposne();

            try
            {
                var roles = await _dbContext.userRole
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.IsActive
                    })
                    .ToListAsync();

                response.Success = true;
                response.Result = roles;
            }
            catch
            {
                response.Success = false;
                response.Message = "Failed to fetch roles";
            }

            return response;
        }

        /// <summary>
        /// Add or Update user role
        /// If role.Id == 0, adds new role
        /// If role.Id > 0, updates existing role
        /// </summary>
        public async Task<ApiResposne> UpsertRole(userRole role)
        {
            var response = new ApiResposne();

            if (string.IsNullOrWhiteSpace(role.Name))
            {
                response.Success = false;
                response.Message = "Role name required";
                return response;
            }

            try
            {
                if (role.Id == 0)
                {
                    var entity = new 
                    {
                        Name = role.Name,
                        IsActive = true
                    };

                    //_dbContext.userRole.Add(entity);
                    response.Message = "Role added successfully";
                }
                else
                {
                    // Update existing role
                    var entity = await _dbContext.userRole
                        .FirstOrDefaultAsync(x => x.Id == role.Id);

                    if (entity == null)
                    {
                        response.Success = false;
                        response.Message = "Role not found";
                        return response;
                    }

                    entity.Name = role.Name;
                    response.Message = "Role updated successfully";
                }

                await _dbContext.SaveChangesAsync();
                response.Success = true;
            }
            catch
            {
                response.Success = false;
                response.Message = "Failed to save role";
            }

            return response;
        }

        /// <summary>
        /// Activate / Deactivate role
        /// </summary>
        public async Task<ApiResposne> ToggleRoleStatus(int roleId)
        {
            var response = new ApiResposne();

            try
            {
                var role = await _dbContext.userRole
                    .FirstOrDefaultAsync(x => x.Id == roleId);

                if (role == null)
                {
                    response.Success = false;
                    response.Message = "Role not found";
                    return response;
                }

                role.IsActive = !role.IsActive;
                await _dbContext.SaveChangesAsync();

                response.Success = true;
                response.Message = "Role status updated";
            }
            catch
            {
                response.Success = false;
                response.Message = "Failed to update role status";
            }

            return response;
        }

        // ================= USER ROLE MODULE END =================
    }
}
