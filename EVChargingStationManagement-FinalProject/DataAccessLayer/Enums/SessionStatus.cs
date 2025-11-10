using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Enums
{
    public enum SessionStatus
    {
        Active = 0,      // Đang sạc
        Completed = 1,   // Hoàn thành
        Paused = 2,      // Tạm dừng
        Cancelled = 3,   // Đã hủy
        Error = 4        // Lỗi
    }
}

