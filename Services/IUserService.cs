using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GSoftPosNew.Services
{
    public interface IUserService
    {
        Task<User> AuthenticateAsync(string username, string password);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> GetUserByIdAsync(int id);
        Task<int> CreateUserAsync(User user, string password);
        Task<bool> UpdateUserAsync(User user, string password = null);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !_passwordHasher.VerifyPassword(password, user.Password))
            {
                return null;
            }

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<int> CreateUserAsync(User user, string password)
        {
            //if (password.Length < 8)
            //    throw new Exception("Password must be at least 8 characters");

            //if (!password.Any(char.IsDigit))
            //    throw new Exception("Password must contain at least one digit");

            //if (!password.Any(char.IsLower))
            //    throw new Exception("Password must contain at least one lowercase letter");

            //if (!password.Any(char.IsUpper))
            //    throw new Exception("Password must contain at least one uppercase letter");

            //if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            //    throw new Exception("Password must contain at least one special character");

            if (await UsernameExistsAsync(user.Username))
                throw new Exception("Username is already taken");

            if (await EmailExistsAsync(user.Email))
                throw new Exception("Email is already registered");

            user.Password = _passwordHasher.HashPassword(password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user.Id;
        }

        public async Task<bool> UpdateUserAsync(User user, string password = null)
        {
            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
                return false;

            // Update fields as necessary
            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            // etc.

            if (!string.IsNullOrEmpty(password))
            {
                existingUser.Password = _passwordHasher.HashPassword(password);
            }

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }

}
