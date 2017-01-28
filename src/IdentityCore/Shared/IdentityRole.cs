using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace IdentityCore.Shared
{
    public class IdentityRole
    {
        public Guid Id
        {
            get
            {
                return RoleId;
            }
        }
        public Guid RoleId { get; set; }
        public string Name { get; set; }

        public IdentityRole()
        {
        }
    }
}
