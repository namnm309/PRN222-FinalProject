using BusinessLayer.DTOs;
using DataAccessLayer.Enums;

namespace BusinessLayer.Services
{
    public interface IUserService
    {
        Task<(IEnumerable<UserManagementDTO> users, int totalCount)> GetAllUsersAsync(
            int page = 1,
            int pageSize = 50,
            UserRole? roleFilter = null,
            bool? isActiveFilter = null,
            string? searchTerm = null);
        Task<(IEnumerable<UserManagementDTO> users, int totalCount)> GetAllUsersByRoleStringAsync(
            int page = 1,
            int pageSize = 50,
            string? roleFilter = null,
            bool? isActiveFilter = null,
            string? searchTerm = null);

        Task<UserManagementDTO?> GetUserByIdAsync(Guid userId);

        Task<UserManagementDTO?> UpdateUserRoleAsync(Guid userId, UserRole newRole, Guid adminId);

        Task<UserManagementDTO?> UpdateUserStatusAsync(Guid userId, bool isActive);

        Task<IEnumerable<UserManagementDTO>> GetUsersByRoleAsync(UserRole? role);

        Task<Dictionary<UserRole, int>> GetUserRoleDistributionAsync();
    }
}

