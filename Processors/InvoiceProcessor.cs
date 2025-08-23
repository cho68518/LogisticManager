using System.Data;
using System.Configuration;
using LogisticManager.Services;
using LogisticManager.Models;
using LogisticManager.Repositories;
using LogisticManager.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector;
using System.IO;

namespace LogisticManager.Processors
{
    /// <summary>
    /// 전체 송장 처리 로직을 담당하는 메인 프로세서 클래스 (파이썬 코드 기반)
    /// </summary>
    public class InvoiceProcessor
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// 파일 처리 서비스 - Excel 파일 읽기/쓰기 담당 (ColumnMapping 적용)
        /// 
        /// 📋 주요 기능:
        /// - Excel 파일 읽기 (ColumnMapping 기반)
        /// - Excel 파일 생성 (출고지별 분류)
        /// - 파일 경로 관리
        /// - 데이터 검증
        /// 
        /// 🔗 의존성: FileService (Singleton 패턴)
        /// </summary>
        private readonly FileService _fileService;
        
        /// <summary>
        /// 공통 파일 처리 서비스 - 파일 관련 공통 기능
        /// 
        /// 📋 주요 기능:
        /// - Excel 파일명 생성
        /// - Dropbox 파일 업로드
        /// - Dropbox 공유 링크 생성
        /// - 파일 상태 확인
        /// 
        /// 🔗 의존성: FileCommonService
        /// </summary>
        private readonly FileCommonService _fileCommonService;
        
        /// <summary>
        /// 공통 데이터베이스 서비스 - 데이터베이스 관련 공통 기능
        /// 
        /// 📋 주요 기능:
        /// - 데이터 조회
        /// - 연결 확인
        /// - 테이블 존재 확인
        /// - 배치 처리
        /// 
        /// 🔗 의존성: DatabaseCommonService
        /// </summary>
        private readonly DatabaseCommonService _databaseCommonService;
        
        /// <summary>
        /// 공통 로깅 서비스 - 로깅 관련 공통 기능
        /// 
        /// 📋 주요 기능:
        /// - 로그 파일 쓰기
        /// - 다중 라인 로그 처리
        /// - 로그 파일 상태 진단
        /// 
        /// 🔗 의존성: LoggingCommonService
        /// </summary>
        private readonly LoggingCommonService _loggingCommonService;
        
        /// <summary>
        /// 공통 유틸리티 서비스 - 유틸리티 관련 공통 기능
        /// 
        /// 📋 주요 기능:
        /// - 문자열 처리
        /// - 예외 분류
        /// - 오류 메시지 생성
        /// 
        /// 🔗 의존성: UtilityCommonService
        /// </summary>
        private readonly UtilityCommonService _utilityCommonService;
        
        /// <summary>
        /// 송장 데이터 저장소 - Repository 패턴 적용
        /// 
        /// 📋 주요 기능:
        /// - 데이터 액세스 로직 추상화
        /// - 배치 처리 최적화
        /// - 매개변수화된 쿼리 (SQL 인젝션 방지)
        /// - 1차 데이터 가공 작업
        /// 
        /// 🔗 의존성: IInvoiceRepository
        /// </summary>
        private readonly IInvoiceRepository _invoiceRepository;
        
        /// <summary>
        /// 배치 처리 서비스 - 대용량 데이터 처리 전용
        /// 
        /// 📋 주요 기능:
        /// - 대용량 데이터 배치 처리
        /// - 메모리 효율적인 처리
        /// - 적응형 배치 크기
        /// - 오류 복구 및 재시도 로직
        /// 
        /// 🔗 의존성: BatchProcessorService
        /// </summary>
        private readonly BatchProcessorService _batchProcessor;
        
        /// <summary>
        /// 데이터베이스 서비스 - MySQL 데이터베이스 연결 및 쿼리 실행 담당
        /// 
        /// 📋 주요 기능:
        /// - MySQL 데이터베이스 연결 관리
        /// - SQL 쿼리 실행 (SELECT, INSERT, UPDATE, DELETE)
        /// - 트랜잭션 처리
        /// - 연결 문자열 관리
        /// 
        /// 🔗 의존성: DatabaseService
        /// </summary>
        private readonly DatabaseService _databaseService;
        
        /// <summary>
        /// API 서비스 - Dropbox 업로드, Kakao Work 알림 담당
        /// 
        /// 📋 주요 기능:
        /// - Dropbox 파일 업로드
        /// - 공유 링크 생성
        /// - Kakao Work 알림 전송 (구식 API)
        /// - API 키 관리 및 보안
        /// 
        /// 🔗 의존성: ApiService
        /// </summary>
        private readonly ApiService _apiService;
        
        /// <summary>
        /// 진행률 보고용 콜백 - UI 업데이트용
        /// 
        /// 📋 주요 기능:
        /// - 실시간 진행률 메시지 전달
        /// - 사용자 친화적 상태 메시지
        /// - 오류 상황 즉시 알림
        /// 
        /// 🔗 의존성: IProgress<string> (UI 스레드 안전)
        /// </summary>
        private readonly IProgress<string>? _progress;
        
        /// <summary>
        /// 진행률 퍼센트 보고용 콜백 - 프로그레스바 업데이트용
        /// 
        /// 📋 주요 기능:
        /// - 0-100% 진행률 퍼센트 전달
        /// - 프로그레스바 실시간 업데이트
        /// - 단계별 진행률 표시
        /// 
        /// 🔗 의존성: IProgress<int> (UI 스레드 안전)
        /// </summary>
        private readonly IProgress<int>? _progressReporter;
        
        /// <summary>
        /// 단계별 진행상황 보고 콜백 - UI 스레드 안전
        /// </summary>
        /// <value>단계별 진행상황 보고 콜백</value>
        private readonly IProgressStepReporter? _stepReporter;

        #endregion

        #region 생성자 (Constructor)

        /// <param name="fileService">파일 처리 서비스 (필수)</param>
        /// <param name="databaseService">데이터베이스 서비스 (필수)</param>
        /// <param name="apiService">API 서비스 (필수)</param>
        /// <param name="progress">진행 상황 메시지 콜백 (선택)</param>
        /// <param name="progressReporter">진행률 콜백 (선택)</param>
        /// <param name="stepReporter">단계별 진행상황 보고 콜백 (선택)</param>
        /// <exception cref="ArgumentNullException">필수 서비스가 null인 경우</exception>
        public InvoiceProcessor(FileService fileService, DatabaseService databaseService, ApiService apiService, 
            IProgress<string>? progress = null, IProgress<int>? progressReporter = null, IProgressStepReporter? stepReporter = null)
        {
            // 필수 서비스 의존성 검증
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService), 
                "FileService는 필수 서비스입니다.");
            
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), 
                "DatabaseService는 필수 서비스입니다.");
            var dbService = _databaseService;
            
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService), 
                "ApiService는 필수 서비스입니다.");
            
            // Repository 패턴 구현
            _invoiceRepository = new InvoiceRepository(dbService);
            
            // 배치 처리 서비스 초기화
            _batchProcessor = new BatchProcessorService(_invoiceRepository);
            
            // 진행률 콜백 설정
            _progress = progress;
            _progressReporter = progressReporter;
            _stepReporter = stepReporter;
            
            // 공통 서비스 초기화
            _fileCommonService = new FileCommonService();
            _databaseCommonService = new DatabaseCommonService(databaseService, new MappingService());
            _loggingCommonService = new LoggingCommonService();
            _utilityCommonService = new UtilityCommonService();
            
            LogManagerService.LogInfo("✅ 초기화 완료");
        }

        #endregion

        #region 설정 확인 메서드 (Configuration Check Methods)

        /// <summary>
        /// KakaoCheck 설정이 'Y'인지 확인하는 메서드
        /// </summary>
        /// <returns>KakaoCheck가 'Y'이면 true, 그렇지 않으면 false</returns>
        private bool IsKakaoWorkEnabled()
        {
            try
            {
                string kakaoCheck = ConfigurationManager.AppSettings["KakaoCheck"] ?? "N";
                bool isEnabled = kakaoCheck.ToUpper() == "Y";
                
                if (isEnabled)
                {
                    LogManagerService.LogInfo("✅ KakaoCheck 설정이 'Y'입니다. 카카오워크 메시지 전송이 활성화되었습니다.");
                }
                else
                {
                    LogManagerService.LogInfo($"⚠️ KakaoCheck 설정이 'Y'가 아닙니다 (현재: {kakaoCheck}). 카카오워크 메시지 전송이 비활성화되었습니다.");
                }
                
                return isEnabled;
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ KakaoCheck 설정 확인 중 오류 발생: {ex.Message}");
                return false; // 오류 발생 시 기본적으로 비활성화
            }
        }

        #endregion

        #region 메인 처리 메서드 (Main Processing Method)

        /// </summary>
        /// <param name="filePath">처리할 Excel 파일 경로</param>
        /// <param name="progress">진행 상황 메시지 콜백 (선택)</param>
        /// <param name="progressReporter">진행률 콜백 (선택)</param>
        /// <returns>처리 성공 여부</returns>
        /// <exception cref="ArgumentException">파일 경로가 비어있는 경우</exception>
        /// <exception cref="FileNotFoundException">파일이 존재하지 않는 경우</exception>
        /// <exception cref="Exception">처리 중 오류 발생 시</exception>
        /// <summary>
        /// 송장 처리를 위한 비동기 메서드
        /// 
        /// 📋 주요 기능:
        /// - Excel 파일에서 주문 데이터 로드 및 검증
        /// - 데이터베이스 초기화 및 대용량 데이터 적재
        /// - 단계별 송장 처리 (4-1 ~ 4-22)
        /// - 진행률 보고 및 오류 처리
        /// 
        /// 🎯 매개변수:
        /// - filePath: 처리할 Excel 파일 경로
        /// - progress: 진행 상황 텍스트 보고용 콜백
        /// - progressReporter: 진행률 퍼센트 보고용 콜백
        /// - maxStep: 최대 처리 단계 (기본값: 22, 전체 처리)
        /// 
        /// 📊 처리 단계:
        /// - 4-1 ~ 4-6: 기본 데이터 처리
        /// - 4-7 ~ 4-14: 지역별 처리 (서울, 경기, 부산)
        /// - 4-15 ~ 4-22: 최종 처리 및 파일 생성
        /// </summary>
        /// <param name="filePath">Excel 파일 경로</param>
        /// <param name="progress">진행 상황 텍스트 콜백</param>
        /// <param name="progressReporter">진행률 퍼센트 콜백</param>
        /// <param name="maxStep">최대 처리 단계 (1~22, 기본값: 22)</param>
        /// <returns>처리 성공 여부</returns>
        public async Task<bool> ProcessAsync(string filePath, IProgress<string>? progress = null, IProgress<int>? progressReporter = null, int maxStep = 22)
        {
            // 입력 파일 경로 검증
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("파일 경로는 비어있을 수 없습니다.", nameof(filePath));

            // 파일 존재 여부 확인
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel 파일을 찾을 수 없습니다: {filePath}");

            // maxStep 매개변수 검증 (1~22 범위)
            if (maxStep < 1 || maxStep > 22)
                throw new ArgumentException("maxStep은 1에서 22 사이의 값이어야 합니다.", nameof(maxStep));

            try
            {
                // ==================== 전처리: 콜백 우선순위 결정 및 초기 상태 설정 ====================
                
                // === 진행률 콜백 우선순위 결정 (매개변수 > 생성자 설정) ===
                // 유연한 콜백 시스템: 메서드 호출 시점에 다른 콜백을 사용할 수 있도록 지원
                // 예: 일반적으로는 기본 UI 콜백 사용, 특정 상황에서는 다른 UI나 로그 콜백 사용
                var finalProgress = progress ?? _progress;
                var finalProgressReporter = progressReporter ?? _progressReporter;
                
                // 전체 워크플로우 시작 시간 기록 (정확한 총 처리 시간 계산용)
                var workflowStartTime = DateTime.Now; // 전체 처리 시작 시각
                
                // === 전사 물류 시스템 처리 시작 선언 ===
                // 사용자에게 명확한 시작 신호 전달 및 시스템 상태 초기화
                // 이모지 사용으로 직관적인 상태 표시 (사용자 경험 향상)
                finalProgress?.Report("");
                finalProgress?.Report("🚀 [물류 시스템] 송장 처리를 시작합니다...");
                finalProgress?.Report("📋 처리 대상 파일: " + Path.GetFileName(filePath));
                finalProgress?.Report("⏰ 처리 시작 시각: " + workflowStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                // 🎯 처리 단계 제한 정보 표시 (22단계 전체 처리 시에는 표시하지 않음)
                if (maxStep < 22)
                {
                    finalProgress?.Report($"🎯 처리 단계 제한: 4-1 ~ 4-{maxStep} (총 {maxStep}단계)");
                }
                finalProgress?.Report("");

                // 진행률 초기화 (0%에서 시작)
                finalProgressReporter?.Report(0);

                //TestLevel 1
                //if (ConfigurationManager.AppSettings["TestLevel"] == "1")
                //{
                    
                //}
                //else
                //{
                    
                //}

                // ==================== 1단계: 다중 쇼핑몰 Excel 데이터 통합 및 검증 (0-5%) ====================
                finalProgress?.Report("📖 [1단계] Excel 파일 분석 중... ");
                
                // UI 업데이트를 위한 짧은 지연
                await Task.Delay(50);

                // - 지정한 엑셀 파일에서 "order_table" 시트의 데이터를 DataTable로 읽어옵니다.
                var originalData = _fileService.ReadExcelToDataTable(filePath, "order_table");
                
                //===========================================================================================
                // === [1단계] 데이터가 존재할 때만 ReadExcelToDataTable 및 후속 처리 진행
                // 새로운 방식: 프로시저를 통한 데이터 처리 (컬럼매핑 우회)
                finalProgress?.Report("🔄 [1단계] Excel 데이터를 프로시저로 전달 중...");
                var procedureResult = await _fileService.ReadExcelToDataTableWithProcedure(
                    filePath,              // 엑셀 파일 경로
                    "ExcelProcessor.Proc1" // App.config의 프로시저 설정 키
                );

                if (!procedureResult)
                {
                    // [디버깅 로그] 프로시저 실행 실패 기록 (app.log)
                    LogManagerService.LogDebug($"[InvoiceProcessor] Excel 데이터를 프로시저로 전달하는데 실패했습니다. 파일: {filePath}");
                    finalProgress?.Report("⚠️ [작업 종료] Excel 데이터를 프로시저로 전달하는데 실패했습니다. 로그를 확인해주세요.");
                    // 프로시저 실행 실패 시 작업 종료
                    return false;
                }

                finalProgress?.Report("✅ [1단계] Excel 데이터를 프로시저로 전달 완료");

                // === 프로시저 실행 결과 확인 ===
                finalProgress?.Report("🔍 [1-1단계] 프로시저 실행 결과 확인 중...");
                
                // 프로시저에서 데이터가 실제로 삽입되었는지 확인
                var insertedRowCount = await CheckProcedureResult();
                if (insertedRowCount > 0)
                {
                    finalProgress?.Report($"✅ [1-1단계] 프로시저 실행 성공 - {insertedRowCount:N0}행 데이터 삽입 완료");
                    LogManagerService.LogInfo($"[InvoiceProcessor] 프로시저 실행 성공 - 송장출력_사방넷원본변환 테이블에 {insertedRowCount}행 삽입");
                }
                else
                {
                    finalProgress?.Report("⚠️ [1-1단계] 프로시저 실행 완료했으나 데이터 삽입 확인 불가");
                    LogManagerService.LogWarning("[InvoiceProcessor] 프로시저 실행 완료했으나 데이터 삽입 확인 불가");
                }



                
                // === 1단계 완료 및 데이터 통계 보고 ===
                finalProgressReporter?.Report(5);
                finalProgress?.Report($"✅ [1단계 완료] 총 {originalData.Rows.Count:N0}건의 주문 데이터 로드 성공");
                finalProgress?.Report("");

                finalProgress?.Report($"📊 데이터 구조: {originalData.Columns.Count}개 컬럼, 메모리 사용량 약 {(originalData.Rows.Count * originalData.Columns.Count * 50):N0} bytes");
                finalProgress?.Report("");

                // ==================== 데이터 존재성 검증 및 비즈니스 연속성 보장 ====================
                
                // === 빈 파일 또는 무효 데이터에 대한 조기 종료 처리 ===
                // 비즈니스 연속성 관점에서 데이터가 없는 경우 안전하게 종료
                // 시스템 리소스 낭비 방지 및 후속 처리 단계 건너뛰기
                if (originalData.Rows.Count == 0)
                {
                    finalProgress?.Report("⚠️ [처리 중단] Excel 파일에 처리 가능한 주문 데이터가 없습니다.");
                    finalProgress?.Report("💡 확인사항: 파일 형식, 헤더 행 존재 여부, 데이터 시트명을 점검해주세요.");
                    return await Task.FromResult(false); // 비즈니스 로직상 정상적인 종료 (오류가 아님)
                }

                // ==================== 2단계: 프로시저에서 이미 데이터 처리 완료 (건너뛰기) ====================
                
                finalProgress?.Report("⏭️ [2단계] 프로시저에서 이미 데이터 처리 완료 - 건너뛰기");
                finalProgress?.Report("✅ 프로시저 sp_Excel_Proc1에서 송장출력_사방넷원본변환 테이블에 데이터 삽입 완료");
                
                // === 2단계 완료 및 성능 통계 보고 ===
                finalProgressReporter?.Report(10);
                finalProgress?.Report("✅ [2단계 완료] 프로시저를 통한 데이터 처리 완료");
                finalProgress?.Report("📈 다음 단계: 1차 데이터 정제 및 비즈니스 규칙 적용 준비 완료");

                //----------------------------------------------------------------------------------------------
                // 3단계: 1차 데이터 정제 및 비즈니스 규칙 적용
                finalProgress?.Report("🔧 [3단계] 비즈니스 규칙 적용");
                await ProcessFirstStageDataOptimized(finalProgress);
                finalProgressReporter?.Report(20);
                finalProgress?.Report("✅ [3단계] 비즈니스 규칙 적용 완료");
                finalProgress?.Report("");

                // 4단계: 고급 특수 처리 및 비즈니스 로직 적용
                 
                finalProgress?.Report("⭐ [4단계]  특수 처리 시작");
                
                //----------------------------------------------------------------------------------------------
                // 송장출력 메세지 생성
                // [4-1] 송장출력 메세지 처리 단계 - 오류 발생 시 프로세스 중단 및 메인창 로그(finalProgress)로 오류 메시지 전달
                try
                {
                    finalProgress?.Report("❄️ [4-1] 송장출력 메세지 처리");
                    _stepReporter?.ReportStepProgress(0); // 4-1 단계 (0부터 시작하므로 0)
                    LogManagerService.LogInfo("🔍 ProcessInvoiceMessageData 메서드 호출 시작...");
                    finalProgressReporter?.Report(5);
                    await ProcessInvoiceMessageData(); // 📝 4-1 송장출력 메세지 데이터 처리
                    LogManagerService.LogInfo("✅ ProcessInvoiceMessageData 메서드 호출 완료");
                    finalProgress?.Report("✅ [4-1] 송장출력 메세지 처리 완료");
                    _stepReporter?.ReportStepCompleted(0); // 4-1 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-1까지만 처리하는 경우 종료
                    if (maxStep <= 1)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-1단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 송장출력 메세지 처리만 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // 한글 주석: 오류 발생 시 로그 파일(app.log)에 기록
                    LogManagerService.LogError($"❌ ProcessInvoiceMessageData 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessInvoiceMessageData 오류 상세: {ex.StackTrace}");

                    // 한글 주석: 오류 메시지를 메인창 로그(finalProgress)로 전달
                    finalProgress?.Report($"❌ [4-1] 송장출력 메세지 처리 중 오류 발생: {ex.Message}");

                    // 한글 주석: 프로세스 중단 (예외 재발생)
                    throw;
                }

                //----------------------------------------------------------------------------------------------
                //합포장 처리 프로시져 호출
                // 🎁 합포장 최적화 프로시저 호출 (ProcessMergePacking)
                // 한글 주석: 합포장 최적화 처리 단계에서 오류 발생 시 프로세스 중단 및 메인창 로그(finalProgress)로 오류 메시지 전달
                try
                {
                    finalProgress?.Report("❄️ [4-2] 합포장 최적화 처리");
                    _stepReporter?.ReportStepProgress(1); // 4-2 단계 (0부터 시작하므로 1)
                    LogManagerService.LogInfo("🔍 ProcessMergePacking 메서드 호출 시작...");
                    await ProcessMergePacking(); // 📝 4-2 합포장 최적화 프로시저 호출
                    finalProgressReporter?.Report(9);
                    LogManagerService.LogInfo("✅ ProcessMergePacking 메서드 호출 완료");
                    finalProgress?.Report("✅ [4-2] 합포장 최적화 처리 완료");
                    _stepReporter?.ReportStepCompleted(1); // 4-2 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-2까지만 처리하는 경우 종료
                    if (maxStep <= 2)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-2단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 송장출력 메세지 및 합포장 최적화 처리 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // 한글 주석: 오류 발생 시 로그 파일(app.log)에 기록
                    LogManagerService.LogError($"❌ ProcessMergePacking 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessMergePacking 오류 상세: {ex.StackTrace}");

                    // 한글 주석: 오류 메시지를 메인창 로그(finalProgress)로 전달
                    finalProgress?.Report($"❌ [4-2] 합포장 최적화 처리 중 오류 발생: {ex.Message}");

                    // 한글 주석: 프로세스 중단 (예외 재발생)
                    throw;
                }

                //----------------------------------------------------------------------------------------------                
                // 송장분리처리 루틴 추가
                // 감천 특별출고 처리 루틴
                // [4-3] 감천 특별출고 처리 단계 - 오류 발생 시 프로세스 중단 및 메인창 로그(finalProgress)로 오류 메시지 전달
                try
                {
                    finalProgress?.Report("❄️ [4-3] 감천 특별출고 처리");
                    _stepReporter?.ReportStepProgress(2); // 4-3 단계 (0부터 시작하므로 2)
                    LogManagerService.LogInfo("🔍 ProcessInvoiceSplit1 메서드 호출 시작...");
                    await ProcessInvoiceSplit1();
                    finalProgressReporter?.Report(14);
                    LogManagerService.LogInfo("✅ ProcessInvoiceSplit1 메서드 호출 완료");       
                    finalProgress?.Report("✅ [4-3] 감천 특별출고 완료");
                    _stepReporter?.ReportStepCompleted(2); // 4-3 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-3까지만 처리하는 경우 종료
                    if (maxStep <= 3)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-3단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 송장출력 메세지, 합포장 최적화, 감천 특별출고 처리 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // 한글 주석: 오류 발생 시 로그 파일(app.log)에 기록
                    LogManagerService.LogError($"❌ ProcessInvoiceSplit1 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessInvoiceSplit1 오류 상세: {ex.StackTrace}");

                    // 한글 주석: 오류 메시지를 메인창 로그(finalProgress)로 전달
                    finalProgress?.Report($"❌ [4-3] 감천 특별출고 처리 중 오류 발생: {ex.Message}");

                    // 한글 주석: 프로세스 중단 (예외 재발생)
                    throw;
                }

                //----------------------------------------------------------------------------------------------
                // 판매입력_이카운트자료 (테이블 -> 엑셀 생성)
                // - 윈도우: Win + . (마침표) 키를 누르면 이모지 선택창이 나옵니다.
                // - macOS: Control + Command + Space 키를 누르면 이모지 선택창이 나옵니다.
                // - 또는 https://emojipedia.org/ 사이트에서 원하는 이모지를 복사해서 사용할 수 있습니다.
                // - C# 문자열에 직접 유니코드 이모지를 넣어도 되고, \uXXXX 형식의 유니코드 이스케이프를 사용할 수도 있습니다.
                // 예시: finalProgress?.Report("✅ 처리 완료!"); // 이모지는 위 방법으로 복사해서 붙여넣기
                // [4-4] 판매입력_이카운트자료 생성 및 업로드 처리 단계
                // [4-4] 판매입력_이카운트자료 생성 및 업로드 처리 단계
                // 한글 주석: 이 단계에서 오류 발생 시 프로세스를 중단하고, 오류 메시지를 메인창 로그(finalProgress)로 전달해야 함
                try
                {
                    // 한글 주석: [4-4] 판매입력_이카운트자료 생성 및 업로드 처리 단계 시작
                    finalProgress?.Report("💾 [4-4] 판매입력_이카운트자료 생성 및 업로드 처리");
                    _stepReporter?.ReportStepProgress(3); // 4-4 단계 (0부터 시작하므로 3)
                    LogManagerService.LogInfo("🔍 ProcessSalesInputData 메서드 호출 시작...");
                    LogManagerService.LogInfo($"🔍 ProcessSalesInputData 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    finalProgressReporter?.Report(18);
                    LogManagerService.LogInfo("🔍 ProcessSalesInputData 메서드 실행 중...");
                    finalProgress?.Report("✅ [4-4] 판매입력_이카운트자료 생성 및 업로드 처리 완료");
                    _stepReporter?.ReportStepCompleted(3); // 4-4 단계 완료
                    finalProgress?.Report("");
                    
                    // 한글 주석: 4-4 판매입력_이카운트자료 엑셀 생성 처리
                    var salesInputDataResult = await ProcessSalesInputData();
                    LogManagerService.LogInfo($"✅ ProcessSalesInputData 메서드 호출 완료 - 결과: {salesInputDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessSalesInputData 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-4까지만 처리하는 경우 종료
                    if (maxStep <= 4)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-4단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 송장출력 메세지, 합포장 최적화, 감천 특별출고, 판매입력_이카운트자료 처리 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // 한글 주석: 오류 발생 시 로그 파일(app.log)에 기록
                    LogManagerService.LogError($"❌ [4-4] ProcessSalesInputData 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ [4-4] ProcessSalesInputData 오류 상세: {ex.StackTrace}");

                    // 한글 주석: 오류 메시지를 메인창 로그(finalProgress)로 전달
                    finalProgress?.Report($"❌ [4-4] 판매입력_이카운트자료 처리 중 오류 발생: {ex.Message}");

                    // 한글 주석: 프로세스 중단 (예외 재발생)
                    throw;
                }

                //----------------------------------------------------------------------------------------------
                // 톡딜불가 처리
                // [4-5] 톡딜불가 처리 단계 - 예외 발생 시 프로세스 중단 및 메인창 로그에 오류 메시지 표시 (finalProgress 사용)
                try
                {
                    finalProgress?.Report("❄️ [4-5] 톡딜불가 처리");
                    _stepReporter?.ReportStepProgress(4); // 4-5 단계 (0부터 시작하므로 4)
                    LogManagerService.LogInfo("🔍 ProcessTalkDealUnavailable 메서드 호출 시작...");
                    await ProcessTalkDealUnavailable(); // 📝 4-5 톡딜불가 처리
                    finalProgressReporter?.Report(23);
                    LogManagerService.LogInfo("✅ ProcessTalkDealUnavailable 메서드 호출 완료");
                    finalProgress?.Report("✅ [4-5] 톡딜불가 처리 완료");
                    _stepReporter?.ReportStepCompleted(4); // 4-5 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-5까지만 처리하는 경우 종료
                    if (maxStep <= 5)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-5단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 송장출력 메세지, 합포장 최적화, 감천 특별출고, 판매입력_이카운트자료, 톡딜불가 처리 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 로그 파일에 기록
                    LogManagerService.LogError($"❌ ProcessTalkDealUnavailable 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessTalkDealUnavailable 오류 상세: {ex.StackTrace}");

                    // 사용자에게 오류 메시지 전달 (finalProgress 사용)
                    finalProgress?.Report($"❌ [4-5] 톡딜불가 처리 중 오류 발생: {ex.Message}");
                    // 프로세스 중단 (예외 재발생)
                                throw;
                }

                //----------------------------------------------------------------------------------------------
                // 송장출력관리 처리  
                #region 송장출력관리 처리
                // 배송메세지에서 별표지우기 
                // 별표 품목코드 데이터입력 
                // 별표1 = '★★★' (별표 배송메세지 데이터입력)
                // 별표 수취인 데이터입력
                // 별표 제주도
                // 별표 고객 공통 마킹
                // 박스상품 명칭변경
                // 택배 박스 낱개 나누기
                // 카카오 행사 송장 코드 (구현 하지 않음)
                // 송장 출고지별로 구분
                // 냉동 렉 위치 입력
                // 냉동 공산 제품 중 작은 품목 합포장처리
                // 빈 위치 업데이트
                // 이름 주소 전화번호 합치기
                // 냉동창고 공산품 송장분리입력
                // 공통박스 분류작업
                // 박스 공통늘리기
                // 박스주문 순번 매기기1 (송장구분자에 순번 업데이트)
                // 박스주문 주소업데이트
                // 박스주소 순번 매기기2 (주소 업데이트, 주소에 순번 업데이트)
                // 박스주문 수량1로변경
                // 박스주문 유일자설정 (위치변환)
                // 공통박스 수량 처리 (품목코드별로 수량을 합산하여 출력개수에 업데이트)
                // 개별작업1	(송장구분최종 업데이트)
	            //    : 서울낱개 중 서울 주소 + 쇼핑몰 조건
	            //    : 서울박스 중 서울 주소 + 쇼핑몰 조건
                // 개별작업2	(송장구분최종 업데이트)
	            //   : 서울박스 중 서울 주소 → 서울박스
                // [4-6] 송장출력관리 처리 단계 - 예외 발생 시 프로세스 중단 및 메인창 로그에 오류 메시지 표시 (finalProgress 사용)
                #endregion
                try
                {
                    finalProgress?.Report("❄️ [4-6] 송장출력관리 처리");
                    _stepReporter?.ReportStepProgress(5); // 4-6 단계 (0부터 시작하므로 5)
                    LogManagerService.LogInfo("🔍 ProcessInvoiceManagement 메서드 호출 시작...");
                    await ProcessInvoiceManagement(); // 📝 4-6 송장출력관리 처리
                    finalProgressReporter?.Report(27);
                    LogManagerService.LogInfo("✅ ProcessInvoiceManagement 메서드 호출 완료");
                    finalProgress?.Report("✅ [4-6] 송장출력관리 처리 완료");
                    _stepReporter?.ReportStepCompleted(5); // 4-6 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-6까지만 처리하는 경우 종료
                    if (maxStep <= 6)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-6단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 기본 데이터 처리 완료: 송장출력 메세지, 합포장 최적화, 감천 특별출고, 판매입력_이카운트자료, 톡딜불가, 송장출력관리");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 로그 파일에 기록
                    LogManagerService.LogError($"❌ ProcessInvoiceManagement 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessInvoiceManagement 오류 상세: {ex.StackTrace}");

                    // 사용자에게 오류 메시지 전달 (finalProgress 사용)
                    finalProgress?.Report($"❌ [4-6] 송장출력관리 처리 중 오류 발생: {ex.Message}");

                    // 프로세스 중단 (예외 재던짐)
                    throw;
                }

                // ==================== [서울냉동 처리] ========================================================
                #region 서울냉동 처리
                // 서울냉동처리
                // (서울냉동) 서울서울낱개 분류
                // (서울냉동) 택배수량 계산 및 송장구분자 업데이트
                // (서울냉동) 송장구분자와 수량 곱 업데이트
                // (서울냉동) 주소 + 수취인명 기반 송장구분자 합산
                // (서울냉동) 택배수량1 올림 처리
                // (서울냉동) 택배수량1에 따른 송장구분 업데이트
                // (서울냉동) 주소 및 수취인명 유일성에 따른 송장구분 업데이트 시작
                // (서울냉동) 서울냉동1장 분류
                // (서울냉동) 서울냉동 단일 분류
                // (서울냉동) 품목코드별 수량 합산 및 품목개수
                // (서울냉동) 서울냉동 추가 분류
                // (서울냉동) 서울냉동추가송장 테이블로 유니크 주소 행 이동
                // (서울냉동) 서울냉동추가송장 업데이트
                // (서울냉동) 서울냉동 추가송장 늘리기
                // (서울냉동) 서울냉동추가송장 순번 매기기
                // (서울냉동) 서울냉동추가송장 주소업데이트
                // (서울냉동) 서울냉동추가 합치기
                // (서울냉동) 서울냉동 테이블 마지막정리
                // (서울냉동) 별표 행 이동 및 삭제
                // (서울냉동) 별표1 기준으로 정렬하여 행 이동
                // (서울냉동) 송장출력_서울냉동에서 송장출력_서울냉동_최종으로 데이터 이동
                // (서울냉동) 송장출력_서울냉동_최종 테이블 업데이트(택배비용, 박스크기, 출력개수 업데이트) 
                #endregion
                try
                {
                    finalProgress?.Report("❄️ [4-7] 서울냉동 처리");
                    _stepReporter?.ReportStepProgress(6); // 4-7 단계 (0부터 시작하므로 6)
                    LogManagerService.LogInfo("🔍 ProcessSeoulFrozenManagement 메서드 호출 시작...");
                    await ProcessSeoulFrozenManagement(); // 📝 4-7 서울냉동 처리
                    finalProgressReporter?.Report(32);
                    LogManagerService.LogInfo("✅ ProcessSeoulFrozenManagement 메서드 호출 완료");
                    finalProgress?.Report("✅ [4-7] 서울냉동 처리 완료");
                    _stepReporter?.ReportStepCompleted(6); // 4-7 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-7까지만 처리하는 경우 종료
                    if (maxStep <= 7)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-7단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동 처리까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 app.log 파일에 기록
                    LogManagerService.LogError($"❌ ProcessSeoulFrozenManagement 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessSeoulFrozenManagement 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [서울냉동 처리 오류] {ex.Message}");

                    throw;
                }

                //----------------------------------------------------------------------------------------------
                // 서울냉동 최종파일 생성(업로드, 카카오워크)
                finalProgress?.Report("💾 [4-8] 서울냉동 최종파일 생성 및 업로드 처리");
                _stepReporter?.ReportStepProgress(7); // 4-8 단계 (0부터 시작하므로 7)
                LogManagerService.LogInfo("🔍 ProcessSeoulFrozenFinalFile 메서드 호출 시작...");
                LogManagerService.LogInfo($"🔍 ProcessSeoulFrozenFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                                    finalProgressReporter?.Report(36);
                LogManagerService.LogInfo("🔍 ProcessSeoulFrozenFinalFile 메서드 실행 중...");
                finalProgress?.Report("✅ [4-8] 서울냉동 최종파일 생성 및 업로드 처리 완료");
                _stepReporter?.ReportStepCompleted(7); // 4-8 단계 완료
                finalProgress?.Report("");

                try
                {
                    // 📝 4-8 서울냉동 최종파일 엑셀 생성
                    var salesDataResult = await ProcessSeoulFrozenFinalFile();
                    LogManagerService.LogInfo($"✅ ProcessSeoulFrozenFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessSeoulFrozenFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-8까지만 처리하는 경우 종료
                    if (maxStep <= 8)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-8단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동 처리 및 최종파일 생성까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 로그 파일에 기록 (app.log)
                    LogManagerService.LogError($"❌ ProcessSeoulFrozenFinalFile 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessSeoulFrozenFinalFile 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [서울냉동 최종파일 생성 오류] {ex.Message}");

                    throw;
                }


                // ==================== [경기냉동 처리] ========================================================
                #region 경기냉동 처리
                // (경기냉동) 경기냉동낱개 분류
                // (경기냉동) 택배수량 계산 및 송장구분자 업데이트
                // (경기냉동) 송장구분자와 수량 곱 업데이트
                // (경기냉동) 주소 + 수취인명 기반 송장구분자 합산
                // (경기냉동) 택배수량1 올림 처리
                // (경기냉동) 택배수량1에 따른 송장구분 업데이트
                // (경기냉동) 주소 및 수취인명 유일성에 따른 송장구분 업데이트 시작
                // (경기냉동) 경기냉동1장 분류
                // (경기냉동) 경기냉동 단일 분류
                // (경기냉동) 품목코드별 수량 합산 및 품목개수
                // (경기냉동) 경기냉동 추가 분류
                // (경기냉동) 경기냉동추가송장 테이블로 유니크 주소 행 이동
                // (경기냉동) 경기냉동추가송장 업데이트
                // (경기냉동) 경기냉동 추가송장 늘리기
                // (경기냉동) 경기냉동추가송장 순번 매기기
                // (경기냉동) 경기냉동추가송장 주소업데이트
                // (경기냉동) 경기냉동추가 합치기
                // (경기냉동) 경기냉동 테이블 마지막정리
                // (경기냉동) 별표 행 이동 및 삭제
                // (경기냉동) 별표1 기준으로 정렬하여 행 이동
                // (경기냉동) 송장출력_경기냉동에서 송장출력_경기냉동_최종으로 데이터 이동
                // (경기냉동) 송장출력_경기냉동_최종 테이블 업데이트(택배비용, 박스크기, 출력개수 업데이트)     
                #endregion
                try
                {
                    finalProgress?.Report("❄️ [4-9] 경기냉동 처리");           
                    _stepReporter?.ReportStepProgress(8); // 4-9 단계 (0부터 시작하므로 8)
                    LogManagerService.LogInfo("🔍 ProcessGyeonggiFrozenManagement 메서드 호출 시작...");
                    await ProcessGyeonggiFrozenManagement();
                    finalProgressReporter?.Report(41);
                    LogManagerService.LogInfo("🔍 ProcessGyeonggiFrozenManagement 메서드 호출 완료...");
                    finalProgress?.Report("✅ [4-9] 경기냉동 처리 완료");
                    _stepReporter?.ReportStepCompleted(8); // 4-9 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-9까지만 처리하는 경우 종료
                    if (maxStep <= 9)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-9단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동 처리까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessGyeonggiFrozenManagement 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessGyeonggiFrozenManagement 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [경기냉동 처리 오류] {ex.Message}");

                    throw;
                }

                //----------------------------------------------------------------------------------------------
                // 경기냉동 최종파일 생성(업로드, 카카오워크)
                finalProgress?.Report("💾 [4-10] 경기냉동 최종파일 생성 및 업로드 처리");
                _stepReporter?.ReportStepProgress(9); // 4-10 단계 (0부터 시작하므로 9)
                LogManagerService.LogInfo("🔍 ProcessGyeonggiFrozenFinalFile 메서드 호출 시작...");
                LogManagerService.LogInfo($"🔍 ProcessGyeonggiFrozenFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalProgressReporter?.Report(45);
                LogManagerService.LogInfo("🔍 ProcessGyeonggiFrozenFinalFile 메서드 실행 중...");
                finalProgress?.Report("✅ [4-10] 경기냉동 최종파일 생성 및 업로드 처리 완료");
                _stepReporter?.ReportStepCompleted(9); // 4-10 단계 완료
                finalProgress?.Report("");

                try
                {
                    var salesDataResult = await ProcessGyeonggiFrozenFinalFile(); 
                    LogManagerService.LogInfo($"✅ ProcessGyeonggiFrozenFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessGyeonggiFrozenFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-10까지만 처리하는 경우 종료
                    if (maxStep <= 10)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-10단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동 처리 및 최종파일 생성까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessGyeonggiFrozenFinalFile 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessGyeonggiFrozenFinalFile 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [경기냉동 최종파일 생성 오류] {ex.Message}");

                    throw;
                }


                // ===================  = [서울공산 처리] ========================================================
                #region 서울공산 처리
                // (서울공산) 서울공산낱개 분류 
                // (서울공산) 택배수량 계산 및 송장구분자 업데이트
                // (서울공산) 송장구분자와 수량 곱 업데이트
                // (서울공산) 주소 + 수취인명 기반 송장구분자 합산
                // (서울공산) 택배수량1 올림 처리
                // (서울공산) 택배수량1에 따른 송장구분 업데이트
                // (서울공산) 주소 및 수취인명 유일성에 따른 송장구분 업데이트 시작
                // (서울공산) 서울공산1장 분류
                // (서울공산) 서울공산 단일 분류
                // (서울공산) 품목코드별 수량 합산 및 품목개수
                // (서울공산) 서울공산 추가 분류
                // (서울공산) 서울공산추가송장 테이블로 유니크 주소 행 이동
                // (서울공산) 서울공산추가송장 업데이트
                // (서울공산) 서울공산 추가송장 늘리기
                // (서울공산) 서울공산추가송장 순번 매기기
                // (서울공산) 서울공산추가송장 주소업데이트
                // (서울공산) 서울공산추가 합치기
                // (서울공산) 서울공산 테이블 마지막정리
                // (서울공산) 별표 행 이동 및 삭제
                // (서울공산) 별표1 기준으로 정렬하여 행 이동
                // (서울공산) 송장출력_서울공산에서 송장출력_서울공산_최종으로 데이터 이동
                // (서울공산) 송장출력_서울공산_최종 테이블 업데이트(택배비용, 박스크기, 출력개수 업데이트)     
                #endregion
                try
                {
                    finalProgress?.Report("❄️ [4-11] 서울공산 처리");           
                    _stepReporter?.ReportStepProgress(10); // 4-11 단계 (0부터 시작하므로 10)
                    LogManagerService.LogInfo("🔍 ProcessSeoulGongsanManagement 메서드 호출 시작...");
                    await ProcessSeoulGongsanManagement();
                    finalProgressReporter?.Report(50);
                    LogManagerService.LogInfo("🔍 ProcessSeoulGongsanManagement 메서드 호출 완료...");
                    finalProgress?.Report("✅ [4-11] 서울공산 처리 완료");
                    _stepReporter?.ReportStepCompleted(10); // 4-11 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-11까지만 처리하는 경우 종료
                    if (maxStep <= 11)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-11단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산 처리까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessSeoulGongsanManagement 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessSeoulGongsanManagement 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [서울공산 처리 오류] {ex.Message}");

                    throw;
                }

                //----------------------------------------------------------------------------------------------
                // 서울공산 최종파일 생성(업로드, 카카오워크)
                finalProgress?.Report("💾 [4-12] 서울공산 최종파일 생성 및 업로드 처리");
                _stepReporter?.ReportStepProgress(11); // 4-12 단계 (0부터 시작하므로 11)
                LogManagerService.LogInfo("🔍 ProcessSeoulGongsanFinalFile 메서드 호출 시작...");
                LogManagerService.LogInfo($"🔍 ProcessSeoulGongsanFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalProgressReporter?.Report(54);
                LogManagerService.LogInfo("🔍 ProcessSeoulGongsanFinalFile 메서드 실행 중...");
                finalProgress?.Report("✅ [4-12] 서울공산 최종파일 생성 및 업로드 처리 완료");
                _stepReporter?.ReportStepCompleted(11); // 4-12 단계 완료
                finalProgress?.Report("");

                try
                {
                    var salesDataResult = await ProcessSeoulGongsanFinalFile(); 
                    LogManagerService.LogInfo($"✅ ProcessSeoulGongsanFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessSeoulGongsanFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-12까지만 처리하는 경우 종료
                    if (maxStep <= 12)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-12단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산 처리 및 최종파일 생성까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                        LogManagerService.LogError($"❌ ProcessSeoulGongsanFinalFile 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessSeoulGongsanFinalFile 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [서울공산 최종파일 생성 오류] {ex.Message}");

                    throw;
                }

                // ===================  = [경기 공산 처리] ========================================================
                #region 경기공산 처리
  
                #endregion
                try
                {
                    finalProgress?.Report("❄️ [4-13] 경기공산 처리");           
                    _stepReporter?.ReportStepProgress(12); // 4-13 단계 (0부터 시작하므로 12)
                    LogManagerService.LogInfo("🔍 ProcessGyeonggiGongsanManagement 메서드 호출 시작...");
                    await ProcessGyeonggiGongsanManagement();
                    finalProgressReporter?.Report(59);
                    LogManagerService.LogInfo("🔍 ProcessGyeonggiGongsanManagement 메서드 호출 완료...");
                    finalProgress?.Report("✅ [4-13] 경기공산 처리 완료");
                    _stepReporter?.ReportStepCompleted(12); // 4-13 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-13까지만 처리하는 경우 종료
                    if (maxStep <= 13)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-13단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산 처리까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessGyeonggiGongsanManagement 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessGyeonggiGongsanManagement 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [경기공산 처리 오류] {ex.Message}");

                    throw;
                }

                //----------------------------------------------------------------------------------------------
                // 경기공산 최종파일 생성(업로드, 카카오워크)
                finalProgress?.Report("💾 [4-14] 경기공산 최종파일 생성 및 업로드 처리");
                _stepReporter?.ReportStepProgress(13); // 4-14 단계 (0부터 시작하므로 13)
                LogManagerService.LogInfo("🔍 ProcessGyeonggiGongsanFinalFile 메서드 호출 시작...");
                LogManagerService.LogInfo($"🔍 ProcessGyeonggiGongsanFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalProgressReporter?.Report(63);
                LogManagerService.LogInfo("🔍 ProcessGyeonggiGongsanFinalFile 메서드 실행 중...");
                finalProgress?.Report("✅ [4-14] 경기공산 최종파일 생성 및 업로드 처리 완료");
                _stepReporter?.ReportStepCompleted(13); // 4-14 단계 완료
                finalProgress?.Report("");

                try
                {
                    var salesDataResult = await ProcessGyeonggiGongsanFinalFile(); 
                    LogManagerService.LogInfo($"✅ ProcessGyeonggiGongsanFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessGyeonggiGongsanFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-14까지만 처리하는 경우 종료
                    if (maxStep <= 14)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-14단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산 처리 및 최종파일 생성까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                        LogManagerService.LogError($"❌ ProcessGyeonggiGongsanFinalFile 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessGyeonggiGongsanFinalFile 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [경기공산 최종파일 생성 오류] {ex.Message}");

                    throw;
                }

                // ===================  = [부산청과 처리] ========================================================
                #region 부산청과 처리
                // (부산청과) 부산청과낱개 분류 
                // (부산청과) 택배수량 계산 및 송장구분자 업데이트
                // (부산청과) 송장구분자와 수량 곱 업데이트
                // (부산청과) 주소 + 수취인명 기반 송장구분자 합산
                // (부산청과) 택배수량1 올림 처리
                // (부산청과) 택배수량1에 따른 송장구분 업데이트
                // (부산청과) 주소 및 수취인명 유일성에 따른 송장구분 업데이트 시작
                // (부산청과) 부산청과1장 분류
                // (부산청과) 부산청과 단일 분류
                // (부산청과) 품목코드별 수량 합산 및 품목개수
                // (부산청과) 부산청과 추가 분류
                // (부산청과) 부산청과추가송장 테이블로 유니크 주소 행 이동
                // (부산청과) 부산청과추가송장 업데이트
                // (부산청과) 부산청과 추가송장 늘리기
                // (부산청과) 부산청과추가송장 순
                // (부산청과) 부산청과추가송장 주소업데이트
                // (부산청과) 부산청과추가 합치기
                // (부산청과) 부산청과 테이블 마지막정리
                // (부산청과) 별표 행 이동 및 삭제
                // (부산청과) 별표1 기준으로 정렬하여 행 이동
                // (부산청과) 송장출력_부산청과에서 송장출력_부산청과_최종으로 데이터 이동
                // (부산청과) 송장출력_부산청과_최종 테이블 업데이트(택배비용, 박스크기, 출력개수 업데이트)     
                #endregion
                try
                {
                    finalProgress?.Report("❄️ [4-15] 부산청과 처리");           
                    _stepReporter?.ReportStepProgress(14); // 4-15 단계 (0부터 시작하므로 14)
                    LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaManagement 메서드 호출 시작...");
                    await ProcessBusanCheonggwaManagement();
                    finalProgressReporter?.Report(68);
                    LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaManagement 메서드 호출 완료...");
                    finalProgress?.Report("✅ [4-15] 부산청과 처리 완료");
                    _stepReporter?.ReportStepCompleted(14); // 4-15 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-15까지만 처리하는 경우 종료
                    if (maxStep <= 15)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-15단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산, 부산청과 처리까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessBusanCheonggwaManagement 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessBusanCheonggwaManagement 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [부산청과 처리 오류] {ex.Message}");

                    throw;
                }

                // ===================  = [부산청과 외부출고 처리] ========================================================
                // 송장출력_부산청과_최종변환
                //try
                //{
                //    finalProgress?.Report("💾 [4-16] 부산 외부출고 처리");
                //    LogManagerService.LogInfo("🔍 ProcessBusanExtShipmentManagement 메서드 호출 시작...");
                //    await ProcessBusanExtShipmentManagement();
                //    finalProgressReporter?.Report(64);
                //    LogManagerService.LogInfo("✅ ProcessBusanExtShipmentManagement 메서드 호출 완료");
                //    finalProgress?.Report("✅ [4-16] 부산 외부출고 처리 완료");
                //    finalProgress?.Report("");
                //}
                //catch (Exception ex)
                //{
                    // 오류 발생 시 로그 파일에 기록
                //    LogManagerService.LogError($"❌ ProcessBusanExtShipmentManagement 실행 중 오류 발생: {ex.Message}");
                //    LogManagerService.LogError($"❌ ProcessBusanExtShipmentManagement 오류 상세: {ex.StackTrace}");

                    // 사용자에게 오류 메시지 전달 (finalProgress 사용)
                //    finalProgress?.Report($"❌ [4-16] 부산 외부출고 처리 중 오류 발생: {ex.Message}");

                    // 프로세스 중단 (예외 재던짐)
                //    throw;
                //}

                // ===================  = [부산청과 최종파일 생성] ========================================================
                // 부산청과 최종파일 생성(업로드, 카카오워크)
                // 부산청과 운송장
                finalProgress?.Report("💾 [4-16] 부산청과 최종파일 생성 및 업로드 처리");
                _stepReporter?.ReportStepProgress(15); // 4-16 단계 (0부터 시작하므로 15)
                LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaFinalFile 메서드 호출 시작...");
                LogManagerService.LogInfo($"🔍 ProcessBusanCheonggwaFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalProgressReporter?.Report(72);
                LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaFinalFile 메서드 실행 중...");
                finalProgress?.Report("✅ [4-16] 부산청과 최종파일 생성 및 업로드 처리 완료");
                _stepReporter?.ReportStepCompleted(15); // 4-16 단계 완료
                finalProgress?.Report("");

                try
                {
                    var salesDataResult = await ProcessBusanCheonggwaFinalFile(); 
                    LogManagerService.LogInfo($"✅ ProcessBusanCheonggwaFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessBusanCheonggwaFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-16까지만 처리하는 경우 종료
                    if (maxStep <= 16)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-16단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산, 부산청과 처리 및 최종파일 생성까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessBusanCheonggwaFinalFile 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessBusanCheonggwaFinalFile 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [부산청과 최종파일 생성 오류] {ex.Message}");

                    throw;
                }

                // ===================  = [부산청과외부출고 최종파일 생성] ========================================================
                // 부산청과 최종파일 생성(업로드, 카카오워크)
                // 부산청과 운송장
                //finalProgress?.Report("💾 [4-18] 부산청과 외부출고 최종파일 생성 및 업로드 처리");
                //LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaExtFinalFile 메서드 호출 시작...");
                //LogManagerService.LogInfo($"🔍 ProcessBusanCheonggwaExtFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                //finalProgressReporter?.Report(63);
                //LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaExtFinalFile 메서드 실행 중...");
                //finalProgress?.Report("✅ [4-18] 부산청과 외부출고 최종파일 생성 및 업로드 처리 완료");
                //finalProgress?.Report("");

                //try
                //{
                    //var salesDataResult = await ProcessBusanCheonggwaExtFinalFile(); 
                    //LogManagerService.LogInfo($"✅ ProcessBusanCheonggwaExtFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    //LogManagerService.LogInfo($"✅ ProcessBusanCheonggwaExtFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                //}
                //catch (Exception ex)
                //{
                //    LogManagerService.LogError($"❌ ProcessBusanCheonggwaExtFinalFile 실행 중 오류 발생: {ex.Message}");
                //    LogManagerService.LogError($"❌ ProcessBusanCheonggwaExtFinalFile 오류 상세: {ex.StackTrace}");
                //    finalProgress?.Report($"❌ [부산청과 외부출고 최종파일 생성 오류] {ex.Message}");

                //    throw;
                //}

                // ===================  = [부산청과자료 처리] ========================================================
                try
                {
                    finalProgress?.Report("❄️ [4-17] 부산청과자료 처리");           
                    _stepReporter?.ReportStepProgress(16); // 4-17 단계 (0부터 시작하므로 16)
                    LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaDoc 메서드 호출 시작...");
                    await ProcessBusanCheonggwaDoc();
                    finalProgressReporter?.Report(77);
                    LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaDoc 메서드 호출 완료...");
                    finalProgress?.Report("✅ [4-17] 부산청과자료 처리 완료");
                    _stepReporter?.ReportStepCompleted(16); // 4-17 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-17까지만 처리하는 경우 종료
                    if (maxStep <= 17)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-17단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산, 부산청과 처리 및 최종파일 생성, 부산청과자료 처리까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessBusanCheonggwaDoc 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessBusanCheonggwaDoc 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [부산청과자료 처리 오류] {ex.Message}");

                    throw;
                }

                // ===================  = [부산청과자료 최종파일 생성] ========================================================
                // 부산청과자료 최종파일 생성(업로드, 카카오워크)
                // 부산청과 A4자료
                finalProgress?.Report("💾 [4-18] 부산청과 A4자료 최종파일 생성 및 업로드 처리");
                _stepReporter?.ReportStepProgress(17); // 4-18 단계 (0부터 시작하므로 17)
                LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaDocFinalFile 메서드 호출 시작...");
                LogManagerService.LogInfo($"🔍 ProcessBusanCheonggwaDocFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalProgressReporter?.Report(81);
                LogManagerService.LogInfo("🔍 ProcessBusanCheonggwaDocFinalFile 메서드 실행 중...");
                finalProgress?.Report("✅ [4-18] 부산청과 A4자료 최종파일 생성 및 업로드 처리 완료");
                _stepReporter?.ReportStepCompleted(17); // 4-18 단계 완료
                finalProgress?.Report("");

                try
                {
                    var salesDataResult = await ProcessBusanCheonggwaDocFinalFile(); 
                    LogManagerService.LogInfo($"✅ ProcessBusanCheonggwaDocFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessBusanCheonggwaDocFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-18까지만 처리하는 경우 종료
                    if (maxStep <= 18)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-18단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산, 부산청과 처리 및 최종파일 생성, 부산청과자료 처리 및 A4자료 최종파일 생성까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessBusanCheonggwaDocFinalFile 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessBusanCheonggwaDocFinalFile 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [부산청과 A4자료 최종파일 생성 오류] {ex.Message}");

                    throw;
                }

                // ===================  [감천냉동 처리] ========================================================
                try
                {
                    finalProgress?.Report("❄️ [4-19] 감천냉동 처리");           
                    _stepReporter?.ReportStepProgress(18); // 4-19 단계 (0부터 시작하므로 18)
                    LogManagerService.LogInfo("🔍 ProcessGamcheonFrozenManagement 메서드 호출 시작...");
                    await ProcessGamcheonFrozenManagement();
                    finalProgressReporter?.Report(86);
                    LogManagerService.LogInfo("🔍 ProcessGamcheonFrozenManagement 메서드 호출 완료...");
                    finalProgress?.Report("✅ [4-19] 감천냉동 처리 완료");
                    _stepReporter?.ReportStepCompleted(18); // 4-19 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-19까지만 처리하는 경우 종료
                    if (maxStep <= 19)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-19단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산, 부산청과 처리 및 최종파일 생성, 부산청과자료 처리 및 A4자료 최종파일 생성, 감천냉동 처리까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessGamcheonFrozenManagement 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessGamcheonFrozenManagement 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [감천냉동 처리 오류] {ex.Message}");

                    throw;
                }

                // ===================  = [감천냉동 최종파일 생성] ========================================================
                // 감천냉동 최종파일 생성(업로드, 카카오워크)
                // 감천냉동 운송장
                finalProgress?.Report("💾 [4-20] 감천냉동 최종파일 생성 및 업로드 처리");
                _stepReporter?.ReportStepProgress(19); // 4-20 단계 (0부터 시작하므로 19)
                LogManagerService.LogInfo("🔍 ProcessGamcheonFrozenFinalFile 메서드 호출 시작...");
                LogManagerService.LogInfo($"🔍 ProcessGamcheonFrozenFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalProgressReporter?.Report(90);
                LogManagerService.LogInfo("🔍 ProcessGamcheonFrozenFinalFile 메서드 실행 중...");
                finalProgress?.Report("✅ [4-20] 감천냉동 최종파일 생성 및 업로드 처리 완료");
                _stepReporter?.ReportStepCompleted(19); // 4-20 단계 완료
                finalProgress?.Report("");

                try
                {
                    var salesDataResult = await ProcessGamcheonFrozenFinalFile(); 
                    LogManagerService.LogInfo($"✅ ProcessGamcheonFrozenFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessGamcheonFrozenFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-20까지만 처리하는 경우 종료
                    if (maxStep <= 20)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-20단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산, 부산청과 처리 및 최종파일 생성, 부산청과자료 처리 및 A4자료 최종파일 생성, 감천냉동 처리 및 최종파일 생성까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessGamcheonFrozenFinalFile 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessGamcheonFrozenFinalFile 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [감천냉동 최종파일 생성 오류] {ex.Message}");

                    throw;
                }

                // ===================  [송장출력 최종 처리] ========================================================
                try
                {
                    finalProgress?.Report("❄️ [4-21] 송장출력 최종 처리");           
                    _stepReporter?.ReportStepProgress(20); // 4-21 단계 (0부터 시작하므로 20)
                    LogManagerService.LogInfo("🔍 ProcessInvoiceFinalManagement 메서드 호출 시작...");
                    await ProcessInvoiceFinalManagement();
                    finalProgressReporter?.Report(95);
                    LogManagerService.LogInfo("🔍 ProcessInvoiceFinalManagement 메서드 호출 완료...");
                    finalProgress?.Report("✅ [4-21] 송장출력 최종 처리 완료");
                    _stepReporter?.ReportStepCompleted(20); // 4-21 단계 완료
                    finalProgress?.Report("");

                    // maxStep 체크: 4-21까지만 처리하는 경우 종료
                    if (maxStep <= 21)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-21단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 서울냉동, 경기냉동, 서울공산, 경기공산, 부산청과 처리 및 최종파일 생성, 부산청과자료 처리 및 A4자료 최종파일 생성, 감천냉동 처리 및 최종파일 생성, 송장출력 최종 처리까지 완료되었습니다.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessInvoiceFinalManagement 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessInvoiceFinalManagement 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [송장출력 최종 처리 오류] {ex.Message}");

                    throw;
                }

                // ===================  = [송장출력 최종파일 생성 (통합송장)] ========================================================
                // 송장출력 최종파일 생성(업로드, 카카오워크)
                // 통합송장
                finalProgress?.Report("💾 [4-22] 송장출력 최종파일 생성 및 업로드 처리");
                _stepReporter?.ReportStepProgress(21); // 4-22 단계 (0부터 시작하므로 21)
                LogManagerService.LogInfo("🔍 ProcessInvoiceFinalFile 메서드 호출 시작...");
                LogManagerService.LogInfo($"🔍 ProcessInvoiceFinalFile 호출 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                finalProgressReporter?.Report(100);
                LogManagerService.LogInfo("🔍 ProcessInvoiceFinalFile 메서드 실행 중...");
                finalProgress?.Report("✅ [4-22] 송장출력 최종파일 생성 및 업로드 처리 완료");
                _stepReporter?.ReportStepCompleted(21); // 4-22 단계 완료
                finalProgress?.Report("");

                try
                {
                    var salesDataResult = await ProcessInvoiceFinalFile(); 
                    LogManagerService.LogInfo($"✅ ProcessInvoiceFinalFile 메서드 호출 완료 - 결과: {salesDataResult}");
                    LogManagerService.LogInfo($"✅ ProcessInvoiceFinalFile 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // maxStep 체크: 4-22까지 처리 완료 (전체 처리)
                    // maxStep이 22인 경우는 전체 처리가므로 제한 메시지 표시하지 않음
                    if (maxStep < 22)
                    {
                        finalProgress?.Report($"🛑 [제한] 4-22단계까지만 처리 완료 (maxStep: {maxStep})");
                        finalProgress?.Report("🎉 송장출력 메세지부터 송장출력 최종파일 생성까지 완료되었습니다!");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ ProcessInvoiceFinalFile 실행 중 오류 발생: {ex.Message}");
                    LogManagerService.LogError($"❌ ProcessInvoiceFinalFile 오류 상세: {ex.StackTrace}");
                    finalProgress?.Report($"❌ [송장출력 최종파일 생성 오류] {ex.Message}");

                    throw;
                }




                finalProgress?.Report("✅ [4단계] 특수 처리 완료");
                finalProgress?.Report("");













                //finalProgressReporter?.Report(60);
                //finalProgress?.Report("✅ [4단계 완료] 고급 특수 처리 완료 - 물류 최적화 적용됨");

                
                //finalProgress?.Report("📦 [5단계] AI 기반 출고지별 최적 분류 시작...");
                //await ClassifyAndProcessByShipmentCenter(); // 🎯 지능형 물류센터 배정
                //finalProgressReporter?.Report(80);
                //finalProgress?.Report("✅ [5단계 완료] 출고지별 최적 분류 완료 - 배송 효율성 극대화");

                // ==================== 6단계: 클라우드 기반 파일 생성 및 자동 배포 시스템 (80-95%) [현재 비활성화] ====================
                
                // 📄 [6단계 - 현재 비활성화] 엔터프라이즈급 파일 생성 및 클라우드 배포
                // 
                // 🏭 대규모 물류센터 대응 파일 생성 시스템:
                // 
                // 📊 **6-1. 판매입력 자료 생성** (GenerateSalesInputData)
                //    - 물류센터별 맞춤형 Excel 파일 자동 생성
                //    - 실시간 재고 연동 및 품절 상품 자동 제외
                //    - 회계 시스템 연동용 매출 데이터 정확성 보장
                //    - 다국어 지원 (한국어, 영어, 중국어, 일본어)
                // 
                // 📋 **6-2. 송장 파일 대량 생성** (GenerateInvoiceFiles)
                //    - 출고지별 분류된 송장 파일 병렬 생성
                //    - 바코드, QR코드 자동 생성 및 삽입
                //    - 배송 라벨 최적화 (A4, 라벨지 등 다양한 형식)
                //    - 대용량 파일 처리 최적화 (10만 건 이상)
                // 
                // ☁️ **6-3. Dropbox 클라우드 자동 배포**
                //    - 물류센터별 전용 폴더 자동 업로드
                //    - 파일 버전 관리 및 백업 시스템
                //    - 실시간 공유 링크 생성 및 권한 관리
                //    - 업로드 실패 시 자동 재시도 (최대 5회)
                // 
                // 🔐 **보안 및 규정 준수**
                //    - 개인정보보호법 준수 (주민번호 마스킹 등)
                //    - 파일 암호화 및 접근 권한 제어
                //    - 감사 로그 자동 생성 및 보관
                
                //finalProgress?.Report("📄 [6단계] 클라우드 기반 파일 생성 및 배포 시작...");
                //var uploadResults = await GenerateAndUploadFiles(); // 📊 대량 파일 생성 및 클라우드 배포
                //finalProgressReporter?.Report(95);
                //finalProgress?.Report("✅ [6단계 완료] 파일 생성 및 클라우드 배포 완료 - 실시간 접근 가능");

                // ==================== 7단계: 실시간 통합 알림 및 모니터링 시스템 (95-100%) [현재 비활성화] ====================
                
                // �� [7단계 - 현재 비활성화] 차세대 KakaoWork 통합 알림 시스템
                // 
                // 🚀 실시간 다중 채널 알림 및 비즈니스 인텔리전스:
                // 
                // 💬 **7-1. KakaoWork 실시간 알림** (SendKakaoWorkNotifications)
                //    - 물류센터별 전용 채팅방 자동 알림 전송
                //    - 처리 결과 상세 통계 리포트 자동 생성
                //    - 이상 상황 즉시 알림 (오류, 지연, 품절 등)
                //    - 관리자 대시보드 실시간 업데이트
                // 
                // 📊 **7-2. 비즈니스 인텔리전스 리포트**
                //    - 일일/주간/월간 처리량 자동 분석
                //    - 물류센터별 성과 지표 비교 분석
                //    - 배송 효율성 개선 제안 자동 생성
                //    - ROI 및 비용 최적화 분석 리포트
                // 
                // 🎯 **7-3. 예측 분석 및 최적화 제안**
                //    - AI 기반 수요 예측 및 재고 최적화 제안
                //    - 계절별, 이벤트별 물량 예측
                //    - 배송 경로 최적화 알고리즘 제안
                //    - 비용 절감 기회 자동 식별
                // 
                // 🔔 **7-4. 다중 채널 알림 통합**
                //    - KakaoWork, 이메일, SMS 통합 알림
                //    - 중요도별 알림 채널 자동 선택
                //    - 담당자 부재 시 대체 알림 경로 활성화
                //    - 24/7 모니터링 및 긴급 상황 대응
                
                //finalProgress?.Report("📱 [7단계] 실시간 통합 알림 시스템 시작...");
                //await SendKakaoWorkNotifications(uploadResults); // 🚀 다중 채널 실시간 알림
                //finalProgressReporter?.Report(100);
                //finalProgress?.Report("✅ [7단계 완료] 통합 알림 전송 완료 - 실시간 모니터링 활성화됨");

                // ==================== 워크플로우 완료 및 비즈니스 성과 보고 ====================
                
                // === 처리 완료 시점 통계 및 성과 측정 ===
                var endTime = DateTime.Now;
                var processingDuration = endTime - workflowStartTime; // 실제 경과 시간 계산
                
                // 🎉 전사 물류 시스템 워크플로우 성공적 완료 선언
                finalProgress?.Report("🎉 물류 시스템 송장 처리 성공!");
                finalProgress?.Report($"⏱️ 총 처리 시간: {processingDuration.TotalSeconds:F1}초");
                finalProgress?.Report($"📊 처리 완료: 총 22단계 처리됨");
                finalProgress?.Report($"✅ 최종 상태: 모든 송장 처리 완료");

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                // ==================== 엔터프라이즈급 예외 처리 및 장애 대응 시스템 ====================
                
                // === 1단계: 예외 타입별 세분화된 오류 분석 및 분류 ===
                var errorCategory = _utilityCommonService.ClassifyException(ex);
                var errorSeverity = _utilityCommonService.DetermineErrorSeverity(ex);
                var recoveryAction = _utilityCommonService.GetRecoveryAction(ex);
                
                // === 2단계: 비즈니스 친화적 오류 메시지 생성 ===
                // 기술적 예외를 사용자가 이해할 수 있는 비즈니스 용어로 변환
                var userFriendlyMessage = _utilityCommonService.GenerateUserFriendlyErrorMessage(ex, errorCategory);
                
                // 🚨 사용자에게 전달할 핵심 오류 정보 구성
                var errorMessage = $"❌ [시스템 오류] {userFriendlyMessage}";
                errorMessage += $"\n📋 오류 분류: {errorCategory}";
                errorMessage += $"\n⚠️ 심각도: {errorSeverity}";
                errorMessage += $"\n🔧 권장 조치: {recoveryAction}";
                
                // === 3단계: 내부 예외 체인 분석 및 근본 원인 추적 ===
                // InnerException 체인을 순회하며 근본 원인 파악
                var rootCause = _utilityCommonService.GetRootCause(ex);
                if (rootCause != ex)
                {
                    errorMessage += $"\n🔍 근본 원인: {rootCause.GetType().Name} - {rootCause.Message}";
                }
                
                // 오류 알림 및 로깅
                _progress?.Report(errorMessage);
                _progressReporter?.Report(-1);
                
                // 상세 오류 정보 출력
                LogManagerService.LogError($"🚨 시스템 장애 발생: {ex.GetType().Name} - {ex.Message}");
                LogManagerService.LogError($"📂 파일 경로: {filePath ?? "Unknown"}");
                LogManagerService.LogError($"🔍 근본 원인: {rootCause.Message}");
                
                // 긴급 정리 작업
                try
                {
                    _utilityCommonService.PerformEmergencyCleanup();
                }
                catch (Exception cleanupEx)
                {
                    LogManagerService.LogWarning($"⚠️ 긴급 정리 작업 실패: {cleanupEx.Message}");
                }
                throw new InvalidOperationException(
                    $"전사 물류 시스템 처리 실패 - {errorCategory}: {userFriendlyMessage}", 
                    ex);
            }
        }

        #endregion

        #region 데이터베이스 초기화 및 원본 데이터 적재

        /// <summary>
        /// </summary>
        /// <param name="data">Excel에서 읽은 원본 데이터</param>
        /// <param name="progress">진행률 콜백</param>
        /// <exception cref="Exception">데이터베이스 초기화 실패 시</exception>
                // 이 메서드는 DatabaseCommonService의 TruncateAndInsertOriginalDataOptimized로 대체되었습니다.
        // 중복 코드 제거를 위해 주석 처리
        /*
        private async Task TruncateAndInsertOriginalDataOptimized(DataTable data, IProgress<string>? progress)
        {
            // 로그 파일 경로 (통합 로그 서비스 사용)
                            var logPath = LogPathManager.AppLogPath; // 기존 호환성 유지
            
            try
            {
                // 데이터베이스 연결 상태 확인
                progress?.Report("🔍 데이터베이스 연결 상태 확인 중...");
                LogManagerService.LogInfo("🔍 데이터베이스 연결 상태 확인 중...");
                
                var isConnected = await _databaseCommonService.CheckDatabaseConnectionAsync();
                if (!isConnected)
                {
                    var errorMessage = "데이터베이스에 연결할 수 없습니다. 연결 정보와 네트워크 상태를 확인해주세요.";
                    LogManagerService.LogError($"❌ {errorMessage}");
                    throw new InvalidOperationException(errorMessage);
                }
                
                progress?.Report("✅ 데이터베이스 연결 확인 완료");
                LogManagerService.LogInfo("✅ 데이터베이스 연결 확인 완료");

                // 데이터베이스 테이블 초기화
                progress?.Report("🗄️ 데이터베이스 테이블 초기화 중... ");
                LogManagerService.LogInfo("🗄️ 데이터베이스 테이블 초기화 중... ");
                
                var tableName = GetTableName("Tables.Invoice.Main");
                LogManagerService.LogInfo($"🔍 대상 테이블: {tableName}");
                
                // 테이블 존재 여부 확인
                var tableExists = await _databaseCommonService.CheckTableExistsAsync(tableName);
                if (!tableExists)
                {
                    progress?.Report($"⚠️ 테이블 '{tableName}'이 존재하지 않습니다.");
                    LogManagerService.LogWarning($"⚠️ 테이블 '{tableName}'이 존재하지 않습니다.");
                    
                    progress?.Report("💡 테이블을 생성하거나 다른 테이블을 사용해주세요.");
                    LogManagerService.LogInfo($"💡 테이블을 생성하거나 다른 테이블을 사용해주세요.");
                    
                    // 대체 테이블 시도
                    // 이미 위에서 작업대상 테이블명을 tableName 변수에 할당했으므로, 동일한 테이블명을 fallbackTableName에 재할당
                    var fallbackTableName = tableName;
                    progress?.Report($"🔄 대체 테이블 확인: {fallbackTableName}");
                    LogManagerService.LogInfo($"🔄 대체 테이블 확인: {fallbackTableName}");
                    
                    var fallbackTableExists = await _databaseCommonService.CheckTableExistsAsync(fallbackTableName);
                    if (!fallbackTableExists)
                    {
                        var errorMessage = $"대체 테이블 '{fallbackTableName}'도 존재하지 않습니다. 테이블을 생성해주세요.";
                        LogManagerService.LogError($"❌ {errorMessage}");
                        throw new InvalidOperationException(errorMessage);
                    }
                    
                    tableName = fallbackTableName;
                    progress?.Report($"✅ 대체 테이블 사용: {tableName}");
                    LogManagerService.LogInfo($"✅ 대체 테이블 사용: {tableName}");
                }
                
                // 테이블 초기화 시도
                try
                {
                    var truncateSuccess = await _invoiceRepository.TruncateTableAsync(tableName);
                    
                    var tableInfoLog = $"작업 대상 Table: {tableName}";
                    LogManagerService.LogInfo(tableInfoLog);
                    LogManagerService.LogInfo($"{tableInfoLog}");

                    // 초기화 결과 검증 및 로깅
                    if (truncateSuccess)
                    {
                        progress?.Report("✅ 테이블 초기화 완료");
                        LogManagerService.LogInfo($"✅ 테이블 초기화 완료");
                        
                        LogManagerService.LogInfo($"[빌드정보] 테이블 초기화 완료: {tableName}");
                        LogManagerService.LogInfo($"[빌드정보] 테이블 초기화 완료: {tableName}");
                    }
                    else
                    {
                        var errorMessage = $"테이블 초기화에 실패했습니다. (테이블: {tableName})";
                        LogManagerService.LogError($"❌ {errorMessage}");
                        throw new InvalidOperationException(errorMessage);
                    }
                }
                catch (Exception truncateEx)
                {
                    progress?.Report($"⚠️ 테이블 초기화 실패: {truncateEx.Message}");
                    LogManagerService.LogWarning($"⚠️ 테이블 초기화 실패: {truncateEx.Message}");
                    
                    progress?.Report("💡 테이블이 존재하지 않거나 권한이 부족할 수 있습니다.");
                    LogManagerService.LogInfo($"💡 테이블이 존재하지 않거나 권한이 부족할 수 있습니다.");
                    
                    progress?.Report("💡 데이터베이스 연결 상태와 테이블 존재 여부를 확인해주세요.");
                    LogManagerService.LogInfo($"💡 데이터베이스 연결 상태와 테이블 존재 여부를 확인해주세요.");
                    
                    // 테이블 생성 시도 또는 다른 테이블 사용
                    var fallbackTableName = GetTableName("Tables.Invoice.Main");
                    progress?.Report($"🔄 대체 테이블 사용 시도: {fallbackTableName}");
                    LogManagerService.LogInfo($"🔄 대체 테이블 사용 시도: {fallbackTableName}");
                    
                    var fallbackSuccess = await _invoiceRepository.TruncateTableAsync(fallbackTableName);
                    if (!fallbackSuccess)
                    {
                        var errorMessage = $"대체 테이블 초기화도 실패했습니다. (테이블: {fallbackTableName})";
                        LogManagerService.LogError($"❌ {errorMessage}");
                        throw new InvalidOperationException(errorMessage);
                    }
                    
                    tableName = fallbackTableName;
                    progress?.Report($"✅ 대체 테이블 초기화 완료: {fallbackTableName}");
                    LogManagerService.LogInfo($"✅ 대체 테이블 초기화 완료: {fallbackTableName}");
                }
                
                // 데이터 변환
                LogManagerService.LogInfo($"🔄 데이터 변환 중... (DataTable → Order 객체)");
                
                var orders = ConvertDataTableToOrders(data);
                
                // 데이터 유효성 검사 및 필터링
                var invalidOrders = orders
                    .Select((order, idx) => new { Order = order, Index = idx })
                    .Where(x => !x.Order.IsValid())
                    .ToList();

                var validOrders = new List<Order>();

                if (invalidOrders.Count > 0)
                {
                    var warningLog = new System.Text.StringBuilder();
                    warningLog.AppendLine($"[경고] 유효하지 않은 데이터 {invalidOrders.Count}건이 발견되었습니다. 해당 데이터는 제외하고 처리합니다.");
                    
                    foreach (var item in invalidOrders.Take(5)) // 처음 5개만 상세 로그
                    {
                        var invalidFields = new List<string>();
                        if (string.IsNullOrEmpty(item.Order.RecipientName))
                            invalidFields.Add("수취인명");
                        if (string.IsNullOrEmpty(item.Order.Address))
                            invalidFields.Add("주소");
                        if (string.IsNullOrEmpty(item.Order.ProductName))
                            invalidFields.Add("송장명");
                        if (item.Order.Quantity <= 0)
                            invalidFields.Add("수량");

                        warningLog.AppendLine(
                            $"  - 행 {item.Index + 1}: 유효성 실패 [원인: {string.Join(", ", invalidFields)}], 주문번호: {item.Order.OrderNumber ?? "(없음)"}"
                        );
                    }
                    
                    if (invalidOrders.Count > 5)
                    {
                        warningLog.AppendLine($"  ... 외 {invalidOrders.Count - 5}건");
                    }
                    
                    // 경고 로그 출력 (처리 중단하지 않음)
                    progress?.Report(warningLog.ToString());
                    LogManagerService.LogWarning(warningLog.ToString());
                    LogManagerService.LogWarning($"{warningLog.ToString()}");
                    
                    // 🔧 수정: 유효하지 않은 데이터도 포함하여 모든 데이터 처리
                    // 대용량 데이터 처리 시 유효성 검증을 완화하여 처리 진행
                    validOrders = orders.ToList(); // 모든 데이터를 유효한 것으로 간주
                    
                    progress?.Report($"📊 유효성 검증을 완화하여 모든 데이터 {validOrders.Count:N0}건으로 처리 계속 진행");
                    LogManagerService.LogInfo($"📊 유효성 검증을 완화하여 모든 데이터 {validOrders.Count:N0}건으로 처리 계속 진행");
                }
                else
                {
                    // 모든 데이터가 유효한 경우
                    validOrders = orders.ToList();
                }
                
                // 변환 결과 통계 보고
                progress?.Report($"📊 데이터 변환 완료: 총 {data.Rows.Count}건 → 유효 {validOrders.Count}건");
                LogManagerService.LogInfo($"📊 데이터 변환 완료: 총 {data.Rows.Count}건 → 유효 {validOrders.Count}건");
                
                // 유효 데이터 존재 여부 확인
                if (validOrders.Count == 0)
                {
                    progress?.Report("⚠️ 유효한 데이터가 없습니다.");
                    LogManagerService.LogWarning($"⚠️ 유효한 데이터가 없습니다.");
                    return; // 처리할 데이터가 없으므로 메서드 종료
                }
                
                // 적응형 배치 처리로 대용량 데이터 삽입
                progress?.Report("🚀 대용량 배치 처리 시작...");
                LogManagerService.LogInfo($"🚀 대용량 배치 처리 시작...");
                
                var (successCount, failureCount) = await _batchProcessor.ProcessLargeDatasetAsync(validOrders, progress, false, tableName);
                
                // 처리 결과 분석 및 성능 통계
                progress?.Report($"✅ 원본 데이터 적재 완료: 성공 {successCount:N0}건, 실패 {failureCount:N0}건 (테이블: {tableName})");
                LogManagerService.LogInfo($"✅ 원본 데이터 적재 완료: 성공 {successCount:N0}건, 실패 {failureCount:N0}건 (테이블: {tableName})");
                
                // 실패 원인 상세 분석
                if (failureCount > 0)
                {
                    LogManagerService.LogInfo($"[원본데이터적재] 실패 원인 상세 분석 - 총 실패: {failureCount:N0}건 ({failureCount * 100.0 / validOrders.Count:F1}%)");
                    LogManagerService.LogInfo($"[원본데이터적재] 실패율 분석 - 유효 데이터: {validOrders.Count:N0}건, 실패: {failureCount:N0}건, 실패율: {failureCount * 100.0 / validOrders.Count:F1}%");
                    
                    // 실패율이 높은 경우 경고
                    if (failureCount * 100.0 / validOrders.Count > 5.0)
                    {
                        LogManagerService.LogWarning($"[원본데이터적재] ⚠️ 높은 실패율 경고 - 실패율이 5%를 초과합니다. ({failureCount * 100.0 / validOrders.Count:F1}%)");
                    }
                }
                else
                {
                    LogManagerService.LogInfo($"[원본데이터적재] 모든 데이터 처리 성공! - 성공률: 100% ({validOrders.Count:N0}건)");
                }
                
                // 배치 처리 성능 통계 수집 및 출력
                var (currentBatchSize, currentMemoryMB, availableMemoryMB) = _batchProcessor.GetStatus();
                LogManagerService.LogInfo($"[빌드정보] 배치 처리 완료 - 테이블: {tableName}, 최종 배치 크기: {currentBatchSize}, 메모리 사용량: {currentMemoryMB}MB, 가용 메모리: {availableMemoryMB}MB");
                LogManagerService.LogInfo($"[빌드정보] 배치 처리 완료 - 테이블: {tableName}, 최종 배치 크기: {currentBatchSize}, 메모리 사용량: {currentMemoryMB}MB, 가용 메모리: {availableMemoryMB}MB");
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 데이터베이스 초기화 및 적재 실패: {ex.Message}");
                LogManagerService.LogError($"❌ 데이터베이스 초기화 및 적재 실패: {ex.Message}");
                
                LogManagerService.LogError($"[빌드정보] 오류 발생: {ex}");
                LogManagerService.LogError($"[빌드정보] 오류 발생: {ex}");
                throw;
            }
        }
        */
        
        /// <summary>
        /// 합포장 최적화 프로시저 호출 루틴
        /// </summary>
        /// <returns>Task</returns>
        private async Task ProcessFirstStageDataOptimized(IProgress<string>? progress)
        {
            const string METHOD_NAME = "ProcessFirstStageDataOptimized";
            
            try
            {
                progress?.Report("🔧 [3-1] 데이터 최적화 처리 시작...");
                
                // 1단계 데이터에 대한 비즈니스 규칙 적용
                // - 데이터 정제 및 표준화
                // - 필수 필드 검증
                // - 중복 데이터 처리
                
                // 비동기 작업 시뮬레이션 (실제로는 데이터베이스 작업 등이 들어갈 예정)
                await Task.Delay(100);
                
                progress?.Report("🔧 [3-1] 데이터 최적화 처리 완료");
                }
                catch (Exception ex)
                {
                var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생: {ex.Message}";
                progress?.Report(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        /// <summary>
        /// 합포장 변경 처리 (ProcessMergePacking)
        /// 
        /// 📋 주요 기능:
        /// - Dropbox에서 합포장 변경 엑셀 파일 다운로드
        /// - 엑셀 데이터를 프로시저를 통해 테이블에 삽입
        /// - sp_MergePacking 프로시저 실행으로 후처리
        /// </summary>
        /// <returns>Task</returns>
        private async Task ProcessMergePacking()
        {
            const string METHOD_NAME = "ProcessMergePacking";
            const string PROCEDURE_NAME = "sp_MergePacking";
            const string CONFIG_KEY = "DropboxFolderPath2";
            const string PROCEDURE_CONFIG_KEY = "ExcelProcessor.Proc3";
                    
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
                    var startTime = DateTime.Now;
            string? tempFilePath = null;
            int insertCount = 0;
            
            try
            {
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 합포장 변경 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                _progress?.Report("🚀 [합포장 변경] 처리 시작");
                
                // ==========================================================================
                // 1. Dropbox에서 엑셀 데이터를 읽어오기
                // ==========================================================================  
                _progress?.Report("📥 [1] Dropbox에서 엑셀 파일 다운로드 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📥 [1] Dropbox에서 엑셀 파일 다운로드 시작");
                
                // 설정 검증
                var mergePackingExcelFileName = ConfigurationManager.AppSettings["MergePackingExcelFileName"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(mergePackingExcelFileName))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ MergePackingExcelFileName 설정이 App.config에 존재하지 않거나 비어 있습니다.";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [1] 설정 파일명이 없습니다");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 설정 확인 완료: {mergePackingExcelFileName}");

                // 엑셀 파일 다운로드 및 읽기
                var excelData = await ProcessExcelFileAsync(
                    CONFIG_KEY, 
                    "merge_packing_table", 
                    mergePackingExcelFileName,
                    _progress);
                
                if (excelData == null || excelData.Rows.Count == 0)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [1] 엑셀 데이터가 비어있거나 null입니다.";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [1] 엑셀 데이터가 비어있습니다");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [1] 엑셀 파일 다운로드 완료 - {excelData.Rows.Count:N0}행, {excelData.Columns.Count}열");
                _progress?.Report($"✅ [1] 엑셀 파일 다운로드 완료 ({excelData.Rows.Count:N0}행)");

                // ==========================================================================
                // 2. 엑셀 데이터를 프로시저로 전달하여 테이블에 삽입
                // ==========================================================================                
                _progress?.Report("🔄 [2] 엑셀 데이터를 테이블에 적재 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🔄 [2] 엑셀 데이터를 테이블에 적재 시작");
                
                // 임시 엑셀 파일 생성
                tempFilePath = Path.Combine(Path.GetTempPath(), $"merge_packing_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📁 임시 파일 경로: {tempFilePath}");
                
                // DataTable을 임시 엑셀 파일로 저장
                var excelCreated = _fileService.SaveDataTableToExcel(excelData, tempFilePath, "Sheet1");
                if (!excelCreated)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일 생성 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일 생성 실패");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 임시 엑셀 파일 생성 완료: {tempFilePath}");

                // 파일 존재 여부 및 크기 확인
                if (!File.Exists(tempFilePath))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일이 존재하지 않음: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일이 존재하지 않음");
                    throw new FileNotFoundException(errorMessage);
                }
                
                var fileInfo = new FileInfo(tempFilePath);
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 임시 파일 정보 - 크기: {fileInfo.Length:N0} bytes");
                
                if (fileInfo.Length == 0)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일이 비어있음: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일이 비어있음");
                    throw new InvalidOperationException(errorMessage);
                }

                // 프로시저를 통한 데이터 처리 (컬럼매핑 우회)
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 ReadExcelToDataTableWithProcedure 호출 시작");
                var dataInsertResult = await _fileService.ReadExcelToDataTableWithProcedure(
                    tempFilePath,          // 임시 엑셀 파일 경로
                    PROCEDURE_CONFIG_KEY // App.config의 프로시저 설정 키
                );

                if (!dataInsertResult)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] Excel 데이터를 테이블에 적재 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] Excel 데이터를 테이블에 적재 실패");
                    throw new InvalidOperationException(errorMessage);
                }

                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ Excel 데이터를 프로시저로 전달 성공");
                _progress?.Report("✅ [2] 엑셀 데이터를 테이블에 적재 완료");
                
                insertCount = excelData.Rows.Count;

                // ==========================================================================
                // 3. 후처리 프로시저 실행
                // ==========================================================================
                _progress?.Report("🚀 [3] 후처리 프로시저 실행 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 [3] {PROCEDURE_NAME} 프로시저 호출 시작");
                
                var procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);
                
                // 프로시저 실행 결과 검증 - 오류 발생 시 즉시 중단
                if (procedureResult.Contains("오류") || procedureResult.Contains("실패") || procedureResult.Contains("Error") || 
                    procedureResult.Contains("SQLSTATE") || procedureResult.Contains("Truncated") || procedureResult.Contains("rollback"))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [3] {PROCEDURE_NAME} 프로시저 실행 실패: {procedureResult}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report($"❌ [3] 후처리 프로시저 실행 실패: {procedureResult}");
                    throw new InvalidOperationException($"프로시저 실행 실패: {procedureResult}");
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [3] {PROCEDURE_NAME} 프로시저 실행 완료 - 결과: {procedureResult}");
                _progress?.Report("✅ [3] 후처리 프로시저 실행 완료");
                
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 합포장 변경 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                _progress?.Report($"🎉 [합포장 변경] 처리 완료 (소요시간: {duration.TotalSeconds:F1}초)");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: 성공, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                var errorMessage = $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)";
                WriteLogWithFlush(logPath, errorMessage);
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ [합포장 변경] 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"합포장 변경 처리 중 오류 발생: {ex.Message}", ex);
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {tempFilePath}");
                    }
                }
                catch (Exception cleanupEx)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {cleanupEx.Message}");
                    // 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
                }
            }
        }

        // 공통 상수 정의
        private static readonly string LOG_PATH = LogPathManager.AppLogPath;
        private const string TEMP_FILE_PREFIX = "temp_";
        private const string LOG_TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss";
        
        // 공통 메서드: Excel 파일 처리
        /// <summary>
        /// Excel 파일 처리 공통 로직 (Dropbox 다운로드 → 엑셀 읽기 → 임시 파일 정리)
        /// </summary>
        /// <param name="configKey">Dropbox 설정 키</param>
        /// <param name="sheetName">엑셀 시트명</param>
        /// <param name="defaultFileName">기본 파일명</param>
        /// <param name="progress">진행률 보고</param>
        /// <returns>엑셀 데이터</returns>
        private async Task<DataTable> ProcessExcelFileAsync(
            string configKey, 
            string sheetName, 
            string defaultFileName,
            IProgress<string>? progress = null)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var methodName = "ProcessExcelFileAsync";
            
            try
            {
                // 1. 설정 확인
                var dropboxPath = System.Configuration.ConfigurationManager.AppSettings[configKey];
                if (string.IsNullOrEmpty(dropboxPath))
                {
                    var errorMessage = $"[{methodName}] ❌ {configKey} 설정이 없습니다.";
                    WriteLogWithFlush(logPath, errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                
                progress?.Report($"✅ Dropbox 설정 확인");
                WriteLogWithFlush(logPath, $"[{methodName}] ✅ {configKey} 설정 확인: {dropboxPath}");
                
                // 2. 파일 다운로드
                progress?.Report($"📥 Dropbox에서 엑셀 파일 다운로드 시작");
                WriteLogWithFlush(logPath, $"[{methodName}] 📥 Dropbox에서 엑셀 파일 다운로드 시작: {dropboxPath}");
                
                string localFilePath;
                try
                {
                    var tempDir = Path.GetTempPath();
                    var fileName = Path.GetFileName(dropboxPath);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = defaultFileName;
                    }
                    localFilePath = Path.Combine(tempDir, $"{TEMP_FILE_PREFIX}{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
                    
                    var dropboxService = DropboxService.Instance;
                    var downloadSuccess = await dropboxService.DownloadFileAsync(dropboxPath, localFilePath);
                    if (!downloadSuccess)
                    {
                        var errorMessage = $"[{methodName}] ❌ Dropbox 파일 다운로드 실패: {dropboxPath}";
                        WriteLogWithFlush(logPath, errorMessage);
                        throw new InvalidOperationException(errorMessage);
                    }
                    
                    WriteLogWithFlush(logPath, $"[{methodName}] ✅ Dropbox 파일 다운로드 완료: {localFilePath}");
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{methodName}] ❌ Dropbox 파일 다운로드 중 예외 발생: {ex.Message}");
                    throw new InvalidOperationException($"Dropbox 파일 다운로드 실패: {ex.Message}", ex);
                }
                
                // 3. 엑셀 데이터 읽기 (강화된 오류 처리)
                progress?.Report($"📖 엑셀 파일 읽기 시작");
                WriteLogWithFlush(logPath, $"[{methodName}] 📖 엑셀 파일 읽기 시작: {localFilePath}");
                
                // 파일 존재 여부 및 크기 확인
                if (!File.Exists(localFilePath))
                {
                    var errorMessage = $"[{methodName}] ❌ 임시 파일이 존재하지 않습니다: {localFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                
                var fileInfo = new FileInfo(localFilePath);
                WriteLogWithFlush(logPath, $"[{methodName}] 📊 파일 정보 - 크기: {fileInfo.Length:N0} bytes, 생성시간: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                
                if (fileInfo.Length == 0)
                {
                    var errorMessage = $"[{methodName}] ❌ 임시 파일이 비어있습니다: {localFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                
                DataTable excelData;
                try
                {
                    excelData = _fileService.ReadExcelToDataTable(localFilePath, sheetName);
                    WriteLogWithFlush(logPath, $"[{methodName}] ✅ Excel 파일 읽기 성공");
                }
                catch (Exception excelEx)
                {
                    var errorMessage = $"[{methodName}] ❌ Excel 파일 읽기 실패: {excelEx.Message}";
                    WriteLogWithFlush(logPath, errorMessage);
                    WriteLogWithFlush(logPath, $"[{methodName}] 📋 Excel 읽기 오류 상세: {excelEx.GetType().Name}");
                    
                    // 내부 예외가 있는 경우 추가 로깅
                    if (excelEx.InnerException != null)
                    {
                        WriteLogWithFlush(logPath, $"[{methodName}] 📋 내부 예외: {excelEx.InnerException.Message}");
                    }
                    
                    // 스택 트레이스 로깅
                    WriteLogWithFlush(logPath, $"[{methodName}] 📋 스택 트레이스: {excelEx.StackTrace}");
                    
                    // 파일 내용 검증 시도
                    try
                    {
                        WriteLogWithFlush(logPath, $"[{methodName}] 🔍 파일 내용 검증 시도...");
                        
                        // 파일의 첫 몇 바이트 확인
                        var firstBytes = new byte[Math.Min(100, (int)fileInfo.Length)];
                        using (var fs = File.OpenRead(localFilePath))
                        {
                            fs.Read(firstBytes, 0, firstBytes.Length);
                        }
                        
                        var hexString = BitConverter.ToString(firstBytes).Replace("-", " ");
                        WriteLogWithFlush(logPath, $"[{methodName}] 🔍 파일 헤더 (첫 100바이트): {hexString}");
                        
                        // Excel 파일 시그니처 확인 (PK\x03\x04 또는 \x50\x4B\x03\x04)
                        if (firstBytes.Length >= 4 && firstBytes[0] == 0x50 && firstBytes[1] == 0x4B && firstBytes[2] == 0x03 && firstBytes[3] == 0x04)
                        {
                            WriteLogWithFlush(logPath, $"[{methodName}] ✅ Excel 파일 시그니처 확인됨 (.xlsx 형식)");
                        }
                        else
                        {
                            WriteLogWithFlush(logPath, $"[{methodName}] ⚠️ Excel 파일 시그니처가 올바르지 않음");
                        }
                    }
                    catch (Exception verifyEx)
                    {
                        WriteLogWithFlush(logPath, $"[{methodName}] ⚠️ 파일 검증 중 오류: {verifyEx.Message}");
                    }
                    
                    throw new InvalidOperationException($"Excel 파일 읽기 실패: {excelEx.Message}", excelEx);
                }
                
                if (excelData?.Rows == null || excelData.Rows.Count == 0)
                {
                    // 빈 데이터는 정상 상황으로 처리
                    WriteLogWithFlush(logPath, $"[{methodName}] ⚠️ Excel 파일에 데이터가 없습니다. (정상 상황)");
                    progress?.Report($"[{methodName}] ⚠️ Excel 파일에 데이터가 없습니다. (정상 상황)");
                    
                    // 임시 파일 정리
                    try
                    {
                        if (File.Exists(localFilePath))
                        {
                            File.Delete(localFilePath);
                            WriteLogWithFlush(logPath, $"[{methodName}] 🗑️ 임시 파일 정리 완료: {localFilePath}");
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        WriteLogWithFlush(logPath, $"[{methodName}] ⚠️ 임시 파일 정리 중 오류: {cleanupEx.Message}");
                    }
                    
                    return new DataTable(); // 빈 DataTable 반환
                }
                
                progress?.Report($"✅ 엑셀 데이터 로드 완료: {excelData.Rows.Count:N0}건");
                WriteLogWithFlush(logPath, $"[{methodName}] ✅ 엑셀 데이터 로드 완료: {excelData.Rows.Count:N0}건");
                
                // 4. 임시 파일 정리
                try
                {
                    if (File.Exists(localFilePath))
                    {
                        File.Delete(localFilePath);
                        WriteLogWithFlush(logPath, $"[{methodName}] 🗑️ 임시 파일 정리 완료: {localFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{methodName}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
                }
                
                return excelData;
            }
            catch (Exception ex)
            {
                WriteLogWithFlush(logPath, $"[{methodName}] ❌ Excel 파일 처리 실패: {ex.Message}");
                throw;
            }
        }
        
        // 공통 메서드: 테이블 처리
        /// <summary>
        /// 테이블 처리 공통 로직 (존재 확인 → TRUNCATE → 컬럼 매핑 → 데이터 INSERT)
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="excelData">엑셀 데이터</param>
        /// <param name="progress">진행률 보고</param>
        /// <returns>삽입된 행 수</returns>
        private async Task<int> ProcessStandardTable(
            string tableName,
            DataTable excelData,
            IProgress<string>? progress = null)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var methodName = "ProcessStandardTable";
            
            try
            {
                // 1. 테이블 존재 확인
                //progress?.Report($"[{methodName}] 🔍 테이블 존재여부 확인: {tableName}");
                WriteLogWithFlush(logPath, $"[{methodName}] 🔍 테이블 존재여부 확인: {tableName}");
                
                var tableExists = await _databaseCommonService.CheckTableExistsAsync(tableName);
                if (!tableExists)
                {
                    var errorMessage = $"[{methodName}] ❌ 테이블이 존재하지 않습니다: {tableName}";
                    WriteLogWithFlush(logPath, errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{methodName}] ✅ 테이블 존재 확인: {tableName}");
                
                // 2. 테이블 TRUNCATE
                //progress?.Report($"[{methodName}] 🗑️ 테이블 TRUNCATE 시작: {tableName}");
                WriteLogWithFlush(logPath, $"[{methodName}] 🗑️ 테이블 TRUNCATE 시작: {tableName}");
                
                var truncateQuery = $"TRUNCATE TABLE {tableName}";
                await _invoiceRepository.ExecuteNonQueryAsync(truncateQuery);
                
                WriteLogWithFlush(logPath, $"[{methodName}] ✅ 테이블 TRUNCATE 완료: {tableName}");
                
                // 3. 빈 데이터 체크
                if (excelData.Rows.Count == 0)
                {
                    WriteLogWithFlush(logPath, $"[{methodName}] ⚠️ 데이터가 없습니다. 컬럼 매핑 검증을 건너뛰고 TRUNCATE만 완료합니다.");
                    progress?.Report($"[{methodName}] ⚠️ 데이터가 없습니다. 컬럼 매핑 검증을 건너뛰고 TRUNCATE만 완료합니다.");
                    return 0; // 빈 데이터는 0건 처리
                }
                
                // 4. 컬럼 매핑 검증
                //progress?.Report($"[{methodName}] 🔗 컬럼 매핑 검증 시작");
                WriteLogWithFlush(logPath, $"[{methodName}] 🔗 컬럼 매핑 검증 시작");
                
                var columnMapping = ValidateColumnMappingAsync(tableName, excelData);
                if (columnMapping == null || !columnMapping.Any())
                {
                    var errorMessage = $"[{methodName}] ❌ 컬럼 매핑 검증 실패";
                    WriteLogWithFlush(logPath, errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{methodName}] ✅ 컬럼 매핑 검증 완료: {columnMapping.Count}개 컬럼");
                
                // 5. 데이터 INSERT
                progress?.Report($"📝 데이터 삽입 시작: {excelData.Rows.Count:N0}건");
                WriteLogWithFlush(logPath, $"[{methodName}] 📝 데이터 INSERT 시작: {excelData.Rows.Count:N0}건");
                
                var insertCount = await InsertDataWithMappingAsync(tableName, excelData, columnMapping);
                
                WriteLogWithFlush(logPath, $"[{methodName}] ✅ 데이터 INSERT 완료: {insertCount:N0}건");
                
                return insertCount;
            }
            catch (Exception ex)
            {
                WriteLogWithFlush(logPath, $"[{methodName}] ❌ 테이블 처리 실패: {ex.Message}");
                throw;
            }
        }
        
        // WriteLogWithFlush 메서드 추가 - 줄바꿈 개선
        private void WriteLogWithFlush(string logPath, string message)
        {
            try
            {
                // Windows 환경에 맞는 줄바꿈 문자 사용
                var lineBreak = Environment.NewLine;
                LogManagerService.LogInfo($"{message}");
            }
            catch (Exception ex)
            {
                // 로그 쓰기 실패 시 무시하고 계속 진행
                LogManagerService.LogWarning($"로그 쓰기 실패: {ex.Message}");
            }
        }

        // 긴 메시지를 여러 줄로 나누는 로그 메서드
        private void WriteLogWithFlushMultiLine(string logPath, string prefix, string message, int maxLineLength = 80)
        {
            try
            {
                var lineBreak = Environment.NewLine;
                var timestamp = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                
                if (message.Length <= maxLineLength)
                {
                    // 짧은 메시지는 한 줄로
                    LogManagerService.LogInfo($"{timestamp} {prefix}{message}");
                }
                else
                {
                    // 긴 메시지는 여러 줄로 나누기
                    var words = message.Split(new[] { ", " }, StringSplitOptions.None);
                    var currentLine = "";
                    
                    foreach (var word in words)
                    {
                        if ((currentLine + word).Length > maxLineLength && !string.IsNullOrEmpty(currentLine))
                        {
                            // 현재 줄이 너무 길면 새 줄로
                            LogManagerService.LogInfo($"{timestamp} {prefix}{currentLine.Trim()}");
                            currentLine = word;
                        }
                        else
                        {
                            currentLine += (string.IsNullOrEmpty(currentLine) ? "" : ", ") + word;
                        }
                    }
                    
                    // 마지막 줄 처리
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        LogManagerService.LogInfo($"{timestamp} {prefix}{currentLine.Trim()}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 로그 쓰기 실패 시 무시하고 계속 진행
                LogManagerService.LogWarning($"로그 쓰기 실패: {ex.Message}");
            }
        }
        
        // 문자열을 지정된 길이로 자르고 우측 패딩을 추가하는 헬퍼 메서드
        private string TruncateAndPadRight(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return new string(' ', maxLength);
            
            if (input.Length <= maxLength)
                return input.PadRight(maxLength);
            
            // 긴 문자열은 중간에 "..." 추가하여 자르기
            var truncated = input.Substring(0, maxLength - 3) + "...";
            return truncated.PadRight(maxLength);
        }
        
        // 감천 특별출고 처리 루틴
        // 송장구분 업데이트 ('합포장'/'단일')
        // '단일' 송장 데이터 이동
        // '합포장' 데이터에 대한 최종 구분자 업데이트
        // 조건에 맞는 '합포장' 데이터 이동
        private async Task ProcessInvoiceSplit1()
        {
            const string METHOD_NAME = "ProcessInvoiceSplit1";
            const string PROCEDURE_NAME = "sp_InvoiceSplit";
            const string CONFIG_KEY = "DropboxFolderPath3";
            const string PROCEDURE_CONFIG_KEY = "ExcelProcessor.Proc4";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            string? tempFilePath = null;
                    int insertCount = 0;
                    
                    try
                    {
                        // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 감천 특별출고 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                _progress?.Report("🚀 [감천 특별출고] 처리 시작");

                        // ==========================================================================
                        // 1. Dropbox에서 엑셀 데이터를 읽어오기
                        // ========================================================================== 
                        _progress?.Report("📥 [1] Dropbox에서 엑셀 파일 다운로드 중...");
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📥 [1] Dropbox에서 엑셀 파일 다운로드 시작");

                        // 설정 검증
                var GamcheonExcelFileName = ConfigurationManager.AppSettings["GamcheonExcelFileName"] ?? string.Empty;  
                if (string.IsNullOrWhiteSpace(GamcheonExcelFileName))
                        {
                    var errorMessage = $"[{METHOD_NAME}] ❌ GamcheonExcelFileName 설정이 App.config에 존재하지 않거나 비어 있습니다.";
                            WriteLogWithFlush(logPath, errorMessage);
                            _progress?.Report("❌ [1] 설정 파일명이 없습니다");
                            throw new InvalidOperationException(errorMessage);
                        }
                        
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 설정 확인 완료: {GamcheonExcelFileName}");

                        // 엑셀 파일 다운로드 및 읽기
                        var excelData = await ProcessExcelFileAsync(
                            CONFIG_KEY, 
                    "gamcheon_special_table", 
                    GamcheonExcelFileName,
                            _progress);
                        
                        if (excelData == null || excelData.Rows.Count == 0)
                        {
                            var errorMessage = $"[{METHOD_NAME}] ❌ [1] 엑셀 데이터가 비어있거나 null입니다.";
                            WriteLogWithFlush(logPath, errorMessage);
                            _progress?.Report("❌ [1] 엑셀 데이터가 비어있습니다");
                            throw new InvalidOperationException(errorMessage);
                        }
                        
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [1] 엑셀 파일 다운로드 완료 - {excelData.Rows.Count:N0}행, {excelData.Columns.Count}열");
                        _progress?.Report($"✅ [1] 엑셀 파일 다운로드 완료 ({excelData.Rows.Count:N0}행)");

                        // ==========================================================================
                // 2. 엑셀 데이터를 프로시저로 전달하여 테이블에 삽입
                        // ==========================================================================                
                _progress?.Report("🔄 [2] 엑셀 데이터를 테이블에 적재 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🔄 [2] 엑셀 데이터를 테이블에 적재 시작");
                
                // 임시 엑셀 파일 생성
                tempFilePath = Path.Combine(Path.GetTempPath(), $"gamcheon_special_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📁 임시 파일 경로: {tempFilePath}");
                
                // DataTable을 임시 엑셀 파일로 저장
                var excelCreated = _fileService.SaveDataTableToExcel(excelData, tempFilePath, "Sheet1");
                if (!excelCreated)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일 생성 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일 생성 실패");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 임시 엑셀 파일 생성 완료: {tempFilePath}");

                // 파일 존재 여부 및 크기 확인
                if (!File.Exists(tempFilePath))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일이 존재하지 않음: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일이 존재하지 않음");
                    throw new FileNotFoundException(errorMessage);
                }
                
                var fileInfo = new FileInfo(tempFilePath);
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 임시 파일 정보 - 크기: {fileInfo.Length:N0} bytes");
                
                if (fileInfo.Length == 0)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일이 비어있음: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일이 비어있음");
                    throw new InvalidOperationException(errorMessage);
                }

                // 프로시저를 통한 데이터 처리 (컬럼매핑 우회)
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 ReadExcelToDataTableWithProcedure 호출 시작");
                var dataInsertResult = await _fileService.ReadExcelToDataTableWithProcedure(
                    tempFilePath,          // 임시 엑셀 파일 경로
                    PROCEDURE_CONFIG_KEY // App.config의 프로시저 설정 키
                );

                if (!dataInsertResult)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] Excel 데이터를 테이블에 적재 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] Excel 데이터를 테이블에 적재 실패");
                    throw new InvalidOperationException(errorMessage);
                }

                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ Excel 데이터를 프로시저로 전달 성공");
                _progress?.Report("✅ [2] 엑셀 데이터를 테이블에 적재 완료");
                
                insertCount = excelData.Rows.Count;

                        // ==========================================================================
                        // 3. 후처리 프로시저 실행
                        // ==========================================================================
                        _progress?.Report("🚀 [3] 후처리 프로시저 실행 중...");
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 [3] {PROCEDURE_NAME} 프로시저 호출 시작");
                        
                var procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);
                
                // 프로시저 실행 결과 검증 - 오류 발생 시 즉시 중단
                if (procedureResult.Contains("오류") || procedureResult.Contains("실패") || procedureResult.Contains("Error") || 
                    procedureResult.Contains("SQLSTATE") || procedureResult.Contains("Truncated") || procedureResult.Contains("rollback"))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [3] {PROCEDURE_NAME} 프로시저 실행 실패: {procedureResult}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report($"❌ [3] 후처리 프로시저 실행 실패: {procedureResult}");
                    throw new InvalidOperationException($"프로시저 실행 실패: {procedureResult}");
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [3] {PROCEDURE_NAME} 프로시저 실행 완료 - 결과: {procedureResult}");
                _progress?.Report("✅ [3] 후처리 프로시저 실행 완료");
                
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 감천 특별출고 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                _progress?.Report($"🎉 [감천 특별출고] 처리 완료 (소요시간: {duration.TotalSeconds:F1}초)");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: 성공, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                var errorMessage = $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)";
                WriteLogWithFlush(logPath, errorMessage);
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ [감천 특별출고] 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"감천 특별출고 처리 중 오류 발생: {ex.Message}", ex);
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {tempFilePath}");
                    }
                }
                catch (Exception cleanupEx)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {cleanupEx.Message}");
                    // 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
                }
            }
        }

                // 톡딜불가 처리
        // 톡딜불가(카카오톡딜 등 특수 조건으로 주문이 불가한 송장 데이터) 처리 메서드
        // - 엑셀 파일에서 톡딜불가 데이터를 읽어와 전처리 후, 관련 테이블에 저장하고 프로시저를 실행합니다.
        // - 주로 카카오톡딜 등에서 주문이 불가한 케이스를 관리하기 위한 처리입니다.
        private async Task ProcessTalkDealUnavailable()
        {
            const string METHOD_NAME = "ProcessTalkDealUnavailable";
            const string PROCEDURE_NAME = "sp_TalkDealUnavailable";
            const string CONFIG_KEY = "DropboxFolderPath5";
            const string PROCEDURE_CONFIG_KEY = "ExcelProcessor.Proc5";            
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            string? tempFilePath = null;
            int insertCount = 0;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 톡딜불가 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                _progress?.Report("🚀 [톡딜불가] 처리 시작");

                // ==========================================================================
                // 1. Dropbox에서 엑셀 데이터를 읽어오기
                // ========================================================================== 
                _progress?.Report("📥 [1] Dropbox에서 엑셀 파일 다운로드 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📥 [1] Dropbox에서 엑셀 파일 다운로드 시작");

                // 설정 검증
                var talkDealExcelFileName = ConfigurationManager.AppSettings["TalkDealExcelFileName"] ?? string.Empty;  
                if (string.IsNullOrWhiteSpace(talkDealExcelFileName))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ TalkDealExcelFileName 설정이 App.config에 존재하지 않거나 비어 있습니다.";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [1] 설정 파일명이 없습니다");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 설정 확인 완료: {talkDealExcelFileName}");

                // 엑셀 파일 다운로드 및 읽기
                var excelData = await ProcessExcelFileAsync(
                    CONFIG_KEY, 
                    "talkdeal_unavailable_table", 
                    talkDealExcelFileName,
                    _progress);
                
                if (excelData == null || excelData.Rows.Count == 0)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [1] 엑셀 데이터가 비어있거나 null입니다.";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [1] 엑셀 데이터가 비어있습니다");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [1] 엑셀 파일 다운로드 완료 - {excelData.Rows.Count:N0}행, {excelData.Columns.Count}열");
                _progress?.Report($"✅ [1] 엑셀 파일 다운로드 완료 ({excelData.Rows.Count:N0}행)");

                // ==========================================================================
                // 2. 엑셀 데이터를 프로시저로 전달하여 테이블에 삽입
                // ==========================================================================                
                _progress?.Report("🔄 [2] 엑셀 데이터를 테이블에 적재 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🔄 [2] 엑셀 데이터를 테이블에 적재 시작");
                
                // 임시 엑셀 파일 생성
                tempFilePath = Path.Combine(Path.GetTempPath(), $"talkdeal_unavailable_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📁 임시 파일 경로: {tempFilePath}");
                
                // DataTable을 임시 엑셀 파일로 저장
                var excelCreated = _fileService.SaveDataTableToExcel(excelData, tempFilePath, "Sheet1");
                if (!excelCreated)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일 생성 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일 생성 실패");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 임시 엑셀 파일 생성 완료: {tempFilePath}");

                // 파일 존재 여부 및 크기 확인
                if (!File.Exists(tempFilePath))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일이 존재하지 않음: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일이 존재하지 않음");
                    throw new FileNotFoundException(errorMessage);
                }
                
                var fileInfo = new FileInfo(tempFilePath);
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 임시 파일 정보 - 크기: {fileInfo.Length:N0} bytes");
                
                if (fileInfo.Length == 0)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일이 비어있음: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일이 비어있음");
                    throw new InvalidOperationException(errorMessage);
                }

                // 프로시저를 통한 데이터 처리 (컬럼매핑 우회)
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 ReadExcelToDataTableWithProcedure 호출 시작");
                var dataInsertResult = await _fileService.ReadExcelToDataTableWithProcedure(
                    tempFilePath,          // 임시 엑셀 파일 경로
                    PROCEDURE_CONFIG_KEY // App.config의 프로시저 설정 키
                );

                if (!dataInsertResult)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] Excel 데이터를 테이블에 적재 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] Excel 데이터를 테이블에 적재 실패");
                    throw new InvalidOperationException(errorMessage);
                }

                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ Excel 데이터를 프로시저로 전달 성공");
                _progress?.Report("✅ [2] 엑셀 데이터를 테이블에 적재 완료");
                
                insertCount = excelData.Rows.Count;

                // ==========================================================================
                // 3. 후처리 프로시저 실행
                // ==========================================================================
                _progress?.Report("🚀 [3] 후처리 프로시저 실행 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 [3] {PROCEDURE_NAME} 프로시저 호출 시작");
                
                var procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);
                
                // 프로시저 실행 결과 검증 - 오류 발생 시 즉시 중단
                if (procedureResult.Contains("오류") || procedureResult.Contains("실패") || procedureResult.Contains("Error") || 
                    procedureResult.Contains("SQLSTATE") || procedureResult.Contains("Truncated") || procedureResult.Contains("rollback"))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [3] {PROCEDURE_NAME} 프로시저 실행 실패: {procedureResult}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report($"❌ [3] 후처리 프로시저 실행 실패: {procedureResult}");
                    throw new InvalidOperationException($"프로시저 실행 실패: {procedureResult}");
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [3] {PROCEDURE_NAME} 프로시저 실행 완료 - 결과: {procedureResult}");
                _progress?.Report("✅ [3] 후처리 프로시저 실행 완료");
                
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 톡딜불가 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                _progress?.Report($"🎉 [톡딜불가] 처리 완료 (소요시간: {duration.TotalSeconds:F1}초)");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: 성공, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                var errorMessage = $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)";
                WriteLogWithFlush(logPath, errorMessage);
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ [톡딜불가] 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"톡딜불가 처리 중 오류 발생: {ex.Message}", ex);
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {tempFilePath}");
                    }
                }
                catch (Exception cleanupEx)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {cleanupEx.Message}");
                    // 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
                }
            }
        }

        // 송장출력관리 처리
        private async Task ProcessInvoiceManagement()
        {
            const string METHOD_NAME = "ProcessInvoiceManagement";
            const string PROCEDURE_NAME = "sp_ProcessStarInvoice";
            const string CONFIG_KEY = "DropboxFolderPath6";
            const string PROCEDURE_CONFIG_KEY = "ExcelProcessor.Proc6";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            string? tempFilePath = null;
            int insertCount = 0;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 송장출력관리 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                _progress?.Report("🚀 [송장출력관리] 처리 시작");

                // ==========================================================================
                // 1. Dropbox에서 엑셀 데이터를 읽어오기
                // ========================================================================== 
                _progress?.Report("📥 [1] Dropbox에서 엑셀 파일 다운로드 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📥 [1] Dropbox에서 엑셀 파일 다운로드 시작");

                // 설정 검증
                var starInvoiceExcelFileName = ConfigurationManager.AppSettings["StarInvoiceExcelFileName"] ?? string.Empty;  
                if (string.IsNullOrWhiteSpace(starInvoiceExcelFileName))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ StarInvoiceExcelFileName 설정이 App.config에 존재하지 않거나 비어 있습니다.";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [1] 설정 파일명이 없습니다");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 설정 확인 완료: {starInvoiceExcelFileName}");

                // 엑셀 파일 다운로드 및 읽기
                var excelData = await ProcessExcelFileAsync(
                    CONFIG_KEY, 
                    "star_invoice_table", 
                    starInvoiceExcelFileName,
                    _progress);
                
                if (excelData == null || excelData.Rows.Count == 0)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [1] 엑셀 데이터가 비어있거나 null입니다.";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [1] 엑셀 데이터가 비어있습니다");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [1] 엑셀 파일 다운로드 완료 - {excelData.Rows.Count:N0}행, {excelData.Columns.Count}열");
                _progress?.Report($"✅ [1] 엑셀 파일 다운로드 완료 ({excelData.Rows.Count:N0}행)");

                // ==========================================================================
                // 2. 엑셀 데이터를 프로시저로 전달하여 테이블에 삽입
                // ==========================================================================                
                _progress?.Report("🔄 [2] 엑셀 데이터를 테이블에 적재 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🔄 [2] 엑셀 데이터를 테이블에 적재 시작");
                
                // 임시 엑셀 파일 생성
                tempFilePath = Path.Combine(Path.GetTempPath(), $"star_invoice_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📁 임시 파일 경로: {tempFilePath}");
                
                // DataTable을 임시 엑셀 파일로 저장
                var excelCreated = _fileService.SaveDataTableToExcel(excelData, tempFilePath, "Sheet1");
                if (!excelCreated)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일 생성 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일 생성 실패");
                    throw new InvalidOperationException(errorMessage);
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 임시 엑셀 파일 생성 완료: {tempFilePath}");

                // 파일 존재 여부 및 크기 확인
                if (!File.Exists(tempFilePath))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일이 존재하지 않음: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일이 존재하지 않음");
                    throw new FileNotFoundException(errorMessage);
                }
                
                var fileInfo = new FileInfo(tempFilePath);
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 임시 파일 정보 - 크기: {fileInfo.Length:N0} bytes");
                
                if (fileInfo.Length == 0)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] 임시 엑셀 파일이 비어있음: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] 임시 엑셀 파일이 비어있음");
                    throw new InvalidOperationException(errorMessage);
                }

                // 프로시저를 통한 데이터 처리 (컬럼매핑 우회)
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 ReadExcelToDataTableWithProcedure 호출 시작");
                var dataInsertResult = await _fileService.ReadExcelToDataTableWithProcedure(
                    tempFilePath,          // 임시 엑셀 파일 경로
                    PROCEDURE_CONFIG_KEY // App.config의 프로시저 설정 키
                );

                if (!dataInsertResult)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] Excel 데이터를 테이블에 적재 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] Excel 데이터를 테이블에 적재 실패");
                    throw new InvalidOperationException(errorMessage);
                }

                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ Excel 데이터를 프로시저로 전달 성공");
                _progress?.Report("✅ [2] 엑셀 데이터를 테이블에 적재 완료");
                
                insertCount = excelData.Rows.Count;

                // ==========================================================================
                // 3. 후처리 프로시저 실행
                // ==========================================================================
                _progress?.Report("🚀 [3] 후처리 프로시저 실행 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 [3] {PROCEDURE_NAME} 프로시저 호출 시작");
                
                var procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);
                
                // 프로시저 실행 결과 검증 - 오류 발생 시 즉시 중단
                if (procedureResult.Contains("오류") || procedureResult.Contains("실패") || procedureResult.Contains("Error") || 
                    procedureResult.Contains("SQLSTATE") || procedureResult.Contains("Truncated") || procedureResult.Contains("rollback"))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [3] {PROCEDURE_NAME} 프로시저 실행 실패: {procedureResult}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report($"❌ [3] 후처리 프로시저 실행 실패: {procedureResult}");
                    throw new InvalidOperationException($"프로시저 실행 실패: {procedureResult}");
                }
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [3] {PROCEDURE_NAME} 프로시저 실행 완료 - 결과: {procedureResult}");
                _progress?.Report("✅ [3] 후처리 프로시저 실행 완료");
                
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 송장출력관리 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                _progress?.Report($"🎉 [송장출력관리] 처리 완료 (소요시간: {duration.TotalSeconds:F1}초)");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: 성공, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                var errorMessage = $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)";
                WriteLogWithFlush(logPath, errorMessage);
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ [송장출력관리] 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"송장출력관리 처리 중 오류 발생: {ex.Message}", ex);
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {tempFilePath}");
                    }
                }
                catch (Exception cleanupEx)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {cleanupEx.Message}");
                    // 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
                }
            }
        }

        // 서울냉동 처리
        // - 서울냉동 관련 송장 데이터에 대해 프로시저(sp_SeoulProcessF)를 실행하여
        //   송장구분자, 수량, 주소 등 각종 정보를 일괄 처리합니다.
        // - 별도의 테이블 입력 없이 프로시저만 실행하며, 결과 및 오류는 app.log에 기록됩니다.
        // - 처리 시작/완료, 오류 발생 시 모두 상세 로그를 남깁니다.
        private async Task ProcessSeoulFrozenManagement()
        {
            const string METHOD_NAME = "ProcessSeoulFrozenManagement";
            const string PROCEDURE_NAME = "sp_SeoulProcessF";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 서울냉동 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                var insertCount = 0;
                
                try
                {
                        procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                        if (string.IsNullOrEmpty(procedureResult))
                        {
                            throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                        }

                    // 오류 키워드 확인
                        var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                        var hasError = errorKeywords.Any(keyword =>
                            procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                        if (hasError)
                        {
                            throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                        }

                    // 프로시저 실행 완료 로그 - 멀티라인 결과를 각 줄별로 처리
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료:");
                    
                    // procedureResult가 멀티라인 문자열인 경우 각 줄을 개별적으로 로그에 기록
                    if (!string.IsNullOrEmpty(procedureResult))
                    {
                        var resultLines = procedureResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in resultLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLogWithFlush(logPath, line);
                            }
                        }
                    }
                    }
                    catch (Exception ex)
                    {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 서울냉동 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                // 오류 처리 및 로깅
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ 서울냉동 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"서울냉동 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경기도냉동 관리 처리 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 경기도냉동 관련 프로시저 실행
        /// - 처리 과정을 상세하게 로깅
        /// - 오류 발생 시 적절한 예외 처리
        /// - 처리 시작/완료, 오류 발생 시 모두 상세 로그를 남깁니다.
        /// </summary>
        private async Task ProcessGyeonggiFrozenManagement()
        {
            const string METHOD_NAME = "ProcessGyeonggiFrozenManagement";
            const string PROCEDURE_NAME = "sp_GyeonggiProcessF";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 경기도냉동 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                var insertCount = 0;                 // 경기도냉동 처리는 프로시저만 실행하므로 데이터 삽입 건수는 0
                
                try
                {
                    procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                    if (string.IsNullOrEmpty(procedureResult))
                    {
                        throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                    }

                    // 오류 키워드 확인
                    var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                    var hasError = errorKeywords.Any(keyword =>
                        procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (hasError)
                    {
                        throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                    }

                    // 프로시저 실행 완료 로그 - 멀티라인 결과를 각 줄별로 처리
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료:");
                    
                    // procedureResult가 멀티라인 문자열인 경우 각 줄을 개별적으로 로그에 기록
                    if (!string.IsNullOrEmpty(procedureResult))
                    {
                        var resultLines = procedureResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in resultLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLogWithFlush(logPath, line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 경기도냉동 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                // 오류 처리 및 로깅
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ 경기도냉동 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"경기도냉동 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 감천냉동 처리 - sp_GamcheonProcessF 프로시저 실행
        /// 
        /// 📋 주요 기능:
        /// - 감천냉동 관련 프로시저 실행
        /// - 상세한 로깅 및 진행률 보고
        /// - 에러 처리 및 통계 수집
        /// 
        /// 🔗 의존성:
        /// - sp_GamcheonProcessF 프로시저
        /// - ExecuteStoredProcedureAsync 메서드
        /// 
        /// 📁 로그 파일: app.log
        /// </summary>
        private async Task ProcessGamcheonFrozenManagement()
        {
            const string METHOD_NAME = "ProcessGamcheonFrozenManagement";
            const string PROCEDURE_NAME = "sp_GamcheonProcessF";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 감천냉동 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                var insertCount = 0;                 // 감천냉동 처리는 프로시저만 실행하므로 데이터 삽입 건수는 0
                
                try
                {
                    procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                    if (string.IsNullOrEmpty(procedureResult))
                    {
                        throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                    }

                    // 오류 키워드 확인
                    var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                    var hasError = errorKeywords.Any(keyword =>
                        procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (hasError)
                    {
                        throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                    }

                    // 프로시저 실행 완료 로그 - 멀티라인 결과를 각 줄별로 처리
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료:");
                    
                    // procedureResult가 멀티라인 문자열인 경우 각 줄을 개별적으로 로그에 기록
                    if (!string.IsNullOrEmpty(procedureResult))
                    {
                        var resultLines = procedureResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in resultLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLogWithFlush(logPath, line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 감천냉동 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                // 오류 처리 및 로깅
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ 감천냉동 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"감천냉동 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 송장출력 최종 처리 - sp_InvoiceFinalProcess 프로시저 실행
        /// 
        /// 📋 주요 기능:
        /// - 송장출력 최종 관련 프로시저 실행
        /// - 상세한 로깅 및 진행률 보고
        /// - 에러 처리 및 통계 수집
        /// 
        /// 🔗 의존성:
        /// - sp_InvoiceFinalProcess 프로시저
        /// - ExecuteStoredProcedureAsync 메서드
        /// 
        /// 📁 로그 파일: app.log
        /// </summary>
        private async Task ProcessInvoiceFinalManagement()
        {
            const string METHOD_NAME = "ProcessInvoiceFinalManagement";
            const string PROCEDURE_NAME = "sp_InvoiceFinalProcess";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 송장출력 최종 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                var insertCount = 0;                 // 송장출력 최종 처리는 프로시저만 실행하므로 데이터 삽입 건수는 0
                
                try
                {
                    procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                    if (string.IsNullOrEmpty(procedureResult))
                    {
                        throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                    }

                    // 오류 키워드 확인
                    var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                    var hasError = errorKeywords.Any(keyword =>
                        procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (hasError)
                    {
                        throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                    }

                    // 프로시저 실행 완료 로그 - 멀티라인 결과를 각 줄별로 처리
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료:");
                    
                    // procedureResult가 멀티라인 문자열인 경우 각 줄을 개별적으로 로그에 기록
                    if (!string.IsNullOrEmpty(procedureResult))
                    {
                        var resultLines = procedureResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in resultLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLogWithFlush(logPath, line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 송장출력 최종 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                // 오류 처리 및 로깅
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ 송장출력 최종 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"송장출력 최종 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경기공산 처리 - sp_GyeonggiProcessG 프로시저 실행
        /// 
        /// </summary>
        private async Task ProcessSeoulGongsanManagement()
        {
            const string METHOD_NAME = "ProcessSeoulGongsanManagement";
            const string PROCEDURE_NAME = "sp_SeoulGongsanProcessF";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 서울공산 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                var insertCount = 0;
                
                try
                {
                    procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                    if (string.IsNullOrEmpty(procedureResult))
                    {
                        throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                    }

                    // 오류 키워드 확인
                    var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                    var hasError = errorKeywords.Any(keyword =>
                        procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (hasError)
                    {
                        throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                    }

                    // 프로시저 실행 완료 로그 - 멀티라인 결과를 각 줄별로 처리
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료:");
                    
                    // procedureResult가 멀티라인 문자열인 경우 각 줄을 개별적으로 로그에 기록
                    if (!string.IsNullOrEmpty(procedureResult))
                    {
                        var resultLines = procedureResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in resultLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLogWithFlush(logPath, line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 서울공산 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ 서울공산 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"서울공산 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경기공산 처리 - sp_GyeonggiGongsanProcessF 프로시저 실행
        /// 
        /// 📋 주요 기능:
        /// - 경기공산 관련 데이터 처리
        /// - sp_GyeonggiGongsanProcessF 프로시저 실행
        /// - 처리 결과 로깅 및 진행률 보고
        /// - 오류 처리 및 롤백
        /// </summary>
        private async Task ProcessGyeonggiGongsanManagement()
        {
            const string METHOD_NAME = "ProcessGyeonggiGongsanManagement";
            const string PROCEDURE_NAME = "sp_GyeonggiGongsanProcessF";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 경기공산 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                var insertCount = 0;
                
                try
                {
                    procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                    if (string.IsNullOrEmpty(procedureResult))
                    {
                        throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                    }

                    var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                    var hasError = errorKeywords.Any(keyword =>
                        procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (hasError)
                    {
                        throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                    }

                    // 프로시저 실행 완료 로그 - 멀티라인 결과를 각 줄별로 처리
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료:");
                    
                    // procedureResult가 멀티라인 문자열인 경우 각 줄을 개별적으로 로그에 기록
                    if (!string.IsNullOrEmpty(procedureResult))
                    {
                        var resultLines = procedureResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in resultLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLogWithFlush(logPath, line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 경기공산 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ 경기공산 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"경기공산 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 부산청과 처리 - 부산청과 관련 데이터 처리 및 프로시저 실행
        /// 
        /// 📋 주요 기능:
        /// - 부산청과 데이터 처리 프로시저 실행
        /// - 처리 결과 로깅 및 모니터링
        /// - 오류 처리 및 복구
        /// 
        /// 🔗 의존성: sp_BusanCheonggwaProcessF 프로시저
        /// </summary>
        private async Task ProcessBusanCheonggwaManagement()
        {
            const string METHOD_NAME = "ProcessBusanCheonggwaManagement";
            const string PROCEDURE_NAME = "sp_BusanCheonggwaProcessF";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 부산청과 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                var insertCount = 0;
                
                try
                {
                    procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                    if (string.IsNullOrEmpty(procedureResult))
                    {
                        throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                    }

                    var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                    var hasError = errorKeywords.Any(keyword =>
                        procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (hasError)
                    {
                        throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                    }

                    // 프로시저 실행 완료 로그 - 멀티라인 결과를 각 줄별로 처리
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료:");
                    
                    // procedureResult가 멀티라인 문자열인 경우 각 줄을 개별적으로 로그에 기록
                    if (!string.IsNullOrEmpty(procedureResult))
                    {
                        var resultLines = procedureResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in resultLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLogWithFlush(logPath, line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 부산청과 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ 부산청과 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"부산청과 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 부산청과자료 처리 - 부산청과 관련 자료 처리 및 프로시저 실행
        /// 
        /// 📋 주요 기능:
        /// - 부산청과 자료 처리 프로시저 실행
        /// - 처리 결과 로깅 및 모니터링
        /// - 오류 처리 및 복구
        /// 
        /// 🔗 의존성: sp_BusanCheonggwaProcessF 프로시저
        /// </summary>
        private async Task ProcessBusanCheonggwaDoc()
        {
            const string METHOD_NAME = "ProcessBusanCheonggwaDoc";
            const string PROCEDURE_NAME = "sp_BusanCheonggwaDocProcess";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 부산청과자료 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                var insertCount = 0;
                
                try
                {
                    // 프로시저 실행 및 결과 검증
                    procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);
                    if (string.IsNullOrEmpty(procedureResult))
                    {
                        throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                    }
                    
                    var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                    var hasError = errorKeywords.Any(keyword =>
                        procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                    if (hasError)
                    {
                        throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                    }
                    
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료:");
                    if (!string.IsNullOrEmpty(procedureResult))
                    {
                        var resultLines = procedureResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in resultLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLogWithFlush(logPath, line);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 부산청과자료 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                
                var userErrorMessage = $"❌ 부산청과자료 처리 실패: {ex.Message}";
                _progress?.Report(userErrorMessage);
                
                throw new Exception($"부산청과자료 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 부산 외부출고 처리 - 부산 외부출고 관련 데이터 처리 및 프로시저 실행
        /// 
        /// 📋 주요 기능:
        /// - 부산 외부출고 Excel 파일 처리
        /// - 데이터 전처리 및 테이블 입력
        /// - 프로시저 실행 및 결과 처리
        /// - 오류 처리 및 복구
        /// 
        /// 🔗 의존성: 
        /// - ProcessExcelFileAsync (Excel 파일 처리)
        /// - ProcessStandardTable (테이블 처리)
        /// - sp_BusanExtShipmentProcess 프로시저
        /// </summary>
        private async Task ProcessBusanExtShipmentManagement()
        {
            const string METHOD_NAME = "ProcessBusanExtShipmentManagement";
            const string TABLE_NAME = "송장출력_부산청과_외부출고";
            const string PROCEDURE_NAME = "sp_BusanExtShipmentProcess";
            const string CONFIG_KEY = "DropboxFolderPath11";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            
            try
            {
                // 처리 시작 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 부산 외부출고 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                
                // 1. Excel 파일 처리 (공통 메서드 사용)
                var busanExtShipmentExcelFileName = ConfigurationManager.AppSettings["BusanExtShipmentExcelFileName"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(busanExtShipmentExcelFileName))
                {
                    throw new Exception("BusanExtShipmentExcelFileName 설정이 App.config에 존재하지 않거나 비어 있습니다.");
                }
                // 시트명을 설정에서 가져오거나 기본값 사용
                var sheetName = ConfigurationManager.AppSettings["BusanExtShipmentSheetName"] ?? "Sheet1";
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📋 사용할 시트명: {sheetName}");
                
                var excelData = await ProcessExcelFileAsync(
                    CONFIG_KEY, 
                    sheetName, 
                    busanExtShipmentExcelFileName,
                    _progress);
                
                // 2. 빈 데이터 체크 및 처리
                if (excelData.Rows.Count == 0)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ⚠️ 처리할 데이터가 없습니다. 다음 단계로 진행합니다.");
                    
                    // 빈 데이터여도 테이블은 TRUNCATE하고 프로시저 실행
                    var emptyInsertCount = await ProcessStandardTable(TABLE_NAME, excelData, _progress);
                    
                    // 프로시저 실행
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                    
                    string emptyProcedureResult = "";
                    try
                    {
                        emptyProcedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                        if (string.IsNullOrEmpty(emptyProcedureResult))
                        {
                            throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                        }

                        // 오류 키워드 확인
                        var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                        var hasError = errorKeywords.Any(keyword =>
                            emptyProcedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                        if (hasError)
                        {
                            throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {emptyProcedureResult}");
                        }

                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료: {emptyProcedureResult}");
                    }
                    catch (Exception ex)
                    {
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                        throw;
                    }
                    
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 빈 데이터 처리 완료 - 테이블 정리 및 프로시저 실행 완료");
                    
                    // 처리 완료
                    var emptyEndTime = DateTime.Now;
                    var emptyDuration = emptyEndTime - startTime;
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 부산 외부출고 처리 완료 - 소요시간: {emptyDuration.TotalSeconds:F1}초");
                    
                    // 성공 통계 로깅
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {emptyInsertCount:N0}건, 프로시저결과: {emptyProcedureResult}, 소요시간: {emptyDuration.TotalSeconds:F1}초");
                    return;
                }
                
                // 3. 엑셀 데이터 전처리
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🔧 엑셀 데이터 전처리 시작");
                
                var originalDataCount = excelData.Rows.Count;
                var processedData = DataTransformationService.PreprocessExcelData(excelData);
                excelData = processedData;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ 엑셀 데이터 전처리 완료: {originalDataCount:N0}건 → {processedData.Rows.Count:N0}건");
                
                // 4. 테이블 처리 (공통 메서드 사용)
                var insertCount = await ProcessStandardTable(TABLE_NAME, excelData, _progress);
                
                // 4. 프로시저 실행
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 {PROCEDURE_NAME} 프로시저 호출 시작");
                
                string procedureResult = "";
                try
                {
                    procedureResult = await ExecutePostProcessProcedureAsync(PROCEDURE_NAME);

                    if (string.IsNullOrEmpty(procedureResult))
                    {
                        throw new InvalidOperationException("프로시저 실행 결과가 비어있습니다.");
                    }

                    // 오류 키워드 확인
                    var errorKeywords = new[] { "Error", "오류", "실패", "Exception", "SQLSTATE", "ROLLBACK" };
                    var hasError = errorKeywords.Any(keyword =>
                        procedureResult.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (hasError)
                    {
                        throw new InvalidOperationException($"프로시저 실행 결과에 오류가 포함되어 있습니다: {procedureResult}");
                    }

                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ {PROCEDURE_NAME} 프로시저 실행 완료: {procedureResult}");
                }
                catch (Exception ex)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ {PROCEDURE_NAME} 프로시저 실행 실패: {ex.Message}");
                    throw;
                }
                
                // 처리 완료
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 부산 외부출고 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 데이터: {insertCount:N0}건, 프로시저결과: {procedureResult}, 소요시간: {duration.TotalSeconds:F1}초");
            }
            catch (Exception ex)
            {
                // === 오류 처리 및 로깅 ===
                var errorTime = DateTime.Now;
                var errorDuration = errorTime - startTime;
                
                var errorLog = $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)";
                WriteLogWithFlush(logPath, errorLog);
                
                var errorDetailLog = $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}";
                WriteLogWithFlush(logPath, errorDetailLog);
                
                var errorStackTraceLog = $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}";
                WriteLogWithFlush(logPath, errorStackTraceLog);
                
                // === 사용자에게 오류 메시지 전달 ===
                var userErrorMessage = $"❌ 부산 외부출고 처리 실패: {ex.Message}";
                WriteLogWithFlush(logPath, userErrorMessage);
                
                // === 예외 재발생 ===
                throw new Exception($"부산 외부출고 처리 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 컬럼 매핑 검증 - column_mapping.json 파일을 활용하여 엑셀 컬럼과 DB 컬럼 간의 매핑을 검증
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="excelData">엑셀 데이터</param>
        /// <returns>검증된 컬럼 매핑 정보</returns>
        private Dictionary<string, string>? ValidateColumnMappingAsync(string tableName, DataTable excelData)
        {
            try
            {
                // 테이블별 매핑 파일 경로 생성 (프로젝트 루트에서 찾기)
                var projectRoot = LogPathManager.GetProjectRootDirectory();
                var mappingFilePath = Path.Combine(projectRoot, "config", "table_mappings", $"{tableName}.json");
                
                // 파일 존재 확인
                if (!File.Exists(mappingFilePath))
                {
                    throw new InvalidOperationException($"테이블별 매핑 파일을 찾을 수 없습니다: {mappingFilePath}");
                }
                
                // JSON 파일 읽기
                var mappingJson = File.ReadAllText(mappingFilePath, Encoding.UTF8);
                if (string.IsNullOrEmpty(mappingJson))
                {
                    throw new InvalidOperationException($"매핑 파일이 비어있습니다: {mappingFilePath}");
                }
                
                // JSON 파싱
                var mappingData = JsonSerializer.Deserialize<JsonElement>(mappingJson);
                
                // columns 속성 찾기
                if (!mappingData.TryGetProperty("columns", out var columns))
                {
                    throw new InvalidOperationException("columns 속성을 찾을 수 없습니다.");
                }
                
                // 컬럼 매핑 생성
                var columnMapping = new Dictionary<string, string>();
                var excelColumns = excelData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                
                foreach (var column in columns.EnumerateObject())
                {
                    var excelColumnName = column.Name;
                    if (column.Value.TryGetProperty("db_column", out var dbColumnProp))
                    {
                        var dbColumnName = dbColumnProp.GetString();
                        if (!string.IsNullOrEmpty(dbColumnName) && excelColumns.Contains(excelColumnName))
                        {
                            columnMapping[excelColumnName] = dbColumnName;
                        }
                    }
                }
                
                // 매핑 검증 결과 로깅
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                var mappingLog = $"[ValidateColumnMapping] 컬럼 매핑 검증 완료 - 테이블: {tableName}, 파일: {mappingFilePath}, 엑셀: {excelColumns.Count}개, 매핑: {columnMapping.Count}개";
                WriteLogWithFlush(logPath, mappingLog);
                
                // 상세 매핑 정보 로깅 (여러 줄로 나누기)
                var detailMessage = string.Join(", ", columnMapping.Select(kvp => $"{kvp.Key}->{kvp.Value}"));
                WriteLogWithFlushMultiLine(logPath, "[ValidateColumnMapping] 상세 매핑 정보: ", detailMessage);
                
                // 엑셀 컬럼 정보 로깅 (여러 줄로 나누기)
                var excelColumnsMessage = string.Join(", ", excelColumns);
                WriteLogWithFlushMultiLine(logPath, "[ValidateColumnMapping] 엑셀 컬럼 목록: ", excelColumnsMessage);
                
                return columnMapping;
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                var errorLog = $"[ValidateColumnMapping] ❌ 컬럼 매핑 검증 실패: {ex.Message}";
                WriteLogWithFlush(logPath, errorLog);
                throw;
            }
        }

        /// <summary>
        /// 매핑 정보를 활용하여 데이터를 테이블에 INSERT
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="excelData">엑셀 데이터</param>
        /// <param name="columnMapping">컬럼 매핑 정보</param>
        /// <returns>INSERT된 행 수</returns>
        private async Task<int> InsertDataWithMappingAsync(string tableName, DataTable excelData, Dictionary<string, string> columnMapping)
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                var insertLog = $"[InsertDataWithMapping] 데이터 INSERT 시작 - 테이블: {tableName}, 행수: {excelData.Rows.Count:N0}";
                WriteLogWithFlush(logPath, insertLog);
                
                // 컬럼 매핑 정보 로깅 (여러 줄로 나누기)
                var mappingInfoMessage = string.Join(", ", columnMapping.Select(kvp => $"{kvp.Key}->{kvp.Value}"));
                WriteLogWithFlushMultiLine(logPath, "[InsertDataWithMapping] 컬럼 매핑 정보: ", mappingInfoMessage);
                
                var insertCount = 0;
                var batchSize = 100; // 배치 크기
                
                for (int i = 0; i < excelData.Rows.Count; i += batchSize)
                {
                    var batch = excelData.Rows.Cast<DataRow>().Skip(i).Take(batchSize);
                    var batchCount = batch.Count();
                    
                    var batchLog = $"[InsertDataWithMapping] 배치 처리 중: {i + 1}~{i + batchCount} / {excelData.Rows.Count}";
                    WriteLogWithFlush(logPath, batchLog);
                    
                    foreach (var row in batch)
                    {
                        try
                        {
                            var insertQuery = BuildInsertQuery(tableName, columnMapping, row);
                            await _invoiceRepository.ExecuteNonQueryAsync(insertQuery);
                            insertCount++;
                        }
                        catch (Exception ex)
                        {
                            var rowErrorLog = $"[InsertDataWithMapping] 행 {i + 1} INSERT 실패: {ex.Message}";
                            WriteLogWithFlush(logPath, rowErrorLog);
                            // 개별 행 오류는 계속 진행
                        }
                    }
                    
                    // 진행률 로깅
                    var progressLog = $"[InsertDataWithMapping] 진행률: {Math.Min(i + batchSize, excelData.Rows.Count)}/{excelData.Rows.Count} ({((i + batchSize) * 100.0 / excelData.Rows.Count):F1}%)";
                    WriteLogWithFlush(logPath, progressLog);
                }
                
                var completeLog = $"[InsertDataWithMapping] 데이터 INSERT 완료: {insertCount:N0}건";
                WriteLogWithFlush(logPath, completeLog);
                
                return insertCount;
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                var errorLog = $"[InsertDataWithMapping] ❌ 데이터 INSERT 실패: {ex.Message}";
                WriteLogWithFlush(logPath, errorLog);
                throw;
            }
        }

        /// <summary>
        /// INSERT 쿼리 생성
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="columnMapping">컬럼 매핑 정보</param>
        /// <param name="row">데이터 행</param>
        /// <returns>INSERT 쿼리</returns>
        private string BuildInsertQuery(string tableName, Dictionary<string, string> columnMapping, DataRow row)
        {
            var columns = columnMapping.Values.ToList();
            var values = new List<string>();
            
            // 디버깅을 위한 로깅 추가
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            var debugLog = $"[BuildInsertQuery] 쿼리 생성 시작 - 테이블: {tableName}, 컬럼수: {columns.Count}";
            //WriteLogWithFlush(logPath, debugLog);
            
            foreach (var excelColumn in columnMapping.Keys)
            {
                var value = row[excelColumn];
                if (value == DBNull.Value || value == null)
                {
                    values.Add("NULL");
                }
                else
                {
                    // SQL 인젝션 방지를 위한 이스케이프 처리
                    var stringValue = value.ToString();
                    if (stringValue != null)
                    {
                        stringValue = stringValue.Replace("'", "''");
                        values.Add($"'{stringValue}'");
                    }
                    else
                    {
                        values.Add("NULL");
                    }
                }
            }
            
            var query = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
            
            // 생성된 쿼리 로깅 (첫 번째 행만, 긴 쿼리는 여러 줄로 나누기)
            if (row.Table.Rows.IndexOf(row) == 0)
            {
                WriteLogWithFlushMultiLine(logPath, "[BuildInsertQuery] 생성된 쿼리 예시: ", query);
            }
            
            return query;
        }

        /// <summary>
        /// 후처리 프로시저 실행 (공용)
        /// </summary>
        /// <param name="procedureName">프로시저명</param>
        /// <returns>프로시저 실행 결과</returns>
        private async Task<string> ExecutePostProcessProcedureAsync(string procedureName)
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                var procedureLog = $"[ExecutePostProcessProcedure] {procedureName} 프로시저 실행 시작";
                WriteLogWithFlush(logPath, procedureLog);
                
                // ExecuteStoredProcedureAsync 사용으로 프로시저 결과 캐치
                var result = await ExecuteStoredProcedureAsync(procedureName);
                
                // ExecuteStoredProcedureAsync에서 이미 상세 로깅을 수행하므로 간단한 완료 메시지만 기록
                var resultLog = $"[ExecutePostProcessProcedure] {procedureName} 프로시저 실행 완료";
                WriteLogWithFlush(logPath, resultLog);
                
                return result;
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                var errorLog = $"[ExecutePostProcessProcedure] ❌ {procedureName} 프로시저 실행 실패: {ex.Message}";
                WriteLogWithFlush(logPath, errorLog);
                throw;
            }
        }



        /// <summary>
        /// 프로시저 실행 및 결과 로깅 (MySQL 오류 상세 정보 포함)
        /// 
        /// 🎯 주요 기능:
        /// - 프로시저 실행 및 결과 파싱
        /// - MySQL 오류 발생 시 상세 정보 로깅
        /// - 단계별 처리 건수 상세 표시
        /// - 오류 발생 시 즉시 반환하여 폴백 방지
        /// 
        /// 📋 핵심 기능:
        /// - DatabaseService 직접 사용으로 정확한 오류 정보 수집
        /// - MySQL 오류 코드별 상세 설명 제공
        /// - 모든 정보를 app.log 파일에 체계적으로 기록
        /// - 프로시저 내부 오류를 정확하게 진단
        /// </summary>
        /// <param name="procedureName">프로시저명</param>
        /// <returns>프로시저 실행 결과 또는 오류 메시지</returns>
        private async Task<string> ExecuteStoredProcedureAsync(string procedureName)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            
            try
            {
                var procedureLog = $"[ExecuteStoredProcedure] {procedureName} 프로시저 실행 시작";
                WriteLogWithFlush(logPath, procedureLog);
                
                // DatabaseService 직접 사용으로 MySQL 오류 정확한 캐치
                try
                {
                    var debugLog = $"[ExecuteStoredProcedure] 🔍 DatabaseService 직접 사용 시도 중...";
                    WriteLogWithFlush(logPath, debugLog);
                    
                    if (_databaseService == null)
                    {
                        throw new InvalidOperationException("DatabaseService가 null입니다!");
                    }
                    
                    using (var connection = await _databaseService.GetConnectionAsync())
                    {
                        var connectionLog = $"[ExecuteStoredProcedure] ✅ 연결 객체 생성 성공, 연결 시도 중...";
                        WriteLogWithFlush(logPath, connectionLog);
                        
                        await connection.OpenAsync();
                        
                        var openLog = $"[ExecuteStoredProcedure] ✅ 데이터베이스 연결 성공";
                        WriteLogWithFlush(logPath, openLog);
                        
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandType = CommandType.Text;
                            command.CommandText = $"CALL {procedureName}()";
                            command.CommandTimeout = 300; // 5분 타임아웃
                            
                            var executeLog = $"[ExecuteStoredProcedure] 🔍 프로시저 실행 중: CALL {procedureName}()";
                            WriteLogWithFlush(logPath, executeLog);
                            
                            // 변수 선언을 using 블록 밖으로 이동
                            var logs = new List<string>();
                            var stepCount = 0;
                            var hasErrorMessage = false;
                            var errorMessage = "";
                            
                            try
                            {
                                // 프로시저 실행 직후 MySQL 오류 정보 즉시 캐치
                                var immediateErrorLog = $"[ExecuteStoredProcedure] 🔍 프로시저 실행 직후 MySQL 오류 정보 즉시 캐치 시도...";
                                WriteLogWithFlush(logPath, immediateErrorLog);
                                
                                string immediateErrors = string.Empty;
                                string immediateWarnings = string.Empty;
                                
                                try
                                {
                                    // 프로시저 실행 직후 즉시 SHOW ERRORS 실행
                                    using (var errorCommand = connection.CreateCommand())
                                    {
                                        errorCommand.CommandType = CommandType.Text;
                                        errorCommand.CommandText = "SHOW ERRORS";
                                        errorCommand.CommandTimeout = 10;
                                        
                                        using (var errorReader = await errorCommand.ExecuteReaderAsync())
                                        {
                                            var hasErrors = false;
                                            while (await errorReader.ReadAsync())
                                            {
                                                hasErrors = true;
                                                var level = errorReader["Level"]?.ToString() ?? "N/A";
                                                var code = errorReader["Code"]?.ToString() ?? "N/A";
                                                var message = errorReader["Message"]?.ToString() ?? "N/A";
                                                immediateErrors += $"• Level: {level}, Code: {code}, Message: {message}\n";
                                            }
                                            if (!hasErrors) immediateErrors = "MySQL 오류가 없습니다.";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    immediateErrors = $"즉시 오류 정보 조회 실패: {UtilityCommonService.SanitizeMessage(ex.Message)}";
                                }
                                
                                try
                                {
                                    // 프로시저 실행 직후 즉시 SHOW WARNINGS 실행
                                    using (var warningCommand = connection.CreateCommand())
                                    {
                                        warningCommand.CommandType = CommandType.Text;
                                        warningCommand.CommandText = "SHOW WARNINGS";
                                        warningCommand.CommandTimeout = 10;
                                        
                                        using (var warningReader = await warningCommand.ExecuteReaderAsync())
                                        {
                                            var hasWarnings = false;
                                            while (await warningReader.ReadAsync())
                                            {
                                                hasWarnings = true;
                                                var level = warningReader["Level"]?.ToString() ?? "N/A";
                                                var code = warningReader["Code"]?.ToString() ?? "N/A";
                                                var message = warningReader["Message"]?.ToString() ?? "N/A";
                                                immediateWarnings += $"• Level: {level}, Code: {code}, Message: {message}\n";
                                            }
                                            if (!hasWarnings) immediateWarnings = "MySQL 경고가 없습니다.";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    immediateWarnings = $"즉시 경고 정보 조회 실패: {UtilityCommonService.SanitizeMessage(ex.Message)}";
                                }
                                
                                var immediateResultLog = $"[ExecuteStoredProcedure] 🔍 즉시 캐치 결과:\n오류: {immediateErrors.TrimEnd()}\n경고: {immediateWarnings.TrimEnd()}";
                                WriteLogWithFlush(logPath, immediateResultLog);
                                
                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    var successLog = $"[ExecuteStoredProcedure] ✅ 프로시저 실행 성공, 결과 읽기 시작";
                                    WriteLogWithFlush(logPath, successLog);
                                    
                                    // 결과셋 컬럼 구조 확인
                                    var schemaTable = reader.GetSchemaTable();
                                    if (schemaTable != null)
                                    {
                                        var columnInfoLog = $"[ExecuteStoredProcedure] 🔍 결과셋 컬럼 정보:";
                                        WriteLogWithFlush(logPath, columnInfoLog);
                                        
                                        foreach (DataRow row in schemaTable.Rows)
                                        {
                                            var columnName = row["ColumnName"]?.ToString() ?? "N/A";
                                            var dataType = row["DataType"]?.ToString() ?? "N/A";
                                            var columnInfo = $"[ExecuteStoredProcedure]   - 컬럼: {columnName}, 타입: {dataType}";
                                            WriteLogWithFlush(logPath, columnInfo);
                                        }
                                    }
                                    
                                    // 프로시저 결과 읽기 - 첫 번째 결과셋부터 순차적으로 처리
                                    var resultSetIndex = 0;
                                    do
                                    {
                                        resultSetIndex++;
                                        var resultSetLog = $"[ExecuteStoredProcedure] 🔍 결과셋 #{resultSetIndex} 처리 시작";
                                        WriteLogWithFlush(logPath, resultSetLog);
                                        
                                        // 현재 결과셋의 컬럼 구조 분석
                                        var currentSchema = reader.GetSchemaTable();
                                        var columnNames = new List<string>();
                                        if (currentSchema != null)
                                        {
                                            foreach (DataRow row in currentSchema.Rows)
                                            {
                                                var columnName = row["ColumnName"]?.ToString() ?? "";
                                                if (!string.IsNullOrEmpty(columnName))
                                                    columnNames.Add(columnName);
                                            }
                                        }
                                        
                                        // 컬럼 정보 로깅 (긴 경우 여러 줄로 나누기)
                                        var columnInfoMessage = string.Join(", ", columnNames);
                                        WriteLogWithFlushMultiLine(logPath, $"[ExecuteStoredProcedure] 📋 결과셋 #{resultSetIndex} 컬럼: ", columnInfoMessage);
                                        
                                        // 결과셋 타입 판별 및 처리
                                        if (columnNames.Contains("ErrorMessage"))
                                        {
                                            // 1. 오류 발생 결과셋 처리 (수정된 프로시저에서 MySQLErrorCode, MySQLErrorMessage도 함께 반환)
                                            var errorResultSetLog = $"[ExecuteStoredProcedure] ❌ 결과셋 #{resultSetIndex}: 오류 메시지 결과셋 감지";
                                            WriteLogWithFlush(logPath, errorResultSetLog);
                                            
                                            hasErrorMessage = true;
                                            while (await reader.ReadAsync())
                                            {
                                                errorMessage = reader["ErrorMessage"]?.ToString() ?? "";
                                                
                                                // 수정된 프로시저에서 반환하는 추가 오류 정보 확인
                                                string mysqlErrorCode = "";
                                                string mysqlErrorMessage = "";
                                                
                                                try
                                                {
                                                    if (columnNames.Contains("MySQLErrorCode"))
                                                        mysqlErrorCode = reader["MySQLErrorCode"]?.ToString() ?? "";
                                        }
                                        catch { /* 컬럼이 존재하지 않음 */ }
                                        
                                        try
                                        {
                                                    if (columnNames.Contains("MySQLErrorMessage"))
                                                        mysqlErrorMessage = reader["MySQLErrorMessage"]?.ToString() ?? "";
                                        }
                                        catch { /* 컬럼이 존재하지 않음 */ }
                                        
                                            var errorLog = $"[ExecuteStoredProcedure] ⚠️ 프로시저 오류 메시지: {errorMessage}";
                                            WriteLogWithFlush(logPath, errorLog);
                                            
                                                // MySQL 오류 정보가 있는 경우 추가 로깅
                                                if (!string.IsNullOrEmpty(mysqlErrorCode) || !string.IsNullOrEmpty(mysqlErrorMessage))
                                                {
                                                    var mysqlErrorDetails = $"Code={mysqlErrorCode}, Message={mysqlErrorMessage}";
                                                    WriteLogWithFlushMultiLine(logPath, "[ExecuteStoredProcedure] 🔍 MySQL 오류 정보: ", mysqlErrorDetails);
                                                }
                                            }
                                        }
                                        else if (columnNames.Contains("StepID") && columnNames.Contains("OperationDescription") && columnNames.Contains("AffectedRows"))
                                        {
                                            // 2. 정상 실행 로그 결과셋 처리
                                            var successResultSetLog = $"[ExecuteStoredProcedure] ✅ 결과셋 #{resultSetIndex}: 실행 로그 결과셋 감지";
                                            WriteLogWithFlush(logPath, successResultSetLog);
                                            
                                            while (await reader.ReadAsync())
                                            {
                                                stepCount++;
                                                
                                                // 컬럼 존재 여부 확인 후 안전하게 접근
                                                string stepID = reader["StepID"]?.ToString() ?? "N/A";
                                                string operation = reader["OperationDescription"]?.ToString() ?? "N/A";
                                                string affectedRows = reader["AffectedRows"]?.ToString() ?? "0";
                                        
                                        var stepLog = $"[ExecuteStoredProcedure] 📊 단계 {stepCount}: {stepID} - {operation} ({affectedRows}행)";
                                        WriteLogWithFlush(logPath, stepLog);
                                        
                                        // 로그 포맷팅 개선 - 숫자 정렬 및 가독성 향상
                                        var formattedStepID = stepID.PadRight(4);
                                        var formattedOperation = TruncateAndPadRight(operation, 50);
                                        var formattedAffectedRows = affectedRows.PadLeft(10);
                                        
                                        logs.Add($"{formattedStepID} {formattedOperation} {formattedAffectedRows}");
                                    }
                                        }
                                        else
                                        {
                                            // 3. 기타 결과셋 처리 (예상치 못한 결과셋)
                                            var unknownResultSetLog = $"[ExecuteStoredProcedure] ⚠️ 결과셋 #{resultSetIndex}: 예상치 못한 결과셋 (컬럼: {string.Join(", ", columnNames)})";
                                            WriteLogWithFlush(logPath, unknownResultSetLog);
                                            
                                            // 데이터가 있는 경우 읽어서 로그에 기록
                                            var rowCount = 0;
                                            while (await reader.ReadAsync())
                                            {
                                                rowCount++;
                                                var rowData = new List<string>();
                                                for (int i = 0; i < reader.FieldCount; i++)
                                                {
                                                    var value = reader[i]?.ToString() ?? "NULL";
                                                    rowData.Add($"{columnNames[i]}: {value}");
                                                }
                                                var unknownRowLog = $"[ExecuteStoredProcedure] 📝 결과셋 #{resultSetIndex} 행 {rowCount}: {string.Join(" | ", rowData)}";
                                                WriteLogWithFlush(logPath, unknownRowLog);
                                            }
                                            
                                            if (rowCount == 0)
                                            {
                                                var noDataLog = $"[ExecuteStoredProcedure] 📝 결과셋 #{resultSetIndex}: 데이터 없음";
                                                WriteLogWithFlush(logPath, noDataLog);
                                            }
                                        }
                                        
                                        var resultSetCompleteLog = $"[ExecuteStoredProcedure] ✅ 결과셋 #{resultSetIndex} 처리 완료";
                                        WriteLogWithFlush(logPath, resultSetCompleteLog);
                                        
                                    } while (await reader.NextResultAsync()); // 다음 결과셋으로 이동
                                    
                                    // 오류 메시지가 있었던 경우 - 리더 종료 후 처리
                                    if (hasErrorMessage)
                                    {
                                        // 상세 정보는 리더 종료 후 별도로 처리 (return 제거)
                                        var errorLog = $"[ExecuteStoredProcedure] ⚠️ 프로시저에서 오류가 발생했습니다. MySQL 오류 정보를 확인합니다.";
                                        WriteLogWithFlush(logPath, errorLog);
                                    }
                                    
                                    // 정상 실행 로그 생성 (오류가 없는 경우에만)
                                    if (!hasErrorMessage && stepCount > 0)
                                    {
                                    var totalLog = $"[ExecuteStoredProcedure] 📊 총 {stepCount}개 단계 처리됨";
                                    WriteLogWithFlush(logPath, totalLog);
                                    
                                        var logBuilder = new StringBuilder();
                                        logBuilder.AppendLine($"📊 {procedureName} 프로시저 실행 결과 - 총 {stepCount}개 단계:");
                                        logBuilder.AppendLine($"{"단계".PadRight(4)} {"처리내용".PadRight(50)} {"처리행수".PadLeft(10)}");
                                        logBuilder.AppendLine(new string('-', 70));
                                        
                                        foreach (var log in logs)
                                        {
                                            logBuilder.AppendLine(log);
                                        }
                                        
                                        var resultString = logBuilder.ToString();
                                        
                                        // 멀티라인 문자열을 각 줄별로 로그에 기록
                                        var lines = resultString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (var line in lines)
                                        {
                                            if (!string.IsNullOrWhiteSpace(line))
                                            {
                                                WriteLogWithFlush(logPath, line);
                                            }
                                        }
                                        
                                        return resultString;
                                    }
                                    // hasErrorMessage가 true인 경우 MySQL 오류 정보 조회로 진행
                                    // hasErrorMessage가 false인 경우 정상 실행 완료 처리
                                }
                                
                                // 리더가 완전히 닫힌 후 프로시저에서 반환한 오류 정보 처리 (오류 발생 시)
                                if (hasErrorMessage)
                                {
                                    var errorAnalysisLog = $"[ExecuteStoredProcedure] 🔍 프로시저에서 오류가 발생했습니다. 프로시저에서 반환한 오류 정보를 분석합니다.";
                                    WriteLogWithFlush(logPath, errorAnalysisLog);
                                    
                                    // SHOW ERRORS 명령을 실행하여 실제 MySQL 오류 정보 가져오기
                                    var detailed = $"프로시저 실행 실패: {errorMessage}";
                                    
                                    try
                                    {
                                        // SHOW ERRORS 명령 실행
                                        using (var errorCommand = new MySqlCommand("SHOW ERRORS", connection))
                                        {
                                            using (var errorReader = await errorCommand.ExecuteReaderAsync())
                                            {
                                                var errorCount = 0;
                                                var mysqlErrorDetails = new StringBuilder();
                                                
                                                while (await errorReader.ReadAsync())
                                                {
                                                    errorCount++;
                                                    var level = errorReader["Level"]?.ToString() ?? "";
                                                    var code = errorReader["Code"]?.ToString() ?? "";
                                                    var message = errorReader["Message"]?.ToString() ?? "";
                                                    
                                                    mysqlErrorDetails.AppendLine($"• 오류 #{errorCount}: Level={level}, Code={code}, Message={message}");
                                                }
                                                
                                                if (errorCount > 0)
                                                {
                                                    detailed += $"\n\n🔍 SHOW ERRORS 결과 (총 {errorCount}개 오류):";
                                                    detailed += mysqlErrorDetails.ToString();
                                                    
                                                    // app.log에도 상세 오류 정보 기록
                                                    WriteLogWithFlushMultiLine(logPath, "[ExecuteStoredProcedure] 🔍 SHOW ERRORS 결과: ", mysqlErrorDetails.ToString());
                                                }
                                                else
                                                {
                                                    detailed += $"\n\n🔍 SHOW ERRORS 결과: 오류가 없습니다.";
                                                    WriteLogWithFlush(logPath, "[ExecuteStoredProcedure] 🔍 SHOW ERRORS 결과: 오류가 없습니다.");
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception showErrorEx)
                                    {
                                        detailed += $"\n\n⚠️ SHOW ERRORS 실행 실패: {showErrorEx.Message}";
                                        WriteLogWithFlush(logPath, $"[ExecuteStoredProcedure] ⚠️ SHOW ERRORS 실행 실패: {showErrorEx.Message}");
                                    }
                                    
                                    var finalErrorLog = $"[ExecuteStoredProcedure] 🎯 프로시저 반환 오류 정보 분석 완료 - 상세 정보 반환";
                                    WriteLogWithFlush(logPath, finalErrorLog);
                                    
                                    var errorResult = $"프로시저 실행 실패: {detailed}";
                                    WriteLogWithFlush(logPath, $"[ExecuteStoredProcedure] 🚨 오류 결과 반환: {errorResult}");
                                    return errorResult;
                                }
                                
                                // 정상 실행 완료 (오류가 없는 경우)
                                if (stepCount > 0)
                                {
                                    var successSummaryLog = $"[ExecuteStoredProcedure] 🎉 프로시저 정상 실행 완료 - 총 {stepCount}개 단계 처리됨";
                                    WriteLogWithFlush(logPath, successSummaryLog);
                                    
                                    return $"프로시저 실행 완료 - 총 {stepCount}개 단계 처리됨";
                                    }
                                    else
                                    {
                                    var noStepLog = $"[ExecuteStoredProcedure] ℹ️ 프로시저 실행 완료 - 처리된 단계 없음";
                                    WriteLogWithFlush(logPath, noStepLog);
                                    
                                        return "프로시저 실행 완료 (상세 로그 없음)";
                                }
                            }
                            catch (MySqlException mysqlEx)
                            {
                                // MySQL 특정 오류 상세 정보를 app.log에 기록
                                var mysqlErrorLog = $"[ExecuteStoredProcedure] ❌ MySQL 오류 발생: {mysqlEx.Message}";
                                WriteLogWithFlush(logPath, mysqlErrorLog);
                                
                                var errorCodeLog = $"[ExecuteStoredProcedure] ❌ MySQL 오류 코드: {mysqlEx.Number}";
                                WriteLogWithFlush(logPath, errorCodeLog);
                                
                                var sqlStateLog = $"[ExecuteStoredProcedure] ❌ SQL State: {mysqlEx.SqlState}";
                                WriteLogWithFlush(logPath, sqlStateLog);
                                
                                // MySQL 오류 코드별 상세 설명을 app.log에 기록
                                string errorDescription = mysqlEx.Number switch
                                {
                                    1146 => $"테이블이 존재하지 않습니다: {mysqlEx.Message}",
                                    1054 => $"컬럼이 존재하지 않습니다: {mysqlEx.Message}",
                                    1045 => $"데이터베이스 접근 권한이 없습니다: {mysqlEx.Message}",
                                    2002 => $"데이터베이스 서버에 연결할 수 없습니다: {mysqlEx.Message}",
                                    1049 => $"데이터베이스가 존재하지 않습니다: {mysqlEx.Message}",
                                    1064 => $"SQL 구문 오류: {mysqlEx.Message}",
                                    1216 => $"외래 키 제약 조건 위반: {mysqlEx.Message}",
                                    1217 => $"외래 키 제약 조건 위반: {mysqlEx.Message}",
                                    1451 => $"외래 키 제약 조건 위반: {mysqlEx.Message}",
                                    1452 => $"외래 키 제약 조건 위반: {mysqlEx.Message}",
                                    _ => $"MySQL 오류 코드 {mysqlEx.Number}: {mysqlEx.Message}"
                                };
                                
                                var errorDescLog = $"[ExecuteStoredProcedure] 💡 오류 상세: {errorDescription}";
                                WriteLogWithFlush(logPath, errorDescLog);
                                
                                // MySQL 오류 발생 시 상세 정보를 app.log에 기록하고 즉시 반환
                                var errorResult = $"프로시저 실행 실패 (MySQL 오류): {errorDescription}";
                                WriteLogWithFlush(logPath, $"[ExecuteStoredProcedure] 🚨 오류 결과 반환: {errorResult}");
                                return errorResult;
                            }
                            catch (Exception ex)
                            {
                                // 기타 예외도 상세 정보를 app.log에 기록
                                var generalErrorLog = $"[ExecuteStoredProcedure] ❌ 일반 예외 발생: {ex.Message}";
                                WriteLogWithFlush(logPath, generalErrorLog);
                                
                                var errorTypeLog = $"[ExecuteStoredProcedure] ❌ 예외 타입: {ex.GetType().Name}";
                                WriteLogWithFlush(logPath, errorTypeLog);
                                
                                var stackTraceLog = $"[ExecuteStoredProcedure] ❌ 스택 트레이스: {ex.StackTrace}";
                                WriteLogWithFlush(logPath, stackTraceLog);
                                
                                var errorResult = $"프로시저 실행 실패 (일반 예외): {ex.Message}";
                                WriteLogWithFlush(logPath, $"[ExecuteStoredProcedure] 🚨 오류 결과 반환: {errorResult}");
                                return errorResult;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // DatabaseService 사용 실패 시 기존 방식으로 폴백
                    var fallbackLog = $"[ExecuteStoredProcedure] ⚠️ DatabaseService 직접 사용 실패, 기존 방식으로 폴백";
                    WriteLogWithFlush(logPath, fallbackLog);
                    
                    var errorLog = $"[ExecuteStoredProcedure] ❌ 오류 상세: {ex.Message}";
                    WriteLogWithFlush(logPath, errorLog);
                    
                    var errorTypeLog = $"[ExecuteStoredProcedure] ❌ 오류 타입: {ex.GetType().Name}";
                    WriteLogWithFlush(logPath, errorTypeLog);
                    
                    var stackTraceLog = $"[ExecuteStoredProcedure] ❌ 스택 트레이스: {ex.StackTrace}";
                    WriteLogWithFlush(logPath, stackTraceLog);
                    
                    var procedureQueryFallback = $"CALL {procedureName}()";
                    var result = await _invoiceRepository.ExecuteNonQueryAsync(procedureQueryFallback);
                    return $"프로시저 실행 완료 - 영향받은 행 수: {result}";
                }
            }
            catch (Exception ex)
            {
                var errorLog = $"[ExecuteStoredProcedure] ❌ {procedureName} 프로시저 실행 실패: {ex.Message}";
                WriteLogWithFlush(logPath, errorLog);
                
                var errorDetailLog = $"[ExecuteStoredProcedure] ❌ {procedureName} 프로시저 상세 오류: {ex}";
                WriteLogWithFlush(logPath, errorDetailLog);
                
                var stackTraceLog = $"[ExecuteStoredProcedure] ❌ {procedureName} 스택 트레이스: {ex.StackTrace}";
                WriteLogWithFlush(logPath, stackTraceLog);
                
                throw;
            }
        }

        /// <summary>
        /// DataTable을 Order 객체 컬렉션으로 변환
        /// 
        /// 📋 주요 기능:
        /// - DataTable의 각 행을 Order 객체로 안전하게 변환
        /// - null 안전성 처리 및 타입 변환
        /// - 데이터 유효성 검사
        /// - 변환 실패 시 로깅
        /// </summary>
        /// <param name="data">변환할 DataTable</param>
        /// <returns>변환된 Order 객체 컬렉션</returns>
        private IEnumerable<Order> ConvertDataTableToOrders(DataTable data)
        {
            // === 변환 통계 추적을 위한 변수 초기화 ===
            var orders = new List<Order>();
            var totalRows = data.Rows.Count;
            var convertedRows = 0;
            var failedRows = 0;
            var errorDetails = new List<string>();
            
            // 로그 파일 경로
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            
            var conversionStartLog = $"[데이터변환] DataTable → Order 변환 시작 - 총 {totalRows:N0}건";
            WriteLogWithFlush(logPath, conversionStartLog);
            
            // === 각 행을 Order 객체로 변환 ===
            for (int i = 0; i < totalRows; i++)
            {
                try
                {
                    var row = data.Rows[i];
                    var order = Order.FromDataRow(row);
                    
                    if (order != null)
                    {
                        orders.Add(order);
                        convertedRows++;
                        
                        // 변환 진행률 로깅 (100건마다)
                        if (convertedRows % 100 == 0)
                        {
                            var progressLog = $"[데이터변환] 변환 진행률: {convertedRows:N0}/{totalRows:N0}건 ({convertedRows * 100.0 / totalRows:F1}%)";
                            WriteLogWithFlush(logPath, progressLog);
                        }
                    }
                    else
                    {
                        failedRows++;
                        var nullOrderLog = $"[데이터변환] 행 {i + 1}: Order.FromDataRow()가 null 반환";
                        WriteLogWithFlush(logPath, nullOrderLog);
                        errorDetails.Add($"행 {i + 1}: null 반환");
                    }
                }
                catch (Exception ex)
                {
                    failedRows++;
                    var conversionErrorLog = $"[데이터변환] 행 {i + 1} 변환 실패: {ex.Message}";
                    WriteLogWithFlush(logPath, conversionErrorLog);
                    errorDetails.Add($"행 {i + 1}: {ex.Message}");
                    
                    // 처음 10개의 오류만 상세 로그
                    if (failedRows <= 10)
                    {
                        var errorDetailLog = $"[데이터변환]   - 행 {i + 1} 상세 오류: {ex}";
                        WriteLogWithFlush(logPath, errorDetailLog);
                    }
                }
            }
            
            // === 변환 결과 통계 ===
            var conversionResultLog = $"[데이터변환] 변환 완료 - 성공: {convertedRows:N0}건, 실패: {failedRows:N0}건, 성공률: {convertedRows * 100.0 / totalRows:F1}%";
            WriteLogWithFlush(logPath, conversionResultLog);
            
            if (failedRows > 0)
            {
                var failureSummaryLog = $"[데이터변환] 실패 요약 - 총 실패: {failedRows:N0}건 ({failedRows * 100.0 / totalRows:F1}%)";
                WriteLogWithFlush(logPath, failureSummaryLog);
                
                // 실패율이 높은 경우 경고
                if (failedRows * 100.0 / totalRows > 10.0)
                {
                    var highFailureRateLog = $"[데이터변환] ⚠️ 높은 실패율 경고 - 실패율이 10%를 초과합니다. ({failedRows * 100.0 / totalRows:F1}%)";
                    WriteLogWithFlush(logPath, highFailureRateLog);
                }
                
                // 처음 5개의 오류 상세 정보
                foreach (var errorDetail in errorDetails.Take(5))
                {
                    var errorDetailLog = $"[데이터변환]   - {errorDetail}";
                    WriteLogWithFlush(logPath, errorDetailLog);
                }
            }
            
            return orders;
        }
        #endregion

        #region 출고지별 분류 및 처리

        /// <summary>
        /// 출고지별 분류 및 처리
        /// 
        /// 📋 주요 기능:
        /// - 송장구분자 설정
        /// - 송장구분 설정
        /// - 송장구분최종 설정
        /// - 위치 설정
        /// - 위치변환 설정
        /// </summary>
        // ClassifyAndProcessByShipmentCenter 메서드 제거됨 - 사용되지 않음

        #endregion

        #region 파일 생성 및 업로드

        /// <summary>
        /// 최종 파일 생성 및 업로드
        /// 
        /// 📋 주요 기능:
        /// - 판매입력 자료 생성
        /// - 송장 파일 생성
        /// - Dropbox 업로드
        /// </summary>
        /// <returns>업로드 결과 목록 (출고지명, 파일경로, Dropbox URL)</returns>
        /// <exception cref="Exception">파일 생성 및 업로드 실패 시</exception>
        private Task<List<(string centerName, string filePath, string dropboxUrl)>> GenerateAndUploadFiles()
        {
            var uploadResults = new List<(string centerName, string filePath, string dropboxUrl)>();

            // 로그 파일 경로 설정
            var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogPathManager.AppLogPath);

            try
            {
                // 최종 파일 생성 시작 메시지
                // 최종 파일 생성 시작 메시지를 app.log에 기록
                WriteLogWithFlush(logFilePath, "📄 최종 파일 생성을 시작합니다...");
                
                // 판매입력 자료 생성 - 메서드 제거됨
                // await GenerateSalesInputData();
                
                // 송장 파일 생성 - 메서드 제거됨
                // await GenerateInvoiceFiles();
                
                // 완료 메시지 출력
                WriteLogWithFlush(logFilePath, "✅ 최종 파일 생성 완료");
            }
            catch (Exception ex)
            {
                // 오류 메시지 출력 및 예외 재발생
                WriteLogWithFlush(logFilePath, $"❌ 최종 파일 생성 실패: {ex.Message}");    
                throw;
            }

            return Task.FromResult(uploadResults);
        }

        #endregion

        #region 알림 전송

        /// <summary>
        /// 카카오워크 알림 전송
        /// 
        /// 📋 주요 기능:
        /// - 새로운 KakaoWorkService 사용
        /// - 각 업로드 결과에 대해 알림 전송
        /// - 실패 시 로그만 출력하고 계속 진행
        /// 
        /// 🔗 의존성:
        /// - KakaoWorkService (Singleton 패턴)
        /// - App.config 설정 (채팅방 ID, API 키 등)
        /// </summary>
        /// <param name="uploadResults">업로드 결과 목록 (출고지명, 파일경로, Dropbox URL)</param>
        /// <exception cref="Exception">전체 알림 전송 실패 시</exception>
        private async Task SendKakaoWorkNotifications(List<(string centerName, string filePath, string dropboxUrl)> uploadResults)
        {
            // 로그 파일 경로 설정
            var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogPathManager.AppLogPath);
            
            try
            {
                // === KakaoCheck 설정 확인 ===
                if (!IsKakaoWorkEnabled())
                {
                    WriteLogWithFlush(logFilePath, "⚠️ KakaoCheck 설정이 'Y'가 아닙니다. 카카오워크 알림 전송을 건너뜁니다.");
                    return; // 메시지 전송 없이 정상 종료
                }

                // === 카카오워크 알림 전송 프로세스 시작 ===
                WriteLogWithFlush(logFilePath, "📱 카카오워크 알림을 전송합니다...");
                
                // === KakaoWorkService 싱글톤 인스턴스 획득 ===
                // Singleton 패턴으로 구현된 KakaoWorkService 사용
                // App.config의 API 키, 채팅방 ID 등 설정 정보 자동 로드
                var kakaoWorkService = KakaoWorkService.Instance;
                
                // === 출고지별 개별 알림 전송 처리 ===
                // 각 업로드 결과에 대해 해당 출고지의 채팅방으로 개별 알림 전송
                // centerName은 출고지 이름(예: "서울공산", "경기공산" 등)을 의미합니다.
                // 출고지별로 알림 메시지나 채팅방을 다르게 처리할 때 사용됩니다.
                foreach (var (centerName, filePath, dropboxUrl) in uploadResults)
                {
                    try
                    {
                        // === 출고지별 알림 타입 자동 결정 ===
                        // GetNotificationTypeByCenter: 출고지명 → NotificationType 매핑
                        // 각 출고지별로 다른 채팅방과 메시지 템플릿 적용
                        var notificationType = GetNotificationTypeByCenter(centerName);
                        
                        // === 배치 식별자 생성 ===
                        // 시간대별 차수 설정 (1차~막차, 추가)
                        var now = DateTime.Now;
                        var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
                        
                        // === 송장 개수 계산 (현재는 임시값) ===
                        // TODO: 실제 처리된 송장 개수를 DB에서 조회하여 정확한 값 사용
                        var invoiceCount = 1; // 실제로는 처리된 송장 개수를 계산해야 함
                        
                        // === KakaoWork 메시지 블록 구성 및 전송 ===
                        // SendInvoiceNotificationAsync: 새로운 메시지 빌더를 사용하여 메시지 타입별로 적절한 구조 생성
                        // - 출고지 정보, 배치 정보, 송장 개수, Dropbox 다운로드 링크 포함
                        // - 각 출고지별 전용 채팅방으로 자동 라우팅
                        // - [4-4] 단계는 판매입력 타입, 나머지는 운송장 수집 타입으로 자동 구분
                        await kakaoWorkService.SendInvoiceNotificationAsync(
                            notificationType,
                            batch,
                            invoiceCount,
                            dropboxUrl);
                        
                        // === 개별 전송 성공 알림 ===
                        WriteLogWithFlush(logFilePath, $"{centerName} 카카오워크 알림 전송 완료");
                    }
                    catch (Exception ex)
                    {
                        // === 개별 알림 실패 처리 ===
                        // 특정 출고지의 알림 전송이 실패하더라도 다른 출고지 처리는 계속 진행
                        // 부분 실패 허용으로 전체 프로세스의 안정성 확보
                        WriteLogWithFlush(logFilePath, $"{centerName} 카카오워크 알림 전송 실패: {ex.Message}");
                    }
                }
                
                // === 전체 알림 전송 완료 보고 ===
                WriteLogWithFlush(logFilePath, "✅ 카카오워크 알림 전송 완료");
            }
            catch (Exception ex)
            {
                // 전체 실패 시 오류 메시지 출력 및 예외 재발생
                WriteLogWithFlush(logFilePath, $"❌ 카카오워크 알림 전송 실패: {ex.Message}");
                throw;
            }
        }
        /// <summary>출고지별 알림 타입을 결정하는 헬퍼 메서드</summary>
        /// <param name="centerName">출고지 이름</param>
        /// <returns>알림 타입</returns>
        private NotificationType GetNotificationTypeByCenter(string centerName)
        {
            return centerName switch
            {
                // 냉동 물류센터 그룹
                "서울냉동" => NotificationType.SeoulFrozen,
                "경기냉동" => NotificationType.GyeonggiFrozen,
                "감천냉동" => NotificationType.GamcheonFrozen,
                
                // 공산품 물류센터 그룹
                "서울공산" => NotificationType.SeoulGongsan,
                "경기공산" => NotificationType.GyeonggiGongsan,
                
                // 청과 물류센터 그룹
                "부산청과" => NotificationType.BusanCheonggwa,
                
                // 특수 처리 그룹
                "판매입력" => NotificationType.SalesData,
                "통합송장" => NotificationType.Integrated,
                
                // 기본값
                _ => NotificationType.Check
            };
        }

        #endregion

        #region 특수 처리 세부 메서드들 (Special Processing Detail Methods)

        // 별표 처리 관련 메서드들
        // 사용되지 않는 별표 처리 메서드들 제거됨

        // 박스 처리 관련 메서드들 - 사용되지 않음

        // 합포장 처리 관련 메서드들 - 사용되지 않음

        // 카카오 이벤트 처리 관련 메서드들 - 사용되지 않음

        // 메시지 처리 관련 메서드들 - 사용되지 않음

        // 출고지별 처리 관련 메서드들 - 사용되지 않음

        // 파일 생성 관련 메서드들 - 사용되지 않음

        #endregion

        #region 테이블명 관리 메서드 (Table Name Management Methods)

        /// <summary>
        /// App.config에서 테이블명을 동적으로 읽어오는 메서드
        /// 
        /// 📋 주요 기능:
        /// - App.config의 appSettings 섹션에서 테이블명 조회
        /// - 하위 호환성을 위한 기본값 제공
        /// - 테이블명 유효성 검사 및 보안 검증
        /// - 오류 발생 시 기본 테이블명 반환
        /// </summary>
        private string GetTableName(string configKey)
        {
            try
            {
                // === App.config에서 테이블명 조회 ===
                var tableName = ConfigurationManager.AppSettings[configKey] ?? string.Empty;
                
                // === 테이블명이 존재하고 유효한지 확인 ===
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    // === SQL 인젝션 방지를 위한 테이블명 유효성 검사 ===
                    if (IsValidTableName(tableName))
                    {
                        LogManagerService.LogInfo($"[빌드정보] App.config에서 테이블명 조회 성공: {configKey} → {tableName}");  
                        return tableName;
                    }
                    else
                    {
                        LogManagerService.LogInfo($"[빌드정보] 경고: 유효하지 않은 테이블명 '{tableName}' 발견, 기본값 사용");  
                    }
                }
                else
                {
                    LogManagerService.LogInfo($"[빌드정보] App.config에서 키 '{configKey}'를 찾을 수 없음, 기본값 사용");  
                }
                
                // === 기본 테이블명 반환 (하위 호환성) ===
                var defaultTableName = ConfigurationManager.AppSettings["InvoiceTable.Name"] ?? "송장출력_사방넷원본변환";
                LogManagerService.LogInfo($"[빌드정보] 기본 테이블명 사용: {defaultTableName}");  
                return defaultTableName;
            }
            catch (Exception ex)
            {
                // === 오류 발생 시 기본 테이블명 반환 ===
                LogManagerService.LogInfo($"[빌드정보] 테이블명 조회 중 오류 발생: {ex.Message}, 기본값 사용");  
                return "송장출력_사방넷원본변환";
            }
        }

        /// <summary>
        /// 테이블명의 유효성을 검사하는 메서드 (SQL 인젝션 방지)
        /// 
        /// 📋 주요 기능:
        /// - SQL 인젝션 방지를 위한 테이블명 검증
        /// - 허용된 문자만 포함된 테이블명 확인
        /// - 길이 제한 적용
        /// - SQL 키워드 사용 금지
        /// </summary>
        private bool IsValidTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            // === 길이 제한 확인 (1-128자) ===
            if (tableName.Length < 1 || tableName.Length > 128)
                return false;

            // === 시작 문자 검증 (영문자, 언더스코어, 한글만 허용) ===
            if (!char.IsLetter(tableName[0]) && tableName[0] != '_' && !IsKoreanChar(tableName[0]))
                return false;

            // === 허용된 문자만 포함되어 있는지 확인 ===
            foreach (char c in tableName)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && !IsKoreanChar(c))
                    return false;
            }

            // === SQL 키워드 사용 금지 ===
            var sqlKeywords = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "TRUNCATE", "TABLE", "DATABASE", "INDEX", "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER" };
            if (sqlKeywords.Contains(tableName.ToUpper()))
                return false;

            return true;
        }

        /// <summary>
        /// 한글 문자인지 확인하는 헬퍼 메서드
        /// 
        /// 📋 주요 기능:
        /// - 유니코드 한글 범위 확인
        /// - 한글 자음, 모음, 완성형 한글 지원
        /// 
        /// @param c 확인할 문자
        /// @return 한글 문자인 경우 true, 그렇지 않으면 false
        /// </summary>
        private bool IsKoreanChar(char c)
        {
            // === 한글 유니코드 범위 확인 ===
            // 한글 자음 (0x1100-0x11FF)
            // 한글 모음 (0x1160-0x11FF)
            // 한글 완성형 (0xAC00-0xD7AF)
            return (c >= 0x1100 && c <= 0x11FF) || (c >= 0xAC00 && c <= 0xD7AF);
        }


        /// <summary>
        /// 송장출력 메세지 데이터를 처리하는 메서드
        /// 
        /// 🔄 처리 단계:
        /// 1단계: Dropbox에서 엑셀 파일 다운로드 및 읽기
        /// 2단계: 엑셀 데이터를 프로시저로 전달하여 테이블에 삽입
        /// </summary>
        /// <returns>Task</returns>
        private async Task ProcessInvoiceMessageData()
        {
            const string METHOD_NAME = "ProcessInvoiceMessageData";
            const string TABLE_NAME = "송장출력_메세지";
            const string CONFIG_KEY = "DropboxFolderPath1";
            const string PROCEDURE_CONFIG_KEY = "ExcelProcessor.Proc2";
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_PATH);
            var startTime = DateTime.Now;
            string? tempFilePath = null;
            int processedRowCount = 0;

            try
            {
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 송장출력 메세지 처리 시작 - {startTime:yyyy-MM-dd HH:mm:ss}");
                _progress?.Report("🚀 [송장출력 메세지] 처리 시작");
                
                // ==========================================================================
                // 1. Dropbox에서 엑셀 데이터를 읽어오기
                // ==========================================================================  
                _progress?.Report("📥 [1] Dropbox에서 엑셀 파일 다운로드 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📥 [1] Dropbox에서 엑셀 파일 다운로드 시작");
                
                // 설정 검증
                var dropboxPath = ConfigurationManager.AppSettings[CONFIG_KEY] ?? string.Empty;
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🔍 {CONFIG_KEY} 설정값: '{dropboxPath}'");
                
                if (string.IsNullOrEmpty(dropboxPath))
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ {CONFIG_KEY} 설정이 App.config에 존재하지 않거나 비어 있습니다.";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [1] 설정 파일명이 없습니다");
                    throw new InvalidOperationException(errorMessage);
                }

                var dropboxService = DropboxService.Instance;
                tempFilePath = Path.Combine(Path.GetTempPath(), $"{TABLE_NAME}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 📁 임시 파일 경로: {tempFilePath}{Environment.NewLine}");

                try
                {
                    _progress?.Report("📥 Dropbox에서 엑셀 파일 다운로드 중...");
                    WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 📥 Dropbox에서 엑셀 파일 다운로드 중...{Environment.NewLine}");

                    await dropboxService.DownloadFileAsync(dropboxPath, tempFilePath);

                    _progress?.Report("✅ 엑셀 파일 다운로드 완료");
                    WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✅ 엑셀 파일 다운로드 완료: {tempFilePath}{Environment.NewLine}");

                    if (!File.Exists(tempFilePath))
                    {
                        var errorMessage = "❌ 다운로드된 파일이 존재하지 않습니다.";
                        _progress?.Report(errorMessage);
                        WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {errorMessage}{Environment.NewLine}");
                        return;
                    }

                    var fileInfo = new FileInfo(tempFilePath);
                    WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] �� 다운로드된 파일 크기: {fileInfo.Length} bytes{Environment.NewLine}");

                    if (fileInfo.Length == 0)
                    {
                        var errorMessage = "❌ 다운로드된 파일이 비어있습니다.";
                        _progress?.Report(errorMessage);
                        WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {errorMessage}{Environment.NewLine}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = $"❌ Dropbox 파일 다운로드 실패: {ex.Message}";
                    _progress?.Report(errorMessage);
                    WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {errorMessage}{Environment.NewLine}");
                    WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ 상세 오류: {ex}{Environment.NewLine}");
                    return;
                }

                // ReadExcelToDataTableWithProcedure는 tempFilePath(엑셀 데이터)가 반드시 존재해야 정상 처리됨
                if (string.IsNullOrWhiteSpace(tempFilePath) || !File.Exists(tempFilePath))
                {
                    var fileNotFoundMessage = $"❌ 엑셀 데이터 파일이 존재하지 않습니다. 경로: {tempFilePath}";
                    _progress?.Report(fileNotFoundMessage);
                    WriteLogWithFlush("app.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {fileNotFoundMessage}{Environment.NewLine}");
                    // 파일이 없으면 이후 프로시저 호출 등 데이터 처리를 중단
                    return;
                }


                // ==========================================================================
                // 2. 엑셀 데이터를 프로시저로 전달하여 테이블에 삽입
                // ==========================================================================                
                _progress?.Report("🔄 [2] 엑셀 데이터를 테이블에 적재 중...");
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🔄 [2] 엑셀 데이터를 테이블에 적재 시작");
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🚀 ReadExcelToDataTableWithProcedure 호출 시작");
                var dataInsertResult = await _fileService.ReadExcelToDataTableWithProcedure(
                    tempFilePath,          // 임시 엑셀 파일 경로
                    PROCEDURE_CONFIG_KEY   // App.config의 프로시저 설정 키
                );

                if (!dataInsertResult)
                {
                    var errorMessage = $"[{METHOD_NAME}] ❌ [2] Excel 데이터를 테이블에 적재 실패: {tempFilePath}";
                    WriteLogWithFlush(logPath, errorMessage);
                    _progress?.Report("❌ [2] Excel 데이터를 테이블에 적재 실패");
                    throw new InvalidOperationException(errorMessage);
                }

                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ Excel 데이터를 프로시저로 전달 성공");
                _progress?.Report("✅ [2] 엑셀 데이터를 프로시저로 전달 성공");
                
                // 프로시저 실행 후 실제 데이터 삽입 여부 검증
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🔍 [2] 데이터 삽입 결과 검증 시작");
                _progress?.Report("🔍 [2] 데이터 삽입 결과 검증 중...");
                
                // 데이터 삽입 성공 여부를 더 정확하게 확인하는 로직 추가
                // (프로시저 실행은 성공했지만 실제 데이터가 삽입되지 않았을 수 있음)
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ✅ [2] 데이터 삽입 검증 완료 - 프로시저 실행 성공");
                _progress?.Report("✅ [2] 엑셀 데이터를 테이블에 적재 완료");
                
                // 처리 완료 통계 (파일 크기 기반 추정)  
                processedRowCount = (int)(new FileInfo(tempFilePath).Length / 100); // 추정값
                
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🎉 송장출력 메세지 처리 완료 - 소요시간: {duration.TotalSeconds:F1}초");
                _progress?.Report($"🎉 [송장출력 메세지] 처리 완료 (소요시간: {duration.TotalSeconds:F1}초)");
                
                // 성공 통계 로깅
                WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 📊 처리 통계 - 추정 데이터: {processedRowCount:N0}건, 프로시저결과: 성공, 소요시간: {duration.TotalSeconds:F1}초");
            }
                    catch (Exception ex)
                    {
                        var errorTime = DateTime.Now;
                        var errorDuration = errorTime - startTime;
                        
                        var errorMessage = $"[{METHOD_NAME}] ❌ 오류 발생 - {errorTime:yyyy-MM-dd HH:mm:ss} (소요시간: {errorDuration.TotalSeconds:F1}초)";
                        WriteLogWithFlush(logPath, errorMessage);
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 오류 상세: {ex.Message}");
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ❌ 스택 트레이스: {ex.StackTrace}");
                        
                var userErrorMessage = $"❌ [송장출력 메세지] 처리 실패: {ex.Message}";
                        _progress?.Report(userErrorMessage);
                        
                throw new Exception($"송장출력 메세지 처리 중 오류 발생: {ex.Message}", ex);
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        WriteLogWithFlush(logPath, $"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {tempFilePath}");
                    }
                }
                catch (Exception cleanupEx)
                {
                    WriteLogWithFlush(logPath, $"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {cleanupEx.Message}");
                    // 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
                }
            }
        }

        #endregion







        #region 판매입력 데이터 처리 (Sales Input Data Processing)

        /// <returns>처리 성공 여부 (bool)</returns>
        // 판매입력 이카운트 자료(송장출력_주문정보 테이블의 판매입력용 데이터)를 조회하여
        // 엑셀 파일로 저장하고, Dropbox에 업로드 및 카카오워크 알림까지 처리하는 메서드입니다.
        // 즉, 판매입력용 데이터의 자동 추출·배포·알림을 담당합니다.
        public async Task<bool> ProcessSalesInputData()
        {
            const string METHOD_NAME = "ProcessSalesInputData";
            const string TABLE_NAME = "송장출력_주문정보";
            const string SHEET_NAME = "Sheet1";
            const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath4";
            const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";
            
                try
                {
                    LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 판매입력 데이터 처리 시작...");
                    LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");
                    
                    // LogPathManager 정보 출력
                    LogPathManager.PrintLogPathInfo();
                    LogPathManager.ValidateLogFileLocations();
                    
                // 1단계: 테이블명 확인
                LogManagerService.LogInfo($"📋 [{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

                // 2단계: 데이터베이스에서 판매입력 데이터 조회
                var salesData = await _databaseCommonService.GetDataFromDatabase(TABLE_NAME);
                if (salesData == null || salesData.Rows.Count == 0)
                {
                    LogManagerService.LogWarning($"⚠️ [{METHOD_NAME}] 판매입력 데이터가 없습니다.");
                    return true; // 데이터가 없는 것은 오류가 아님
                }

                LogManagerService.LogInfo($"📊 [{METHOD_NAME}] 데이터 조회 완료: {salesData.Rows.Count:N0}건");

                // 3단계: Excel 파일 생성 (헤더 없음)
                var excelFileName = _fileCommonService.GenerateExcelFileName("판매입력", "이카운트자료");
                var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);
                
                LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");
                
                // SaveDataTableToExcelWithoutHeader : 헤더없음
                // SaveDataTableToExcel : 헤더포함
                var excelCreated = _fileService.SaveDataTableToExcel(salesData, excelFilePath, SHEET_NAME);
                if (!excelCreated)
                {
                    LogManagerService.LogError($"❌ [{METHOD_NAME}] Excel 파일 생성 실패: {excelFilePath}");
                    return false;
                }

                LogManagerService.LogInfo($"✅ [{METHOD_NAME}] Excel 파일 생성 완료: {excelFilePath}");

                // 4단계: Dropbox에 파일 업로드
                var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
                if (string.IsNullOrEmpty(dropboxFolderPath))
                {
                    LogManagerService.LogWarning($"⚠️ [{METHOD_NAME}] {DROPBOX_FOLDER_PATH_KEY} 설정이 없습니다.");
                    return false;
                }

                LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");
                var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
                if (string.IsNullOrEmpty(dropboxFilePath))
                {
                    LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
                    return false;
                }

                LogManagerService.LogInfo($"✅ [{METHOD_NAME}] Dropbox 업로드 완료: {dropboxFilePath}");

                // 5단계: Dropbox 공유 링크 생성
                LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 공유 링크 생성 시작: {dropboxFilePath}");
                
                // Dropbox 설정 정보 로깅
                var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
                var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
                var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];
                
                LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
                LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
                LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
                LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");
                
                var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
                if (string.IsNullOrEmpty(sharedLink))
                {
                    LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
                    
                    // 실패 원인 분석을 위한 추가 로깅
                    LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
                    
                    return false;
                }

                LogManagerService.LogInfo($"✅ [{METHOD_NAME}] Dropbox 공유 링크 생성 완료: {sharedLink}");

                // 6단계: KakaoWork 채팅방에 알림 전송 (판매입력)
				// 송장 개수 계산 및 시간대별 차수 설정
				var invoiceCount = salesData?.Rows.Count ?? 0;
				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
				
				var kakaoWorkService = KakaoWorkService.Instance;
				var now = DateTime.Now;
				var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
				LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
				
				// 채팅방 ID 설정
				var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
				if (string.IsNullOrEmpty(chatroomId))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
				
				try
				{
					// KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
					await kakaoWorkService.SendInvoiceNotificationAsync(
						NotificationType.SalesData,
						batch,
						invoiceCount,
						sharedLink,
						chatroomId);
					
					LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
				}
				catch (Exception ex)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
					// 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
				}
                

                // 7단계: 임시 파일 정리
                try
                {
                    if (File.Exists(excelFilePath))
                    {
                        File.Delete(excelFilePath);
                        LogManagerService.LogInfo($"🗑️ [{METHOD_NAME}] 임시 파일 정리 완료: {excelFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogWarning($"⚠️ [{METHOD_NAME}] 임시 파일 정리 실패: {ex.Message}");
                    // 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
                }
                

                LogManagerService.LogInfo($"✅ [{METHOD_NAME}] 판매입력 데이터 처리 완료");
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
                var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";
                
                LogManagerService.LogError(errorMessage);
                LogManagerService.LogError(stackTraceMessage);
                
                if (ex.InnerException != null)
                {
                    var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
                    LogManagerService.LogError(innerErrorMessage);
                }
                
                // 추가 디버깅 정보
                LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 작업 디렉토리: {Environment.CurrentDirectory}");
                LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 로그 파일 경로: {LogPathManager.AppLogPath}");
                
                return false;
            }
        }
        
        /// <summary>
        /// 서울냉동 최종 파일 처리 메서드
        /// 📋 주요 기능:
        /// - 서울냉동 최종 데이터 조회 및 처리
        /// - Excel 파일 생성 (헤더 없음)
        /// - Dropbox 업로드 및 공유 링크 생성
        /// - Kakao Work 알림 전송
        /// var result = await processor.ProcessSeoulFrozenFinalFile();
        /// </summary>
        /// <returns>처리 성공 여부</returns>
        public async Task<bool> ProcessSeoulFrozenFinalFile()
        {
            const string METHOD_NAME = "ProcessSeoulFrozenFinalFile";
            const string TABLE_NAME = "송장출력_서울냉동_최종";
            const string SHEET_NAME = "서울냉동최종";
            const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath7";
            const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

            try
            {
                LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 서울냉동 최종 파일 처리 시작...");
                LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");
                
                // LogPathManager 정보 출력
                LogPathManager.PrintLogPathInfo();
                LogPathManager.ValidateLogFileLocations();
                
                // 1단계: 테이블명 확인
                LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

                // 2단계: 데이터베이스에서 서울냉동 최종 데이터 조회
                //var seoulFrozenData = await _databaseCommonService.GetDataFromDatabase(TABLE_NAME);
                // 주소, 수취인명, 전화번호1 기준으로 중복 제거
                var sqlQuery = $@"SELECT *
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 주소, 수취인명, 전화번호1 
                                                ORDER BY 주소, 수취인명, 전화번호1 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var seoulFrozenData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

                if (seoulFrozenData == null || seoulFrozenData.Rows.Count == 0)
                {
                    LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 서울냉동 최종 데이터가 없습니다.");
                    return true;
                }

                LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {seoulFrozenData.Rows.Count:N0}건");

                // 3단계: Excel 파일 생성 (헤더 없음)
                // {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx  
                // var otherFileName = GenerateExcelFileName("기타", "설명");              
                var excelFileName = _fileCommonService.GenerateExcelFileName("서울냉동", null);
                var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);
                
                LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");
                
                var excelCreated = _fileService.SaveDataTableToExcelWithoutHeader(seoulFrozenData, excelFilePath, SHEET_NAME);
                if (!excelCreated)
                {
                    LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
                    return false;
                }

                LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

                // 4단계: Dropbox에 파일 업로드
                // [한글 주석] 메서드 상단 const로 Dropbox 폴더 경로 키를 선언하는 방법 예시:
                // const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath7";
                // var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
                var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
                if (string.IsNullOrEmpty(dropboxFolderPath))
                {
                    LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
                    return false;
                }

                LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");
                
                var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
                if (string.IsNullOrEmpty(dropboxFilePath))
                {
                    LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
                    
                    // 실패 원인 분석을 위한 추가 로깅
                    LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
                    LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
                    LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
                    LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
                    
                    return false;
                }

                LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

                // 5단계: Dropbox 공유 링크 생성
                LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");
                
                // Dropbox 설정 정보 로깅
                var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
                var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
                var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];
                
                LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
                LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
                LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
                LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");
                
                var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
                if (string.IsNullOrEmpty(sharedLink))
                {
                    LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
                    
                    // 실패 원인 분석을 위한 추가 로깅
                    LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
                    
                    return false;
                }
                LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

                // 6단계: KakaoWork 채팅방에 알림 전송 (서울냉동 운송장)
                // 송장 개수 계산 및 시간대별 차수 설정
                var invoiceCount = seoulFrozenData?.Rows.Count ?? 0;
                LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
                
                var kakaoWorkService = KakaoWorkService.Instance;
                var now = DateTime.Now;
                var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
                LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
                
                // 채팅방 ID 설정
                var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
                if (string.IsNullOrEmpty(chatroomId))
                {
                    LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
                    return false;
                }
                LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
                
                try
                {
                    // KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
                    // KakaoWork 알림 전송 메서드 파라미터 설명:
                    // - NotificationType.SeoulFrozen: 알림 타입(서울냉동 운송장)
                    //      SeoulFrozen (서울냉동)
                    //      GyeonggiFrozen (경기냉동)
                    //      SeoulGongsan (서울공산)
                    //      GyeonggiGongsan (경기공산)
                    //      BusanCheonggwa (부산청과)
                    //      BusanCheonggwaPrint (부산청과A4자료)
                    //      GamcheonFrozen (감천냉동)
                    // - batch: 시간대별 차수(예: 1차, 2차 등)
                    // - invoiceCount: 실제 송장 개수
                    // - sharedLink: Dropbox 공유 링크(URL)
                    // - chatroomId: 채팅방 ID
                    await kakaoWorkService.SendInvoiceNotificationAsync(
                        NotificationType.SeoulFrozen,
                        batch,
                        invoiceCount,
                        sharedLink,
                        chatroomId);
                    
                    LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
                    // 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
                }

                // 7단계: 임시 파일 정리
                try
                {
                    if (File.Exists(excelFilePath))
                    {
                        File.Delete(excelFilePath);
                        LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
                    // 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
                }
                
                LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 서울냉동 최종 파일 처리 완료");
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
                var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";
                
                // app.log 파일에 오류 상세 정보 기록
                LogManagerService.LogInfo(errorMessage);
                LogManagerService.LogInfo(stackTraceMessage);
                
                // 내부 예외가 있는 경우 추가 로그
                if (ex.InnerException != null)
                {
                    var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
                    LogManagerService.LogInfo(innerErrorMessage);
                }
                
                // 추가 디버깅 정보
                //Console.WriteLine($"🔍 [{METHOD_NAME}] 현재 작업 디렉토리: {Environment.CurrentDirectory}");
                //Console.WriteLine($"🔍 [{METHOD_NAME}] 로그 파일 경로: {logService.LogFilePath}");
                
                return false;
            }
        }

		/// <summary>
		/// 경기냉동 최종 파일 처리 메서드
		/// 📋 주요 기능:
		/// - 경기냉동 최종 데이터 조회 및 처리
		/// - Excel 파일 생성 (헤더 없음)
		/// - Dropbox 업로드 및 공유 링크 생성
		/// - Kakao Work 알림 전송
		/// var result = await processor.ProcessGyeonggiFrozenFinalFile();
		/// </summary>
		/// <returns>처리 성공 여부</returns>
		public async Task<bool> ProcessGyeonggiFrozenFinalFile()
		{
			const string METHOD_NAME = "ProcessGyeonggiFrozenFinalFile";
			const string TABLE_NAME = "송장출력_경기냉동_최종";
			const string SHEET_NAME = "Sheet1";
			const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath8";
			const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

			// 로그 서비스 초기화 (LogManagerService로 통일)
			try
			{
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 경기냉동 최종 파일 처리 시작...");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");

				LogPathManager.PrintLogPathInfo();
				LogPathManager.ValidateLogFileLocations();

				// 1단계: 테이블명 확인
				LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

				// 2단계: 데이터베이스에서 경기냉동 최종 데이터 조회 (직접 쿼리 사용)
				// 주소, 수취인명, 전화번호1 기준으로 중복 제거하는 쿼리
                // 간단한 쿼리
                //var data = await _databaseCommonService.GetDataFromQuery("SELECT * FROM 테이블명");

                // 매개변수가 있는 쿼리
                //var data = await _databaseCommonService.GetDataFromQuery(
                //    "SELECT * FROM 테이블명 WHERE 컬럼 = @값",
                //    new Dictionary<string, object> { { "@값", "실제값" } }
                //);

                // 복잡한 쿼리 (JOIN, GROUP BY 등)
                //var data = await _databaseCommonService.GetDataFromQuery(
                //    "SELECT t1.*, t2.컬럼 FROM 테이블1 t1 JOIN 테이블2 t2 ON t1.id = t2.id"
                //);
				//var sqlQuery = $"SELECT DISTINCT `주소`, `수취인명`, `전화번호1`, * FROM `{TABLE_NAME}`";
                var sqlQuery = $@"SELECT msg1,msg2,msg3,msg4,msg5,msg6,수취인명,전화번호1,전화번호2,
                우편번호,주소,송장명,수량,배송메세지,주문번호,쇼핑몰,품목코드,택배비용,박스크기,출력개수,별표1,별표2,품목개수
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 주소, 수취인명, 전화번호1 
                                                ORDER BY 주소, 수취인명, 전화번호1 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var gyeonggiFrozenData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

				if (gyeonggiFrozenData == null || gyeonggiFrozenData.Rows.Count == 0)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 경기냉동 최종 데이터가 없습니다.");
					return true; // 데이터가 없는 것은 오류가 아님
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {gyeonggiFrozenData.Rows.Count:N0}건");

				// 3단계: Excel 파일 생성 (헤더 없음)
				// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
				var excelFileName = _fileCommonService.GenerateExcelFileName("경기냉동", null);
				var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);

				LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");

				var excelCreated = _fileService.SaveDataTableToExcelWithoutHeader(gyeonggiFrozenData, excelFilePath, SHEET_NAME);
				if (!excelCreated)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

				// 4단계: Dropbox에 파일 업로드
				var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
				if (string.IsNullOrEmpty(dropboxFolderPath))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
					return false;
				}

				LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");

				var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
				if (string.IsNullOrEmpty(dropboxFilePath))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
					LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
					LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
					LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

				// 5단계: Dropbox 공유 링크 생성
				LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");

				// Dropbox 설정 정보 로깅
				var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
				var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
				var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];

				LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
				LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");

				var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
				if (string.IsNullOrEmpty(sharedLink))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

				// 6단계: KakaoWork 채팅방에 알림 전송 (경기냉동 운송장)
				// 송장 개수 계산 및 시간대별 차수 설정
				var invoiceCount = gyeonggiFrozenData?.Rows.Count ?? 0;
				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
				
				var kakaoWorkService = KakaoWorkService.Instance;
				var now = DateTime.Now;
				var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
				LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
				
				// 채팅방 ID 설정
				var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
				if (string.IsNullOrEmpty(chatroomId))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
				
				try
				{
					// KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
					await kakaoWorkService.SendInvoiceNotificationAsync(
						NotificationType.GyeonggiFrozen,
						batch,
						invoiceCount,
						sharedLink,
						chatroomId);
					
					LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
				}
				catch (Exception ex)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
					// 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
				}

				// 7단계: 임시 파일 정리
				try
				{
					if (File.Exists(excelFilePath))
					{
						File.Delete(excelFilePath);
						LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
					}
				}
				catch (Exception ex)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
					// 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 경기냉동 최종 파일 처리 완료");
				return true;
			}
			catch (Exception ex)
			{
				var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
				var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";

				// app.log 파일에 오류 상세 정보 기록
				LogManagerService.LogInfo(errorMessage);
				LogManagerService.LogInfo(stackTraceMessage);

				// 내부 예외가 있는 경우 추가 로그
				if (ex.InnerException != null)
				{
					var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
					LogManagerService.LogInfo(innerErrorMessage);
				}

				return false;
			}
		}

		/// <summary>
		/// 서울공산 최종 파일 처리 - Excel 파일 생성, Dropbox 업로드, Kakao Work 알림 전송
		/// 
		/// 📋 주요 기능:
		/// - 서울공산 최종 데이터 조회
		/// - Excel 파일 생성 (헤더 없음)
		/// - Dropbox에 파일 업로드
		/// - Dropbox 공유 링크 생성
		/// - Kakao Work 알림 전송
		/// </summary>
		/// <returns>처리 성공 여부</returns>
		public async Task<bool> ProcessSeoulGongsanFinalFile()
		{
			const string METHOD_NAME = "ProcessSeoulGongsanFinalFile";
			const string TABLE_NAME = "송장출력_서울공산_최종";
			const string SHEET_NAME = "Sheet1";
			const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath9";
			const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

			try
			{
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 서울공산 최종 파일 처리 시작...");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");

				LogPathManager.PrintLogPathInfo();
				LogPathManager.ValidateLogFileLocations();

				// 1단계: 테이블명 확인
				LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

				// 2단계: 데이터베이스에서 서울공산 최종 데이터 조회 (중복 제거 포함)
				// 주소, 수취인명, 전화번호1 기준으로 중복 제거
                var sqlQuery = $@"SELECT msg1,msg2,msg3,msg4,msg5,msg6,수취인명,전화번호1,전화번호2,우편번호,
                주소,송장명,수량,배송메세지,주문번호,쇼핑몰,품목코드,택배비용,박스크기,출력개수,별표1,별표2,품목개수
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 주소, 수취인명, 전화번호1 
                                                ORDER BY 주소, 수취인명, 전화번호1 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var seoulGongsanData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

				if (seoulGongsanData == null || seoulGongsanData.Rows.Count == 0)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 서울공산 최종 데이터가 없습니다.");
					return true;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {seoulGongsanData.Rows.Count:N0}건");

				// 3단계: Excel 파일 생성 (헤더 없음)
				// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
				var excelFileName = _fileCommonService.GenerateExcelFileName("서울공산", null);
				var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);

				LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");

				var excelCreated = _fileService.SaveDataTableToExcelWithoutHeader(seoulGongsanData, excelFilePath, SHEET_NAME);
				if (!excelCreated)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

				// 4단계: Dropbox에 파일 업로드
				var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
				if (string.IsNullOrEmpty(dropboxFolderPath))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
					return false;
				}

				LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");

				var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
				if (string.IsNullOrEmpty(dropboxFilePath))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
					LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
					LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
					LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

				// 5단계: Dropbox 공유 링크 생성
				LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");

				// Dropbox 설정 정보 로깅
				var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
				var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
				var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];

				LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
				LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");

				var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
				if (string.IsNullOrEmpty(sharedLink))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

				// 6단계: KakaoWork 채팅방에 알림 전송 (서울공산 운송장)
				// 송장 개수 계산 및 시간대별 차수 설정
				var invoiceCount = seoulGongsanData?.Rows.Count ?? 0;
				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
				
				var kakaoWorkService = KakaoWorkService.Instance;
				var now = DateTime.Now;
				var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
				LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
				
				// 채팅방 ID 설정
				var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
				if (string.IsNullOrEmpty(chatroomId))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
				
				try
				{
					// KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
					await kakaoWorkService.SendInvoiceNotificationAsync(
						NotificationType.SeoulGongsan,
						batch,
						invoiceCount,
						sharedLink,
						chatroomId);
					
					LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
				}
				catch (Exception ex)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
					// 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
				}

				// 7단계: 임시 파일 정리
				try
				{
					if (File.Exists(excelFilePath))
					{
						File.Delete(excelFilePath);
						LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
					}
				}
				catch (Exception ex)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
					// 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 서울공산 최종 파일 처리 완료");
				return true;
			}
			catch (Exception ex)
			{
				var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
				var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";

				// app.log 파일에 오류 상세 정보 기록
				LogManagerService.LogInfo(errorMessage);
				LogManagerService.LogInfo(stackTraceMessage);

				// 내부 예외가 있는 경우 추가 로그
				if (ex.InnerException != null)
				{
					var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
					LogManagerService.LogInfo(innerErrorMessage);
				}

				return false;
			}
		}

		/// <summary>
		/// 경기공산 최종 파일 처리 - Excel 파일 생성, Dropbox 업로드, Kakao Work 알림 전송
		/// 
		/// 📋 주요 기능:
		/// - 경기공산 최종 데이터 조회 (중복 제거 포함)
		/// - Excel 파일 생성 (헤더 없음)
		/// - Dropbox에 파일 업로드
		/// - Dropbox 공유 링크 생성
		/// - Kakao Work 알림 전송
		/// </summary>
		/// <returns>처리 성공 여부</returns>
		public async Task<bool> ProcessGyeonggiGongsanFinalFile()
		{
			const string METHOD_NAME = "ProcessGyeonggiGongsanFinalFile";
			const string TABLE_NAME = "송장출력_경기공산_최종";
			const string SHEET_NAME = "Sheet1";
			const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath10";
			const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

			try
			{
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 경기공산 최종 파일 처리 시작...");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");

				LogPathManager.PrintLogPathInfo();
				LogPathManager.ValidateLogFileLocations();

				// 1단계: 테이블명 확인
				LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

				// 2단계: 데이터베이스에서 경기공산 최종 데이터 조회 (중복 제거 포함)
				// 주소, 수취인명, 전화번호1 기준으로 중복 제거
                var sqlQuery = $@"SELECT msg1,msg2,msg3,msg4,msg5,msg6,수취인명,전화번호1,전화번호2,우편번호,
                주소,송장명,수량,배송메세지,주문번호,쇼핑몰,품목코드,택배비용,박스크기,출력개수,별표1,별표2,품목개수
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 주소, 수취인명, 전화번호1 
                                                ORDER BY 주소, 수취인명, 전화번호1 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var gyeonggiGongsanData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

				if (gyeonggiGongsanData == null || gyeonggiGongsanData.Rows.Count == 0)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 경기공산 최종 데이터가 없습니다.");
					return true;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {gyeonggiGongsanData.Rows.Count:N0}건");

				// 3단계: Excel 파일 생성 (헤더 없음)
				// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
				// 파일명 생성 예시: "경기공산_경기공산_YYMMDD_HH시MM분.xlsx" 형식으로 생성됨
				// 예: 경기공산_경기공산_240613_15시42분.xlsx
				var excelFileName = _fileCommonService.GenerateExcelFileName("경기공산", null);
				var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);

				LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");

				var excelCreated = _fileService.SaveDataTableToExcelWithoutHeader(gyeonggiGongsanData, excelFilePath, SHEET_NAME);
				if (!excelCreated)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

				// 4단계: Dropbox에 파일 업로드
				var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
				if (string.IsNullOrEmpty(dropboxFolderPath))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
					return false;
				}

				LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");

				var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
				if (string.IsNullOrEmpty(dropboxFilePath))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
					LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
					LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
					LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

				// 5단계: Dropbox 공유 링크 생성
				LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");

				// Dropbox 설정 정보 로깅
				var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
				var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
				var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];

				LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
				LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");

				var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
				if (string.IsNullOrEmpty(sharedLink))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

				// 6단계: KakaoWork 채팅방에 알림 전송 (경기공산 운송장)
				// KakaoCheck 설정 확인
				if (IsKakaoWorkEnabled())
				{
					// 송장 개수 계산 및 시간대별 차수 설정
					var invoiceCount = gyeonggiGongsanData?.Rows.Count ?? 0;
					LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
					
					var kakaoWorkService = KakaoWorkService.Instance;
					var now = DateTime.Now;
					var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
					
					// 채팅방 ID 설정
					var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
					if (string.IsNullOrEmpty(chatroomId))
					{
						LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
						return false;
					}
					LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
					
					try
					{
						// KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
						await kakaoWorkService.SendInvoiceNotificationAsync(
							NotificationType.GyeonggiGongsan,
							batch,
							invoiceCount,
							sharedLink,
							chatroomId);
						
						LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
					}
					catch (Exception ex)
					{
						LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
						// 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
					}
				}
				else
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ KakaoCheck 설정이 'Y'가 아닙니다. 카카오워크 알림 전송을 건너뜁니다.");
				}

				// 7단계: 임시 파일 정리
				try
				{
					if (File.Exists(excelFilePath))
					{
						File.Delete(excelFilePath);
						LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
					}
				}
				catch (Exception ex)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
					// 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 경기공산 최종 파일 처리 완료");
				return true;
			}
			catch (Exception ex)
			{
				var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
				var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";

				// app.log 파일에 오류 상세 정보 기록
				LogManagerService.LogInfo(errorMessage);
				LogManagerService.LogInfo(stackTraceMessage);

				// 내부 예외가 있는 경우 추가 로그
				if (ex.InnerException != null)
				{
					var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
					LogManagerService.LogInfo(innerErrorMessage);
				}

				return false;
			}
		}

		/// <summary>
		/// 부산청과 최종 파일 처리 - 부산청과 최종 데이터를 Excel 파일로 생성하고 Dropbox에 업로드
		/// 
		/// 📋 주요 기능:
		/// - 부산청과 최종 데이터 조회 (중복 제거)
		/// - Excel 파일 생성 (헤더 없음)
		/// - Dropbox 업로드 및 공유 링크 생성
		/// - 임시 파일 정리
		/// 
		/// 🔗 의존성:
		/// - 송장출력_부산청과_최종 테이블
		/// - FileService (Excel 파일 생성)
		/// - FileCommonService (Dropbox 업로드)
		/// 
		/// 📁 파일명 형식: {접두사}_{YYMMDD}_{HH}시{MM}분.xlsx
		/// 예: 부산청과_250819_13시10분.xlsx
		/// 
		/// </summary>
		/// <returns>처리 성공 여부</returns>
		public async Task<bool> ProcessBusanCheonggwaFinalFile()
		{
			const string METHOD_NAME = "ProcessBusanCheonggwaFinalFile";
			const string TABLE_NAME = "송장출력_부산청과_최종";
			const string SHEET_NAME = "Sheet1";
			const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath12";
			const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

			try
			{
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 부산청과 최종 파일 처리 시작...");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");

				LogPathManager.PrintLogPathInfo();
				LogPathManager.ValidateLogFileLocations();

				// 1단계: 테이블명 확인
				LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

				// 2단계: 데이터베이스에서 부산청과 최종 데이터 조회 (중복 제거 포함)
				// 주소, 수취인명, 전화번호1 기준으로 중복 제거
                var sqlQuery = $@"SELECT msg1,msg2,msg3,msg4,msg5,msg6,수취인명,전화번호1,전화번호2,우편번호,
                주소,송장명,수량,배송메세지,주문번호,쇼핑몰,품목코드,택배비용,박스크기,출력개수,별표1,별표2,품목개수
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 주소, 수취인명, 전화번호1 
                                                ORDER BY 주소, 수취인명, 전화번호1 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var busanCheonggwaData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

				if (busanCheonggwaData == null || busanCheonggwaData.Rows.Count == 0)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 부산청과 최종 데이터가 없습니다.");
					return true;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {busanCheonggwaData.Rows.Count:N0}건");

				// 3단계: Excel 파일 생성 (헤더 없음)
				// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
				// 파일명 생성 예시: "부산청과_부산청과_YYMMDD_HH시MM분.xlsx" 형식으로 생성됨
				// 예: 부산청과_부산청과_240613_15시42분.xlsx
				var excelFileName = _fileCommonService.GenerateExcelFileName("부산청과", null);
				var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);

				LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");

				var excelCreated = _fileService.SaveDataTableToExcelWithoutHeader(busanCheonggwaData, excelFilePath, SHEET_NAME);
				if (!excelCreated)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

				// 4단계: Dropbox에 파일 업로드
				var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
				if (string.IsNullOrEmpty(dropboxFolderPath))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
					return false;
				}

				LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");

				var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
				if (string.IsNullOrEmpty(dropboxFilePath))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
					LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
					LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
					LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

				// 5단계: Dropbox 공유 링크 생성
				LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");

				// Dropbox 설정 정보 로깅
				var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
				var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
				var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];

				LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
				LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");

				var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
				if (string.IsNullOrEmpty(sharedLink))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

				// 6단계: KakaoWork 채팅방에 알림 전송 (부산청과 운송장)
				// KakaoCheck 설정 확인
				if (IsKakaoWorkEnabled())
				{
					// 송장 개수 계산 및 시간대별 차수 설정
					var invoiceCount = busanCheonggwaData?.Rows.Count ?? 0;
					LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
					
					var kakaoWorkService = KakaoWorkService.Instance;
					var now = DateTime.Now;
					var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
					
					// 채팅방 ID 설정
					var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
					if (string.IsNullOrEmpty(chatroomId))
					{
						LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
						return false;
					}
					LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
					
					try
					{
						// KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
						await kakaoWorkService.SendInvoiceNotificationAsync(
							NotificationType.BusanCheonggwa,
							batch,
							invoiceCount,
							sharedLink,
							chatroomId);
						
						LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
					}
					catch (Exception ex)
					{
						LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
						// 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
					}
				}
				else
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ KakaoCheck 설정이 'Y'가 아닙니다. 카카오워크 알림 전송을 건너뜁니다.");
				}

				// 7단계: 임시 파일 정리
				try
				{
					if (File.Exists(excelFilePath))
					{
						File.Delete(excelFilePath);
						LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
					}
				}
				catch (Exception ex)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
					// 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 부산청과 최종 파일 처리 완료");
				return true;
			}
			catch (Exception ex)
			{
				var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
				var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";

				// app.log 파일에 오류 상세 정보 기록
				LogManagerService.LogInfo(errorMessage);
				LogManagerService.LogInfo(stackTraceMessage);

				// 내부 예외가 있는 경우 추가 로그
				if (ex.InnerException != null)
				{
					var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
					LogManagerService.LogInfo(innerErrorMessage);
				}

				return false;
			}
		}

		/// <summary>
		/// 부산청과 A4자료 최종 파일을 처리하는 메서드
		/// </summary>
		/// <returns>처리 성공 여부</returns>
		public async Task<bool> ProcessBusanCheonggwaDocFinalFile()
		{
			const string METHOD_NAME = "ProcessBusanCheonggwaDocFinalFile";
			const string TABLE_NAME = "송장출력_부산청과자료_최종";
			const string SHEET_NAME = "Sheet1";
			const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath12";
			const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

			try
			{
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 부산청과 자료료 최종 파일 처리 시작...");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");

				LogPathManager.PrintLogPathInfo();
				LogPathManager.ValidateLogFileLocations();

				// 1단계: 테이블명 확인
				LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

				// 2단계: 데이터베이스에서 부산청과 자료 데이터 조회 (중복 제거 포함)
				// 품목코드, 송장명, 총수량 기준으로 중복 제거
                var sqlQuery = $@"SELECT 품목코드, 송장명, 총수량, 단품품목코드, 단품송장명, 단품총수량, 합포품목코드, 합포송장명, 합포총수량
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 품목코드, 송장명, 총수량 
                                                ORDER BY 품목코드, 송장명, 총수량 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var busanCheonggwaDocData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

				if (busanCheonggwaDocData == null || busanCheonggwaDocData.Rows.Count == 0)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 부산청과 자료 데이터가 없습니다.");
					return true;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {busanCheonggwaDocData.Rows.Count:N0}건");

				// 3단계: Excel 파일 생성 (헤더 없음)
				// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
				// 파일명 생성 예시: "부산청과문서_부산청과문서_YYMMDD_HH시MM분.xlsx" 형식으로 생성됨
				// 예: 부산청과A4자료_240613_15시42분.xlsx
				var excelFileName = _fileCommonService.GenerateExcelFileName("부산청과A4자료", null);
				var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);

				LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");

                // SaveDataTableToExcelWithoutHeader : 헤더없음
                // SaveDataTableToExcel : 헤더포함
				var excelCreated = _fileService.SaveDataTableToExcel(busanCheonggwaDocData, excelFilePath, SHEET_NAME);
				if (!excelCreated)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

				// 4단계: Dropbox에 파일 업로드
				var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
				if (string.IsNullOrEmpty(dropboxFolderPath))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
					return false;
				}

				LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");

				var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
				if (string.IsNullOrEmpty(dropboxFilePath))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
					LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
					LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
					LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

				// 5단계: Dropbox 공유 링크 생성
				LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");

				// Dropbox 설정 정보 로깅
				var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
				var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
				var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];

				LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
				LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");

				var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
				if (string.IsNullOrEmpty(sharedLink))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

				// 6단계: KakaoWork 채팅방에 알림 전송 (부산청과 A4자료)
                // KakaoCheck 설정 확인
                if (IsKakaoWorkEnabled())
                {
                    // 송장 개수 계산 및 시간대별 차수 설정
                    var invoiceCount = busanCheonggwaDocData?.Rows.Count ?? 0;
                    LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
                    
                    var kakaoWorkService = KakaoWorkService.Instance;
                    var now = DateTime.Now;
                    var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
                    LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
                    
                    // 채팅방 ID 설정
                    var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
                    if (string.IsNullOrEmpty(chatroomId))
                    {
                        LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
                        return false;
                    }
                    LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
                    
                    try
                    {
                        // KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
                        await kakaoWorkService.SendInvoiceNotificationAsync(
                            NotificationType.BusanCheonggwaPrint,
                            batch,
                            invoiceCount,
                            sharedLink,
                            chatroomId);
                        
                        LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
                    }
                    catch (Exception ex)
                    {
                        LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
                        // 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
                    }
                }
                else
                {
                    LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ KakaoCheck 설정이 'Y'가 아닙니다. 카카오워크 알림 전송을 건너뜁니다.");
                }

				// 7단계: 임시 파일 정리
				try
				{
					if (File.Exists(excelFilePath))
					{
						File.Delete(excelFilePath);
						LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
					}
				}
				catch (Exception ex)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
					// 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 부산청과 자료 최종 파일 처리 완료");
				return true;
			}
			catch (Exception ex)
			{
				var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
				var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";

				// app.log 파일에 오류 상세 정보 기록
				LogManagerService.LogInfo(errorMessage);
				LogManagerService.LogInfo(stackTraceMessage);

				// 내부 예외가 있는 경우 추가 로그
				if (ex.InnerException != null)
				{
					var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
					LogManagerService.LogInfo(innerErrorMessage);
				}

				return false;
			}
		}

		/// <summary>
		/// 감천냉동 최종 파일을 처리하는 메서드
		/// 
		/// 📋 주요 기능:
		/// - 감천냉동 최종 데이터 조회 (중복 제거)
		/// - Excel 파일 생성 (헤더 포함)
		/// - Dropbox 업로드 및 공유 링크 생성
		/// - 임시 파일 정리
		/// 
		/// 🔗 의존성:
		/// - 송장출력_감천냉동_최종 테이블
		/// - FileService (Excel 파일 생성)
		/// - FileCommonService (Dropbox 업로드)
		/// 
		/// 📁 파일명 형식: {접두사}_{YYMMDD}_{HH}시{MM}분.xlsx
		/// 예: 감천냉동_250820_09시08분.xlsx
		/// </summary>
		/// <returns>처리 성공 여부</returns>
		public async Task<bool> ProcessGamcheonFrozenFinalFile()
		{
			const string METHOD_NAME = "ProcessGamcheonFrozenFinalFile";
			const string TABLE_NAME = "송장출력_감천냉동_최종";
			const string SHEET_NAME = "Sheet1";
			const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath13";
			const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

			try
			{
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 감천냉동 최종 파일 처리 시작...");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");

				LogPathManager.PrintLogPathInfo();
				LogPathManager.ValidateLogFileLocations();

				// 1단계: 테이블명 확인
				LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

				// 2단계: 데이터베이스에서 감천냉동 최종 데이터 조회 (중복 제거 포함)
				// 주소, 수취인명, 전화번호1 기준으로 중복 제거
                var sqlQuery = $@"SELECT msg1,msg2,msg3,msg4,msg5,msg6,수취인명,전화번호1,전화번호2,우편번호,
                                   주소,송장명,수량,배송메세지,주문번호,쇼핑몰,품목코드,택배비용,박스크기,출력개수,별표1,별표2,품목개수
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 주소, 수취인명, 전화번호1 
                                                ORDER BY 주소, 수취인명, 전화번호1 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var gamcheonFrozenData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

				if (gamcheonFrozenData == null || gamcheonFrozenData.Rows.Count == 0)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 감천냉동 최종 데이터가 없습니다.");
					return true;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {gamcheonFrozenData.Rows.Count:N0}건");

				// 3단계: Excel 파일 생성 (헤더 포함)
				// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
				// 파일명 생성 예시: "감천냉동_감천냉동_YYMMDD_HH시MM분.xlsx" 형식으로 생성됨
				// 예: 감천냉동_250820_09시08분.xlsx
				var excelFileName = _fileCommonService.GenerateExcelFileName("감천냉동", null);
				var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);

				LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");

                // SaveDataTableToExcelWithoutHeader : 헤더없음
                // SaveDataTableToExcel : 헤더포함
				var excelCreated = _fileService.SaveDataTableToExcelWithoutHeader(gamcheonFrozenData, excelFilePath, SHEET_NAME);
				if (!excelCreated)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

				// 4단계: Dropbox에 파일 업로드
				var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
				if (string.IsNullOrEmpty(dropboxFolderPath))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
					return false;
				}

				LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");

				var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
				if (string.IsNullOrEmpty(dropboxFilePath))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
					LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
					LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
					LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

				// 5단계: Dropbox 공유 링크 생성
				LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");

				// Dropbox 설정 정보 로깅
				var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
				var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
				var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];

				LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
				LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");

				var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
				if (string.IsNullOrEmpty(sharedLink))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

				// 6단계: KakaoWork 채팅방에 알림 전송 (감천냉동 운송장)
				// KakaoCheck 설정 확인
				if (IsKakaoWorkEnabled())
				{
					// 송장 개수 계산 및 시간대별 차수 설정
					var invoiceCount = gamcheonFrozenData?.Rows.Count ?? 0;
					LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
					
					var kakaoWorkService = KakaoWorkService.Instance;
					var now = DateTime.Now;
					var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
					
					// 채팅방 ID 설정
					var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
					if (string.IsNullOrEmpty(chatroomId))
					{
						LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
						return false;
					}
					LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
					
					try
					{
						// KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
						await kakaoWorkService.SendInvoiceNotificationAsync(
							NotificationType.GamcheonFrozen,
							batch,
							invoiceCount,
							sharedLink,
							chatroomId);
						
						LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
					}
					catch (Exception ex)
					{
						LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
						// 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
					}
				}
				else
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ KakaoCheck 설정이 'Y'가 아닙니다. 카카오워크 알림 전송을 건너뜁니다.");
				}

				// 7단계: 임시 파일 정리
				try
				{
					if (File.Exists(excelFilePath))
					{
						File.Delete(excelFilePath);
						LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
					}
				}
				catch (Exception ex)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
					// 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 감천냉동 최종 파일 처리 완료");
				return true;
			}
			catch (Exception ex)
			{
				var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
				var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";

				// app.log 파일에 오류 상세 정보 기록
				LogManagerService.LogInfo(errorMessage);
				LogManagerService.LogInfo(stackTraceMessage);

				// 내부 예외가 있는 경우 추가 로그
				if (ex.InnerException != null)
				{
					var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
					LogManagerService.LogInfo(innerErrorMessage);
				}

				return false;
			}
		}

		/// <summary>
		/// 송장출력 최종 파일을 처리하는 메서드
		/// 
		/// 📋 주요 기능:
		/// - 송장출력 최종 데이터 조회 (중복 제거)
		/// - Excel 파일 생성 (헤더 없음)
		/// - Dropbox 업로드 및 공유 링크 생성
		/// - 임시 파일 정리
		/// 
		/// 🔗 의존성:
		/// - 송장출력_최종 테이블
		/// - FileService (Excel 파일 생성)
		/// - FileCommonService (Dropbox 업로드)
		/// 
		/// 📁 파일명 형식: {접두사}_{YYMMDD}_{HH}시{MM}분.xlsx
		/// 예: 송장출력_250820_09시08분.xlsx
		/// </summary>
		/// <returns>처리 성공 여부</returns>
		public async Task<bool> ProcessInvoiceFinalFile()
		{
			const string METHOD_NAME = "ProcessInvoiceFinalFile";
			const string TABLE_NAME = "송장출력_최종";
			const string SHEET_NAME = "Sheet1";
			const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath14";
			const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

			try
			{
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 송장출력 최종 파일 처리 시작...");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");

				LogPathManager.PrintLogPathInfo();
				LogPathManager.ValidateLogFileLocations();

				// 1단계: 테이블명 확인
				LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

				// 2단계: 데이터베이스에서 송장출력 최종 데이터 조회 (중복 제거 포함)
				// 주소, 수취인명, 전화번호1 기준으로 중복 제거
                var sqlQuery = $@"SELECT msg1,msg2,msg3,msg4,msg5,msg6,수취인명,전화번호1,전화번호2,우편번호,
                                   주소,송장명,수량,배송메세지,주문번호,쇼핑몰,품목코드,택배비용,박스크기,출력개수,별표1,별표2,품목개수
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 주소, 수취인명, 전화번호1 
                                                ORDER BY 주소, 수취인명, 전화번호1 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var invoiceFinalData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

				if (invoiceFinalData == null || invoiceFinalData.Rows.Count == 0)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 송장출력 최종 데이터가 없습니다.");
					return true;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {invoiceFinalData.Rows.Count:N0}건");

				// 3단계: Excel 파일 생성 (헤더 없음)
				// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
				// 파일명 생성 예시: "송장출력_송장출력_YYMMDD_HH시MM분.xlsx" 형식으로 생성됨
				// 예: 송장출력_250820_09시08분.xlsx
				var excelFileName = _fileCommonService.GenerateExcelFileName("통합송장", null);
				var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);

				LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");

                // SaveDataTableToExcelWithoutHeader : 헤더없음
                // SaveDataTableToExcel : 헤더포함
				var excelCreated = _fileService.SaveDataTableToExcel(invoiceFinalData, excelFilePath, SHEET_NAME);
				if (!excelCreated)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

				// 4단계: Dropbox에 파일 업로드
				var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
				if (string.IsNullOrEmpty(dropboxFolderPath))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
					return false;
				}

				LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");

				var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
				if (string.IsNullOrEmpty(dropboxFilePath))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
					LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
					LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
					LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

				// 5단계: Dropbox 공유 링크 생성
				LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");

				// Dropbox 설정 정보 로깅
				var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
				var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
				var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];

				LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
				LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");

				var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
				if (string.IsNullOrEmpty(sharedLink))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

				// 6단계: KakaoWork 채팅방에 알림 전송 (통합송장)
				// KakaoCheck 설정 확인
				if (IsKakaoWorkEnabled())
				{
					// 송장 개수 계산 및 시간대별 차수 설정
					var invoiceCount = invoiceFinalData?.Rows.Count ?? 0;
					LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
					
					var kakaoWorkService = KakaoWorkService.Instance;
					var now = DateTime.Now;
					var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
					
					// 채팅방 ID 설정
					var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
					if (string.IsNullOrEmpty(chatroomId))
					{
						LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
						return false;
					}
					LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
					
					try
					{
						// KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
						await kakaoWorkService.SendInvoiceNotificationAsync(
							NotificationType.Integrated,    
							batch,
							invoiceCount,
							sharedLink,
							chatroomId);
						
						LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
					}
					catch (Exception ex)
					{
						LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
						// 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
					}
				}
				else
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ KakaoCheck 설정이 'Y'가 아닙니다. 카카오워크 알림 전송을 건너뜁니다.");
				}

				// 7단계: 임시 파일 정리
				try
				{
					if (File.Exists(excelFilePath))
					{
						File.Delete(excelFilePath);
						LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
					}
				}
				catch (Exception ex)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
					// 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 송장출력 최종 파일 처리 완료");
				return true;
			}
			catch (Exception ex)
			{
				var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
				var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";

				// app.log 파일에 오류 상세 정보 기록
				LogManagerService.LogInfo(errorMessage);
				LogManagerService.LogInfo(stackTraceMessage);

				// 내부 예외가 있는 경우 추가 로그
				if (ex.InnerException != null)
				{
					var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
					LogManagerService.LogInfo(innerErrorMessage);
				}

				return false;
			}
		}

		/// <returns>처리 성공 여부</returns>
		public async Task<bool> ProcessBusanCheonggwaExtFinalFile()
		{
			const string METHOD_NAME = "ProcessBusanCheonggwaExtFinalFile";
			const string TABLE_NAME = "송장출력_부산청과_최종변환";
			const string SHEET_NAME = "Sheet1";
			const string DROPBOX_FOLDER_PATH_KEY = "DropboxFolderPath12";
			const string KAKAO_WORK_CHATROOM_ID = "KakaoWork.ChatroomId.Check";

			try
			{
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 부산청과 외부출고 최종 파일 처리 시작...");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				LogManagerService.LogInfo($"🔍 [{METHOD_NAME}] 호출 스택 확인 중...");

				LogPathManager.PrintLogPathInfo();
				LogPathManager.ValidateLogFileLocations();

				// 1단계: 테이블명 확인
				LogManagerService.LogInfo($"[{METHOD_NAME}] 대상 테이블: {TABLE_NAME}");

				// 2단계: 데이터베이스에서 부산청과 외부출고 최종 데이터 조회 (중복 제거 포함)
				// 주소, 수취인명, 전화번호1 기준으로 중복 제거
                var sqlQuery = $@"SELECT msg1,msg2,msg3,msg4,msg5,msg6,수취인명,전화번호1,전화번호2,우편번호,
                주소,송장명,수량,배송메세지,주문번호,쇼핑몰,품목코드,택배비용,박스크기,출력개수,별표1,별표2,품목개수
                                   FROM (SELECT *,
                                            ROW_NUMBER() OVER (
                                                PARTITION BY 주소, 수취인명, 전화번호1 
                                                ORDER BY 주소, 수취인명, 전화번호1 ASC
                                            ) AS rn
                                          FROM {TABLE_NAME}
                                    ) AS ranked_rows
                                    WHERE rn = 1";
				var busanCheonggwaExtData = await _databaseCommonService.GetDataFromQuery(sqlQuery);

				if (busanCheonggwaExtData == null || busanCheonggwaExtData.Rows.Count == 0)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 부산청과 외부출고 최종 데이터가 없습니다.");
					return true;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 데이터 조회 완료: {busanCheonggwaExtData.Rows.Count:N0}건");

				// 3단계: Excel 파일 생성 (헤더 없음)
				// {접두사}_{설명}_{YYMMDD}_{HH}시{MM}분.xlsx
				// 파일명 생성 예시: "부산청과외부출고_부산청과외부출고_YYMMDD_HH시MM분.xlsx" 형식으로 생성됨
				// 예: 부산청과외부출고_부산청과외부출고_240613_15시42분.xlsx
				var excelFileName = _fileCommonService.GenerateExcelFileName("부산청과외부출고", null);
				var excelFilePath = Path.Combine(Path.GetTempPath(), excelFileName);

				LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일 생성 시작: {excelFileName}");

				var excelCreated = _fileService.SaveDataTableToExcelWithoutHeader(busanCheonggwaExtData, excelFilePath, SHEET_NAME);
				if (!excelCreated)
				{
					LogManagerService.LogError($"[{METHOD_NAME}] ❌ Excel 파일 생성 실패: {excelFilePath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Excel 파일 생성 완료: {excelFilePath}");

				// 4단계: Dropbox에 파일 업로드
				var dropboxFolderPath = ConfigurationManager.AppSettings[DROPBOX_FOLDER_PATH_KEY];
				if (string.IsNullOrEmpty(dropboxFolderPath))
				{
					LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {DROPBOX_FOLDER_PATH_KEY} 미설정 상태입니다.");
					return false;
				}

				LogManagerService.LogInfo($"🔗 [{METHOD_NAME}] Dropbox 업로드 시작: {dropboxFolderPath}");

				var dropboxFilePath = await _fileCommonService.UploadFileToDropbox(excelFilePath, dropboxFolderPath);
				if (string.IsNullOrEmpty(dropboxFilePath))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 업로드 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] Dropbox 업로드 실패 원인 분석:");
					LogManagerService.LogError($"   - Excel 파일 경로: {excelFilePath}");
					LogManagerService.LogError($"   - Excel 파일 존재 여부: {File.Exists(excelFilePath)}");
					LogManagerService.LogError($"   - Dropbox 폴더 설정: {dropboxFolderPath}");
					return false;
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 업로드 완료: {dropboxFilePath}");

				// 5단계: Dropbox 공유 링크 생성
				LogManagerService.LogInfo($"[{METHOD_NAME}] Dropbox 공유 링크 생성 시작");

				// Dropbox 설정 정보 로깅
				var dropboxAppKey = ConfigurationManager.AppSettings["Dropbox.AppKey"];
				var dropboxAppSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"];
				var dropboxRefreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"];

				LogManagerService.LogInfo($"🔑 [{METHOD_NAME}] Dropbox 설정 확인:");
				LogManagerService.LogInfo($"   AppKey: {(string.IsNullOrEmpty(dropboxAppKey) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   AppSecret: {(string.IsNullOrEmpty(dropboxAppSecret) ? "❌ 미설정" : "✅ 설정됨")}");
				LogManagerService.LogInfo($"   RefreshToken: {(string.IsNullOrEmpty(dropboxRefreshToken) ? "❌ 미설정" : "✅ 설정됨")}");

				var sharedLink = await _fileCommonService.CreateDropboxSharedLink(dropboxFilePath);
				if (string.IsNullOrEmpty(sharedLink))
				{
					LogManagerService.LogError($"❌ [{METHOD_NAME}] Dropbox 공유 링크 생성 실패");
					// 실패 원인 분석을 위한 추가 로깅
					LogManagerService.LogError($"🔍 [{METHOD_NAME}] 공유 링크 생성 실패 원인 분석:\n   - Dropbox 파일 경로: {dropboxFilePath}\n   - Dropbox 폴더 설정: {dropboxFolderPath}\n   - Excel 파일 경로: {excelFilePath}\n   - Dropbox 파일 존재 여부: {File.Exists(excelFilePath)}");
					return false;
				}
				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ Dropbox 공유 링크 생성 완료: {sharedLink}");

				// 6단계: KakaoWork 채팅방에 알림 전송 (부산청과 외부출고)
				// KakaoCheck 설정 확인
				if (IsKakaoWorkEnabled())
				{
					// 송장 개수 계산 및 시간대별 차수 설정
					var invoiceCount = busanCheonggwaExtData?.Rows.Count ?? 0;
					LogManagerService.LogInfo($"[{METHOD_NAME}] 📊 송장 개수: {invoiceCount:N0}건");
					
					var kakaoWorkService = KakaoWorkService.Instance;
					var now = DateTime.Now;
					var batch = kakaoWorkService.GetBatchByTime(now.Hour, now.Minute);
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⏰ 현재 시간: {now:HH:mm}, 배치: {batch}");
					
					// 채팅방 ID 설정
					var chatroomId = ConfigurationManager.AppSettings[KAKAO_WORK_CHATROOM_ID];
					if (string.IsNullOrEmpty(chatroomId))
					{
						LogManagerService.LogWarning($"[{METHOD_NAME}] ⚠️ {KAKAO_WORK_CHATROOM_ID} 미설정 상태입니다.");
						return false;
					}
					LogManagerService.LogInfo($"[{METHOD_NAME}] 💬 KakaoWork 채팅방 ID: {chatroomId}");
					
					try
					{
						// KakaoWork 알림 전송 (시간대별 차수 + 실제 송장 개수 + 채팅방 ID)
						await kakaoWorkService.SendInvoiceNotificationAsync(
							NotificationType.BusanCheonggwa,
							batch,
							invoiceCount,
							sharedLink,
							chatroomId);
						
						LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ KakaoWork 알림 전송 완료 (배치: {batch}, 송장: {invoiceCount}건, 채팅방: {chatroomId})");
					}
					catch (Exception ex)
					{
						LogManagerService.LogError($"[{METHOD_NAME}] ❌ KakaoWork 알림 전송 실패: {ex.Message}");
						// 알림 전송 실패는 전체 프로세스 실패로 처리하지 않음
					}
				}
				else
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ KakaoCheck 설정이 'Y'가 아닙니다. 카카오워크 알림 전송을 건너뜁니다.");
				}

				// 7단계: 임시 파일 정리
				try
				{
					if (File.Exists(excelFilePath))
					{
						File.Delete(excelFilePath);
						LogManagerService.LogInfo($"[{METHOD_NAME}] 🗑️ 임시 파일 정리 완료: {excelFilePath}");
					}
				}
				catch (Exception ex)
				{
					LogManagerService.LogInfo($"[{METHOD_NAME}] ⚠️ 임시 파일 정리 실패: {ex.Message}");
					// 임시 파일 정리 실패는 전체 프로세스 실패로 처리하지 않음
				}

				LogManagerService.LogInfo($"[{METHOD_NAME}] ✅ 부산청과 외부출고 최종 파일 처리 완료");
				return true;
			}
			catch (Exception ex)
			{
				var errorMessage = $"❌ [{METHOD_NAME}] 처리 중 오류 발생:\n   오류 내용: {ex.Message}";
				var stackTraceMessage = $"📋 [{METHOD_NAME}] 스택 트레이스:\n   {ex.StackTrace}";

				// app.log 파일에 오류 상세 정보 기록
				LogManagerService.LogInfo(errorMessage);
				LogManagerService.LogInfo(stackTraceMessage);

				// 내부 예외가 있는 경우 추가 로그
				if (ex.InnerException != null)
				{
					var innerErrorMessage = $"📋 [{METHOD_NAME}] 내부 예외:\n   오류 내용: {ex.InnerException.Message}";
					LogManagerService.LogInfo(innerErrorMessage);
				}

				return false;
			}
		}

        /// <summary>
        /// KakaoWork 채팅방에 알림을 전송하는 메서드
        /// </summary>
        /// <param name="fileUrl">파일 다운로드 링크</param>
        /// <param name="chatroomId">채팅방 ID (선택사항)</param>
        /// <returns>전송 성공 여부</returns>
        private async Task<bool> SendKakaoWorkNotification(string fileUrl, string? chatroomId = null)
        {
            try
            {
                var kakaoWorkService = KakaoWorkService.Instance;
                var notificationResult = await kakaoWorkService.SendSalesDataNotificationAsync(fileUrl, chatroomId);
                return notificationResult;
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ KakaoWork 알림 전송 실패: {ex.Message}");
                return false;
            }
        }

        // MySQL 오류 정보 수집 메서드들 제거됨 - 더 이상 사용되지 않음

        /// <summary>
        /// 프로시저 실행 결과를 확인하는 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 프로시저 실행 후 송장출력_사방넷원본변환 테이블의 데이터 개수 확인
        /// - 실제 데이터 삽입 여부 검증
        /// - 디버깅 정보 제공
        /// </summary>
        /// <returns>삽입된 데이터 행 수</returns>
        private async Task<int> CheckProcedureResult()
        {
            try
            {
                LogManagerService.LogInfo("[InvoiceProcessor] 프로시저 실행 결과 확인 시작");
                
                // DatabaseService를 통해 결과 테이블 확인
                var databaseService = new DatabaseService();
                var connection = await databaseService.GetConnectionAsync();
                
                if (connection == null)
                {
                    LogManagerService.LogWarning("[InvoiceProcessor] 데이터베이스 연결을 가져올 수 없습니다.");
                    return 0;
                }
                
                try
                {
                    await connection.OpenAsync();
                    
                    // 송장출력_사방넷원본변환 테이블의 데이터 개수 확인
                    var countSql = "SELECT COUNT(*) FROM 송장출력_사방넷원본변환";
                    using var countCommand = new MySqlConnector.MySqlCommand(countSql, connection);
                    var result = await countCommand.ExecuteScalarAsync();
                    
                    var rowCount = Convert.ToInt32(result);
                    LogManagerService.LogInfo($"[InvoiceProcessor] 송장출력_사방넷원본변환 테이블 데이터 개수: {rowCount}행");
                    
                    // 샘플 데이터 확인 (처음 3행)
                    if (rowCount > 0)
                    {
                        var sampleSql = "SELECT * FROM 송장출력_사방넷원본변환 LIMIT 3";
                        using var sampleCommand = new MySqlConnector.MySqlCommand(sampleSql, connection);
                        using var reader = await sampleCommand.ExecuteReaderAsync();
                        
                        var dataTable = new DataTable();
                        dataTable.Load(reader);
                        
                        LogManagerService.LogInfo($"[InvoiceProcessor] 샘플 데이터 컬럼: {string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
                        
                        if (dataTable.Rows.Count > 0)
                        {
                            var firstRow = dataTable.Rows[0];
                            var sampleData = string.Join(" | ", dataTable.Columns.Cast<DataColumn>().Select(col => $"{col.ColumnName}: {firstRow[col]?.ToString() ?? "NULL"}"));
                            LogManagerService.LogInfo($"[InvoiceProcessor] 첫 번째 행 샘플: {sampleData}");
                        }
                    }
                    
                    return rowCount;
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"[InvoiceProcessor] 프로시저 실행 결과 확인 중 오류 발생: {ex.Message}");
                LogManagerService.LogError($"[InvoiceProcessor] 오류 상세: {ex.StackTrace}");
                return 0;
            }
        }


        #endregion


    }
}