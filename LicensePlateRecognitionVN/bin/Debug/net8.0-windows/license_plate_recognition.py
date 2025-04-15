
import cv2
import easyocr
import numpy as np
from ultralytics import YOLO
import threading
import re
import logging

class LicensePlateRecognizer:
    def __init__(self, model_path, ocr_languages=['vi', 'en']):
        # Cấu hình logging
        logging.basicConfig(level=logging.INFO,
                            format='%(asctime)s - %(levelname)s: %(message)s')

        # Cấu hình model và OCR
        logging.info('Đang khởi tạo model...')
        self.model = YOLO(model_path)
        self.reader = easyocr.Reader(
            ocr_languages,
            gpu=False,
            download_enabled=True,
            model_storage_directory='./ocr_models'
        )
        logging.info('Khởi tạo model hoàn tất!')

        # Cấu hình nhận diện
        self.min_confidence = 0.6
        self.min_plate_width = 50
        self.min_plate_height = 20

        # Khóa đồng bộ cho threading
        self.ocr_lock = threading.Lock()
        self.latest_ocr_text = ''
        self.last_reported_text = ''

    def preprocess_plate(self, roi):
        try:
            # Chuyển ảnh xám và cân bằng histogram
            gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
            clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
            enhanced = clahe.apply(gray)

            # Làm sắc nét
            kernel = np.array([[0, -1, 0], [-1, 5, -1], [0, -1, 0]])
            sharpened = cv2.filter2D(enhanced, -1, kernel)

            # Phân ngưỡng nhị phân
            _, binary = cv2.threshold(sharpened, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
            return binary
        except Exception as e:
            logging.error(f'Lỗi tiền xử lý ảnh: {e}')
            return None

    def format_vietnam_plate(self, text):
        text = text.upper().replace(' ', '').replace('.', '-')

        # Kiểm tra biển xe máy
        motorbike_pattern = re.compile(r'^[0-9]{2}[A-Z][0-9]{3,4}$')
        if motorbike_pattern.match(text):
            return text

        # Thử định dạng biển ô tô
        # Trường hợp 1: 2 chữ số, 1 chữ cái, 4 hoặc 5 chữ số
        car_pattern1 = re.compile(r'^([0-9]{2})([A-Z])([0-9]{4,5})$')
        match = car_pattern1.match(text)
        if match:
            return f'{match.group(1)}{match.group(2)}-{match.group(3)}'

        # Trường hợp 2: 2 chữ số, 2 ký tự chữ-số, 5 chữ số
        car_pattern2 = re.compile(r'^([0-9]{2})([A-Z0-9]{2})([0-9]{5})$')
        match = car_pattern2.match(text)
        if match:
            return f'{match.group(1)}{match.group(2)}-{match.group(3)}'

        # Nếu không khớp, trả về chuỗi rỗng
        return ''

    def async_ocr(self, roi):
        try:
            processed_img = self.preprocess_plate(roi)
            if processed_img is None:
                return

            results = self.reader.readtext(
                processed_img,
                decoder='beamsearch',
                beamWidth=5,
                batch_size=1,
                allowlist='0123456789ABCDEFGHKLMNPSTUVXYZ-',
                detail=0
            )

            combined_text = ''.join(results)
            formatted_text = self.format_vietnam_plate(combined_text)

            # Chỉ cập nhật nếu kết quả OCR hợp lệ
            if formatted_text:
                with self.ocr_lock:
                    self.latest_ocr_text = formatted_text
                    # Print to stdout for C# to capture
                    if formatted_text != self.last_reported_text:
                        print(f'PLATE: {formatted_text}', flush=True)
                        self.last_reported_text = formatted_text
        except Exception as e:
            logging.error(f'Lỗi OCR: {e}')

    def process_camera_stream(self, camera_id=0):
        cap = cv2.VideoCapture(camera_id)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)

        logging.info('Bắt đầu nhận diện. Nhấn ESC hoặc Q để thoát...')

        while True:
            ret, frame = cap.read()
            if not ret:
                logging.error('Không thể đọc frame từ camera!')
                break

            # Nhận diện biển số với YOLO
            results = self.model.predict(
                frame,
                imgsz=320,
                conf=self.min_confidence,
                verbose=False
            )

            for result in results:
                boxes = result.boxes.xyxy.cpu().numpy()

                for box in boxes:
                    x1, y1, x2, y2 = map(int, box)

                    # Bỏ qua các vùng quá nhỏ
                    if (x2 - x1) < self.min_plate_width or (y2 - y1) < self.min_plate_height:
                        continue

                    plate_roi = frame[y1:y2, x1:x2]

                    # Xử lý OCR song song
                    threading.Thread(
                        target=self.async_ocr,
                        args=(plate_roi.copy(),),
                        daemon=True
                    ).start()

                    # Vẽ khung và text
                    cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)

            # Hiển thị biển số nhận dạng được
            with self.ocr_lock:
                display_text = self.latest_ocr_text

            if display_text:
                cv2.putText(
                    frame,
                    f'Bien so: {display_text}',
                    (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.8,
                    (0, 255, 0),
                    2
                )

            cv2.imshow('Nhan Dien Bien So Xe Viet Nam', frame)

            # Điều kiện thoát
            key = cv2.waitKey(1)
            if key in (27, ord('q')):
                break

        # Giải phóng tài nguyên
        cap.release()
        cv2.destroyAllWindows()
        logging.info('Đã thoát chương trình.')


def main():
    # Đường dẫn đến mô hình YOLO
    MODEL_PATH = 'best.pt'

    # Khởi tạo và chạy hệ thống nhận diện
    recognizer = LicensePlateRecognizer(MODEL_PATH)
    recognizer.process_camera_stream()


if __name__ == '__main__':
    main()
