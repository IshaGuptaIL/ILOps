using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Common.Login
{
    public interface ILogin
    {

        Task<ApiResposne> Login(LoginBO login);

    }
}
