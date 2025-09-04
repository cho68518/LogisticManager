-- Users 테이블에 name 컬럼 추가
ALTER TABLE gramwonlogis2.Users 
ADD COLUMN name VARCHAR(100) NOT NULL DEFAULT '' AFTER username;

-- 기존 사용자들의 name 컬럼 값을 username과 동일하게 설정
UPDATE gramwonlogis2.Users SET name = username WHERE name = '';

-- name 컬럼이 추가된 Users 테이블 구조 확인
DESCRIBE gramwonlogis2.Users;

-- 업데이트된 사용자 정보 확인
SELECT id, username, name, email, created_at, last_login FROM gramwonlogis2.Users;
