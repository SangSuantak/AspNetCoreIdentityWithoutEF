using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace IdentityCore.Shared
{
    public class IdentityConnection
    {
        public string ConnectionString { get; set; }

        public IdentityConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }        
    }
}
