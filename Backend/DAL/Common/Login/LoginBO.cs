using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Common.Login
{
    public class LoginBO
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class ApiResposne
    {
        public string Message { get; set; }
        public object Result { get; set; }
        public object Activity { get; set; }
        public int Count { get; set; }
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        
    }
    public class userRole
    {
        public short Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

}
