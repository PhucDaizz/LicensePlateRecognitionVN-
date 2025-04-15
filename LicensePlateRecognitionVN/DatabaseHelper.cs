using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Text;

namespace LicensePlateRecognitionVN
{
    public class DatabaseHelper
    {
        private readonly string connectionString;

        // Constructor gốc dùng tham số
        public DatabaseHelper(string server, string database, string userId, string password, int port = 1433)
        {
            // Tạo chuỗi kết nối dựa trên tham số
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                // Sử dụng Windows Authentication nếu không có userId/password
                connectionString = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=Yes";
            }
            else
            {
                // Sử dụng SQL Authentication với userId và password
                connectionString = $"Server={server};Database={database};User Id={userId};Password={password};TrustServerCertificate=Yes";
            }
            
            if (string.IsNullOrEmpty(connectionString)) 
            { 
                throw new InvalidOperationException("Connection string không được để trống"); 
            }
        }

        // Constructor không tham số
        public DatabaseHelper()
        {
            // Tạo connection string với Windows Authentication để đơn giản
            var dbConfig = DbConfig.Load();
            connectionString = $"Server={dbConfig.Server};Database={dbConfig.Database};Trusted_Connection=True;TrustServerCertificate=Yes";
            
            if (string.IsNullOrEmpty(connectionString)) 
            { 
                throw new InvalidOperationException("Connection string không được để trống"); 
            }
        }

        // Test connection to database
        public bool TestConnection()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Check if database exists, if not create it
        public bool InitializeDatabase()
        {
            try
            {
                // Get only server connection without database
                string serverConnection = connectionString.Replace($"Database={GetDatabaseName()};", "");
                
                using (SqlConnection connection = new SqlConnection(serverConnection))
                {
                    connection.Open();
                    
                    // Check if database exists
                    string dbName = GetDatabaseName();
                    string checkDbQuery = $"SELECT DB_ID('{dbName}')";
                    
                    using (SqlCommand command = new SqlCommand(checkDbQuery, connection))
                    {
                        var result = command.ExecuteScalar();
                        
                        // If database doesn't exist, create it
                        if (result == null || result == DBNull.Value)
                        {
                            string createDbQuery = $"CREATE DATABASE [{dbName}]";
                            using (SqlCommand createDbCommand = new SqlCommand(createDbQuery, connection))
                            {
                                createDbCommand.ExecuteNonQuery();
                                Console.WriteLine($"Database [{dbName}] created successfully.");
                            }
                        }
                    }
                    
                    // Now create table if it doesn't exist
                    using (SqlConnection dbConnection = new SqlConnection(connectionString))
                    {
                        dbConnection.Open();
                        
                        string createTableQuery = @"
                            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PlateNumber]') AND type in (N'U'))
                            BEGIN
                                CREATE TABLE [dbo].[PlateNumber] (
                                    [id] INT IDENTITY(1,1) PRIMARY KEY,
                                    [license_plate] VARCHAR(20) NOT NULL,
                                    [entry_time] DATETIME NULL,
                                    [exit_time] DATETIME NULL,
                                    [image_path] VARCHAR(255) NULL,
                                    [created_at] DATETIME DEFAULT GETDATE()
                                );

                                -- Create indexes
                                CREATE INDEX idx_license_plate ON PlateNumber(license_plate);
                                CREATE INDEX idx_entry_time ON PlateNumber(entry_time);
                                CREATE INDEX idx_exit_time ON PlateNumber(exit_time);
                                
                                PRINT 'Table PlateNumber created successfully.';
                            END
                            ELSE
                            BEGIN
                                PRINT 'Table PlateNumber already exists.';
                            END";
                        
                        using (SqlCommand createTableCommand = new SqlCommand(createTableQuery, dbConnection))
                        {
                            createTableCommand.ExecuteNonQuery();
                        }
                        
                        // Ensure the image_path column exists (in case of older versions of the table)
                        EnsureImagePathColumnExists(dbConnection);
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}, {ex.StackTrace}");
                return false;
            }
        }

        // Get database name from connection string
        private string GetDatabaseName()
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("Database="))
                {
                    return part.Substring(9);
                }
            }
            return string.Empty;
        }

        // Record vehicle entry
        public bool RecordEntry(string licensePlate)
        {
            if (string.IsNullOrEmpty(licensePlate))
                return false;

            // Chuẩn hóa biển số - loại bỏ các ký tự không hợp lệ và thêm dấu cách
            string normalizedPlate = NormalizeLicensePlate(licensePlate);

            // Kiểm tra kết nối
            if (!TestConnection())
            {
                Console.WriteLine("DatabaseHelper: Không thể kết nối đến CSDL khi ghi nhận xe vào.");
                return false;
            }

            try
            {
                // Kiểm tra xem biển số đã có trong bãi chưa (có record vào mà chưa có giờ ra)
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Kiểm tra nếu bảng PlateNumber tồn tại
                    if (!CheckTableExists(conn, "PlateNumber"))
                    {
                        Console.WriteLine("DatabaseHelper: Bảng PlateNumber không tồn tại. Đang khởi tạo.");
                        InitializeDatabase();
                    }

                    // Kiểm tra xem biển số này đã có trong bãi chưa (chưa có giờ ra)
                    string checkQuery = "SELECT COUNT(*) FROM PlateNumber WHERE license_plate = @license_plate AND exit_time IS NULL";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@license_plate", normalizedPlate);
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            Console.WriteLine($"DatabaseHelper: Xe biển số {normalizedPlate} đã trong bãi.");
                            return false; // Xe đã trong bãi
                        }
                    }

                    // Thêm bản ghi mới cho xe vào
                    string insertQuery = @"
                        INSERT INTO PlateNumber (license_plate, entry_time)
                        VALUES (@license_plate, @entry_time);
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        // Thêm các tham số
                        cmd.Parameters.AddWithValue("@license_plate", normalizedPlate);
                        cmd.Parameters.AddWithValue("@entry_time", DateTime.Now);

                        try 
                        {
                            // Thực thi câu lệnh và lấy ID
                            var result = cmd.ExecuteScalar();
                            
                            if (result != null)
                            {
                                int newId = Convert.ToInt32(result);
                                Console.WriteLine($"DatabaseHelper: Đã thêm thành công xe {normalizedPlate} vào bãi với ID={newId}");
                                return true;
                            }
                            else
                            {
                                Console.WriteLine($"DatabaseHelper: Lỗi khi thêm xe {normalizedPlate} - ExecuteScalar trả về null");
                                return false;
                            }
                        }
                        catch (SqlException sqlEx)
                        {
                            Console.WriteLine($"DatabaseHelper - SQL Error trong RecordEntry: {sqlEx.Message}, Error code: {sqlEx.Number}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DatabaseHelper - Lỗi RecordEntry: {ex.Message}, StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Record vehicle exit
        public bool RecordExit(string licensePlate)
        {
            if (string.IsNullOrEmpty(licensePlate))
                return false;

            // Chuẩn hóa biển số - loại bỏ các ký tự không hợp lệ và thêm dấu cách
            string normalizedPlate = NormalizeLicensePlate(licensePlate);

            // Kiểm tra kết nối
            if (!TestConnection())
            {
                Console.WriteLine("DatabaseHelper: Không thể kết nối đến CSDL khi ghi nhận xe ra.");
                return false;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Kiểm tra nếu bảng PlateNumber tồn tại
                    if (!CheckTableExists(conn, "PlateNumber"))
                    {
                        Console.WriteLine("DatabaseHelper: Bảng PlateNumber không tồn tại. Đang khởi tạo.");
                        InitializeDatabase();
                    }

                    // Kiểm tra xem biển số có trong bãi không
                    string checkQuery = "SELECT COUNT(*) FROM PlateNumber WHERE license_plate = @license_plate AND exit_time IS NULL";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@license_plate", normalizedPlate);
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count == 0)
                        {
                            Console.WriteLine($"DatabaseHelper: Không tìm thấy xe {normalizedPlate} trong bãi để ghi nhận ra.");
                            return false; // Xe không có trong bãi
                        }
                    }

                    // Tìm bản ghi xe vào mà chưa có giờ ra
                    string updateQuery = @"
                        UPDATE PlateNumber 
                        SET exit_time = @exit_time
                        WHERE license_plate = @license_plate AND exit_time IS NULL;
                        SELECT @@ROWCOUNT;";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@license_plate", normalizedPlate);
                        cmd.Parameters.AddWithValue("@exit_time", DateTime.Now);

                        try
                        {
                            int rowsAffected = (int)cmd.ExecuteScalar();
                            
                            if (rowsAffected > 0)
                            {
                                Console.WriteLine($"DatabaseHelper: Đã cập nhật thành công thời gian ra cho xe {normalizedPlate}");
                                return true;
                            }
                            else
                            {
                                Console.WriteLine($"DatabaseHelper: Không cập nhật được thời gian ra cho xe {normalizedPlate}");
                                return false;
                            }
                        }
                        catch (SqlException sqlEx)
                        {
                            Console.WriteLine($"DatabaseHelper - SQL Error trong RecordExit: {sqlEx.Message}, Error code: {sqlEx.Number}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DatabaseHelper - Lỗi RecordExit: {ex.Message}, StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Chuẩn hóa biển số, loại bỏ ký tự không hợp lệ
        private string NormalizeLicensePlate(string licensePlate)
        {
            if (string.IsNullOrEmpty(licensePlate))
                return string.Empty;

            // Loại bỏ các ký tự đặc biệt không nằm trong A-Z, 0-9, dấu cách và dấu gạch ngang
            string normalized = System.Text.RegularExpressions.Regex.Replace(
                licensePlate, 
                @"[^A-Z0-9\-\. ]", 
                "", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            // Loại bỏ khoảng trắng thừa
            normalized = System.Text.RegularExpressions.Regex.Replace(
                normalized, 
                @"\s+", 
                " "
            ).Trim();
            
            return normalized;
        }

        // Kiểm tra bảng có tồn tại không
        private bool CheckTableExists(SqlConnection connection, string tableName)
        {
            string checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @tableName";
                
            using (SqlCommand cmd = new SqlCommand(checkTableQuery, connection))
            {
                cmd.Parameters.AddWithValue("@tableName", tableName);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        // Get all current vehicles in parking lot (with no exit time)
        public DataTable GetCurrentVehicles()
        {
            DataTable result = new DataTable();
            
            try
            {
                if (!TestConnection())
                {
                    Console.WriteLine("Không thể kết nối đến CSDL khi lấy danh sách xe.");
                    return result;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT id, license_plate as 'Biển số', 
                            entry_time as 'Thời gian vào',
                            DATEDIFF(MINUTE, entry_time, GETDATE()) as 'Thời gian đỗ (phút)'
                        FROM PlateNumber 
                        WHERE exit_time IS NULL
                        ORDER BY entry_time DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy danh sách xe hiện tại: {ex.Message}");
            }

            return result;
        }

        // Get vehicle by license plate
        public DataTable GetVehicleByLicensePlate(string licensePlate)
        {
            DataTable result = new DataTable();
            
            try
            {
                if (string.IsNullOrEmpty(licensePlate))
                    return result;

                if (!TestConnection())
                {
                    Console.WriteLine("Không thể kết nối đến CSDL khi tìm kiếm xe.");
                    return result;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT id, license_plate as 'Biển số', 
                            entry_time as 'Thời gian vào',
                            exit_time as 'Thời gian ra',
                            CASE
                                WHEN exit_time IS NULL THEN DATEDIFF(MINUTE, entry_time, GETDATE())
                                ELSE DATEDIFF(MINUTE, entry_time, exit_time)
                            END as 'Thời gian đỗ (phút)'
                        FROM PlateNumber 
                        WHERE license_plate LIKE @license_plate
                        ORDER BY entry_time DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@license_plate", "%" + licensePlate + "%");
                        conn.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tìm kiếm xe: {ex.Message}");
            }

            return result;
        }

        // Get parking history (list of vehicle entries and exits)
        public DataTable GetParkingHistory(int limit = 100)
        {
            DataTable result = new DataTable();
            
            try
            {
                if (!TestConnection())
                {
                    Console.WriteLine("Không thể kết nối đến CSDL khi lấy lịch sử.");
                    return result;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT TOP (@limit) id, license_plate as 'Biển số', 
                            FORMAT(entry_time, 'dd/MM/yyyy HH:mm:ss') as 'Thời gian vào',
                            CASE
                                WHEN exit_time IS NULL THEN 'Đang trong bãi'
                                ELSE FORMAT(exit_time, 'dd/MM/yyyy HH:mm:ss')
                            END as 'Thời gian ra',
                            CASE
                                WHEN exit_time IS NULL THEN DATEDIFF(MINUTE, entry_time, GETDATE())
                                ELSE DATEDIFF(MINUTE, entry_time, exit_time)
                            END as 'Thời gian đỗ (phút)'
                        FROM PlateNumber 
                        ORDER BY entry_time DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@limit", limit);
                        conn.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy lịch sử bãi đỗ: {ex.Message}");
            }

            return result;
        }
        
        // Search parking history by license plate and date range
        public DataTable SearchParkingHistory(string licensePlate = "", DateTime? fromDate = null, DateTime? toDate = null)
        {
            DataTable result = new DataTable();
            
            try
            {
                if (!TestConnection())
                {
                    Console.WriteLine("Không thể kết nối đến CSDL khi tìm kiếm lịch sử.");
                    return result;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    // Xây dựng câu truy vấn cơ bản
                    StringBuilder queryBuilder = new StringBuilder();
                    queryBuilder.Append(@"
                        SELECT id, license_plate as 'Biển số', 
                            FORMAT(entry_time, 'dd/MM/yyyy HH:mm:ss') as 'Thời gian vào',
                            CASE
                                WHEN exit_time IS NULL THEN 'Đang trong bãi'
                                ELSE FORMAT(exit_time, 'dd/MM/yyyy HH:mm:ss')
                            END as 'Thời gian ra',
                            CASE
                                WHEN exit_time IS NULL THEN DATEDIFF(MINUTE, entry_time, GETDATE())
                                ELSE DATEDIFF(MINUTE, entry_time, exit_time)
                            END as 'Thời gian đỗ (phút)'
                        FROM PlateNumber 
                        WHERE 1=1");

                    // Thêm điều kiện biển số nếu có
                    if (!string.IsNullOrEmpty(licensePlate))
                    {
                        queryBuilder.Append(" AND license_plate LIKE @license_plate");
                    }

                    // Thêm điều kiện ngày từ nếu có
                    if (fromDate.HasValue)
                    {
                        queryBuilder.Append(" AND entry_time >= @from_date");
                    }

                    // Thêm điều kiện ngày đến nếu có
                    if (toDate.HasValue)
                    {
                        // Để bao gồm cả ngày toDate, chúng ta cần lấy đến cuối ngày đó
                        queryBuilder.Append(" AND entry_time <= @to_date");
                    }

                    // Thêm điều kiện sắp xếp
                    queryBuilder.Append(" ORDER BY entry_time DESC");

                    using (SqlCommand cmd = new SqlCommand(queryBuilder.ToString(), conn))
                    {
                        // Thêm tham số cho biển số nếu có
                        if (!string.IsNullOrEmpty(licensePlate))
                        {
                            cmd.Parameters.AddWithValue("@license_plate", "%" + licensePlate + "%");
                        }

                        // Thêm tham số cho ngày từ nếu có
                        if (fromDate.HasValue)
                        {
                            // Đặt thời gian là đầu ngày
                            DateTime fromDateValue = fromDate.Value.Date;
                            cmd.Parameters.AddWithValue("@from_date", fromDateValue);
                        }

                        // Thêm tham số cho ngày đến nếu có
                        if (toDate.HasValue)
                        {
                            // Đặt thời gian là cuối ngày
                            DateTime toDateValue = toDate.Value.Date.AddDays(1).AddSeconds(-1);
                            cmd.Parameters.AddWithValue("@to_date", toDateValue);
                        }

                        conn.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tìm kiếm lịch sử bãi đỗ: {ex.Message}");
            }

            return result;
        }
        
        // Embedded DbConfig class
        public class DbConfig
        {
            public string Server { get; set; } = "localhost";
            public string Database { get; set; } = "LicensePlateRecognition";
            public string UserId { get; set; } = "sa";
            public string Password { get; set; } = "";
            public int Port { get; set; } = 1433;

            private static readonly string ConfigFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LicensePlateRecognitionVN",
                "dbconfig.json");

            // Load configuration from file
            public static DbConfig Load()
            {
                try
                {
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));

                    if (File.Exists(ConfigFilePath))
                    {
                        string json = File.ReadAllText(ConfigFilePath);
                        return JsonSerializer.Deserialize<DbConfig>(json) ?? new DbConfig();
                    }
                }
                catch
                {
                    // If any error occurs, return default config
                }

                return new DbConfig();
            }

            // Save configuration to file
            public void Save()
            {
                try
                {
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));

                    string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(ConfigFilePath, json);
                }
                catch
                {
                    // Ignore save errors
                }
            }

            // Create a DatabaseHelper instance using this configuration
            public DatabaseHelper CreateHelper()
            {
                return new DatabaseHelper(Server, Database, UserId, Password, Port);
            }
        }
        
        // Embedded DbConfigForm class
        public class DbConfigForm : Form
        {
            private readonly DbConfig config;
            private TextBox txtServer;
            private TextBox txtDatabase;
            private TextBox txtPort;
            private TextBox txtUserId;
            private TextBox txtPassword;
            private Button btnTest;
            private Button btnSave;
            private Button btnCancel;

            public DbConfigForm()
            {
                InitializeComponent();
                config = DbConfig.Load();
                LoadConfigToForm();
            }

            private void LoadConfigToForm()
            {
                txtServer.Text = config.Server;
                txtDatabase.Text = config.Database;
                txtPort.Text = config.Port.ToString();
                txtUserId.Text = config.UserId;
                txtPassword.Text = config.Password;
            }

            private void SaveFormToConfig()
            {
                config.Server = txtServer.Text.Trim();
                config.Database = txtDatabase.Text.Trim();
                
                if (int.TryParse(txtPort.Text, out int port))
                {
                    config.Port = port;
                }
                
                config.UserId = txtUserId.Text.Trim();
                config.Password = txtPassword.Text;
            }

            private void btnTest_Click(object sender, EventArgs e)
            {
                SaveFormToConfig();
                var dbHelper = config.CreateHelper();
                
                Cursor = Cursors.WaitCursor;
                bool success = dbHelper.TestConnection();
                Cursor = Cursors.Default;

                if (success)
                {
                    MessageBox.Show("Kết nối thành công!", "Kết nối CSDL", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Không thể kết nối đến cơ sở dữ liệu! Vui lòng kiểm tra lại thông tin kết nối.", 
                        "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void btnSave_Click(object sender, EventArgs e)
            {
                SaveFormToConfig();
                
                try
                {
                    config.Save();
                    
                    var dbHelper = config.CreateHelper();
                    bool initialized = dbHelper.InitializeDatabase();
                    
                    if (initialized)
                    {
                        MessageBox.Show("Đã lưu cấu hình và khởi tạo cơ sở dữ liệu thành công!", 
                            "Lưu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Đã lưu cấu hình nhưng không thể khởi tạo cơ sở dữ liệu. Vui lòng kiểm tra lại thông tin kết nối.", 
                            "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi lưu cấu hình: {ex.Message}", 
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void btnCancel_Click(object sender, EventArgs e)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }

            private void InitializeComponent()
            {
                this.txtServer = new TextBox();
                this.txtDatabase = new TextBox();
                this.txtPort = new TextBox();
                this.txtUserId = new TextBox();
                this.txtPassword = new TextBox();
                this.btnTest = new Button();
                this.btnSave = new Button();
                this.btnCancel = new Button();
                
                // Form
                this.Text = "Cấu hình cơ sở dữ liệu";
                this.ClientSize = new System.Drawing.Size(474, 349);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterParent;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                
                // Labels
                Label lblTitle = new Label();
                lblTitle.Text = "Cấu hình kết nối cơ sở dữ liệu";
                lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
                lblTitle.Location = new System.Drawing.Point(12, 9);
                lblTitle.Size = new System.Drawing.Size(395, 32);
                
                Label lblServer = new Label();
                lblServer.Text = "Server:";
                lblServer.Location = new System.Drawing.Point(14, 60);
                lblServer.Size = new System.Drawing.Size(65, 25);
                
                Label lblDatabase = new Label();
                lblDatabase.Text = "Database:";
                lblDatabase.Location = new System.Drawing.Point(14, 100);
                lblDatabase.Size = new System.Drawing.Size(95, 25);
                
                Label lblPort = new Label();
                lblPort.Text = "Port:";
                lblPort.Location = new System.Drawing.Point(14, 140);
                lblPort.Size = new System.Drawing.Size(50, 25);
                
                Label lblUserId = new Label();
                lblUserId.Text = "User ID:";
                lblUserId.Location = new System.Drawing.Point(14, 180);
                lblUserId.Size = new System.Drawing.Size(75, 25);
                
                Label lblPassword = new Label();
                lblPassword.Text = "Password:";
                lblPassword.Location = new System.Drawing.Point(14, 220);
                lblPassword.Size = new System.Drawing.Size(93, 25);
                
                // TextBoxes
                txtServer.Location = new System.Drawing.Point(150, 60);
                txtServer.Size = new System.Drawing.Size(280, 30);
                txtServer.Text = "localhost";
                
                txtDatabase.Location = new System.Drawing.Point(150, 100);
                txtDatabase.Size = new System.Drawing.Size(280, 30);
                txtDatabase.Text = "LicensePlateRecognition";
                
                txtPort.Location = new System.Drawing.Point(150, 140);
                txtPort.Size = new System.Drawing.Size(100, 30);
                txtPort.Text = "1433";
                
                txtUserId.Location = new System.Drawing.Point(150, 180);
                txtUserId.Size = new System.Drawing.Size(280, 30);
                txtUserId.Text = "sa";
                
                txtPassword.Location = new System.Drawing.Point(150, 220);
                txtPassword.Size = new System.Drawing.Size(280, 30);
                txtPassword.UseSystemPasswordChar = true;
                
                // Buttons
                btnTest.Text = "Kiểm tra";
                btnTest.Location = new System.Drawing.Point(75, 280);
                btnTest.Size = new System.Drawing.Size(100, 40);
                btnTest.Click += btnTest_Click;
                
                btnSave.Text = "Lưu";
                btnSave.Location = new System.Drawing.Point(200, 280);
                btnSave.Size = new System.Drawing.Size(100, 40);
                btnSave.Click += btnSave_Click;
                
                btnCancel.Text = "Hủy";
                btnCancel.Location = new System.Drawing.Point(325, 280);
                btnCancel.Size = new System.Drawing.Size(100, 40);
                btnCancel.Click += btnCancel_Click;
                
                // Add controls to form
                this.Controls.Add(lblTitle);
                this.Controls.Add(lblServer);
                this.Controls.Add(txtServer);
                this.Controls.Add(lblDatabase);
                this.Controls.Add(txtDatabase);
                this.Controls.Add(lblPort);
                this.Controls.Add(txtPort);
                this.Controls.Add(lblUserId);
                this.Controls.Add(txtUserId);
                this.Controls.Add(lblPassword);
                this.Controls.Add(txtPassword);
                this.Controls.Add(btnTest);
                this.Controls.Add(btnSave);
                this.Controls.Add(btnCancel);
            }
        }

        // Save license plate recognition result
        public bool SaveRecognitionResult(string licensePlate, DateTime recognitionTime, string imagePath = null)
        {
            if (string.IsNullOrEmpty(licensePlate))
                return false;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Trước tiên đảm bảo cột image_path tồn tại
                    EnsureImagePathColumnExists(conn);

                    // Kiểm tra xem biển số đã có bản ghi chưa có thời gian ra không
                    string checkQuery = "SELECT id FROM PlateNumber WHERE license_plate = @license_plate AND exit_time IS NULL";
                    int existingId = -1;

                    using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@license_plate", licensePlate);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            existingId = Convert.ToInt32(result);
                        }
                    }

                    if (existingId > 0)
                    {
                        // Cập nhật đường dẫn ảnh cho bản ghi hiện có
                        string updateQuery = "UPDATE PlateNumber SET image_path = @image_path WHERE id = @id";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", existingId);
                            cmd.Parameters.AddWithValue("@image_path", (object)imagePath ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Thêm bản ghi mới với biển số và đường dẫn ảnh
                        string insertQuery = @"
                            INSERT INTO PlateNumber (license_plate, entry_time, image_path)
                            VALUES (@license_plate, @entry_time, @image_path);";

                        using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@license_plate", licensePlate);
                            cmd.Parameters.AddWithValue("@entry_time", recognitionTime);
                            cmd.Parameters.AddWithValue("@image_path", (object)imagePath ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lưu kết quả nhận diện: {ex.Message}");
                return false;
            }
        }

        private void EnsureImagePathColumnExists(SqlConnection connection)
        {
            try
            {
                string checkColumnQuery = @"
                    IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'PlateNumber' AND COLUMN_NAME = 'image_path'
                    )
                    BEGIN
                        ALTER TABLE PlateNumber
                        ADD image_path VARCHAR(255) NULL;
                    END";

                using (SqlCommand cmd = new SqlCommand(checkColumnQuery, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kiểm tra/thêm cột image_path: {ex.Message}");
            }
        }

        // Cung cấp chuỗi kết nối cho các đối tượng bên ngoài
        public string GetConnectionString()
        {
            return connectionString;
        }
    }
} 