using Emgu.CV;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LicensePlateRecognitionVN
{
    public class LicensePlateRecognizer
    {
        private VideoCapture capture; // Đối tượng để truy cập camera
        private HttpClient httpClient; // Đối tượng để gọi API
        private Image recognizedRoiImage; // Lưu ảnh ROI biển số
        private Action<string, Color> statusUpdateCallback;
        private Action<string> plateNumberUpdateCallback;

        public LicensePlateRecognizer(Action<string, Color> statusUpdateCallback, Action<string> plateNumberUpdateCallback)
        {
            this.statusUpdateCallback = statusUpdateCallback;
            this.plateNumberUpdateCallback = plateNumberUpdateCallback;
            InitializeApiClient();
        }

        public Image RecognizedRoiImage
        {
            get { return recognizedRoiImage; }
        }

        public bool InitializeCamera()
        {
            try
            {
                capture = new VideoCapture(0); // Hoặc chỉ định index đúng nếu 0 không hoạt động

                if (!capture.IsOpened)
                {
                    statusUpdateCallback("Không thể mở camera. Vui lòng kiểm tra kết nối và driver.", Color.Red);
                    return false;
                }
                return true;
            }
            catch (NullReferenceException ex) // Bắt lỗi cụ thể của EmguCV
            {
                statusUpdateCallback($"Lỗi khởi tạo camera: Emgu.CV không thể tìm thấy camera hoặc có lỗi thư viện. {ex.Message}", Color.Red);
                return false;
            }
            catch (Exception ex)
            {
                statusUpdateCallback($"Lỗi khởi tạo camera không xác định: {ex.Message}", Color.Red);
                return false;
            }
        }

        private void InitializeApiClient()
        {
            httpClient = new HttpClient();
            // Đặt BaseAddress MÀ KHÔNG có đường dẫn endpoint cụ thể
            httpClient.BaseAddress = new Uri("http://127.0.0.1:5000/");
            httpClient.Timeout = TimeSpan.FromSeconds(10); // Tăng timeout một chút
        }

        public async Task<Mat> QueryFrameAsync()
        {
            if (capture == null || !capture.IsOpened) return null;
            
            return capture.QueryFrame();
        }

        public void StopCamera()
        {
            capture?.Stop();
        }

        public void DisposeCamera()
        {
            capture?.Dispose();
            httpClient?.Dispose();
        }

        public bool IsCameraOpened()
        {
            return capture != null && capture.IsOpened;
        }

        // Tách riêng logic gọi API và cập nhật UI
        public async Task<string> RecognizeAndDisplayPlateAsync(Mat frame)
        {
            try
            {
                var result = await RecognizePlateWithRoiAsync(frame);
                string plateNumber = result.Item1;
                Mat roiMat = result.Item2;

                if (!string.IsNullOrEmpty(plateNumber))
                {
                    // Lưu ảnh ROI biển số vào biến thành viên
                    if (roiMat != null && !roiMat.IsEmpty)
                    {
                        // Giải phóng ảnh ROI cũ nếu có
                        if (recognizedRoiImage != null)
                        {
                            recognizedRoiImage.Dispose();
                            recognizedRoiImage = null;
                        }

                        // Chuyển đổi và lưu ảnh ROI mới
                        recognizedRoiImage = roiMat.ToBitmap();
                    }

                    plateNumberUpdateCallback(plateNumber);
                    statusUpdateCallback("Đã nhận diện thành công!", Color.Green);
                    return plateNumber;
                }
                else
                {
                    statusUpdateCallback("Không tìm thấy biển số.", Color.Orange);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                statusUpdateCallback($"Lỗi trong quá trình nhận diện: {ex.Message}", Color.Red);
                return string.Empty;
            }
            finally
            {
                frame?.Dispose(); // Giải phóng frame đã clone nếu RecognizePlateAsync không làm
            }
        }

        // Gửi frame đến API và nhận kết quả cùng với ảnh ROI
        private async Task<Tuple<string, Mat>> RecognizePlateWithRoiAsync(Mat frame)
        {
            byte[] imageBytes = null;
            Mat roiImage = null;

            try
            {
                // Chuyển đổi thành dữ liệu JPEG
                imageBytes = frame.ToImage<Bgr, byte>().ToJpegData(90);

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    statusUpdateCallback("Lỗi: Không thể chuyển đổi ảnh thành mảng byte.", Color.Red);
                    return new Tuple<string, Mat>(string.Empty, null);
                }

                // Tạo nội dung multipart
                using (var content = new MultipartFormDataContent())
                using (var fileContent = new ByteArrayContent(imageBytes))
                {
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    content.Add(fileContent, "file", "image.jpg");

                    try
                    {
                        var response = await httpClient.PostAsync("recognize", content);

                        if (!response.IsSuccessStatusCode)
                        {
                            string errorContent = await response.Content.ReadAsStringAsync();
                            statusUpdateCallback($"Lỗi API: {response.StatusCode} - {response.ReasonPhrase}. Chi tiết: {errorContent}", Color.Red);
                            return new Tuple<string, Mat>(string.Empty, null);
                        }

                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<RecognitionResult>(jsonResponse);

                        if (result != null && result.Success && result.Plates != null && result.Plates.Length > 0)
                        {
                            // Nếu API trả về tọa độ ROI, tạo ảnh cắt từ frame gốc
                            if (result.Roi != null && result.Roi.Length == 4)
                            {
                                // Roi format: [x, y, width, height]
                                int x = result.Roi[0];
                                int y = result.Roi[1];
                                int width = result.Roi[2];
                                int height = result.Roi[3];

                                // Đảm bảo tọa độ hợp lệ
                                if (x >= 0 && y >= 0 && width > 0 && height > 0 &&
                                    x + width <= frame.Width && y + height <= frame.Height)
                                {
                                    // Tạo ROI từ frame
                                    var rect = new Rectangle(x, y, width, height);
                                    roiImage = new Mat(frame, rect);
                                }
                            }

                            return new Tuple<string, Mat>(result.Plates[0], roiImage);
                        }
                        else if (result != null && !result.Success)
                        {
                            statusUpdateCallback($"API báo lỗi: {result.Error ?? "Lỗi không xác định"}", Color.Orange);
                        }
                    }
                    catch (Exception ex)
                    {
                        statusUpdateCallback($"Lỗi khi gọi API: {ex.Message}", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                statusUpdateCallback($"Lỗi cục bộ trước khi gọi API: {ex.Message}", Color.Red);
            }

            return new Tuple<string, Mat>(string.Empty, null);
        }

        // Cập nhật RecognitionResult để hỗ trợ tọa độ ROI
        private class RecognitionResult
        {
            public bool Success { get; set; }
            public string[] Plates { get; set; }
            public string Error { get; set; }
            public int[] Roi { get; set; } // Tọa độ ROI: [x, y, width, height]
        }
    }
} 