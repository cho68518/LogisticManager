-- Users 테이블 생성
CREATE TABLE IF NOT EXISTS gramwonlogis2.Users (
  id int(11) NOT NULL AUTO_INCREMENT,
  username varchar(50) NOT NULL,
  password varchar(255) NOT NULL,
  email varchar(100) DEFAULT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP(),
  last_login datetime DEFAULT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 샘플 사용자 계정 생성 (비밀번호는 'password123')
INSERT INTO gramwonlogis2.Users (username, password, email, created_at) VALUES
('admin', 'password123', 'admin@gramwonlogis.com', NOW()),
('user1', 'password123', 'user1@gramwonlogis.com', NOW()),
('user2', 'password123', 'user2@gramwonlogis.com', NOW())
ON DUPLICATE KEY UPDATE
  password = VALUES(password),
  email = VALUES(email);

-- 사용자 계정 확인
SELECT id, username, email, created_at, last_login FROM gramwonlogis2.Users;
