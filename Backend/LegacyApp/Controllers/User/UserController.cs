using DAL.Common.Login;
using DAL.Common.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegacyApp.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUser _user;

        public UserController(IUser user)
        {
            _user = user;
        }


        [HttpGet("GetUsers")]
        public async Task<ApiResposne> GetUsers(int page, int pageSize)
        {
            return await _user.GetUsers(page, pageSize);
        }


        [HttpGet("GetUserById")]
        public async Task<ApiResposne> GetUserById(int id)
        {
            return await _user.GetUserById(id);
        }


        [HttpPost("CreateUser")]
        public async Task<ApiResposne> CreateUser(UserModelBO model)
        {
            return await _user.CreateUser(model);
        }

        [HttpPost("UpdateUser")]
        public async Task<ApiResposne> UpdateUser(UserModelBO model)
        {
            return await _user.UpdateUser(model);
        }

        [HttpDelete("DeleteUser")]
        public async Task<ApiResposne> DeleteUser(int id)
        {
            return await _user.DeleteUser(id);
        }

        [HttpGet("GetUserRoles")]
        public async Task<ApiResposne> GetUserRoles()
        {
            return await _user.GetUserRoles();
        }



        // ROLE PERMISSIONS
        [HttpGet("GetRoles")]
        public async Task<ApiResposne> GetRoles()
        {
            return await _user.GetRoles();
        }

        [HttpGet("GetMenus")]
        public async Task<ApiResposne> GetMenus()
        {
            return await _user.GetMenus();
        }

        [HttpGet("GetRolePermissions")]
        public async Task<ApiResposne> GetRolePermissions(short roleId)
        {
            return await _user.GetRolePermissions(roleId);
        }

        [HttpPost("SaveRolePermissions")]
        public async Task<ApiResposne> SavePermissions([FromBody] SaveRolePermissionBO model)
        {
            return await _user.SaveRolePermissions(model);
        }
        [HttpGet("GetUserMenuPermissions")]
        public async Task<ApiResposne> GetUserMenuPermissions(int? roleId)
        {
            return await _user.GetUserMenuPermissions(roleId);
        }

        [HttpGet("GetActiveRoles")]
        public async Task<ApiResposne> GetActiveRoles()
        {
            return await _user.GetActiveRoles();
        }



        // ROLE 
        [HttpPost("AddUserRole")]
        public async Task<ApiResposne> Create(CreateRoleDto dto)
       => await _user.CreateRole(dto);

        [HttpPost("UpdateUserRole")]
        public async Task<ApiResposne> Update(UpdateRoleDto dto)
            => await _user.UpdateRole(dto);

        [HttpPost("GetByIDUserRole")]
        public async Task<ApiResposne> Toggle(short id)
            => await _user.ToggleRole(id);




    }


}
