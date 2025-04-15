using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace LicensePlateRecognitionVN
{
    public class DatabaseManager
    {
        private string connectionString;
        private Action<string, System.Drawing.Color> updateStatusCallback;
        private Action<string> addLogMessageCallback;

        public DatabaseManager(Action<string, System.Drawing.Color> updateStatus, Action<string> addLogMessage)
        {
            updateStatusCallback = updateStatus;
            addLogMessageCallback = addLogMessage;
        }

        // Khởi tạo kết nối đến CSDL
        public bool Initialize()
        {
            try
            {
                // Sử dụng DatabaseInitializer để thiết lập kết nối
                connectionString = DatabaseInitializer.SetupDatabase(updateStatusCallback, addLogMessageCallback);
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    updateStatusCallback?.Invoke("Không thể khởi tạo kết nối CSDL", System.Drawing.Color.Red);
                    addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Không thể khởi tạo kết nối CSDL");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi khởi tạo DatabaseManager: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi khởi tạo DatabaseManager: {ex.Message}");
                return false;
            }
        }

        // Thêm bản ghi biển số mới (xe vào)
        public async Task<int> AddEntryRecordAsync(string licensePlate, string imagePath)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"
                        INSERT INTO PlateNumber (license_plate, entry_time, image_path) 
                        VALUES (@licensePlate, @entryTime, @imagePath);
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@licensePlate", licensePlate);
                        cmd.Parameters.AddWithValue("@entryTime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@imagePath", imagePath ?? (object)DBNull.Value);

                        // Thực thi và lấy ID vừa thêm
                        var result = await cmd.ExecuteScalarAsync();
                        int newId = Convert.ToInt32(result);

                        updateStatusCallback?.Invoke($"Đã thêm bản ghi xe vào: {licensePlate}", System.Drawing.Color.Green);
                        addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Đã thêm bản ghi xe vào: {licensePlate}");

                        return newId;
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi thêm bản ghi: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi thêm bản ghi: {ex.Message}");
                return -1;
            }
        }

        // Cập nhật thời gian ra (xe ra)
        public async Task<bool> UpdateExitTimeAsync(int recordId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "UPDATE PlateNumber SET exit_time = @exitTime WHERE id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@exitTime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", recordId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            updateStatusCallback?.Invoke($"Đã cập nhật thời gian ra cho bản ghi ID: {recordId}", System.Drawing.Color.Green);
                            addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Đã cập nhật thời gian ra cho bản ghi ID: {recordId}");
                            return true;
                        }
                        else
                        {
                            updateStatusCallback?.Invoke($"Không tìm thấy bản ghi ID: {recordId}", System.Drawing.Color.Orange);
                            addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Không tìm thấy bản ghi ID: {recordId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi cập nhật thời gian ra: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi cập nhật thời gian ra: {ex.Message}");
                return false;
            }
        }

        // Cập nhật thời gian ra bằng biển số xe
        public async Task<bool> UpdateExitTimeByLicensePlateAsync(string licensePlate)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    
                    // Tìm bản ghi gần nhất chưa có exit_time
                    string findQuery = @"
                        SELECT TOP 1 id 
                        FROM PlateNumber 
                        WHERE license_plate = @licensePlate 
                        AND exit_time IS NULL 
                        ORDER BY entry_time DESC";
                    
                    int recordId = -1;
                    
                    using (SqlCommand findCmd = new SqlCommand(findQuery, conn))
                    {
                        findCmd.Parameters.AddWithValue("@licensePlate", licensePlate);
                        var result = await findCmd.ExecuteScalarAsync();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            recordId = Convert.ToInt32(result);
                        }
                    }
                    
                    if (recordId > 0)
                    {
                        // Cập nhật exit_time
                        string updateQuery = "UPDATE PlateNumber SET exit_time = @exitTime WHERE id = @id";
                        
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@exitTime", DateTime.Now);
                            updateCmd.Parameters.AddWithValue("@id", recordId);
                            
                            int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                            
                            if (rowsAffected > 0)
                            {
                                updateStatusCallback?.Invoke($"Đã cập nhật thời gian ra cho xe: {licensePlate}", System.Drawing.Color.Green);
                                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Đã cập nhật thời gian ra cho xe: {licensePlate}");
                                return true;
                            }
                        }
                    }
                    
                    updateStatusCallback?.Invoke($"Không tìm thấy bản ghi xe vào cho biển số: {licensePlate}", System.Drawing.Color.Orange);
                    addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Không tìm thấy bản ghi xe vào cho biển số: {licensePlate}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi cập nhật thời gian ra: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi cập nhật thời gian ra: {ex.Message}");
                return false;
            }
        }

        // Kiểm tra xe đã vào chưa (kiểm tra có bản ghi chưa có exit_time không)
        public async Task<bool> CheckVehicleInsideAsync(string licensePlate)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT COUNT(*) 
                        FROM PlateNumber 
                        WHERE license_plate = @licensePlate 
                        AND exit_time IS NULL";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@licensePlate", licensePlate);
                        int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi kiểm tra xe trong bãi: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi kiểm tra xe trong bãi: {ex.Message}");
                return false;
            }
        }

        // Lấy tất cả bản ghi trong ngày hiện tại
        public async Task<DataTable> GetTodayRecordsAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT id, license_plate, entry_time, exit_time, image_path
                        FROM PlateNumber
                        WHERE CAST(entry_time AS DATE) = CAST(GETDATE() AS DATE)
                        ORDER BY entry_time DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi lấy dữ liệu: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi lấy dữ liệu: {ex.Message}");
                return new DataTable();
            }
        }

        // Tìm kiếm bản ghi theo biển số
        public async Task<DataTable> SearchRecordsByLicensePlateAsync(string licensePlate)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT id, license_plate, entry_time, exit_time, image_path
                        FROM PlateNumber
                        WHERE license_plate LIKE @licensePlate
                        ORDER BY entry_time DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@licensePlate", "%" + licensePlate + "%");
                        
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi tìm kiếm: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi tìm kiếm: {ex.Message}");
                return new DataTable();
            }
        }

        // Tìm kiếm bản ghi theo khoảng thời gian
        public async Task<DataTable> SearchRecordsByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT id, license_plate, entry_time, exit_time, image_path
                        FROM PlateNumber
                        WHERE entry_time BETWEEN @startTime AND @endTime
                        ORDER BY entry_time DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@startTime", startTime);
                        cmd.Parameters.AddWithValue("@endTime", endTime);
                        
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi tìm kiếm theo thời gian: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi tìm kiếm theo thời gian: {ex.Message}");
                return new DataTable();
            }
        }

        // Lấy thông tin chi tiết của một bản ghi
        public async Task<Dictionary<string, object>> GetRecordDetailsAsync(int recordId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT id, license_plate, entry_time, exit_time, image_path
                        FROM PlateNumber
                        WHERE id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", recordId);
                        
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                Dictionary<string, object> record = new Dictionary<string, object>();
                                record["id"] = reader["id"];
                                record["license_plate"] = reader["license_plate"];
                                record["entry_time"] = reader["entry_time"];
                                
                                if (reader["exit_time"] != DBNull.Value)
                                {
                                    record["exit_time"] = reader["exit_time"];
                                }
                                else
                                {
                                    record["exit_time"] = null;
                                }
                                
                                if (reader["image_path"] != DBNull.Value)
                                {
                                    record["image_path"] = reader["image_path"];
                                }
                                else
                                {
                                    record["image_path"] = null;
                                }
                                
                                return record;
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi lấy chi tiết bản ghi: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi lấy chi tiết bản ghi: {ex.Message}");
                return null;
            }
        }

        // Xóa một bản ghi
        public async Task<bool> DeleteRecordAsync(int recordId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    
                    // Trước tiên lấy thông tin bản ghi để xóa ảnh liên kết
                    string getImageQuery = "SELECT image_path FROM PlateNumber WHERE id = @id";
                    string imagePath = null;
                    
                    using (SqlCommand getCmd = new SqlCommand(getImageQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@id", recordId);
                        var result = await getCmd.ExecuteScalarAsync();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            imagePath = result.ToString();
                        }
                    }
                    
                    // Xóa bản ghi từ CSDL
                    string deleteQuery = "DELETE FROM PlateNumber WHERE id = @id";
                    
                    using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", recordId);
                        int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            // Nếu có đường dẫn ảnh, xóa file ảnh
                            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                            {
                                try
                                {
                                    File.Delete(imagePath);
                                }
                                catch
                                {
                                    // Bỏ qua lỗi xóa file
                                }
                            }
                            
                            updateStatusCallback?.Invoke($"Đã xóa bản ghi ID: {recordId}", System.Drawing.Color.Green);
                            addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Đã xóa bản ghi ID: {recordId}");
                            return true;
                        }
                        else
                        {
                            updateStatusCallback?.Invoke($"Không tìm thấy bản ghi ID: {recordId}", System.Drawing.Color.Orange);
                            addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Không tìm thấy bản ghi ID: {recordId}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi xóa bản ghi: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi xóa bản ghi: {ex.Message}");
                return false;
            }
        }

        // Lấy số xe đang trong bãi
        public async Task<int> GetVehiclesInsideCountAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT COUNT(*) FROM PlateNumber WHERE exit_time IS NULL";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi lấy số xe trong bãi: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi lấy số xe trong bãi: {ex.Message}");
                return 0;
            }
        }

        // Lấy số xe ra vào trong ngày
        public async Task<int> GetTodayVehicleCountAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT COUNT(*) FROM PlateNumber WHERE CAST(entry_time AS DATE) = CAST(GETDATE() AS DATE)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                updateStatusCallback?.Invoke($"Lỗi lấy số xe trong ngày: {ex.Message}", System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Lỗi lấy số xe trong ngày: {ex.Message}");
                return 0;
            }
        }

        // Phương thức để chuẩn hóa biển số xe
        public string NormalizeLicensePlate(string licensePlate)
        {
            if (string.IsNullOrEmpty(licensePlate))
                return string.Empty;

            // Loại bỏ các ký tự đặc biệt không nằm trong A-Z, 0-9, dấu cách và dấu gạch ngang
            string normalized = Regex.Replace(
                licensePlate, 
                @"[^A-Z0-9\-\. ]", 
                "", 
                RegexOptions.IgnoreCase
            );
            
            // Loại bỏ khoảng trắng thừa
            normalized = Regex.Replace(
                normalized, 
                @"\s+", 
                " "
            ).Trim();
            
            return normalized;
        }

        // Phương thức để kiểm tra xem xe đã có trong bãi chưa
        public async Task<bool> IsVehicleInLotAsync(string licensePlate)
        {
            try
            {
                if (string.IsNullOrEmpty(licensePlate) || string.IsNullOrEmpty(connectionString))
                    return false;

                string normalizedPlate = NormalizeLicensePlate(licensePlate);
                
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    
                    // Kiểm tra xem biển số này đã có trong bãi chưa (chưa có giờ ra)
                    string checkQuery = "SELECT COUNT(*) FROM PlateNumber WHERE license_plate = @license_plate AND exit_time IS NULL";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@license_plate", normalizedPlate);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi khi kiểm tra xe trong bãi: {ex.Message}";
                updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                return false;
            }
        }

        // Phiên bản đồng bộ hiện có để tương thích ngược
        public bool IsVehicleInLot(string licensePlate)
        {
            try
            {
                // Tạo và chạy task bất đồng bộ trên một luồng mới
                var task = Task.Run(async () => await IsVehicleInLotAsync(licensePlate));
                return task.Result; // Vẫn dùng .Result nhưng đã chạy trên thread khác
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi khi kiểm tra xe trong bãi: {ex.Message}";
                updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                return false;
            }
        }

        // Phương thức ghi nhận xe vào bãi
        public async Task<bool> RecordVehicleEntryAsync(string licensePlate, Image plateImage = null)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    updateStatusCallback?.Invoke("Không có kết nối đến cơ sở dữ liệu", System.Drawing.Color.Red);
                    return false;
                }

                // Chuẩn hóa biển số
                string normalizedPlate = NormalizeLicensePlate(licensePlate);

                // Lưu ảnh biển số nếu có
                string imagePath = null;
                if (plateImage != null)
                {
                    try
                    {
                        // Tạo thư mục lưu trữ nếu chưa tồn tại
                        string storagePath = Path.Combine(Application.StartupPath, "RecognizedPlates");
                        if (!Directory.Exists(storagePath))
                        {
                            Directory.CreateDirectory(storagePath);
                        }

                        // Tạo tên file ảnh duy nhất dựa trên biển số và thời gian
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string sanitizedPlate = string.Join("_", licensePlate.Split(Path.GetInvalidFileNameChars()));
                        imagePath = Path.Combine(storagePath, $"{sanitizedPlate}_{timestamp}.jpg");

                        // Lưu ảnh biển số
                        plateImage.Save(imagePath);
                        
                        updateStatusCallback?.Invoke($"Đã lưu ảnh biển số: {imagePath}", System.Drawing.Color.Green);
                        addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] Đã lưu ảnh biển số: {imagePath}");
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Không thể lưu ảnh biển số: {ex.Message}";
                        updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Orange);
                        addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                        // Tiếp tục xử lý dù không lưu được ảnh
                    }
                }

                // Ghi nhận xe vào - sử dụng await thay vì .Result
                int recordId = await AddEntryRecordAsync(normalizedPlate, imagePath);
                bool success = recordId > 0;
                
                if (success)
                {
                    string successMsg = $"XE VÀO: Biển số {licensePlate} đã được ghi nhận thành công.";
                    updateStatusCallback?.Invoke(successMsg, System.Drawing.Color.Green);
                    addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {successMsg}");
                    return true;
                }
                else
                {
                    string errorMsg = $"Lỗi ghi nhận xe vào: Không thể thêm xe {licensePlate} vào bãi.";
                    updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                    addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi khi ghi nhận xe vào: {ex.Message}";
                updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                return false;
            }
        }

        // Giữ method cũ để tương thích với code hiện tại
        public bool RecordVehicleEntry(string licensePlate, Image plateImage = null)
        {
            try
            {
                // Tạo và chạy task bất đồng bộ trên một luồng mới
                var task = Task.Run(async () => await RecordVehicleEntryAsync(licensePlate, plateImage));
                return task.Result; // Vẫn dùng .Result nhưng đã chạy trên thread khác
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi khi ghi nhận xe vào: {ex.Message}";
                updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                return false;
            }
        }

        // Phương thức ghi nhận xe ra khỏi bãi - phiên bản async
        public async Task<bool> RecordVehicleExitAsync(string licensePlate)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    updateStatusCallback?.Invoke("Không có kết nối đến cơ sở dữ liệu", System.Drawing.Color.Red);
                    return false;
                }

                // Chuẩn hóa biển số
                string normalizedPlate = NormalizeLicensePlate(licensePlate);

                // Ghi nhận xe ra - sử dụng await thay vì .Result
                bool success = await UpdateExitTimeByLicensePlateAsync(normalizedPlate);
                
                if (success)
                {
                    string successMsg = $"XE RA: Biển số {licensePlate} đã được ghi nhận thành công.";
                    updateStatusCallback?.Invoke(successMsg, System.Drawing.Color.Green);
                    addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {successMsg}");
                    return true;
                }
                else
                {
                    string errorMsg = $"Lỗi ghi nhận xe ra: Không thể cập nhật thời gian ra cho xe {licensePlate}.";
                    updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                    addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi khi ghi nhận xe ra: {ex.Message}";
                updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                return false;
            }
        }

        // Giữ method cũ để tương thích với code hiện tại
        public bool RecordVehicleExit(string licensePlate)
        {
            try
            {
                // Tạo và chạy task bất đồng bộ trên một luồng mới
                var task = Task.Run(async () => await RecordVehicleExitAsync(licensePlate));
                return task.Result; // Vẫn dùng .Result nhưng đã chạy trên thread khác
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi khi ghi nhận xe ra: {ex.Message}";
                updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                return false;
            }
        }

        // Phương thức lấy tất cả lịch sử ra/vào
        public DataTable GetParkingHistory()
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    updateStatusCallback?.Invoke("Không có kết nối đến cơ sở dữ liệu", System.Drawing.Color.Red);
                    return new DataTable();
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            id, 
                            license_plate as 'Biển số xe', 
                            entry_time as 'Thời gian vào', 
                            exit_time as 'Thời gian ra',
                            CASE 
                                WHEN exit_time IS NOT NULL 
                                THEN DATEDIFF(MINUTE, entry_time, exit_time)
                                ELSE NULL 
                            END as 'Tổng thời gian (phút)'
                        FROM PlateNumber 
                        ORDER BY entry_time DESC";
                        
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            
                            // Thêm cột tính tổng thời gian dạng giờ:phút
                            if (!dataTable.Columns.Contains("Thời gian gửi"))
                            {
                                dataTable.Columns.Add("Thời gian gửi", typeof(string));
                                
                                foreach (DataRow row in dataTable.Rows)
                                {
                                    if (row["Thời gian ra"] != DBNull.Value && row["Tổng thời gian (phút)"] != DBNull.Value)
                                    {
                                        int totalMinutes = Convert.ToInt32(row["Tổng thời gian (phút)"]);
                                        int hours = totalMinutes / 60;
                                        int minutes = totalMinutes % 60;
                                        row["Thời gian gửi"] = $"{hours:D2}:{minutes:D2}";
                                    }
                                    else
                                    {
                                        row["Thời gian gửi"] = "Đang gửi";
                                    }
                                }
                                
                                // Ẩn cột tổng thời gian phút để chỉ hiển thị tổng thời gian dạng HH:MM
                                dataTable.Columns["Tổng thời gian (phút)"].ColumnMapping = MappingType.Hidden;
                            }
                            
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi khi lấy lịch sử bãi đỗ: {ex.Message}";
                updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                return new DataTable();
            }
        }

        // Phương thức tìm kiếm xe theo biển số và khoảng thời gian
        public DataTable SearchVehicles(string licensePlate, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    updateStatusCallback?.Invoke("Không có kết nối đến cơ sở dữ liệu", System.Drawing.Color.Red);
                    return new DataTable();
                }

                updateStatusCallback?.Invoke("Đang tìm kiếm dữ liệu...", System.Drawing.Color.Blue);
                
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, license_plate, entry_time, exit_time, image_path FROM PlateNumber WHERE license_plate LIKE @licensePlate AND entry_time BETWEEN @fromDate AND @toDate ORDER BY entry_time DESC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@licensePlate", "%" + licensePlate + "%");
                        cmd.Parameters.AddWithValue("@fromDate", fromDate ?? DateTime.MinValue);
                        cmd.Parameters.AddWithValue("@toDate", toDate ?? DateTime.MaxValue);
                        
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi khi tìm kiếm xe: {ex.Message}";
                updateStatusCallback?.Invoke(errorMsg, System.Drawing.Color.Red);
                addLogMessageCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {errorMsg}");
                return new DataTable();
            }
        }
        
        // Hiển thị thông tin debug trong trường hợp lỗi
        public void ShowDebugInfo(string licensePlate, Exception ex)
        {
            // Tạo form hiển thị thông tin debug
            Form debugForm = new Form
            {
                Text = "Thông tin lỗi kết nối CSDL",
                Width = 600,
                Height = 400
            };

            TextBox txtDebug = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical
            };

            string debugInfo = $"Thời gian: {DateTime.Now}\n" +
                              $"Biển số: {licensePlate}\n" +
                              $"Lỗi: {ex.Message}\n" +
                              $"Stack: {ex.StackTrace}\n\n" +
                              "Hướng dẫn khắc phục:\n" +
                              "1. Kiểm tra SQL Server có đang chạy không\n" +
                              "2. Kiểm tra chuỗi kết nối trong App.config\n" +
                              "3. Kiểm tra quyền truy cập database\n" +
                              "4. Kiểm tra bảng PlateNumber đã được tạo chưa\n";

            txtDebug.Text = debugInfo;
            debugForm.Controls.Add(txtDebug);
            debugForm.Show();
        }
        
        // Kiểm tra xem có kết nối đến CSDL không
        public bool HasDatabaseConnection()
        {
            return !string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(connectionString);
        }
    }
} 