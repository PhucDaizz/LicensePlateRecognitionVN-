namespace LicensePlateRecognitionVN
{
    partial class Form3
    {
        private System.ComponentModel.IContainer components = null;

        // Các control chính
        private System.Windows.Forms.PictureBox cameraBox;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblPlate;
        private System.Windows.Forms.TextBox txtPlateNumber;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.Button btnEntry;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Timer apiTimer;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.DataGridView dgvParkingHistory;
        private System.Windows.Forms.Label lblHistory;
        private System.Windows.Forms.Button btnAddImage;
        private System.Windows.Forms.Button btnCamera;
        private System.Windows.Forms.RichTextBox rtbLogs;
        // Thêm các control tìm kiếm
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearchPlate;
        private System.Windows.Forms.Label lblDateFrom;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.Label lblDateTo;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnClearSearch;
        private System.Windows.Forms.Label lblResultCount;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            cameraBox = new PictureBox();
            lblTitle = new Label();
            lblPlate = new Label();
            txtPlateNumber = new TextBox();
            btnConfirm = new Button();
            btnEntry = new Button();
            btnExit = new Button();
            lblStatus = new Label();
            apiTimer = new System.Windows.Forms.Timer(components);
            lblTime = new Label();
            dgvParkingHistory = new DataGridView();
            lblHistory = new Label();
            btnAddImage = new Button();
            btnCamera = new Button();
            rtbLogs = new RichTextBox();
            lblSearch = new Label();
            txtSearchPlate = new TextBox();
            lblDateFrom = new Label();
            dtpFrom = new DateTimePicker();
            lblDateTo = new Label();
            dtpTo = new DateTimePicker();
            btnSearch = new Button();
            btnClearSearch = new Button();
            lblResultCount = new Label();
            ((System.ComponentModel.ISupportInitialize)cameraBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvParkingHistory).BeginInit();
            SuspendLayout();
            // 
            // cameraBox
            // 
            cameraBox.BorderStyle = BorderStyle.FixedSingle;
            cameraBox.Location = new Point(18, 60);
            cameraBox.Margin = new Padding(2);
            cameraBox.Name = "cameraBox";
            cameraBox.Size = new Size(575, 385);
            cameraBox.SizeMode = PictureBoxSizeMode.StretchImage;
            cameraBox.TabIndex = 0;
            cameraBox.TabStop = false;
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.FromArgb(0, 71, 160);
            lblTitle.Font = new Font("Microsoft Sans Serif", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(9, 0);
            lblTitle.Margin = new Padding(2, 0, 2, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(1175, 44);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "HỆ THỐNG QUẢN LÝ BÃI XE BẰNG NHẬN DIỆN BIỂN SỐ";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblPlate
            // 
            lblPlate.AutoSize = true;
            lblPlate.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPlate.Location = new Point(614, 60);
            lblPlate.Margin = new Padding(2, 0, 2, 0);
            lblPlate.Name = "lblPlate";
            lblPlate.Size = new Size(81, 20);
            lblPlate.TabIndex = 0;
            lblPlate.Text = "BIỂN SỐ";
            // 
            // txtPlateNumber
            // 
            txtPlateNumber.Font = new Font("Microsoft Sans Serif", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            txtPlateNumber.Location = new Point(619, 94);
            txtPlateNumber.Margin = new Padding(2);
            txtPlateNumber.Name = "txtPlateNumber";
            txtPlateNumber.Size = new Size(267, 38);
            txtPlateNumber.TabIndex = 0;
            txtPlateNumber.TextAlign = HorizontalAlignment.Center;
            // 
            // btnConfirm
            // 
            btnConfirm.BackColor = Color.FromArgb(0, 71, 160);
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.FlatStyle = FlatStyle.Flat;
            btnConfirm.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnConfirm.ForeColor = Color.White;
            btnConfirm.Location = new Point(619, 154);
            btnConfirm.Margin = new Padding(2);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Size = new Size(267, 50);
            btnConfirm.TabIndex = 1;
            btnConfirm.Text = "XÁC NHẬN BIỂN SỐ";
            btnConfirm.UseVisualStyleBackColor = false;
            btnConfirm.Click += btnConfirm_Click;
            // 
            // btnEntry
            // 
            btnEntry.BackColor = Color.Green;
            btnEntry.FlatAppearance.BorderSize = 0;
            btnEntry.FlatStyle = FlatStyle.Flat;
            btnEntry.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnEntry.ForeColor = Color.White;
            btnEntry.Location = new Point(619, 214);
            btnEntry.Margin = new Padding(2);
            btnEntry.Name = "btnEntry";
            btnEntry.Size = new Size(129, 50);
            btnEntry.TabIndex = 2;
            btnEntry.Text = "XE VÀO";
            btnEntry.UseVisualStyleBackColor = false;
            btnEntry.Click += btnEntry_Click;
            // 
            // btnExit
            // 
            btnExit.BackColor = Color.FromArgb(192, 0, 0);
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnExit.ForeColor = Color.White;
            btnExit.Location = new Point(756, 214);
            btnExit.Margin = new Padding(2);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(129, 50);
            btnExit.TabIndex = 3;
            btnExit.Text = "XE RA";
            btnExit.UseVisualStyleBackColor = false;
            btnExit.Click += btnExit_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatus.ForeColor = Color.DarkGreen;
            lblStatus.Location = new Point(615, 282);
            lblStatus.Margin = new Padding(2, 0, 2, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(120, 15);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Trạng thái: Sẵn sàng";
            // 
            // apiTimer
            // 
            apiTimer.Interval = 500;
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTime.Location = new Point(615, 309);
            lblTime.Margin = new Padding(2, 0, 2, 0);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(104, 15);
            lblTime.TabIndex = 0;
            lblTime.Text = "Thời gian hiện tại:";
            // 
            // dgvParkingHistory
            // 
            dgvParkingHistory.AllowUserToAddRows = false;
            dgvParkingHistory.AllowUserToDeleteRows = false;
            dgvParkingHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvParkingHistory.BackgroundColor = SystemColors.Control;
            dgvParkingHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvParkingHistory.Location = new Point(18, 522);
            dgvParkingHistory.Margin = new Padding(2);
            dgvParkingHistory.Name = "dgvParkingHistory";
            dgvParkingHistory.ReadOnly = true;
            dgvParkingHistory.RowHeadersWidth = 62;
            dgvParkingHistory.RowTemplate.Height = 28;
            dgvParkingHistory.Size = new Size(1175, 290);
            dgvParkingHistory.TabIndex = 2;
            // 
            // lblHistory
            // 
            lblHistory.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHistory.Location = new Point(18, 454);
            lblHistory.Name = "lblHistory";
            lblHistory.Size = new Size(175, 28);
            lblHistory.TabIndex = 0;
            lblHistory.Text = "LỊCH SỬ RA/VÀO";
            // 
            // btnAddImage
            // 
            btnAddImage.BackColor = Color.FromArgb(0, 71, 160);
            btnAddImage.FlatAppearance.BorderSize = 0;
            btnAddImage.FlatStyle = FlatStyle.Flat;
            btnAddImage.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnAddImage.ForeColor = Color.White;
            btnAddImage.Location = new Point(691, 350);
            btnAddImage.Margin = new Padding(2);
            btnAddImage.Name = "btnAddImage";
            btnAddImage.Size = new Size(87, 40);
            btnAddImage.TabIndex = 4;
            btnAddImage.Text = "Tải ảnh";
            btnAddImage.UseVisualStyleBackColor = false;
            btnAddImage.Click += btnAddImage_Click;
            // 
            // btnCamera
            // 
            btnCamera.BackColor = Color.DarkGreen;
            btnCamera.FlatAppearance.BorderSize = 0;
            btnCamera.FlatStyle = FlatStyle.Flat;
            btnCamera.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCamera.ForeColor = Color.White;
            btnCamera.Location = new Point(788, 350);
            btnCamera.Margin = new Padding(2);
            btnCamera.Name = "btnCamera";
            btnCamera.Size = new Size(87, 40);
            btnCamera.TabIndex = 5;
            btnCamera.Text = "Camera";
            btnCamera.UseVisualStyleBackColor = false;
            btnCamera.Click += btnCamera_Click;
            // 
            // rtbLogs
            // 
            rtbLogs.BackColor = Color.Black;
            rtbLogs.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbLogs.ForeColor = Color.Lime;
            rtbLogs.Location = new Point(927, 60);
            rtbLogs.Margin = new Padding(2);
            rtbLogs.Name = "rtbLogs";
            rtbLogs.ReadOnly = true;
            rtbLogs.Size = new Size(290, 380);
            rtbLogs.TabIndex = 10;
            rtbLogs.Text = "";
            // 
            // lblSearch
            // 
            lblSearch.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSearch.Location = new Point(196, 454);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new Size(91, 28);
            lblSearch.TabIndex = 12;
            lblSearch.Text = "Biển số:";
            lblSearch.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtSearchPlate
            // 
            txtSearchPlate.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtSearchPlate.Location = new Point(287, 459);
            txtSearchPlate.Margin = new Padding(2);
            txtSearchPlate.Name = "txtSearchPlate";
            txtSearchPlate.Size = new Size(106, 23);
            txtSearchPlate.TabIndex = 13;
            txtSearchPlate.KeyDown += txtSearchPlate_KeyDown;
            // 
            // lblDateFrom
            // 
            lblDateFrom.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDateFrom.Location = new Point(399, 454);
            lblDateFrom.Name = "lblDateFrom";
            lblDateFrom.Size = new Size(42, 28);
            lblDateFrom.TabIndex = 14;
            lblDateFrom.Text = "Từ:";
            lblDateFrom.TextAlign = ContentAlignment.MiddleRight;
            // 
            // dtpFrom
            // 
            dtpFrom.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dtpFrom.Format = DateTimePickerFormat.Short;
            dtpFrom.Location = new Point(441, 459);
            dtpFrom.Margin = new Padding(2);
            dtpFrom.Name = "dtpFrom";
            dtpFrom.Size = new Size(106, 23);
            dtpFrom.TabIndex = 15;
            // 
            // lblDateTo
            // 
            lblDateTo.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDateTo.Location = new Point(546, 454);
            lblDateTo.Name = "lblDateTo";
            lblDateTo.Size = new Size(42, 28);
            lblDateTo.TabIndex = 16;
            lblDateTo.Text = "Đến:";
            lblDateTo.TextAlign = ContentAlignment.MiddleRight;
            // 
            // dtpTo
            // 
            dtpTo.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dtpTo.Format = DateTimePickerFormat.Short;
            dtpTo.Location = new Point(588, 459);
            dtpTo.Margin = new Padding(2);
            dtpTo.Name = "dtpTo";
            dtpTo.Size = new Size(106, 23);
            dtpTo.TabIndex = 17;
            // 
            // btnSearch
            // 
            btnSearch.BackColor = Color.FromArgb(0, 71, 160);
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSearch.ForeColor = Color.White;
            btnSearch.Location = new Point(698, 454);
            btnSearch.Margin = new Padding(2);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(92, 29);
            btnSearch.TabIndex = 18;
            btnSearch.Text = "Tìm kiếm";
            btnSearch.UseVisualStyleBackColor = false;
            btnSearch.Click += btnSearch_Click;
            // 
            // btnClearSearch
            // 
            btnClearSearch.BackColor = Color.Gray;
            btnClearSearch.FlatAppearance.BorderSize = 0;
            btnClearSearch.FlatStyle = FlatStyle.Flat;
            btnClearSearch.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnClearSearch.ForeColor = Color.White;
            btnClearSearch.Location = new Point(794, 454);
            btnClearSearch.Margin = new Padding(2);
            btnClearSearch.Name = "btnClearSearch";
            btnClearSearch.Size = new Size(102, 29);
            btnClearSearch.TabIndex = 19;
            btnClearSearch.Text = "Xóa tìm";
            btnClearSearch.UseVisualStyleBackColor = false;
            btnClearSearch.Click += btnClearSearch_Click;
            // 
            // lblResultCount
            // 
            lblResultCount.AutoSize = true;
            lblResultCount.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblResultCount.Location = new Point(25, 1085);
            lblResultCount.Margin = new Padding(4, 0, 4, 0);
            lblResultCount.Name = "lblResultCount";
            lblResultCount.Size = new Size(107, 15);
            lblResultCount.TabIndex = 20;
            lblResultCount.Text = "Tổng số kết quả: 0";
            // 
            // Form3
            // 
            AutoScaleDimensions = new SizeF(8F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1228, 1061);
            Controls.Add(lblResultCount);
            Controls.Add(btnClearSearch);
            Controls.Add(btnSearch);
            Controls.Add(dtpTo);
            Controls.Add(lblDateTo);
            Controls.Add(dtpFrom);
            Controls.Add(lblDateFrom);
            Controls.Add(txtSearchPlate);
            Controls.Add(lblSearch);
            Controls.Add(rtbLogs);
            Controls.Add(btnCamera);
            Controls.Add(btnAddImage);
            Controls.Add(dgvParkingHistory);
            Controls.Add(lblHistory);
            Controls.Add(btnExit);
            Controls.Add(btnEntry);
            Controls.Add(btnConfirm);
            Controls.Add(txtPlateNumber);
            Controls.Add(lblTime);
            Controls.Add(lblStatus);
            Controls.Add(lblPlate);
            Controls.Add(lblTitle);
            Controls.Add(cameraBox);
            Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Margin = new Padding(4);
            Name = "Form3";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Hệ thống nhận diện biển số";
            Load += Form3_Load;
            ((System.ComponentModel.ISupportInitialize)cameraBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvParkingHistory).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}