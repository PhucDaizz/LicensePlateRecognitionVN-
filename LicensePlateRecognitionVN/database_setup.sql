-- Create database if not exists
CREATE DATABASE IF NOT EXISTS LicensePlateRecognition;

-- Use the database
USE LicensePlateRecognition;

-- Create the PlateNumber table
CREATE TABLE IF NOT EXISTS `PlateNumber` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `license_plate` VARCHAR(20) NOT NULL,
  `entry_time` DATETIME NULL,
  `exit_time` DATETIME NULL,
  `image_path` VARCHAR(255) NULL,
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Optional: Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_license_plate ON PlateNumber(license_plate);
CREATE INDEX IF NOT EXISTS idx_entry_time ON PlateNumber(entry_time);
CREATE INDEX IF NOT EXISTS idx_exit_time ON PlateNumber(exit_time);

-- Optional: Insert some sample data
INSERT INTO PlateNumber (license_plate, entry_time, exit_time) VALUES
('59A-12345', NOW() - INTERVAL 2 HOUR, NOW() - INTERVAL 1 HOUR),
('51G-67890', NOW() - INTERVAL 3 HOUR, NOW() - INTERVAL 2 HOUR),
('43B-54321', NOW() - INTERVAL 1 HOUR, NULL);

-- Show the table schema
DESCRIBE PlateNumber;

-- Show the sample data
SELECT * FROM PlateNumber; 