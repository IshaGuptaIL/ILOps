using DAL.Common.Login;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Common.User
{
    public interface IUser
    {
        Task<ApiResposne> GetUsers(int page, int pageSize);

       Task<ApiResposne> GetUserById(int id);
      Task<ApiResposne> CreateUser(UserModelBO model);

         Task<ApiResposne> UpdateUser(UserModelBO model);
       Task<ApiResposne> DeleteUser(int id);
        Task<ApiResposne> GetUserRoles();



        // ROLE


        Task<ApiResposne> GetRoles();
        Task<ApiResposne> CreateRole(CreateRoleDto dto);
        Task<ApiResposne> UpdateRole(UpdateRoleDto dto);
        Task<ApiResposne> ToggleRole(short id);





        // ROLE PERMISSIONS 
        Task<ApiResposne> GetMenus();
        Task<ApiResposne> GetRolePermissions(short roleId);
        Task<ApiResposne> SaveRolePermissions(short roleId, List<int> menuIds);
        Task<ApiResposne> GetUserMenuPermissions(int? roleId);


    }
}
