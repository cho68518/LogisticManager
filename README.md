# 송장 처리 자동화 시스템

사방넷 주문 엑셀 파일을 입력받아 데이터를 가공/분류하고, 출고지별 최종 송장 엑셀 파일을 생성한 후, Dropbox 업로드 및 카카오워크 알림을 보내는 데스크톱 애플리케이션입니다.

## 🎨 UI 특징

- **모던한 디자인**: 세련된 그라데이션 배경과 둥근 모서리 버튼
- **직관적인 아이콘**: 각 기능별 이모지 아이콘으로 사용자 친화적 인터페이스
- **실시간 피드백**: 호버 효과와 애니메이션으로 상호작용 향상
- **진행률 표시**: 실시간 진행률 바와 상태 표시로 작업 진행 상황 파악
- **다크 테마 로그**: 터미널 스타일의 로그 창으로 가독성 향상

## 주요 기능

- **📁 Excel 파일 처리**: 사방넷 주문 엑셀 파일 읽기 및 가공
- **🏢 출고지별 분류**: 서울냉동, 경기공산, 부산 등 출고지별 자동 분류
- **📦 합포장 처리**: 같은 수취인에게 여러 상품이 있는 경우 합포장 송장 생성
- **⭐ 특수 출고지 처리**: 감천, 카카오 등 특수 출고지별 맞춤 처리
- **☁️ Dropbox 업로드**: 처리된 송장 파일을 Dropbox에 자동 업로드
- **💬 카카오워크 알림**: 처리 완료 시 카카오워크로 알림 전송

## 시스템 요구사항

- **운영체제**: Windows 10 이상
- **.NET**: .NET 8.0
- **데이터베이스**: MySQL
- **외부 서비스**: Dropbox API, Kakao Work API

## 설치 및 설정

### 1. 프로젝트 빌드

```bash
dotnet restore
dotnet build
```

### 2. 보안 설정

#### 프로그램 내 설정 (권장)
애플리케이션 내에서 직접 설정할 수 있습니다:

1. **애플리케이션 실행 후 ⚙️ 설정 버튼 클릭**
2. **🗄️ 데이터베이스 설정 탭에서 다음 정보 입력**:
   - 🌐 서버 주소: 데이터베이스 서버 주소 (기본값: localhost)
   - 🗄️ 데이터베이스명: 데이터베이스 이름 (기본값: logistic_manager)
   - 👤 사용자명: 데이터베이스 사용자명 (기본값: root)
   - 🔒 비밀번호: 데이터베이스 비밀번호 (필수)
   - 🔌 포트: 데이터베이스 포트 (기본값: 3306)

3. **🔗 API 설정 탭에서 다음 정보 입력**:
   - ☁️ Dropbox API 키: Dropbox API 키
   - 💬 Kakao Work API 키: Kakao Work API 키
   - 💬 채팅방 ID: Kakao Work 채팅방 ID

4. **📁 파일 경로 설정 탭에서 폴더 경로 설정**:
   - 📥 입력 폴더 경로: Excel 파일이 저장되는 위치
   - 📤 출력 폴더 경로: 처리된 파일이 저장되는 위치
   - 📁 임시 폴더 경로: 임시 파일이 저장되는 위치

5. **🔍 연결 테스트 버튼으로 데이터베이스 연결 확인**
6. **💾 저장 버튼으로 설정 완료**

#### App.config 설정 (개발 환경용)
환경 변수가 설정되지 않은 경우 App.config에서 읽습니다:

```xml
<connectionStrings>
  <add name="DefaultConnection" 
       connectionString="Server=localhost;Database=logistic_manager;Uid=root;Pwd=%DB_PASSWORD%;CharSet=utf8;Port=3306;SslMode=none;AllowPublicKeyRetrieval=true;" 
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>

<appSettings>
  <!-- Dropbox API 설정 -->
  <add key="DropboxApiKey" value="%DROPBOX_API_KEY%" />
  <add key="DropboxFolderPath" value="/LogisticManager/" />
  
  <!-- Kakao Work API 설정 -->
  <add key="KakaoWorkApiKey" value="%KAKAO_WORK_API_KEY%" />
  <add key="KakaoWorkChatroomId" value="%KAKAO_CHATROOM_ID%" />
  
  <!-- 파일 경로 설정 -->
  <add key="InputFolderPath" value="C:\Work\Input\" />
  <add key="OutputFolderPath" value="C:\Work\Output\" />
  <add key="TempFolderPath" value="C:\Work\Temp\" />
  
  <!-- 출고지별 배송비 설정 -->
  <add key="SeoulColdShippingCost" value="5000" />
  <add key="GyeonggiIndustrialShippingCost" value="4000" />
  <add key="BusanShippingCost" value="6000" />
  <add key="GamcheonShippingCost" value="5500" />
  
  <!-- 보안 설정 -->
  <add key="EncryptionEnabled" value="true" />
  <add key="ConnectionStringEncrypted" value="false" />
</appSettings>
```

### 3. 필요한 API 키 발급

#### Dropbox API
1. [Dropbox 개발자 콘솔](https://www.dropbox.com/developers)에서 앱 생성
2. API 키 발급
3. 설정 창의 `Dropbox API 키`에 입력

#### Kakao Work API
1. [Kakao Work 개발자 콘솔](https://developers.kakao.com/)에서 앱 생성
2. Bot API 키 발급
3. 설정 창의 `Kakao Work API 키`에 입력
4. 채팅방 ID를 `Kakao Work 채팅방 ID`에 입력

## 사용법

### 1. 애플리케이션 실행

```bash
dotnet run
```

### 2. 파일 선택

1. **📁 파일 선택** 버튼을 클릭
2. 사방넷 주문 엑셀 파일 선택
3. 선택된 파일명이 화면에 표시됨
4. 파일 크기 정보도 함께 표시됨

### 3. 송장 처리 시작

1. **🚀 송장 처리 시작** 버튼을 클릭
2. 진행 상황이 로그 창에 실시간으로 표시됨
3. 진행률 바로 전체 진행 상황 확인 가능
4. 상태 라벨로 현재 작업 상태 확인

### 4. 결과 확인

- **📋 로그 창**: 처리 과정의 상세한 로그 확인 (다크 테마)
- **📁 출력 폴더**: 생성된 송장 파일들 확인
- **☁️ Dropbox**: 업로드된 파일들 확인
- **💬 카카오워크**: 처리 완료 알림 확인

## 프로젝트 구조

```
LogisticManager/
├── Forms/
│   ├── MainForm.cs          # 메인 UI 폼 (모던한 디자인)
│   └── SettingsForm.cs      # 설정 UI 폼 (탭 기반 인터페이스)
├── Models/
│   └── Order.cs             # 주문 데이터 모델
├── Services/
│   ├── DatabaseService.cs   # 데이터베이스 서비스
│   ├── FileService.cs       # 파일 처리 서비스
│   ├── ApiService.cs        # 외부 API 서비스
│   └── SecurityService.cs   # 보안 서비스 (환경 변수 관리)
├── Processors/
│   ├── InvoiceProcessor.cs  # 전체 송장 처리 로직
│   └── ShipmentProcessor.cs # 출고지별 처리 로직
├── App.config               # 설정 파일
├── Program.cs               # 애플리케이션 진입점
└── README.md               # 프로젝트 설명서
```

## 주요 클래스 설명

### MainForm
- **모던한 UI**: 그라데이션 배경, 둥근 모서리 버튼, 아이콘 포함
- **실시간 피드백**: 호버 효과, 진행률 표시, 상태 라벨
- **사용자 친화적**: 직관적인 아이콘과 명확한 상태 표시

### SettingsForm
- **탭 기반 인터페이스**: 데이터베이스, API, 파일 경로 설정을 탭으로 구분
- **보안 기능**: 비밀번호 필드 마스킹, 환경 변수 기반 설정 관리
- **연결 테스트**: 데이터베이스 연결 상태를 즉시 확인 가능

### InvoiceProcessor
- 전체 송장 처리 로직을 담당
- 파일 읽기 → 데이터 가공 → 출고지별 분류 → 파일 생성 → 업로드 → 알림 순서로 처리

### ShipmentProcessor
- 하나의 출고지를 처리하는 재사용 가능한 로직
- 낱개/박스 분류, 합포장 계산, 별표 처리 등을 수행

### Order
- 엑셀 데이터를 담는 강력한 타입 모델
- DataRow와 Order 객체 간 변환 메서드 제공

## 처리 과정

1. **📄 Excel 파일 읽기**: EPPlus 라이브러리를 사용하여 엑셀 파일 읽기
2. **🔧 1차 데이터 가공**: 주소 정리, 수취인명 정리, 결제방법 정리
3. **🏢 출고지별 분류**: 출고지 정보를 기준으로 데이터 분류
4. **⚙️ 출고지별 처리**: 각 출고지별 맞춤 처리 로직 적용
   - 낱개/박스 분류
   - 합포장 계산
   - 별표 처리
   - 특수 출고지 처리 (감천, 카카오 등)
5. **📁 파일 생성**: 처리된 데이터를 엑셀 파일로 생성
6. **☁️ Dropbox 업로드**: 생성된 파일을 Dropbox에 업로드
7. **💬 카카오워크 알림**: 처리 완료 알림 전송

## 오류 처리

- **파일 읽기 오류**: 엑셀 파일 형식 오류 시 상세한 오류 메시지 표시
- **데이터베이스 오류**: 연결 실패 시 재시도 로직 포함
- **API 오류**: Dropbox/Kakao Work API 오류 시 로그에 기록
- **UI 응답성**: 모든 시간이 걸리는 작업은 비동기로 처리하여 UI 멈춤 방지
- **사용자 친화적 오류 메시지**: 이모지와 함께 명확한 오류 설명 제공

## 개발 환경

- **언어**: C# 12.0
- **프레임워크**: .NET 8.0
- **UI**: Windows Forms (모던한 디자인 적용)
- **데이터베이스**: MySQL
- **라이브러리**: 
  - MySql.Data (8.3.0)
  - EPPlus (7.0.5)
  - Newtonsoft.Json (13.0.3)
  - System.Configuration.ConfigurationManager (8.0.0)

## 라이센스

이 프로젝트는 MIT 라이센스 하에 배포됩니다. 