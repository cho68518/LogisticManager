# 📦 송장 처리 자동화 시스템

사방넷 주문 엑셀 파일을 입력받아 데이터를 가공/분류하고, 출고지별 최종 송장 엑셀 파일을 생성한 후, Dropbox 업로드 및 카카오워크 알림을 보내는 데스크톱 애플리케이션입니다.

## 🎨 UI 특징

- **모던한 디자인**: 세련된 그라데이션 배경과 둥근 모서리 버튼
- **직관적인 아이콘**: 각 기능별 이모지 아이콘으로 사용자 친화적 인터페이스
- **실시간 피드백**: 호버 효과와 애니메이션으로 상호작용 향상
- **진행률 표시**: 실시간 진행률 바와 상태 표시로 작업 진행 상황 파악
- **다크 테마 로그**: 터미널 스타일의 로그 창으로 가독성 향상
- **반응형 레이아웃**: 창 크기 조절에 따라 UI 요소들이 자동으로 조정

## 🚀 주요 기능

- **📁 Excel 파일 처리**: 사방넷 주문 엑셀 파일 읽기 및 가공
- **🏢 출고지별 분류**: 서울냉동, 경기공산, 부산 등 출고지별 자동 분류
- **📦 합포장 처리**: 같은 수취인에게 여러 상품이 있는 경우 합포장 송장 생성
- **⭐ 특수 출고지 처리**: 감천, 카카오 등 특수 출고지별 맞춤 처리
- **☁️ Dropbox 업로드**: 처리된 송장 파일을 Dropbox에 자동 업로드
- **💬 카카오워크 알림**: 처리 완료 시 카카오워크로 알림 전송
- **🔧 설정 관리**: 데이터베이스 및 파일 경로 설정을 위한 직관적인 설정 화면

## 📋 시스템 요구사항

- **운영체제**: Windows 10 이상
- **.NET**: .NET 8.0
- **데이터베이스**: MySQL
- **외부 서비스**: Dropbox API, Kakao Work API

## ⚙️ 설치 및 설정

### 1. 프로젝트 빌드

```bash
# 프로젝트 복원
dotnet restore

# 프로젝트 빌드
dotnet build

# 애플리케이션 실행
dotnet run
```

### 2. 설정 파일 구성

#### settings.json 파일 생성
프로젝트 루트에 `settings.json` 파일을 생성하고 다음 내용을 입력합니다:

```json
{
  "DB_SERVER": "gramwonlogis.mycafe24.com",
  "DB_NAME": "gramwonlogis",
  "DB_USER": "gramwonlogis",
  "DB_PASSWORD": "jung5516!",
  "DB_PORT": "3306",
  "DROPBOX_ACCESS_TOKEN": "",
  "KAKAO_WORK_APP_KEY": "b36ed46e.0b55706350e94ef8b49e8647a97ae1b7",
  "KAKAO_WORK_APP_SECRET": "",
  "KAKAO_WORK_BOT_TOKEN": ""
}
```

#### App.config 설정
`App.config` 파일에서 추가 설정을 관리합니다:

```xml
<appSettings>
  <!-- Dropbox API 설정 -->
  <add key="Dropbox.AppKey" value="vlxh3ec9nq5fi5t" />
  <add key="Dropbox.AppSecret" value="h5rp5n0w9cp4ifk" />
  <add key="Dropbox.RefreshToken" value="1CLvznLL7BwAAAAAAAAAAagAlVx3w9P6NGyYjsITAxrSG9vlAEc31ohZZwYhUAB_" />
  <add key="DropboxFolderPath" value="/LogisticManager/" />
  
  <!-- Kakao Work API 설정 -->
  <add key="KakaoWork.AppKey" value="b36ed46e.0b55706350e94ef8b49e8647a97ae1b7" />
  <add key="KakaoWork.ChatroomId.Integrated" value="10545642" />
  
  <!-- 파일 경로 설정 -->
  <add key="InputFolderPath" value="C:\Work\Input\" />
  <add key="OutputFolderPath" value="C:\Work\Output\" />
  <add key="TempFolderPath" value="C:\Work\Temp\" />
  
  <!-- 출고지별 배송비 설정 -->
  <add key="SeoulColdShippingCost" value="5000" />
  <add key="GyeonggiIndustrialShippingCost" value="4000" />
  <add key="BusanShippingCost" value="6000" />
  <add key="GamcheonShippingCost" value="5500" />
</appSettings>
```

### 3. 필요한 API 키 발급

#### Dropbox API
1. [Dropbox 개발자 콘솔](https://www.dropbox.com/developers)에서 앱 생성
2. API 키 발급
3. `App.config`의 `Dropbox.AppKey`에 입력

#### Kakao Work API
1. [Kakao Work 개발자 콘솔](https://developers.kakao.com/)에서 앱 생성
2. Bot API 키 발급
3. `App.config`의 `KakaoWork.AppKey`에 입력
4. 채팅방 ID를 `KakaoWork.ChatroomId.Integrated`에 입력

## 🎯 사용법

### 1. 애플리케이션 실행

```bash
dotnet run
```

### 2. 설정 확인

1. **⚙️ 설정** 버튼을 클릭하여 설정 화면 열기
2. **🗄️ 데이터베이스 설정** 탭에서 데이터베이스 연결 정보 확인
3. **📁 파일 경로 설정** 탭에서 입력/출력 폴더 경로 확인
4. **❌ 취소** 버튼으로 설정 화면 닫기

### 3. 파일 선택

1. **📁 파일 선택** 버튼을 클릭
2. 사방넷 주문 엑셀 파일 선택
3. 선택된 파일명이 화면에 표시됨

### 4. 송장 처리 시작

1. **🚀 송장 처리 시작** 버튼을 클릭
2. 진행 상황이 로그 창에 실시간으로 표시됨
3. 진행률 바로 전체 진행 상황 확인 가능
4. 상태 라벨로 현재 작업 상태 확인

### 5. 결과 확인

- **📋 로그 창**: 처리 과정의 상세한 로그 확인 (다크 테마)
- **📁 출력 폴더**: 생성된 송장 파일들 확인
- **☁️ Dropbox**: 업로드된 파일들 확인
- **💬 카카오워크**: 처리 완료 알림 확인

## 📁 프로젝트 구조

```
LogisticManager/
├── Forms/
│   ├── MainForm.cs          # 메인 UI 폼 (모던한 디자인)
│   └── SettingsForm.cs      # 설정 UI 폼 (탭 기반 인터페이스)
├── Models/
│   ├── Order.cs             # 주문 데이터 모델
│   ├── KakaoWorkBlocks.cs   # 카카오워크 블록 모델
│   └── NotificationType.cs  # 알림 타입 열거형
├── Services/
│   ├── DatabaseService.cs   # 데이터베이스 서비스
│   ├── FileService.cs       # 파일 처리 서비스
│   ├── ApiService.cs        # 외부 API 서비스
│   ├── DropboxService.cs    # Dropbox 서비스
│   ├── KakaoWorkService.cs  # 카카오워크 서비스
│   └── SecurityService.cs   # 보안 서비스
├── Processors/
│   ├── InvoiceProcessor.cs  # 전체 송장 처리 로직
│   └── ShipmentProcessor.cs # 출고지별 처리 로직
├── App.config               # 설정 파일
├── settings.json            # 데이터베이스 설정 파일
├── Program.cs               # 애플리케이션 진입점
├── DatabaseTest.cs          # 데이터베이스 연결 테스트
└── README.md               # 프로젝트 설명서
```

## 🔧 주요 클래스 설명

### MainForm
- **모던한 UI**: 그라데이션 배경, 둥근 모서리 버튼, 아이콘 포함
- **실시간 피드백**: 호버 효과, 진행률 표시, 상태 라벨
- **사용자 친화적**: 직관적인 아이콘과 명확한 상태 표시
- **반응형 레이아웃**: 창 크기 조절에 따른 UI 자동 조정

### SettingsForm
- **탭 기반 인터페이스**: 데이터베이스, 파일 경로 설정을 탭으로 구분
- **보안 기능**: 비밀번호 필드 마스킹, 환경 변수 기반 설정 관리
- **연결 테스트**: 데이터베이스 연결 상태를 즉시 확인 가능
- **API 설정 숨김**: API 설정 탭은 주석 처리되어 보이지 않음

### InvoiceProcessor
- 전체 송장 처리 로직을 담당
- 파일 읽기 → 데이터 가공 → 출고지별 분류 → 파일 생성 → 업로드 → 알림 순서로 처리

### ShipmentProcessor
- 하나의 출고지를 처리하는 재사용 가능한 로직
- 낱개/박스 분류, 합포장 계산, 별표 처리 등을 수행

### Order
- 엑셀 데이터를 담는 강력한 타입 모델
- DataRow와 Order 객체 간 변환 메서드 제공

## 🔄 처리 과정

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

## 🛡️ 오류 처리

- **파일 읽기 오류**: 엑셀 파일 형식 오류 시 상세한 오류 메시지 표시
- **데이터베이스 오류**: 연결 실패 시 재시도 로직 포함
- **API 오류**: Dropbox/Kakao Work API 오류 시 로그에 기록
- **UI 응답성**: 모든 시간이 걸리는 작업은 비동기로 처리하여 UI 멈춤 방지
- **사용자 친화적 오류 메시지**: 이모지와 함께 명확한 오류 설명 제공
- **로깅 시스템**: 모든 작업 과정을 `app.log` 파일에 상세히 기록

## 🛠️ 개발 환경

- **언어**: C# 12.0
- **프레임워크**: .NET 8.0
- **UI**: Windows Forms (모던한 디자인 적용)
- **데이터베이스**: MySQL
- **라이브러리**: 
  - MySqlConnector (2.3.5)
  - EPPlus (7.0.5)
  - Newtonsoft.Json (13.0.3)
  - System.Configuration.ConfigurationManager (8.0.0)
  - Dropbox.Api (7.0.0)

## 📝 로그 시스템

애플리케이션은 모든 작업 과정을 `app.log` 파일에 상세히 기록합니다:

- **애플리케이션 시작/종료**: 실행 시간과 상태 기록
- **데이터베이스 연결**: 연결 시도와 결과 기록
- **파일 처리**: 파일 읽기/쓰기 과정 기록
- **API 호출**: Dropbox 업로드, 카카오워크 알림 과정 기록
- **오류 발생**: 상세한 오류 정보와 스택 트레이스 기록

## 🔒 보안 기능

- **환경 변수 관리**: 민감한 정보는 환경 변수로 관리
- **설정 파일 암호화**: 필요시 설정 파일 암호화 지원
- **API 키 보안**: API 키는 설정 파일에서 안전하게 관리
- **데이터베이스 연결 보안**: SSL 연결 및 인증서 검증

## 🚀 성능 최적화

- **비동기 처리**: 모든 I/O 작업을 비동기로 처리
- **메모리 효율성**: 대용량 파일 처리 시 메모리 사용량 최적화
- **UI 응답성**: 백그라운드 작업으로 UI 블로킹 방지
- **캐싱**: 자주 사용되는 데이터는 메모리에 캐싱

## 📞 지원

문제가 발생하거나 개선 사항이 있으시면 다음을 확인해주세요:

1. **로그 파일 확인**: `app.log` 파일에서 상세한 오류 정보 확인
2. **설정 파일 검증**: `settings.json`과 `App.config` 파일의 설정값 확인
3. **데이터베이스 연결**: 데이터베이스 서버 연결 상태 확인
4. **API 키 유효성**: Dropbox와 Kakao Work API 키의 유효성 확인

## 📄 라이센스

이 프로젝트는 MIT 라이센스 하에 배포됩니다.

---

**개발자**: LogisticManager Team  
**버전**: 1.0.0  
**최종 업데이트**: 2024년 8월 