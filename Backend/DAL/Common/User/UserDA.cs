using DAL.Common.Login;
using DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using userRole = DAL.Models.userRole;

namespace DAL.Common.User
{
    public class UserDA : IUser
    {
        public readonly AppDBContext _dbContext;

        public UserDA(AppDBContext context)
        {
            _dbContext = context;
        }

        public async Task<ApiResposne> GetUsers(int page, int pageSize)
        {
            var response = new ApiResposne();

            try
            {
                var query = _dbContext.usermaster
                    .Where(x => x.IsActive);

                var totalCount = await query.CountAsync();

                var users = await query
                    .OrderByDescending(x => x.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        x.Id,
                        x.FullName,
                        x.Email,
                        x.ContactNumber,
                        x.UserRoleId,
                        RoleName = _dbContext.userRole
                            .Where(r => r.Id == x.UserRoleId)
                            .Select(r => r.Name)
                            .FirstOrDefault(),
                        x.IsActive,
                        x.CreatedDate
                    })
                    .ToListAsync();

                response.Success = true;
                response.Message = "Users loaded successfully";
                response.Result = users;
                response.Count = totalCount;
            }
            catch
            {
                response.Success = false;
                response.Message = "Failed to load users";
            }

            return response;
        }

        public async Task<ApiResposne> GetUserById(int id)
        {
            var response = new ApiResposne();

            try
            {
                var user = await _dbContext.usermaster
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.FullName,
                        x.Email,
                        x.ContactNumber,
                        x.Address,
                        x.State,
                        x.ZipCode,
                        x.Country,
                        x.City,
                        x.UserRoleId,
                        x.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    return response;
                }

                response.Success = true;
                response.Result = user;
            }
            catch
            {
                response.Success = false;
                response.Message = "Error fetching user";
            }

            return response;
        }

        public async Task<ApiResposne> CreateUser(UserModelBO model)
        {
            var response = new ApiResposne();

            try
            {
                var user = new usermaster
                {
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    ContactNumber = model.ContactNumber,
                    Address = model.Address,
                    State = model.State,
                    ZipCode = model.ZipCode,
                    Country = model.Country,
                    City = model.City,
                    Password = model.Password, // ⚠️ hash later
                    UserRoleId = model.UserRoleId,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                _dbContext.usermaster.Add(user);
                await _dbContext.SaveChangesAsync();

                response.Success = true;
                response.Message = "User created successfully";
                response.Result = new { UserId = user.Id };
            }
            catch
            {
                response.Success = false;
                response.Message = "Failed to create user";
            }

            return response;
        }
        public async Task<ApiResposne> UpdateUser(UserModelBO model)
        {
            var response = new ApiResposne();

            try
            {
                var user = await _dbContext.usermaster
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    return response;
                }

                user.FullName = model.FullName.Trim();
                user.Email = model.Email.Trim();
                user.ContactNumber = model.ContactNumber;
                user.Address = model.Address;
                user.State = model.State;
                user.ZipCode = model.ZipCode;
                user.Country = model.Country;
                user.City = model.City;
                user.UserRoleId = model.UserRoleId;
                user.IsActive = model.IsActive;

                await _dbContext.SaveChangesAsync();

                response.Success = true;
                response.Message = "User updated successfully";
            }
            catch
            {
                response.Success = false;
                response.Message = "Failed to update user";
            }

            return response;
        }

        public async Task<ApiResposne> DeleteUser(int id)
        {
            var response = new ApiResposne();

            try
            {
                var user = await _dbContext.usermaster
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    return response;
                }

                user.IsActive = false;
                await _dbContext.SaveChangesAsync();

                response.Success = true;
                response.Message = "User deactivated successfully";
            }
            catch
            {
                response.Success = false;
                response.Message = "Failed to delete user";
            }

            return response;
        }


        public async Task<ApiResposne> GetUserRoles()
        {
            var response = new ApiResposne();

            try
            {
                var roles = await _dbContext.userRole
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name
                    })
                    .ToListAsync();

                response.Success = true;
                response.Message = "Roles loaded successfully";
                response.Result = roles;
                response.Count = roles.Count;
            }
            catch
            {
                response.Success = false;
                response.Message = "Failed to load roles";
            }

            return response;
        }



        // ROLE 


        public async Task<ApiResposne> GetRoles()
        {
            var response = new ApiResposne();
            try
            {
                var roles = await _dbContext.userRole
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                response.Success = true;
                response.Result = roles;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ApiResposne> CreateRole(CreateRoleDto dto)
        {
            var response = new ApiResposne();

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    response.Success = false;
                    response.Message = "Role name required";
                    return response;
                }

                bool exists = await _dbContext.userRole
                    .AnyAsync(x => x.Name.ToLower() == dto.Name.Trim().ToLower());

                if (exists)
                {
                    response.Success = false;
                    response.Message = "Role already exists";
                    return response;
                }

                var role = new userRole
                {
                    Name = dto.Name.Trim(),
                    IsActive = true
                };

                _dbContext.userRole.Add(role);
                await _dbContext.SaveChangesAsync();

                response.Success = true;
                response.Message = "Role created!";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ApiResposne> UpdateRole(UpdateRoleDto dto)
        {
            var response = new ApiResposne();

            try
            {
                var role = await _dbContext.userRole.FindAsync(dto.Id);
                if (role == null)
                {
                    response.Success = false;
                    response.Message = "Role not found";
                    return response;
                }

                bool exists = await _dbContext.userRole
                    .AnyAsync(x => x.Id != dto.Id &&
                                  x.Name.ToLower() == dto.Name.Trim().ToLower());

                if (exists)
                {
                    response.Success = false;
                    response.Message = "Role name already exists";
                    return response;
                }

                role.Name = dto.Name.Trim();
                await _dbContext.SaveChangesAsync();

                response.Success = true;
                response.Message = "Role updated!";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ApiResposne> ToggleRole(short id)
        {
            var response = new ApiResposne();

            try
            {
                var role = await _dbContext.userRole.FindAsync(id);
                if (role == null)
                {
                    response.Success = false;
                    response.Message = "Role not found";
                    return response;
                }

                role.IsActive = !role.IsActive;
                await _dbContext.SaveChangesAsync();

                response.Success = true;
                response.Message = role.IsActive
                    ? "Role activated!"
                    : "Role deactivated!";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }




        // ROLE PERMISSIONS 



        

        public async Task<ApiResposne> GetMenus()
        {
            var response = new ApiResposne();
            try
            {
                var menus = await _dbContext.usermastermenus
                    .Select(m => new
                    {
                        m.Id,
                        m.MenuName,
                        m.Controller,
                        m.ParentId,
                        m.Icon
                    })
                    .ToListAsync();

                response.Success = true;
                response.Result = menus;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ApiResposne> GetRolePermissions(short roleId)
        {
            var response = new ApiResposne();
            try
            {
                var permittedMenus = await _dbContext.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.MenuId)
                    .ToListAsync();

                response.Success = true;
                response.Result = permittedMenus;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ApiResposne> SaveRolePermissions( SaveRolePermissionBO model)
        {
            var response = new ApiResposne();
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Purane permissions delete karein - model.RoleId use karein
                var existing = _dbContext.RolePermissions.Where(rp => rp.RoleId == model.RoleId);
                _dbContext.RolePermissions.RemoveRange(existing);

                // 2. Naye permissions add karein - model.SelectedMenus use karein
                if (model.SelectedMenus != null && model.SelectedMenus.Count > 0)
                {
                    var newPermissions = model.SelectedMenus.Select(mId => new RolePermissions
                    {
                        RoleId = model.RoleId,
                        MenuId = mId
                    });
                    await _dbContext.RolePermissions.AddRangeAsync(newPermissions);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                response.Success = true;
                response.Message = $" {model.SelectedMenus?.Count ?? 0} permissions updated.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<ApiResposne> GetUserMenuPermissions(int? roleId)
        {
            var response = new ApiResposne();
            if (!roleId.HasValue)
            {
                response.Success = false;
                response.Result = new List<object>();
                return response;
            }

            try
            {
                var menus = await (from um in _dbContext.usermastermenus
                                   join rp in _dbContext.RolePermissions
                                   on um.Id equals rp.MenuId
                                   where rp.RoleId == roleId
                                   orderby um.IndexId ?? (short)0
                                   select new
                                   {
                                       Id = um.Id,
                                       MenuName = um.MenuName,
                                       Icon = um.Icon ?? "bi-house-door",
                                       Controller = um.Controller,
                                       ParentId = um.ParentId,
                                       MenuUrl=um.MenuUrl,
                                       IndexId = um.IndexId ?? (short)0
                                   }).ToListAsync();

                response.Success = true;
                response.Result = menus;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }





        public async Task<ApiResposne> GetActiveRoles()
        {
            var response = new ApiResposne();
            try
            {
                var roles = await _dbContext.userRole
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                response.Success = true;
                response.Result = roles;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }


    }
}



