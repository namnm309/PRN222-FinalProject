using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repository
{
    // Interface generic repository cho các thao tác CRUD cơ bản, dùng cho tất cả entity kế thừa từ BaseEntity
    public interface IGenericRepository<T> where T : BaseEntity
    {
        // Lấy entity theo ID, nếu không tìm thấy thì trả về null
        Task<T?> GetByIdAsync(Guid id);

        // Lấy tất cả entities trong database, cẩn thận với dữ liệu lớn
        Task<IEnumerable<T>> GetAllAsync();

        // Tìm entities theo điều kiện (predicate), ví dụ: FindAsync(x => x.IsActive == true)
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        // Thêm entity mới vào database, tự động set CreatedAt và UpdatedAt, cần gọi SaveChangesAsync() sau khi thêm
        Task<T> AddAsync(T entity);

        // Cập nhật entity đã tồn tại, tự động set UpdatedAt, cần gọi SaveChangesAsync() sau khi update
        void Update(T entity);

        // Xóa entity khỏi database (hard delete), cần gọi SaveChangesAsync() sau khi xóa
        void Delete(T entity);

        // Kiểm tra xem có entity nào thỏa điều kiện không, dùng để check tồn tại trước khi thêm hoặc validate
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        // Đếm số lượng entities thỏa điều kiện, nếu không truyền predicate thì đếm tất cả, dùng cho pagination hoặc thống kê
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        // Lấy IQueryable để có thể chain thêm các LINQ operations, dùng khi cần query phức tạp hoặc join với các entity khác
        IQueryable<T> GetQueryable();
    }
}
