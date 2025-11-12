using DataAccessLayer.Entities;
using BusinessLayer.DTOs;

namespace BusinessLayer.DTOs
{
    public static class UserProfileMapper
    {
        public static UserProfileDTO ToDTO(Users user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return new UserProfileDTO
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                GoogleId = user.GoogleId,
                Role = user.Role.ToString()
            };
        }
    }
}

