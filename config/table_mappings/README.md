# 테이블별 컬럼 매핑 관리 시스템

## 개요

이 시스템은 각 테이블의 컬럼 매핑 정보를 개별 JSON 파일로 관리하여 유지보수성과 확장성을 향상시킵니다.

## 파일 구조

```
config/table_mappings/
├── index.json                                    # 테이블 매핑 인덱스
├── 송장출력_사방넷원본변환.json                  # 사방넷 원본 데이터 변환 테이블
├── 송장출력_특수출력_합포장변경.json            # 합포장 변경 정보 테이블
├── 송장출력_톡딜불가.json                       # 톡딜 불가 상품 정보 테이블
└── README.md                                     # 이 파일
```

## 파일 명명 규칙

- **파일명**: `테이블명.json`
- **인코딩**: UTF-8
- **형식**: JSON

## 각 매핑 파일의 구조

### 1. 기본 정보
```json
{
  "version": "1.0",
  "description": "테이블 설명",
  "created_date": "YYYY-MM-DD",
  "last_updated": "YYYY-MM-DD"
}
```

### 2. 테이블 정보
```json
{
  "table_info": {
    "table_name": "테이블명",
    "description": "테이블 설명",
    "is_active": true,
    "processing_order": 1
  }
}
```

### 3. 컬럼 정의
```json
{
  "columns": {
    "컬럼명": {
      "db_column": "DB컬럼명",
      "data_type": "데이터타입",
      "required": true/false,
      "description": "컬럼 설명",
      "excel_column_index": 1,
      "validation": {
        "max_length": 255,
        "required": true,
        "pattern": "^[0-9-]+$"
      }
    }
  }
}
```

### 4. 비즈니스 규칙
```json
{
  "business_rules": {
    "description": "규칙 설명",
    "rules": [
      "규칙 1",
      "규칙 2"
    ]
  }
}
```

### 5. 데이터 품질 규칙
```json
{
  "data_quality": {
    "description": "품질 규칙 설명",
    "rules": [
      "검증 규칙 1",
      "검증 규칙 2"
    ]
  }
}
```

## 검증 규칙

### 데이터 타입
- `varchar(n)`: 문자열 (최대 n자)
- `int`: 정수
- `decimal(p,s)`: 소수점 (전체 p자리, 소수점 s자리)
- `datetime`: 날짜시간
- `date`: 날짜

### 검증 옵션
- `max_length`: 최대 길이
- `min_value`: 최소값
- `max_value`: 최대값
- `required`: 필수 여부
- `pattern`: 정규식 패턴
- `allowed_values`: 허용 값 목록
- `format`: 날짜/시간 형식

## 사용 방법

### 1. 새 테이블 추가
1. `테이블명.json` 파일 생성
2. 컬럼 매핑 정보 작성
3. `index.json`에 테이블 정보 추가

### 2. 기존 테이블 수정
1. 해당 `테이블명.json` 파일 수정
2. `last_updated` 날짜 업데이트
3. `index.json`의 메타데이터 업데이트

### 3. 테이블 비활성화
1. `table_info.is_active`를 `false`로 설정
2. `index.json`에서 `active_tables` 카운트 감소

## 장점

1. **유지보수성**: 각 테이블별로 독립적인 관리
2. **확장성**: 새로운 테이블 추가 시 기존 파일에 영향 없음
3. **가독성**: 파일 크기가 작아 읽기 쉬움
4. **버전 관리**: Git에서 변경 이력 추적 용이
5. **팀 협업**: 팀별로 담당 테이블 분리 관리 가능

## 주의사항

1. **파일명**: 테이블명과 정확히 일치해야 함
2. **JSON 형식**: 유효한 JSON 형식으로 작성
3. **인덱스 동기화**: `index.json`과 실제 파일 상태 일치 유지
4. **백업**: 중요한 변경 전 백업 생성
5. **검증**: JSON 스키마 검증 도구 사용 권장

## 문제 해결

### 일반적인 오류
1. **JSON 파싱 오류**: JSON 형식 검증
2. **파일 누락**: `index.json`과 실제 파일 일치 확인
3. **컬럼 불일치**: DB 스키마와 매핑 파일 동기화

### 디버깅
1. 로그 파일에서 오류 메시지 확인
2. JSON 파일 형식 검증
3. 컬럼 매핑 정보 일치성 확인

## 연락처

문제가 발생하거나 개선 사항이 있으면 개발팀에 문의하세요.
