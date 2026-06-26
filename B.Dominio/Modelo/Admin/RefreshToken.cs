using System;
using System.Collections.Generic;

namespace Modelo.Admin
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime Expiry_Date { get; set; }
    }
}
