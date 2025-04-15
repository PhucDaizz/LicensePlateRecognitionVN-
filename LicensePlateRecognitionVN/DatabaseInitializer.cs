using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using System.Text;

namespace LicensePlateRecognitionVN
{
    public class DatabaseInitializer
    {
        private static bool _logEnabled = true;
        private static string _logFile = Path.Combine(Application.StartupPath, "connection_log.txt");

        // Ghi log lỗi để debug
        private static void LogMessage(string message)
        {
            if (!_logEnabled) return;
            
            try
            {
                File.AppendAllText(_logFile, $"[{DateTime.Now}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Ignore logging errors
            }
        }

        // Cấu trúc để lưu trữ thông tin cấu hình kết nối
        public class DbConfig
        {
            public string Server { get; set; } = "RYAN";
            public string Database { get; set; } = "LicensePlateRecognition";
            public bool IntegratedSecurity { get; set; } = true;
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";

            // Tạo chuỗi kết nối từ config
            public string CreateConnectionString()
            {
                string connStr;
                
                if (IntegratedSecurity)
                {
                    connStr = $"Server={Server};Database={Database};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
                }
                else
                {
                    connStr = $"Server={Server};Database={Database};User Id={Username};Password={Password};TrustServerCertificate=True;Connect Timeout=30;";
                }
                
                LogMessage($"Created connection string: {connStr}");
                return connStr;
            }

            // Tạo chuỗi kết nối đến master database (để tạo database mới nếu cần)
            public string CreateMasterConnectionString()
            {
                string connStr;
                
                if (IntegratedSecurity)
                {
                    // Thêm quyền cao hơn khi kết nối đến master
                    connStr = $"Server={Server};Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
                }
                else
                {
                    connStr = $"Server={Server};Database=master;User Id={Username};Password={Password};TrustServerCertificate=True;Connect Timeout=30;";
                }
                
                LogMessage($"Created master connection string: {connStr}");
                return connStr;
            }

            // Lưu cấu hình vào file
            public void Save()
            {
                try
                {
                    string configPath = Path.Combine(Application.StartupPath, "dbconfig.json");
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(configPath, json);
                    LogMessage($"Saved configuration to {configPath}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error saving configuration: {ex.Message}");
                    MessageBox.Show($"Không thể lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Tải cấu hình từ file
            public static DbConfig Load()
            {
                try
                {
                    string configPath = Path.Combine(Application.StartupPath, "dbconfig.json");
                    LogMessage($"Attempting to load configuration from {configPath}");
                    
                    if (File.Exists(configPath))
                    {
                        string json = File.ReadAllText(configPath);
                        var config = Newtonsoft.Json.JsonConvert.DeserializeObject<DbConfig>(json) ?? new DbConfig();
                        LogMessage($"Loaded configuration: Server={config.Server}, Database={config.Database}, IntegratedSecurity={config.IntegratedSecurity}");
                        return config;
                    }
                    else
                    {
                        LogMessage("Configuration file not found, returning default config");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error loading configuration: {ex.Message}");
                    MessageBox.Show($"Không thể đọc cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                var defaultConfig = new DbConfig();
                LogMessage($"Using default configuration: Server={defaultConfig.Server}, Database={defaultConfig.Database}, IntegratedSecurity={defaultConfig.IntegratedSecurity}");
                return defaultConfig; // Trả về cấu hình mặc định nếu không đọc được
            }
        }

        // Form cấu hình database
        public class DbConfigForm : Form
        {
            private TextBox txtServer;
            private TextBox txtDatabase;
            private RadioButton rdoIntegrated;
            private RadioButton rdoSqlAuth;
            private TextBox txtUsername;
            private TextBox txtPassword;
            private Button btnTest;
            private Button btnSave;
            private Button btnCancel;
            private Label lblStatus;

            public DbConfigForm()
            {
                InitializeComponent();
                LoadConfig();
                LogMessage("DbConfigForm initialized");
            }

            private void InitializeComponent()
            {
                this.Text = "Cấu hình kết nối SQL Server";
                this.Width = 450;
                this.Height = 350;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterScreen;

                // Labels
                Label lblServer = new Label { Text = "Server:", Left = 20, Top = 20, Width = 100 };
                Label lblDatabase = new Label { Text = "Database:", Left = 20, Top = 50, Width = 100 };
                Label lblAuth = new Label { Text = "Xác thực:", Left = 20, Top = 80, Width = 100 };
                Label lblUsername = new Label { Text = "Username:", Left = 30, Top = 140, Width = 100 };
                Label lblPassword = new Label { Text = "Password:", Left = 30, Top = 170, Width = 100 };

                // TextBoxes
                txtServer = new TextBox { Left = 120, Top = 20, Width = 250, Text = "RYAN" };
                txtDatabase = new TextBox { Left = 120, Top = 50, Width = 250, Text = "LicensePlateRecognition" };
                txtUsername = new TextBox { Left = 120, Top = 140, Width = 250, Enabled = false };
                txtPassword = new TextBox { Left = 120, Top = 170, Width = 250, PasswordChar = '*', Enabled = false };

                // Radio buttons
                rdoIntegrated = new RadioButton { Text = "Windows Authentication", Left = 120, Top = 80, Width = 200, Checked = true };
                rdoSqlAuth = new RadioButton { Text = "SQL Server Authentication", Left = 120, Top = 110, Width = 200 };

                // Buttons
                btnTest = new Button { Text = "Kiểm tra kết nối", Left = 20, Top = 210, Width = 120 };
                btnSave = new Button { Text = "Lưu", Left = 150, Top = 210, Width = 100, DialogResult = DialogResult.OK };
                btnCancel = new Button { Text = "Hủy", Left = 270, Top = 210, Width = 100, DialogResult = DialogResult.Cancel };

                // Status label
                lblStatus = new Label { Left = 20, Top = 250, Width = 350, Height = 40, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };

                // Events
                rdoIntegrated.CheckedChanged += (s, e) => {
                    txtUsername.Enabled = !rdoIntegrated.Checked;
                    txtPassword.Enabled = !rdoIntegrated.Checked;
                };

                btnTest.Click += (s, e) => TestConnection();
                btnSave.Click += (s, e) => SaveConfig();

                // Add controls
                Controls.AddRange(new Control[] {
                    lblServer, txtServer,
                    lblDatabase, txtDatabase,
                    lblAuth, rdoIntegrated, rdoSqlAuth,
                    lblUsername, txtUsername,
                    lblPassword, txtPassword,
                    btnTest, btnSave, btnCancel,
                    lblStatus
                });
            }

            private void LoadConfig()
            {
                DbConfig config = DbConfig.Load();
                txtServer.Text = config.Server;
                txtDatabase.Text = config.Database;
                rdoIntegrated.Checked = config.IntegratedSecurity;
                rdoSqlAuth.Checked = !config.IntegratedSecurity;
                txtUsername.Text = config.Username;
                txtPassword.Text = config.Password;
                txtUsername.Enabled = !config.IntegratedSecurity;
                txtPassword.Enabled = !config.IntegratedSecurity;
                
                LogMessage("Configuration loaded into form");
            }

            private void SaveConfig()
            {
                DbConfig config = new DbConfig
                {
                    Server = txtServer.Text,
                    Database = txtDatabase.Text,
                    IntegratedSecurity = rdoIntegrated.Checked,
                    Username = txtUsername.Text,
                    Password = txtPassword.Text
                };

                config.Save();
                LogMessage("Configuration saved from form");
            }

            private void TestConnection()
            {
                lblStatus.ForeColor = System.Drawing.Color.Blue;
                lblStatus.Text = "Đang kiểm tra kết nối...";
                Application.DoEvents();

                DbConfig config = new DbConfig
                {
                    Server = txtServer.Text,
                    Database = txtDatabase.Text,
                    IntegratedSecurity = rdoIntegrated.Checked,
                    Username = txtUsername.Text,
                    Password = txtPassword.Text
                };

                try
                {
                    // Kiểm tra kết nối đến master
                    string masterConnectionString = config.CreateMasterConnectionString();
                    LogMessage($"Testing connection with: {masterConnectionString}");
                    
                    using (SqlConnection conn = new SqlConnection(masterConnectionString))
                    {
                        conn.Open();
                        lblStatus.Text = "Kết nối đến SQL Server thành công!";
                        lblStatus.ForeColor = System.Drawing.Color.Green;
                        LogMessage("Connection test successful");
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Lỗi kết nối: {ex.Message}";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    LogMessage($"Connection test failed: {ex.Message}");
                }
            }
        }

        // Hàm khởi tạo CSDL
        public static bool InitializeDatabase(string connectionString, Action<string, System.Drawing.Color> updateStatus, Action<string> addLogMessage)
        {
            LogMessage("InitializeDatabase called");
            
            try
            {
                // Kiểm tra xem database đã tồn tại chưa
                DbConfig config = DbConfig.Load();
                bool dbExists = false;

                try
                {
                    // Thử kết nối đến database được chỉ định
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        LogMessage("Attempting to connect to database");
                        conn.Open();
                        dbExists = true;
                        string message = $"Kết nối tới database {config.Database} thành công";
                        updateStatus?.Invoke(message, System.Drawing.Color.Green);
                        addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                        LogMessage(message);
                    }
                }
                catch (SqlException ex)
                {
                    // Database có thể không tồn tại
                    string message = $"Không thể kết nối tới database: {ex.Message}";
                    updateStatus?.Invoke(message, System.Drawing.Color.Orange);
                    addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                    LogMessage($"Database connection failed: {ex.Message}");
                    
                    dbExists = false;
                }

                if (!dbExists)
                {
                    LogMessage("Database does not exist, attempting to create");
                    
                    try 
                    {
                        // Kết nối đến master để tạo DB mới
                        using (SqlConnection masterConn = new SqlConnection(config.CreateMasterConnectionString()))
                        {
                            LogMessage("Connecting to master database");
                            masterConn.Open();
                            
                            string message = "Đã kết nối tới SQL Server, đang tạo database...";
                            updateStatus?.Invoke(message, System.Drawing.Color.Blue);
                            addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                            LogMessage(message);

                            string createDbQuery = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{config.Database}') CREATE DATABASE [{config.Database}]";
                            using (SqlCommand cmd = new SqlCommand(createDbQuery, masterConn))
                            {
                                LogMessage($"Executing query: {createDbQuery}");
                                cmd.ExecuteNonQuery();
                                
                                message = $"Đã tạo database {config.Database}";
                                updateStatus?.Invoke(message, System.Drawing.Color.Green);
                                addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                                LogMessage(message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = $"Lỗi khi tạo database: {ex.Message}";
                        updateStatus?.Invoke(message, System.Drawing.Color.Red);
                        addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                        LogMessage($"Error creating database: {ex.Message}, {ex.StackTrace}");
                        return false;
                    }
                }

                try
                {
                    // Kết nối đến DB và tạo bảng nếu chưa có
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        LogMessage("Connecting to database to create table if needed");
                        conn.Open();
                        
                        string message = "Đang kiểm tra và tạo bảng dữ liệu...";
                        updateStatus?.Invoke(message, System.Drawing.Color.Blue);
                        addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                        LogMessage(message);

                        string createTableQuery = @"
                            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlateNumber')
                            BEGIN
                                CREATE TABLE PlateNumber (
                                    id INT IDENTITY(1,1) PRIMARY KEY,
                                    license_plate NVARCHAR(50) NOT NULL,
                                    entry_time DATETIME NOT NULL,
                                    exit_time DATETIME NULL,
                                    image_path NVARCHAR(255) NULL
                                )
                            END";

                        using (SqlCommand cmd = new SqlCommand(createTableQuery, conn))
                        {
                            LogMessage("Executing create table query");
                            cmd.ExecuteNonQuery();
                            
                            message = "Đã kiểm tra và tạo bảng dữ liệu thành công";
                            updateStatus?.Invoke(message, System.Drawing.Color.Green);
                            addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                            LogMessage(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string message = $"Lỗi khi tạo bảng: {ex.Message}";
                    updateStatus?.Invoke(message, System.Drawing.Color.Red);
                    addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                    LogMessage($"Error creating table: {ex.Message}, {ex.StackTrace}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                string message = $"Lỗi khởi tạo CSDL: {ex.Message}";
                updateStatus?.Invoke(message, System.Drawing.Color.Red);
                addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                LogMessage($"Database initialization error: {ex.Message}, {ex.StackTrace}");
                return false;
            }
        }

        // Hàm kiểm tra kết nối CSDL
        public static bool TestConnection(string connectionString)
        {
            LogMessage($"Testing connection with: {connectionString}");
            
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    LogMessage("Connection test successful");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Connection test failed: {ex.Message}, {ex.StackTrace}");
                return false;
            }
        }

        // Hàm hỗ trợ hiển thị form cấu hình và khởi tạo CSDL
        public static string SetupDatabase(Action<string, System.Drawing.Color> updateStatus, Action<string> addLogMessage)
        {
            LogMessage("SetupDatabase called");
            
            try
            {
                // Xóa file log để bắt đầu ghi mới
                if (File.Exists(_logFile))
                {
                    try { File.Delete(_logFile); } catch { }
                }
                
                LogMessage("Starting database setup");
                
                DbConfig config = DbConfig.Load();
                string connectionString = config.CreateConnectionString();

                // Kiểm tra kết nối
                string message = "Đang kiểm tra kết nối đến SQL Server...";
                updateStatus?.Invoke(message, System.Drawing.Color.Blue);
                addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                LogMessage(message);

                if (!TestConnection(connectionString))
                {
                    LogMessage("Initial connection test failed");
                    
                    // Thử kết nối đến master
                    string masterConnection = config.CreateMasterConnectionString();
                    bool canConnectToMaster = TestConnection(masterConnection);

                    if (!canConnectToMaster)
                    {
                        LogMessage("Cannot connect to master database, showing config form");
                        
                        message = "Không thể kết nối đến SQL Server. Hiển thị form cấu hình...";
                        updateStatus?.Invoke(message, System.Drawing.Color.Orange);
                        addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");

                        // Hiển thị form cấu hình
                        using (DbConfigForm configForm = new DbConfigForm())
                        {
                            if (configForm.ShowDialog() == DialogResult.OK)
                            {
                                LogMessage("Configuration updated by user");
                                
                                config = DbConfig.Load(); // Tải lại config sau khi lưu
                                connectionString = config.CreateConnectionString();
                                masterConnection = config.CreateMasterConnectionString();

                                // Kiểm tra lại kết nối
                                if (TestConnection(masterConnection))
                                {
                                    message = "Kết nối SQL Server thành công sau khi cấu hình lại";
                                    updateStatus?.Invoke(message, System.Drawing.Color.Green);
                                    addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                                    LogMessage(message);
                                }
                                else
                                {
                                    message = "Vẫn không thể kết nối đến SQL Server sau khi cấu hình lại";
                                    updateStatus?.Invoke(message, System.Drawing.Color.Red);
                                    addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                                    LogMessage(message);
                                    
                                    // Hiển thị nội dung file log
                                    if (File.Exists(_logFile))
                                    {
                                        string logContent = File.ReadAllText(_logFile);
                                        MessageBox.Show($"Chi tiết lỗi kết nối:\n\n{logContent}", "Thông tin lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    
                                    return null;
                                }
                            }
                            else
                            {
                                // Người dùng hủy việc cấu hình
                                message = "Người dùng đã hủy việc cấu hình CSDL";
                                updateStatus?.Invoke(message, System.Drawing.Color.Red);
                                addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                                LogMessage(message);
                                return null;
                            }
                        }
                    }
                    else
                    {
                        // Có thể kết nối đến master nhưng không thể kết nối database - có thể cần tạo database
                        message = "Có thể kết nối đến SQL Server nhưng không tìm thấy database. Đang tạo database...";
                        updateStatus?.Invoke(message, System.Drawing.Color.Blue);
                        addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                        LogMessage(message);
                    }
                }

                // Khởi tạo CSDL
                if (InitializeDatabase(connectionString, updateStatus, addLogMessage))
                {
                    message = "Khởi tạo CSDL thành công";
                    updateStatus?.Invoke(message, System.Drawing.Color.Green);
                    addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                    LogMessage(message);
                    return connectionString;
                }

                // Hiển thị nội dung file log nếu có lỗi
                if (File.Exists(_logFile))
                {
                    string logContent = File.ReadAllText(_logFile);
                    MessageBox.Show($"Chi tiết quá trình khởi tạo CSDL:\n\n{logContent}", "Thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                LogMessage("Database initialization failed");
                return null;
            }
            catch (Exception ex)
            {
                string message = $"Lỗi không xác định: {ex.Message}";
                updateStatus?.Invoke(message, System.Drawing.Color.Red);
                addLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
                LogMessage($"Unhandled error in SetupDatabase: {ex.Message}, {ex.StackTrace}");
                
                // Hiển thị nội dung file log
                if (File.Exists(_logFile))
                {
                    string logContent = File.ReadAllText(_logFile);
                    MessageBox.Show($"Chi tiết lỗi:\n\n{logContent}", "Thông tin lỗi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                return null;
            }
        }
    }
} 