using Emgu.CV;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace LicensePlateRecognitionVN
{
    public partial class Form3 : Form
    {
        // Các đối tượng quản lý chức năng riêng biệt
        private LicensePlateRecognizer plateRecognizer;
        private DatabaseManager dbManager;
        private SearchManager searchManager;
        
        public Form3()
        {
            InitializeComponent();
            
            // Khởi tạo các đối tượng với callback tương ứng
            dbManager = new DatabaseManager(UpdateStatus, AddLogMessage);
            
            // Khởi tạo kết nối CSDL
            bool dbInitialized = dbManager.Initialize();
            if (!dbInitialized)
            {
                UpdateStatus("Không thể kết nối đến CSDL. Một số chức năng có thể không hoạt động.", Color.Red);
                AddLogMessage($"[{DateTime.Now:HH:mm:ss}] Lỗi kết nối CSDL");
                MessageBox.Show("Không thể kết nối đến cơ sở dữ liệu. Vui lòng kiểm tra lại cấu hình kết nối.", 
                    "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                UpdateStatus("Đã kết nối thành công đến CSDL", Color.Green);
                AddLogMessage($"[{DateTime.Now:HH:mm:ss}] Kết nối CSDL thành công");
            }

            plateRecognizer = new LicensePlateRecognizer(UpdateStatus, UpdatePlateNumber);
            searchManager = new SearchManager(
                dbManager, 
                txtSearchPlate, 
                dtpFrom, 
                dtpTo, 
                UpdateStatus, 
                AddLogMessage,
                UpdateDataGridView,
                UpdateResultCount);

            // Khởi tạo timer xử lý camera
            apiTimer = new System.Windows.Forms.Timer();
            apiTimer.Tick += async (s, e) => await ProcessFrameAsync();
            apiTimer.Interval = 500;
            
            // Khởi tạo camera
            InitializeCamera();
            
            // Hiển thị dữ liệu ban đầu
            RefreshParkingData();
        }

        private void InitializeCamera()
        {
            bool success = plateRecognizer.InitializeCamera();
            if (success && plateRecognizer.IsCameraOpened())
            {
                apiTimer.Start();
            }
        }

        // Phương thức được gọi bởi timer
        private async Task ProcessFrameAsync()
        {
            if (!apiTimer.Enabled) return;

            Mat frame = null;
            Bitmap frameBitmap = null;

            try
            {
                frame = await plateRecognizer.QueryFrameAsync();
                if (frame != null && !frame.IsEmpty)
                {
                    frameBitmap = frame.ToBitmap();

                    // Clone frame Mat nếu cần gửi đi API và frameBitmap sẽ hiển thị
                    Mat frameToSend = frame.Clone();

                    // Cập nhật PictureBox trên luồng UI
                    if (cameraBox.InvokeRequired)
                    {
                        cameraBox.Invoke(new Action(() => {
                            // Giải phóng ảnh cũ TRƯỚC KHI gán ảnh mới
                            Bitmap oldBitmap = cameraBox.Image as Bitmap;
                            cameraBox.Image = frameBitmap; // Gán bitmap mới
                            oldBitmap?.Dispose(); // Giải phóng bitmap cũ
                        }));
                    }
                    else
                    {
                        // Giải phóng ảnh cũ TRƯỚC KHI gán ảnh mới
                        Bitmap oldBitmap = cameraBox.Image as Bitmap;
                        cameraBox.Image = frameBitmap; // Gán bitmap mới
                        oldBitmap?.Dispose(); // Giải phóng bitmap cũ
                    }

                    // Gọi API để nhận diện biển số với frame đã clone
                    await plateRecognizer.RecognizeAndDisplayPlateAsync(frameToSend);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi xử lý frame: {ex.Message}", Color.Red);
                // Giải phóng bitmap nếu có lỗi trước khi gán
                frameBitmap?.Dispose();
            }
            finally
            {
                // Giải phóng frame Mat gốc nếu chưa được giải phóng
                frame?.Dispose();
            }
        }

        #region Callback Methods

        // Cập nhật trạng thái hiển thị
        private void UpdateStatus(string message, Color color)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => {
                    lblStatus.Text = message;
                    lblStatus.ForeColor = color;
                }));
            }
            else
            {
                lblStatus.Text = message;
                lblStatus.ForeColor = color;
            }
        }

        // Cập nhật số biển số xe
        private void UpdatePlateNumber(string plateNumber)
        {
            if (txtPlateNumber.InvokeRequired)
            {
                txtPlateNumber.Invoke(new Action(() => txtPlateNumber.Text = plateNumber));
            }
            else
            {
                txtPlateNumber.Text = plateNumber;
            }
        }

        // Ghi log
        private void AddLogMessage(string message)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.Invoke(new Action(() => rtbLogs.AppendText(message + Environment.NewLine)));
            }
            else
            {
                rtbLogs.AppendText(message + Environment.NewLine);
            }
        }

        // Cập nhật DataGridView
        private void UpdateDataGridView(DataTable data)
        {
            if (dgvParkingHistory.InvokeRequired)
            {
                dgvParkingHistory.Invoke(new Action(() => dgvParkingHistory.DataSource = data));
            }
            else
            {
                dgvParkingHistory.DataSource = data;
            }
        }

        // Cập nhật số lượng kết quả
        private void UpdateResultCount(string countText)
        {
            if (lblResultCount.InvokeRequired)
            {
                lblResultCount.Invoke(new Action(() => lblResultCount.Text = countText));
            }
            else
            {
                lblResultCount.Text = countText;
            }
        }

        #endregion

        #region Event Handlers

        private async void btnAddImage_Click(object sender, EventArgs e)
        {
            apiTimer.Stop(); // Dừng timer xử lý camera

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Chọn ảnh biển số";
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedImagePath = openFileDialog.FileName;

                    try
                    {
                        // Giải phóng ảnh cũ trên PictureBox trước khi tải ảnh mới
                        Image oldImage = cameraBox.Image;
                        cameraBox.Image = null;
                        oldImage?.Dispose();

                        // Hiển thị ảnh lên PictureBox
                        Image selectedImage = Image.FromFile(selectedImagePath);
                        cameraBox.Image = selectedImage;

                        // Đặt SizeMode cho ảnh tĩnh
                        cameraBox.SizeMode = PictureBoxSizeMode.Zoom;

                        UpdateStatus("Đã tải ảnh: " + Path.GetFileName(selectedImagePath) + ". Đang nhận diện...", Color.Blue);
                        txtPlateNumber.Text = ""; // Xóa biển số cũ

                        // Gửi ảnh tới API để nhận diện
                        using (Mat imageMat = CvInvoke.Imread(selectedImagePath, Emgu.CV.CvEnum.ImreadModes.Color))
                        {
                            if (!imageMat.IsEmpty)
                            {
                                await plateRecognizer.RecognizeAndDisplayPlateAsync(imageMat);
                            }
                            else
                            {
                                UpdateStatus("Lỗi: Không thể đọc ảnh bằng EmguCV.", Color.Red);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể mở hoặc xử lý ảnh: " + ex.Message);
                        Image oldImage = cameraBox.Image;
                        cameraBox.Image = null;
                        oldImage?.Dispose();
                        UpdateStatus("Lỗi khi tải ảnh.", Color.Red);
                    }
                }
                else
                {
                    // Nếu người dùng hủy chọn file, bật lại camera
                    btnCamera_Click(null, null);
                }
            }
        }

        private void btnCamera_Click(object sender, EventArgs e)
        {
            // Giải phóng ảnh tĩnh cũ nếu đang ở chế độ ảnh tĩnh
            if (cameraBox.SizeMode == PictureBoxSizeMode.Zoom)
            {
                Image oldImage = cameraBox.Image;
                cameraBox.Image = null;
                oldImage?.Dispose();
            }

            // Đặt lại SizeMode cho camera
            cameraBox.SizeMode = PictureBoxSizeMode.StretchImage;

            // Khởi động lại timer nếu camera đã mở
            if (plateRecognizer.IsCameraOpened())
            {
                apiTimer.Start();
                UpdateStatus("Đã bật lại chế độ camera.", Color.Blue);
            }
            else
            {
                InitializeCamera();
            }
            
            txtPlateNumber.Text = ""; // Xóa biển số khi chuyển sang camera
        }

        private async void btnConfirm_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPlateNumber.Text))
            {
                MessageBox.Show("Chưa có biển số xe để lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Hiển thị hộp thoại xác nhận
            DialogResult result = MessageBox.Show(
                $"Bạn có muốn xác nhận biển số {txtPlateNumber.Text} không?\n\nHệ thống sẽ tự động xác định đây là xe vào hay xe ra dựa trên dữ liệu trong bãi.", 
                "Xác nhận biển số", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                string licensePlate = txtPlateNumber.Text;
                UpdateStatus($"Đã xác nhận biển số: {licensePlate}", Color.Green);
                
                // Disable các nút để tránh người dùng nhấn nhiều lần
                btnConfirm.Enabled = false;
                btnEntry.Enabled = false;
                btnExit.Enabled = false;
                
                try
                {
                    // Kiểm tra xe đã có trong bãi chưa - phiên bản bất đồng bộ
                    bool isVehicleInLot = await dbManager.IsVehicleInLotAsync(licensePlate);

                    // Hiển thị thông báo
                    string status = isVehicleInLot ? "RA" : "VÀO";
                    string message = $"Đã xác định biển số {licensePlate} là xe {status}";
                        
                    UpdateStatus(message, Color.Blue);
                    AddLogMessage($"[{DateTime.Now:HH:mm:ss}] {message}");
                    MessageBox.Show(message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                    // Lưu dữ liệu dựa trên trạng thái xe - phiên bản bất đồng bộ
                    bool success;
                    if (isVehicleInLot)
                    {
                        // Xe đã trong bãi, ghi nhận ra
                        success = await dbManager.RecordVehicleExitAsync(licensePlate);
                    }
                    else
                    {
                        // Xe chưa trong bãi, ghi nhận vào
                        success = await dbManager.RecordVehicleEntryAsync(licensePlate, plateRecognizer.RecognizedRoiImage);
                    }

                    // Làm mới dữ liệu hiển thị
                    RefreshParkingData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus($"Lỗi: {ex.Message}", Color.Red);
                }
                finally
                {
                    // Kích hoạt lại các nút
                    btnConfirm.Enabled = true;
                    btnEntry.Enabled = true;
                    btnExit.Enabled = true;
                }
            }
        }

        private async void btnEntry_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPlateNumber.Text))
            {
                MessageBox.Show("Chưa có biển số xe để ghi nhận!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Hiển thị hộp thoại xác nhận
            DialogResult result = MessageBox.Show(
                $"Bạn có đồng ý lưu biển số xe {txtPlateNumber.Text} vào dữ liệu với thời gian vào là {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}?",
                "Xác nhận ghi nhận xe vào",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                // Disable các nút để tránh người dùng nhấn nhiều lần
                btnConfirm.Enabled = false;
                btnEntry.Enabled = false;
                btnExit.Enabled = false;
                
                try
                {
                    // Kiểm tra xem xe đã có trong bãi chưa
                    bool isVehicleInLot = await dbManager.IsVehicleInLotAsync(txtPlateNumber.Text);
                    
                    if (isVehicleInLot)
                    {
                        // Xe đã trong bãi, hỏi người dùng có muốn ghi nhận ra không
                        result = MessageBox.Show(
                            $"Xe biển số {txtPlateNumber.Text} đang trong bãi. Bạn có muốn ghi nhận xe này ra khỏi bãi không?",
                            "Ghi nhận xe ra",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                            
                        if (result == DialogResult.Yes)
                        {
                            bool success = await dbManager.RecordVehicleExitAsync(txtPlateNumber.Text);
                            if (success)
                            {
                                MessageBox.Show($"XE RA: Biển số {txtPlateNumber.Text} đã được ghi nhận thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else
                    {
                        // Ghi nhận xe vào
                        bool success = await dbManager.RecordVehicleEntryAsync(txtPlateNumber.Text, plateRecognizer.RecognizedRoiImage);
                        if (success)
                        {
                            MessageBox.Show($"XE VÀO: Biển số {txtPlateNumber.Text} đã được ghi nhận thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    
                    // Làm mới dữ liệu hiển thị
                    RefreshParkingData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus($"Lỗi: {ex.Message}", Color.Red);
                }
                finally
                {
                    // Kích hoạt lại các nút
                    btnConfirm.Enabled = true;
                    btnEntry.Enabled = true;
                    btnExit.Enabled = true;
                }
            }
        }

        private async void btnExit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPlateNumber.Text))
            {
                MessageBox.Show("Chưa có biển số xe để ghi nhận!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Hiển thị hộp thoại xác nhận
            DialogResult result = MessageBox.Show(
                $"Bạn có đồng ý ghi nhận xe {txtPlateNumber.Text} ra khỏi bãi với thời gian ra là {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}?",
                "Xác nhận ghi nhận xe ra",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                // Disable các nút để tránh người dùng nhấn nhiều lần
                btnConfirm.Enabled = false;
                btnEntry.Enabled = false;
                btnExit.Enabled = false;
                
                try
                {
                    // Kiểm tra xem xe có trong bãi không
                    bool isVehicleInLot = await dbManager.IsVehicleInLotAsync(txtPlateNumber.Text);
                    
                    if (!isVehicleInLot)
                    {
                        // Xe không có trong bãi, hỏi người dùng có muốn ghi nhận vào không
                        result = MessageBox.Show(
                            $"Xe biển số {txtPlateNumber.Text} không có trong bãi. Bạn có muốn ghi nhận xe này vào bãi không?",
                            "Ghi nhận xe vào",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                            
                        if (result == DialogResult.Yes)
                        {
                            bool success = await dbManager.RecordVehicleEntryAsync(txtPlateNumber.Text, plateRecognizer.RecognizedRoiImage);
                            if (success)
                            {
                                MessageBox.Show($"XE VÀO: Biển số {txtPlateNumber.Text} đã được ghi nhận thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else
                    {
                        // Ghi nhận xe ra
                        bool success = await dbManager.RecordVehicleExitAsync(txtPlateNumber.Text);
                        if (success)
                        {
                            MessageBox.Show($"XE RA: Biển số {txtPlateNumber.Text} đã được ghi nhận thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    
                    // Làm mới dữ liệu hiển thị
                    RefreshParkingData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus($"Lỗi: {ex.Message}", Color.Red);
                }
                finally
                {
                    // Kích hoạt lại các nút
                    btnConfirm.Enabled = true;
                    btnEntry.Enabled = true;
                    btnExit.Enabled = true;
                }
            }
        }

        // Xử lý sự kiện khi nhấn nút Tìm kiếm
        private void btnSearch_Click(object sender, EventArgs e)
        {
            searchManager.SearchVehicles();
        }

        // Xử lý sự kiện khi nhấn nút Xóa tìm
        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            searchManager.ClearSearch();
        }

        // Xử lý sự kiện khi nhấn Enter trong ô tìm kiếm
        private void txtSearchPlate_KeyDown(object sender, KeyEventArgs e)
        {
            searchManager.HandleEnterKeyPress(e);
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            // Không cần làm gì thêm vì đã khởi tạo trong constructor
        }

        // Đảm bảo giải phóng tài nguyên khi đóng form
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            apiTimer?.Stop();
            plateRecognizer?.StopCamera();
            plateRecognizer?.DisposeCamera();
        }

        #endregion

        // Làm mới dữ liệu hiển thị
        private void RefreshParkingData()
        {
            // Hiện Progress Cursor để cho người dùng biết đang xử lý
            this.Cursor = Cursors.WaitCursor;
            
            try
            {
                searchManager.RefreshData();
                
                // Cập nhật số lượng xe - gọi trực tiếp thay vì await
                UpdateVehicleCount();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi làm mới dữ liệu: {ex.Message}", Color.Red);
                AddLogMessage($"[{DateTime.Now:HH:mm:ss}] Lỗi làm mới dữ liệu: {ex.Message}");
            }
            finally
            {
                // Đảm bảo luôn đặt lại con trỏ chuột
                this.Cursor = Cursors.Default;
            }
        }

        // Cập nhật số lượng xe trong bãi
        private void UpdateVehicleCount()
        {
            Task.Run(async () => {
                try
                {
                    // Lấy số xe đang trong bãi
                    int vehiclesInside = await dbManager.GetVehiclesInsideCountAsync();
                    
                    // Lấy tổng số xe trong ngày
                    int todayVehicles = await dbManager.GetTodayVehicleCountAsync();
                    
                    // Hiển thị thông tin
                    string countInfo = $"Số xe trong bãi: {vehiclesInside} | Tổng số xe hôm nay: {todayVehicles}";
                    UpdateResultCount(countInfo);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Lỗi lấy số lượng xe: {ex.Message}", Color.Red);
                }
            });
        }
    }
}



