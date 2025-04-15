

# 🚗 Hệ thống Nhận diện Biển số xe Việt Nam (Vietnamese License Plate Recognition System)

Dự án Bài tập lớn môn học **Chuyên đề Hệ thống giao thông thông minh** tại Trường Đại học Giao thông Vận tải TP.HCM.  
Hệ thống này cho phép nhận diện biển số xe Việt Nam từ camera trực tiếp hoặc từ file ảnh, đồng thời lưu trữ lịch sử xe ra/vào.

---

## 🏗️ Kiến trúc Hệ thống

Hệ thống được xây dựng theo kiến trúc **Client-Server** gồm 3 phần chính:

### 1. **Frontend (Client)**

Ứng dụng Desktop viết bằng **C# WinForms (.NET 8)**:

- Hiển thị hình ảnh từ camera hoặc file ảnh.
- Gửi yêu cầu nhận diện đến Backend API.
- Hiển thị kết quả biển số nhận diện được.
- Xác nhận và ghi nhận thời gian xe vào/ra.
- Tương tác với SQL Server để lưu và truy vấn lịch sử.

🔗 **Repo:** [https://github.com/PhucDaizz/LicensePlateRecognitionVN-](https://github.com/PhucDaizz/LicensePlateRecognitionVN-)

---

### 2. **Backend (API Server)**

API viết bằng **Python FastAPI**, với chức năng:

- Nhận ảnh từ Frontend (dạng file).
- Phát hiện vùng biển số bằng mô hình YOLOv8 (`best.pt`).
- Nhận diện ký tự bằng EasyOCR.
- Chuẩn hóa biển số theo định dạng Việt Nam.
- Trả kết quả về dạng JSON.

🔗 **Repo:** `👉 [Đang cập nhật...]`

---

### 3. **Database**

- Hệ quản trị: **Microsoft SQL Server**
- Dùng để lưu các lượt xe vào/ra trong bảng `vehicle_logs`.

---

## ✨ Tính năng nổi bật

- Nhận diện biển số xe từ camera theo thời gian thực.
- Nhận diện từ file ảnh tĩnh (JPG, PNG, BMP).
- Giao diện dễ sử dụng, hiển thị kết quả rõ ràng.
- Ghi nhận thời gian xe vào và xe ra.
- Truy vấn và hiển thị lịch sử xe ra/vào.

---

## 🧪 Công nghệ sử dụng

### **Frontend:**

- Ngôn ngữ: C#
- Framework: .NET 8, Windows Forms (WinForms)
- Thư viện: Emgu CV, Newtonsoft.Json, System.Data.SqlClient

### **Backend:**

- Ngôn ngữ: Python 3.x
- Framework: FastAPI
- Server: Uvicorn
- Object Detection: YOLOv8 (`ultralytics`)
- OCR: EasyOCR
- Xử lý ảnh: OpenCV, Pillow
- Khác: Numpy, Regex, Logging, IO

### **Database:**

- Microsoft SQL Server

---

## ⚙️ Yêu cầu hệ thống

| Thành phần             | Mô tả                                      |
|------------------------|---------------------------------------------|
| .NET 8 SDK             | Tải từ Microsoft                             |
| Visual Studio 2022+    | Cài workload ".NET desktop development"      |
| Python 3.8+            | Nên chọn "Add Python to PATH" khi cài       |
| Pip                    | Quản lý gói Python (thường cài sẵn)         |
| Microsoft SQL Server   | Developer/Express/Standard đều được         |
| SQL Server Management Studio (SSMS) | Để quản lý database dễ dàng       |
| Git                    | Clone repository                            |

---

## 🚀 Cài đặt và Chạy hệ thống

### 1. Clone Repository

```bash
# Clone Frontend
git clone https://github.com/PhucDaizz/LicensePlateRecognitionVN-.git

# Clone Backend (thay URL nếu có)
git clone [URL_BACKEND_REPO]
```

---

### 2. Cài đặt SQL Server Database

Mở SSMS → Tạo database và bảng bằng đoạn SQL sau:

```sql
-- Tạo database nếu chưa có
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'LicensePlateRecognition')
BEGIN
    CREATE DATABASE LicensePlateRecognition;
END
GO

USE LicensePlateRecognition;
GO

-- Tạo bảng vehicle_logs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[vehicle_logs]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.vehicle_logs (
        id INT IDENTITY(1,1) PRIMARY KEY,
        license_plate VARCHAR(20) NOT NULL,
        entry_time DATETIME NULL,
        exit_time DATETIME NULL,
        created_at DATETIME DEFAULT GETDATE()
    );
END
GO
```

---

### 3. Cài đặt Backend (Python API)

```bash
# Tạo môi trường ảo
python -m venv venv
# Kích hoạt:
# Windows:
venv\Scripts\activate
# macOS/Linux:
source venv/bin/activate

# Cài thư viện
pip install opencv-python easyocr numpy ultralytics fastapi uvicorn[standard] Pillow python-multipart regex
# (Tùy chọn) Thêm PyTorch nếu cần:
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cpu
```

> 🔁 Nếu bạn có `requirements.txt`, chạy:
```bash
pip install -r requirements.txt
```

📁 Đảm bảo `best.pt` nằm đúng thư mục theo code bạn viết.

---

### 4. Cấu hình Frontend (C#)

- Mở solution `.sln` bằng Visual Studio.
- Restore NuGet Packages nếu cần.
- Chỉnh sửa file `DatabaseHelper.cs`:

```csharp
private readonly string _connectionString = "Server=TEN_MAY\\TEN_INSTANCE;Database=LicensePlateRecognition;Trusted_Connection=True;TrustServerCertificate=Yes";
```

---

## ▶️ Chạy ứng dụng

### ✅ Chạy Backend

```bash
# Kích hoạt môi trường ảo
venv\Scripts\activate

# Chạy FastAPI
uvicorn main_api:app --reload --host 0.0.0.0 --port 5000
```

### ✅ Chạy Frontend

- Mở Visual Studio → Nhấn **Start (F5)**

---

## 👨‍👩‍👧‍👦 Thành viên nhóm

| STT | MSSV       | Họ và tên                  |
|-----|------------|----------------------------|
| 1   | 2251120339 | Nguyễn Phúc Đại            |
| 2   | 2251120340 | Nguyễn Cao Thành Đạt       |
| 3   | 2251120382 | Trần Văn Tài               |
| 4   | 2251120277 | Huỳnh Long Bảo Duy         |

---

## 📌 Ghi chú

- Nếu EasyOCR tải model ngôn ngữ lần đầu, cần Internet.
- Nếu không lưu ảnh, có thể bỏ cột `image_path`.
- Cấu trúc bảng phải đồng bộ với C# (DbHelper hoặc EF nếu có).

---

## ✅ TODOs & Nâng cấp đề xuất

- [ ] Thêm nhận diện loại xe (ô tô / xe máy).
- [ ] Lưu ảnh chụp biển số vào ổ đĩa hoặc cloud.
- [ ] Thêm dashboard thống kê lượt xe.
- [ ] Hỗ trợ nhiều camera cùng lúc.

---

## 💬 Đóng góp

Mọi ý kiến đóng góp hoặc Pull Request đều được chào đón!  

---

## 📝 License

[MIT License](LICENSE)
```

---
