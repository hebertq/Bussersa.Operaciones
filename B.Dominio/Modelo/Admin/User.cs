using System;
using System.Collections.Generic;

namespace Modelo.Admin
{
    public  class User
    {
        public int id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string nombres { get; set; }
        public string apellidos { get; set; }
        public string nav_menu { get; set; } 
        public int grupoid { get; set; }

    }
    public class UserLogin
    {
        public string Email_Address { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public bool Validate { get; set; }
    }
}
