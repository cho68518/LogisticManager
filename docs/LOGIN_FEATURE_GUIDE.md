# 로그인 기능 사용법 가이드

## 개요
LogisticManager 애플리케이션에 로그인 기능이 추가되었습니다. 이 기능은 App.config 설정에 따라 활성화/비활성화할 수 있습니다.

## 설정 방법

### 1. App.config 설정
```xml
<add key="Login" value="Y" />
```
- `value="Y"`: 로그인 기능 활성화
- `value="N"` 또는 설정 없음: 로그인 기능 비활성화

### 2. 데이터베이스 설정
Users 테이블이 데이터베이스에 생성되어 있어야 합니다:
```sql
CREATE TABLE gramwonlogis2.Users (
  id int(11) NOT NULL AUTO_INCREMENT,
  username varchar(50) NOT NULL,
  password varchar(255) NOT NULL,
  email varchar(100) DEFAULT NULL,
  created_at datetime DEFAULT CURRENT_TIMESTAMP(),
  last_login datetime DEFAULT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_username (username)
);
```

## 사용법

### 로그인 기능 활성화 시
1. 애플리케이션 실행
2. 로그인 폼이 자동으로 표시됨
3. 사용자명과 비밀번호 입력
4. 로그인 성공 시 MainForm 표시
5. 로그인 실패 시 오류 메시지 표시

### 로그인 기능 비활성화 시
1. 애플리케이션 실행
2. 로그인 폼 없이 바로 MainForm 표시

## 샘플 사용자 계정

테스트용 샘플 계정:
- **사용자명**: admin
- **비밀번호**: password123
- **이메일**: admin@gramwonlogis.com

## 보안 고려사항

### 현재 구현
- 비밀번호는 평문으로 저장 (개발/테스트용)
- 기본적인 입력값 검증

### 향후 개선사항
- 비밀번호 해싱 (bcrypt, SHA256 등)
- 로그인 시도 제한
- 세션 타임아웃
- 로그인 실패 로그

## 문제 해결

### 로그인 폼이 표시되지 않는 경우
1. App.config의 Login 값 확인
2. 데이터베이스 연결 상태 확인
3. Users 테이블 존재 여부 확인

### 로그인 실패 시
1. 사용자명과 비밀번호 정확성 확인
2. 데이터베이스 연결 상태 확인
3. Users 테이블에 해당 사용자 존재 여부 확인

### 데이터베이스 오류 시
1. 연결 문자열 확인
2. Users 테이블 스키마 확인
3. 데이터베이스 권한 확인

## 개발자 정보

### 주요 클래스
- `AuthenticationService`: 사용자 인증 처리
- `LoginForm`: 로그인 UI
- `User`: 사용자 정보 모델

### 주요 메서드
- `LoginAsync()`: 로그인 처리
- `GetUserByUsernameAsync()`: 사용자 정보 조회
- `VerifyPasswordAsync()`: 비밀번호 검증
- `UpdateLastLoginTimeAsync()`: 로그인 시간 업데이트
