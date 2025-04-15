using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LicensePlateRecognitionVN
{
    public class SearchManager
    {
        private readonly DatabaseManager dbManager;
        private readonly Action<string, Color> statusUpdateCallback;
        private readonly Action<string> logCallback;
        private readonly Action<DataTable> displayResultsCallback;
        private readonly Action<string> updateResultCountCallback;

        private readonly TextBox searchPlateTextBox;
        private readonly DateTimePicker fromDatePicker;
        private readonly DateTimePicker toDatePicker;

        public SearchManager(
            DatabaseManager dbManager,
            TextBox searchPlateTextBox,
            DateTimePicker fromDatePicker,
            DateTimePicker toDatePicker,
            Action<string, Color> statusUpdateCallback,
            Action<string> logCallback,
            Action<DataTable> displayResultsCallback,
            Action<string> updateResultCountCallback)
        {
            this.dbManager = dbManager;
            this.searchPlateTextBox = searchPlateTextBox;
            this.fromDatePicker = fromDatePicker;
            this.toDatePicker = toDatePicker;
            this.statusUpdateCallback = statusUpdateCallback;
            this.logCallback = logCallback;
            this.displayResultsCallback = displayResultsCallback;
            this.updateResultCountCallback = updateResultCountCallback;

            // Thiết lập giá trị mặc định cho DateTimePicker
            this.fromDatePicker.Value = DateTime.Today.AddDays(-30); // 30 ngày trước
            this.toDatePicker.Value = DateTime.Today.AddDays(1).AddSeconds(-1); // Hết ngày hôm nay
        }

        // Tìm kiếm biển số xe trong CSDL
        public void SearchVehicles()
        {
            try
            {
                if (dbManager == null || !dbManager.HasDatabaseConnection())
                {
                    MessageBox.Show("Không có kết nối đến cơ sở dữ liệu.", "Lỗi kết nối", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Lấy các điều kiện tìm kiếm
                string searchPlate = searchPlateTextBox.Text.Trim();
                DateTime? fromDate = null;
                DateTime? toDate = null;

                if (fromDatePicker.Checked)
                {
                    fromDate = fromDatePicker.Value.Date;
                }
                
                if (toDatePicker.Checked)
                {
                    toDate = toDatePicker.Value.Date.AddDays(1).AddSeconds(-1); // Hết ngày được chọn
                }

                // Hiển thị thông báo đang tìm kiếm
                statusUpdateCallback("Đang tìm kiếm dữ liệu...", Color.Blue);

                // Thực hiện tìm kiếm
                DataTable searchResults = null;

                // Nếu không có điều kiện tìm kiếm nào, sử dụng GetParkingHistory để lấy tất cả lịch sử
                if (string.IsNullOrEmpty(searchPlate) && 
                    !fromDate.HasValue && 
                    !toDate.HasValue)
                {
                    searchResults = dbManager.GetParkingHistory();
                }
                else
                {
                    // Gọi phương thức tìm kiếm với các điều kiện
                    searchResults = dbManager.SearchVehicles(searchPlate, fromDate, toDate);
                }

                // Hiển thị kết quả
                if (searchResults != null)
                {
                    // Hiển thị kết quả lên DataGridView
                    displayResultsCallback(searchResults);

                    // Cập nhật số lượng kết quả
                    int resultCount = searchResults.Rows.Count;
                    updateResultCountCallback($"Tổng số kết quả: {resultCount}");

                    // Hiển thị thông báo
                    statusUpdateCallback($"Tìm thấy {resultCount} kết quả.", 
                        resultCount > 0 ? Color.Green : Color.Orange);

                    // Ghi log
                    string searchInfo = $"[{DateTime.Now:HH:mm:ss}] Tìm kiếm: Biển số='{searchPlate}', " +
                                     $"Từ={(fromDate.HasValue ? fromDate.Value.ToString("dd/MM/yyyy") : "không giới hạn")}, " +
                                     $"Đến={(toDate.HasValue ? toDate.Value.ToString("dd/MM/yyyy") : "không giới hạn")}. Kết quả: {resultCount}";
                    logCallback(searchInfo);
                }
                else
                {
                    statusUpdateCallback("Lỗi khi tìm kiếm dữ liệu.", Color.Red);
                }
            }
            catch (Exception ex)
            {
                statusUpdateCallback($"Lỗi tìm kiếm: {ex.Message}", Color.Red);
                logCallback($"[{DateTime.Now:HH:mm:ss}] Lỗi tìm kiếm: {ex.Message}");
                
                MessageBox.Show($"Lỗi khi tìm kiếm: {ex.Message}", "Lỗi", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Xóa điều kiện tìm kiếm và hiển thị tất cả lịch sử
        public void ClearSearch()
        {
            try
            {
                // Xóa các điều kiện tìm kiếm
                searchPlateTextBox.Clear();
                fromDatePicker.Value = DateTime.Today.AddDays(-30);
                toDatePicker.Value = DateTime.Today.AddDays(1).AddSeconds(-1); // Hết ngày hôm nay

                // Lấy lại tất cả dữ liệu
                DataTable allData = dbManager.GetParkingHistory();
                
                // Hiển thị kết quả
                if (allData != null)
                {
                    displayResultsCallback(allData);
                    
                    // Cập nhật số lượng kết quả
                    updateResultCountCallback($"Tổng số kết quả: {allData.Rows.Count}");
                    
                    // Hiển thị thông báo
                    statusUpdateCallback("Đã xóa điều kiện tìm kiếm.", Color.Green);
                    
                    // Ghi log
                    logCallback($"[{DateTime.Now:HH:mm:ss}] Đã xóa điều kiện tìm kiếm. Hiển thị {allData.Rows.Count} bản ghi.");
                }
            }
            catch (Exception ex)
            {
                statusUpdateCallback($"Lỗi khi xóa tìm kiếm: {ex.Message}", Color.Red);
                logCallback($"[{DateTime.Now:HH:mm:ss}] Lỗi khi xóa tìm kiếm: {ex.Message}");
            }
        }

        // Xử lý sự kiện khi nhấn Enter trong ô tìm kiếm
        public void HandleEnterKeyPress(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Ngăn tiếng beep khi nhấn Enter
                SearchVehicles(); // Thực hiện tìm kiếm
            }
        }

        // Tải lại dữ liệu mặc định
        public void RefreshData()
        {
            try
            {
                if (dbManager == null || !dbManager.HasDatabaseConnection())
                {
                    logCallback($"[{DateTime.Now:HH:mm:ss}] Không thể tải dữ liệu: Không có kết nối đến CSDL");
                    statusUpdateCallback("Không thể tải dữ liệu: Không có kết nối đến CSDL", Color.Red);
                    return;
                }

                // Hiển thị thông báo đang tải dữ liệu
                statusUpdateCallback("Đang tải lịch sử giao dịch...", Color.Blue);

                DataTable defaultData = dbManager.GetParkingHistory();
                if (defaultData != null)
                {
                    displayResultsCallback(defaultData);
                    updateResultCountCallback($"Tổng số kết quả: {defaultData.Rows.Count}");
                    
                    if (defaultData.Rows.Count > 0)
                    {
                        statusUpdateCallback($"Đã tải {defaultData.Rows.Count} bản ghi lịch sử", Color.Green);
                    }
                    else
                    {
                        statusUpdateCallback("Không có dữ liệu lịch sử giao dịch", Color.Orange);
                    }
                    
                    logCallback($"[{DateTime.Now:HH:mm:ss}] Đã tải {defaultData.Rows.Count} bản ghi lịch sử");
                }
            }
            catch (Exception ex)
            {
                statusUpdateCallback($"Lỗi khi tải lại dữ liệu: {ex.Message}", Color.Red);
                logCallback($"[{DateTime.Now:HH:mm:ss}] Lỗi khi tải lại dữ liệu: {ex.Message}");
            }
        }
    }
} 