-- Users 테이블의 비밀번호를 암호화된 형태로 업데이트
-- LogisticManager 프로젝트의 SecurityService를 사용하여 암호화된 비밀번호로 변경
-- 암호화 키: "MySecretKey123!" (SecurityService에서 사용)

-- 기존 사용자들의 비밀번호를 암호화된 형태로 업데이트
-- 원본 비밀번호: 'password123' (모든 사용자)

-- admin 사용자 비밀번호 업데이트
UPDATE gramwonlogis2.Users 
SET password = 'ugP02jQXJEU+a9ol72FULg==' 
WHERE username = 'admin';

-- user1 사용자 비밀번호 업데이트
UPDATE gramwonlogis2.Users 
SET password = 'ugP02jQXJEU+a9ol72FULg==' 
WHERE username = 'user1';

-- user2 사용자 비밀번호 업데이트
UPDATE gramwonlogis2.Users 
SET password = 'ugP02jQXJEU+a9ol72FULg==' 
WHERE username = 'user2';

-- 업데이트 결과 확인
SELECT id, username, email, created_at, last_login FROM gramwonlogis2.Users;

-- 암호화된 비밀번호 확인 (password 컬럼은 보안상 표시하지 않음)
SELECT id, username, email, created_at, last_login, 
       CASE 
           WHEN password IS NOT NULL AND LENGTH(password) > 10 
           THEN CONCAT(LEFT(password, 10), '...') 
           ELSE password 
       END AS password_preview
FROM gramwonlogis2.Users;
