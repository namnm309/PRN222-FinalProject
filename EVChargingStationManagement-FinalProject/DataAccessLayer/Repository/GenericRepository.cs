using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repository
{
    // Implementation của IGenericRepository, xử lý tất cả các thao tác CRUD cơ bản với database
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly EVDbContext _context;
        protected readonly DbSet<T> _dbSet;

        // Constructor, inject EVDbContext để truy cập database
        public GenericRepository(EVDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        // Lấy entity theo ID, nếu không tìm thấy thì trả về null
        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        // Lấy tất cả entities trong database, chuyển sang list để thực thi query
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        // Filter theo điều kiện, chuyển sang list để thực thi query
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        // Thêm entity mới, tự động set CreatedAt và UpdatedAt, chưa lưu vào database (cần SaveChangesAsync)
        public async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbSet.AddAsync(entity);

            return entity;
        }

        // Cập nhật entity, tự động set UpdatedAt, đánh dấu entity để EF Core track và update (cần SaveChangesAsync)
        public void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
        }

        // Xóa entity khỏi DbSet, chưa xóa khỏi database, cần gọi SaveChangesAsync() để xác nhận xóa
        public void Delete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
        }

        // Kiểm tra xem có bất kỳ entity nào thỏa điều kiện không
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        // Đếm số lượng entities, nếu có điều kiện thì đếm theo điều kiện, không thì đếm tất cả
        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();
            
            return await _dbSet.CountAsync(predicate);
        }

        // Trả về IQueryable để có thể tiếp tục chain các LINQ operations
        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }
    }
}
