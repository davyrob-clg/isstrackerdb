CREATE DATABASE IF NOT EXISTS space_tracking;
USE space_tracking;

CREATE TABLE IF NOT EXISTS iss_positions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    latitude DECIMAL(10, 7) NOT NULL,
    longitude DECIMAL(10, 7) NOT NULL,
    altitude DECIMAL(10, 2),
    velocity DECIMAL(10, 2),
    timestamp BIGINT,
    recorded_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
