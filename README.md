# HƯỚNG DẪN CHẠY BACKEND WEB API TRÊN VS CODE

1. Mở thư mục này (`nhdphuong_1787_api`) trong **VS Code**.
2. Kiểm tra chuỗi kết nối PostgreSQL trong `appsettings.json` (mặc định trỏ đến database `nhdphuong_1787_ktra_db` trên localhost:5432).
3. Mở Terminal trong VS Code (`Ctrl + \``) và chạy lệnh:
   ```bash
   dotnet run --urls "http://0.0.0.0:5000"
   ```
4. Truy cập giao diện Swagger UI trực tiếp trên trình duyệt tại:
   👉 **http://localhost:5000/**

## Tài khoản Seed sẵn:
- **Admin:** `admin1787` / `Password123@`
- **Staff:** `staff1787` / `Password123@`
- **Parent:** `parent1787` / `Password123@`
