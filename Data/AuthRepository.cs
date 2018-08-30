using System;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    /// <summary>
    /// Implementation of the IAuthRepository Interface.
    /// </summary>
    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext _context;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context">data context to access</param>
        public AuthRepository(DataContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// asynchronously authenticates the user
        /// </summary>
        /// <param name="username">user attempting to login</param>
        /// <param name="password">password for the user</param>
        /// <returns>an async operation returning a User model</returns>
        public async Task<User> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);

            if(user == null){
                return null;
            }
            if(!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt)){
                return null;
            } 
            return user;
        }

        /// <summary>
        /// determines if the password and salt are the same by the hash they create.
        /// </summary>
        /// <param name="password">password from the user</param>
        /// <param name="passwordHash">hash from the db</param>
        /// <param name="passwordSalt">salt from the db</param>
        /// <returns>if the password+salt => hash = hash from db</returns>
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using(var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for(int i = 0; i< computedHash.Length; i++){
                    if (computedHash[i] != passwordHash[i])
                        return false;
                }
                return true;
            };
        }


        /// <summary>
        /// Adds a user to the system. must be a unique username.
        /// </summary>
        /// <param name="user">User entity to add to the data context</param>
        /// <param name="password">plain text password from user</param>
        /// <returns>an async operation returning the user added to the data context</returns>
        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using(var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            };
        }

        /// <summary>
        /// Checks to see if this username has already been added to the data context
        /// </summary>
        /// <param name="username">username to check</param>
        /// <returns>an async operation returning bool, true = user exists</returns>
        public async Task<bool> UserExists(string username)
        {
            if(await _context.Users.AnyAsync(x => x.Username == username))
                return true;
            return false;
        }
    }
}