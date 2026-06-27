using System.Collections.Generic;

namespace Modelo.Admin
{
    public class RoleMenusRequest
    {
        public int rolid { get; set; }
        public List<int> menus { get; set; } = new List<int>();
    }
}
