using BusinessLayer.DTOs;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly EVDbContext _context;

        public UserService(EVDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<UserManagementDTO> users, int totalCount)> GetAllUsersAsync(
            int page = 1,
            int pageSize = 50,
            UserRole? roleFilter = null,
            bool? isActiveFilter = null,
            string? searchTerm = null)
        {
            var query = _context.Users.AsQueryable();

            // Apply filters
            if (roleFilter.HasValue)
            {
                query = query.Where(u => u.Role == roleFilter.Value);
            }

            if (isActiveFilter.HasValue)
            {
                query = query.Where(u => u.IsActive == isActiveFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(search) ||
                    u.FullName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.Phone.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserManagementDTO
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    GoogleId = u.GoogleId
                })
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<(IEnumerable<UserManagementDTO> users, int totalCount)> GetAllUsersByRoleStringAsync(
            int page = 1,
            int pageSize = 50,
            string? roleFilter = null,
            bool? isActiveFilter = null,
            string? searchTerm = null)
        {
            UserRole? role = null;
            if (!string.IsNullOrEmpty(roleFilter) && Enum.TryParse<UserRole>(roleFilter, true, out var parsedRole))
            {
                role = parsedRole;
            }

            return await GetAllUsersAsync(page, pageSize, role, isActiveFilter, searchTerm);
        }

        public async Task<UserManagementDTO?> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            return new UserManagementDTO
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                GoogleId = user.GoogleId
            };
        }

        public async Task<UserManagementDTO?> UpdateUserRoleAsync(Guid userId, UserRole newRole, Guid adminId)
        {
            // Kiểm tra admin có tồn tại và là Admin không
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId);
            if (admin == null || admin.Role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Chỉ Admin mới có quyền thay đổi role của user");
            }

            // Lấy user cần cập nhật
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return null;

            // Kiểm tra: Không cho phép thay đổi role của Admin
            if (user.Role == UserRole.Admin)
            {
                throw new InvalidOperationException("Không thể thay đổi role của Admin");
            }

            // Validate role mới: Chỉ cho phép EVDriver hoặc CSStaff
            if (newRole != UserRole.EVDriver && newRole != UserRole.CSStaff)
            {
                throw new ArgumentException("Role mới phải là EVDriver hoặc CSStaff");
            }

            // Cập nhật role
            user.Role = newRole;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UserManagementDTO
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                GoogleId = user.GoogleId
            };
        }

        public async Task<UserManagementDTO?> UpdateUserStatusAsync(Guid userId, bool isActive)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return null;

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UserManagementDTO
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                GoogleId = user.GoogleId
            };
        }

        public async Task<IEnumerable<UserManagementDTO>> GetUsersByRoleAsync(UserRole? role)
        {
            var query = _context.Users.AsQueryable();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserManagementDTO
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    GoogleId = u.GoogleId
                })
                .ToListAsync();

            return users;
        }

        public async Task<Dictionary<UserRole, int>> GetUserRoleDistributionAsync()
        {
            var distribution = await _context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<UserRole, int>();
            foreach (var role in Enum.GetValues<UserRole>())
            {
                result[role] = distribution.FirstOrDefault(d => d.Role == role)?.Count ?? 0;
            }

            return result;
        }
    }
}

