# 📋 테이블명 설정 가이드

DB 처리 시 특정 테이블명을 설정하는 방법에 대한 완전한 가이드입니다.

## 🎯 현재 구현된 방법

### 1. App.config를 통한 설정 (기본 방법)

#### 📁 App.config 설정
```xml
<appSettings>
  <!-- 환경 설정 -->
  <add key="Environment" value="Test" />
  
  <!-- 환경별 테이블명 설정 -->
  <add key="InvoiceTable.Name" value="송장출력_사방넷원본변환_Prod" />        <!-- 운영 환경 -->
  <add key="InvoiceTable.TestName" value="송장출력_사방넷원본변환_Test" />    <!-- 테스트 환경 -->
  <add key="InvoiceTable.DevName" value="송장출력_사방넷원본변환_Dev" />      <!-- 개발 환경 -->
  <add key="InvoiceTable.BackupName" value="송장출력_사방넷원본변환_Backup" /> <!-- 백업용 -->
</appSettings>
```

#### 💡 사용법
```csharp
// 기본 생성자 - App.config에서 자동으로 테이블명 결정
var repository = new InvoiceRepository(databaseService);

// 환경 변수 "Environment"에 따라 자동 선택:
// - "Test" → InvoiceTable.TestName
// - "Prod" → InvoiceTable.Name  
// - "Dev" → InvoiceTable.DevName
```

### 2. 생성자를 통한 직접 지정

#### 💡 사용법
```csharp
// 커스텀 테이블명 직접 지정
var repository = new InvoiceRepository(databaseService, "custom_table_name");

// 특정 환경용 테이블 지정
var prodRepository = new InvoiceRepository(databaseService, "송장출력_사방넷원본변환_Prod");
var testRepository = new InvoiceRepository(databaseService, "송장출력_사방넷원본변환_Test");
```

## 🔧 환경별 테이블명 설정 방법

### 1. 테스트 환경
```xml
<add key="Environment" value="Test" />
<add key="InvoiceTable.TestName" value="송장출력_사방넷원본변환_Test" />
```

### 2. 개발 환경
```xml
<add key="Environment" value="Dev" />
<add key="InvoiceTable.DevName" value="송장출력_사방넷원본변환_Dev" />
```

### 3. 운영 환경
```xml
<add key="Environment" value="Prod" />
<add key="InvoiceTable.Name" value="송장출력_사방넷원본변환_Prod" />
```

## 🎯 실제 사용 예시

### InvoiceProcessor에서 사용
```csharp
public class InvoiceProcessor
{
    private readonly IInvoiceRepository _invoiceRepository;
    
    public InvoiceProcessor(FileService fileService, DatabaseService databaseService, ApiService apiService)
    {
        // App.config 설정에 따라 자동으로 테이블명 결정
        _invoiceRepository = new InvoiceRepository(databaseService);
        
        // 또는 특정 테이블 지정
        // _invoiceRepository = new InvoiceRepository(databaseService, "특정테이블명");
    }
}
```

### 테스트 코드에서 사용
```csharp
[Test]
public async Task ProcessAsync_ShouldWork_WithTestTable()
{
    // 테스트용 테이블 지정
    var testRepository = new InvoiceRepository(databaseService, "송장출력_사방넷원본변환_Test");
    var processor = new InvoiceProcessor(fileService, testRepository, apiService);
    
    var result = await processor.ProcessAsync("test.xlsx");
    Assert.IsTrue(result);
}
```

## 🔄 런타임에 테이블명 변경

### 방법 1: 새 인스턴스 생성
```csharp
// 기존 Repository
var oldRepository = new InvoiceRepository(databaseService, "old_table");

// 새 테이블명으로 새 인스턴스 생성
var newRepository = new InvoiceRepository(databaseService, "new_table");
```

### 방법 2: Factory 패턴 사용 (고급)
```csharp
var factory = new InvoiceRepositoryFactory(databaseService);

// 환경에 따라 자동 생성
var repository = factory.CreateByEnvironment();

// 특정 환경용 생성
var testRepo = factory.CreateForTesting();
var prodRepo = factory.CreateForProduction();
```

## 📊 환경별 설정 예시

### 개발 환경 (개발자 PC)
```xml
<add key="Environment" value="Dev" />
<add key="InvoiceTable.DevName" value="송장출력_사방넷원본변환_Dev" />
```

### 테스트 서버
```xml
<add key="Environment" value="Test" />
<add key="InvoiceTable.TestName" value="송장출력_사방넷원본변환_Test" />
```

### 운영 서버
```xml
<add key="Environment" value="Prod" />
<add key="InvoiceTable.Name" value="송장출력_사방넷원본변환_Prod" />
```

## 🛡️ 안전한 테이블명 설정 팁

### 1. 환경별 명명 규칙
```
송장출력_사방넷원본변환_Prod    # 운영
송장출력_사방넷원본변환_Test    # 테스트  
송장출력_사방넷원본변환_Dev     # 개발
송장출력_사방넷원본변환_Backup  # 백업
```

### 2. 설정 검증
```csharp
// 현재 사용 중인 테이블명 확인
Console.WriteLine($"현재 테이블: {repository.GetCurrentTableName()}");

// 테이블 존재 여부 확인
var exists = await repository.TableExistsAsync();
if (!exists)
{
    throw new InvalidOperationException($"테이블이 존재하지 않습니다: {tableName}");
}
```

### 3. 로깅 및 모니터링
```csharp
// Repository 초기화 시 자동으로 로그 출력됨
// ✅ InvoiceRepository 초기화 완료 - 테이블: 송장출력_사방넷원본변환_Test
// ✅ Configuration에서 테이블명 로드: 송장출력_사방넷원본변환_Test (환경: Test)
```

## ⚠️ 주의사항

### 1. 테이블명 변경 시
- **기존 데이터 백업 필수**
- **테이블 구조 일치 확인**
- **권한 설정 확인**

### 2. 환경 분리
- **운영과 테스트 환경 완전 분리**
- **실수로 운영 테이블 조작 방지**
- **환경별 데이터베이스 서버 분리 권장**

### 3. 성능 고려사항
- **테이블명은 인덱스에 영향 없음**
- **기존 인덱스 유지됨**
- **쿼리 성능 동일**

## 🎉 결론

현재 구현된 테이블명 설정 방법:

1. **App.config 기반 설정** (권장): 환경별 자동 선택
2. **생성자 직접 지정**: 특정 테이블명 명시적 지정
3. **Factory 패턴**: 고급 사용자용 (예시로 제공)

가장 실용적인 방법은 **App.config의 Environment 키를 변경**하는 것입니다:

```xml
<!-- 테스트할 때 -->
<add key="Environment" value="Test" />

<!-- 운영 배포할 때 -->
<add key="Environment" value="Prod" />
```

이렇게 설정하면 코드 변경 없이 환경별로 다른 테이블을 사용할 수 있습니다! 🚀