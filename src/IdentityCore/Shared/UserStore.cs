using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace IdentityCore.Shared
{
    public class UserStore<TUser> :
        IUserStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserEmailStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserRoleStore<TUser>,
        IUserLockoutStore<TUser>,
        IUserPhoneNumberStore<TUser>,
        IUserTwoFactorStore<TUser>,
        IUserLoginStore<TUser>,
        IQueryableUserStore<TUser>        
        where TUser : IdentityUser
    {
        private string _connection;

        public UserStore(IConfigurationRoot config)
        {
            _connection = config.GetConnectionString("DefaultConnection");
        }

        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            //if (user.Audit == null)
            //    throw new ArgumentNullException("user.Audit");

            //ensure UserId
            //if (user.UserId == default(Guid))
            user.UserId = Convert.ToString(Guid.NewGuid());

            //conver to sql min date
            var sqlMinDate = new DateTimeOffset(1753, 1, 1, 0, 0, 0, TimeSpan.FromHours(0));
            if (user.LockoutEndDateUtc < sqlMinDate)
                user.LockoutEndDateUtc = sqlMinDate;

            using (var connection = new SqlConnection(_connection))
            {
                using (var cmd = new SqlCommand())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    cmd.Connection = connection;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.usp_iden_CreateUser";
                    cmd.Parameters.AddWithValue("@UserId", user.UserId);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@EmailConfirmed", user.EmailConfirmed);
                    cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    cmd.Parameters.AddWithValue("@SecurityStamp", user.SecurityStamp);
                    cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                    cmd.Parameters.AddWithValue("@PhoneNumberConfirmed", user.PhoneNumberConfirmed);
                    cmd.Parameters.AddWithValue("@TwoFactorEnabled", user.TwoFactorEnabled);
                    cmd.Parameters.AddWithValue("@LockoutEndDateUtc", (user.LockoutEndDateUtc ?? (DateTime.Now)).ToString("yyyyMMdd"));
                    cmd.Parameters.AddWithValue("@LockoutEnabled", user.LockoutEnabled);
                    cmd.Parameters.AddWithValue("@AccessFailedCount", user.AccessFailedCount);
                    cmd.Parameters.AddWithValue("@UserName", user.UserName);
                    cmd.Parameters.AddWithValue("@CreateBy", Guid.Empty);
                    cmd.Parameters.AddWithValue("@ModifyBy", Guid.Empty);

                    if (await cmd.ExecuteNonQueryAsync() > 0)
                    {
                        return IdentityResult.Success;
                    }
                    else
                    {
                        return IdentityResult.Failed();
                    }
                }
            }
        }

        public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            IdentityUser user = null;
            IdentityProfile profile = null;
            List<IdentityRole> roles = null;

            using (var connection = new SqlConnection(_connection))
            {
                using (var cmd = new SqlCommand())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    cmd.Connection = connection;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "usp_iden_FindUserById";
                    cmd.Parameters.AddWithValue("@USERID", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        int resultIndex = 0;

                        while (reader.HasRows)
                        {
                            if (resultIndex == 0)
                            {
                                user = new IdentityUser();
                            }
                            else if (resultIndex == 1)
                            {
                                profile = new IdentityProfile();
                            }
                            else
                            {
                                roles = new List<IdentityRole>();
                            }
                            while (reader.Read())
                            {
                                if (resultIndex == 0)
                                {
                                    user.UserId = Convert.ToString(reader["UserId"]);
                                    user.Email = Convert.ToString(reader["Email"]);
                                    user.EmailConfirmed = Convert.ToBoolean(reader["EmailConfirmed"]);
                                    user.PasswordHash = Convert.ToString(reader["PasswordHash"]);
                                    user.SecurityStamp = Convert.ToString(reader["SecurityStamp"]);
                                    user.PhoneNumber = Convert.ToString(reader["PhoneNumber"]);
                                    user.PhoneNumberConfirmed = Convert.ToBoolean(reader["PhoneNumberConfirmed"]);
                                    user.TwoFactorEnabled = Convert.ToBoolean(reader["TwoFactorEnabled"]);
                                    var lockoutDateTime = Convert.ToDateTime(reader["LockoutEndDateUtc"]);
                                    lockoutDateTime = DateTime.SpecifyKind(lockoutDateTime, DateTimeKind.Utc);
                                    user.LockoutEndDateUtc = lockoutDateTime;
                                    user.LockoutEnabled = Convert.ToBoolean(reader["LockoutEnabled"]);
                                    user.AccessFailedCount = Convert.ToInt32(reader["AccessFailedCount"]);
                                    user.UserName = Convert.ToString(reader["UserName"]);
                                    //user.Audit = new Audit(reader.GetGuid(reader.GetOrdinal("CreateBy")));
                                    //user.Audit.CreateDate = Convert.ToDateTime(reader["CreateDate"]);
                                    //user.Audit.ModifyBy = reader.GetGuid(reader.GetOrdinal("ModifyBy"));
                                    //user.Audit.ModifyDate = Convert.ToDateTime(reader["ModifyDate"]);
                                }
                                else if (resultIndex == 1)
                                {
                                    profile.UserId = reader.GetGuid(reader.GetOrdinal("UserId"));
                                    profile.FirstName = Convert.ToString(reader["FirstName"]);
                                    profile.MiddleName = Convert.ToString(reader["MiddleName"]);
                                    profile.LastName = Convert.ToString(reader["LastName"]);
                                }
                                else
                                {
                                    roles.Add(new IdentityRole
                                    {
                                        RoleId = reader.GetGuid(reader.GetOrdinal("RoleId")),
                                        Name = Convert.ToString(reader["Name"])
                                    });
                                }
                            }

                            resultIndex++;
                            reader.NextResult();
                        }

                        if (user != null)
                        {
                            user.Profile = profile;
                            //user.Roles = roles;
                        }
                    }
                    return Task.FromResult((TUser)user);
                }
            }
        }

        public Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            IdentityUser user = null;
            IdentityProfile profile = null;
            List<IdentityRole> roles = null;

            using (var connection = new SqlConnection(_connection))
            {
                using (var cmd = new SqlCommand())
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    cmd.Connection = connection;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "usp_iden_FindUserByName";
                    cmd.Parameters.AddWithValue("@USERNAME", normalizedUserName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        int resultIndex = 0;

                        while (reader.HasRows)
                        {
                            if (resultIndex == 0)
                            {
                                user = new IdentityUser();
                            }
                            else if (resultIndex == 1)
                            {
                                profile = new IdentityProfile();
                            }
                            else
                            {
                                roles = new List<IdentityRole>();
                            }
                            while (reader.Read())
                            {
                                if (resultIndex == 0)
                                {
                                    user.UserId = Convert.ToString(reader["UserId"]);
                                    user.Email = Convert.ToString(reader["Email"]);
                                    user.EmailConfirmed = Convert.ToBoolean(reader["EmailConfirmed"]);
                                    user.PasswordHash = Convert.ToString(reader["PasswordHash"]);
                                    user.SecurityStamp = Convert.ToString(reader["SecurityStamp"]);
                                    user.PhoneNumber = Convert.ToString(reader["PhoneNumber"]);
                                    user.PhoneNumberConfirmed = Convert.ToBoolean(reader["PhoneNumberConfirmed"]);
                                    user.TwoFactorEnabled = Convert.ToBoolean(reader["TwoFactorEnabled"]);
                                    var lockoutDateTime = Convert.ToDateTime(reader["LockoutEndDateUtc"]);
                                    lockoutDateTime = DateTime.SpecifyKind(lockoutDateTime, DateTimeKind.Utc);
                                    user.LockoutEndDateUtc = lockoutDateTime;
                                    user.LockoutEnabled = Convert.ToBoolean(reader["LockoutEnabled"]);
                                    user.AccessFailedCount = Convert.ToInt32(reader["AccessFailedCount"]);
                                    user.UserName = Convert.ToString(reader["UserName"]);
                                    //user.Audit = new Audit(reader.GetGuid(reader.GetOrdinal("CreateBy")));
                                    //user.Audit.CreateDate = Convert.ToDateTime(reader["CreateDate"]);
                                    //user.Audit.ModifyBy = reader.GetGuid(reader.GetOrdinal("ModifyBy"));
                                    //user.Audit.ModifyDate = Convert.ToDateTime(reader["ModifyDate"]);
                                }
                                else if (resultIndex == 1)
                                {
                                    profile.UserId = reader.GetGuid(reader.GetOrdinal("UserId"));
                                    profile.FirstName = Convert.ToString(reader["FirstName"]);
                                    profile.MiddleName = Convert.ToString(reader["MiddleName"]);
                                    profile.LastName = Convert.ToString(reader["LastName"]);
                                }
                                else
                                {
                                    roles.Add(new IdentityRole
                                    {
                                        RoleId = reader.GetGuid(reader.GetOrdinal("RoleId")),
                                        Name = Convert.ToString(reader["Name"])
                                    });
                                }
                            }

                            resultIndex++;
                            reader.NextResult();
                        }

                        if (user != null)
                        {
                            user.Profile = profile;
                            //user.Roles = roles;
                        }
                    }
                    return Task.FromResult((TUser)user);
                }
            }
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.UserName);
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.UserId);
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            
            return Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            //TODO

            return Task.FromResult(0);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.UserName = userName;

            return Task.FromResult(0);
        }

        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public IQueryable<TUser> Users
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~UserStore() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.PasswordHash = passwordHash;

            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            if(user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.Email = email;

            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.EmailConfirmed = confirmed;

            return Task.FromResult(0);
        }

        public Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            //TODO

            return Task.FromResult(user.Email);
        }

        public Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            //TODO

            return Task.FromResult(0);
        }

        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.SecurityStamp = stamp;

            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.SecurityStamp);
        }

        public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            //TO DO
            IList<string> str = new List<string>();
            return Task.FromResult(str);
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            //TO DO
            return Task.FromResult(true);
        }

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.LockoutEndDateUtc);
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            var sqlMinDate = new DateTimeOffset(1753, 1, 1, 0, 0, 0, TimeSpan.FromHours(0));

            if (lockoutEnd < sqlMinDate)
            {
                lockoutEnd = sqlMinDate;
            }

            user.LockoutEndDateUtc = lockoutEnd;

            return Task.FromResult(0);
        }

        public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.AccessFailedCount++;

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.AccessFailedCount = 0;

            return Task.FromResult(0);
        }

        public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.LockoutEnabled);
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.LockoutEnabled = enabled;

            return Task.FromResult(0);
        }

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.PhoneNumber = phoneNumber;

            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.PhoneNumberConfirmed = confirmed;

            return Task.FromResult(0);
        }

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            user.TwoFactorEnabled = enabled;

            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            return Task.FromResult(user.TwoFactorEnabled);
        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        #endregion


    }
}
