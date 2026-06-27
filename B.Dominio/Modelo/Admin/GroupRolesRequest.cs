using System.Collections.Generic;

namespace Modelo.Admin
{
    public class GroupRolesRequest
    {
        public int grupoid { get; set; }
        public List<int> roles { get; set; } = new List<int>();
    }
}
