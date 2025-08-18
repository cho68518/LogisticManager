using LogisticManager.Forms;
using LogisticManager.Services;
using System.Text;

namespace LogisticManager
{
    /// <summary>
    /// 송장 처리 자동화 애플리케이션의 진입점
    /// 
    /// 주요 기능:
    /// - 애플리케이션 초기화 및 설정
    /// - 데이터베이스 연결 테스트
    /// - Windows Forms 애플리케이션 실행
    /// - 오류 처리 및 로깅
    /// 
    /// 실행 과정:
    /// 1. 애플리케이션 시작 로그 기록
    /// 2. Windows Forms 설정
    /// 3. 설정 파일 리소스에서 로드
    /// 4. 데이터베이스 연결 테스트 (선택사항)
    /// 5. 메인 폼 생성 및 실행
    /// 6. 오류 발생 시 사용자에게 알림
    /// 
    /// 오류 처리:
    /// - 데이터베이스 연결 실패: 애플리케이션 계속 실행
    /// - 폼 실행 실패: 치명적 오류로 처리
    /// - 예상치 못한 오류: 사용자에게 메시지 표시
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 애플리케이션의 메인 진입점
        /// 
        /// 실행 순서:
        /// 1. 로그 파일 초기화
        /// 2. Windows Forms 설정
        /// 3. 설정 파일 리소스에서 로드
        /// 4. 데이터베이스 연결 테스트
        /// 5. 메인 폼 실행
        /// 6. 오류 처리
        /// 
        /// 특별한 설정:
        /// - [STAThread]: Windows Forms 애플리케이션에 필요한 스레드 모델 설정
        /// - Application.EnableVisualStyles(): 시각적 스타일 활성화
        /// - Application.SetCompatibleTextRenderingDefault(false): 텍스트 렌더링 호환성 설정
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 로그 파일 경로 정보 출력
            LogPathManager.PrintLogPathInfo();
            LogPathManager.ValidateLogFileLocations();
            LogManagerService.LogInfo($"=== 애플리케이션 시작: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            
            try
            {
                LogManagerService.LogInfo("🔍 Program.Main: 애플리케이션 시작");
                
                // Windows Forms 애플리케이션 설정
                LogManagerService.LogInfo("🔍 Program.Main: Windows Forms 설정 시작");
                Application.EnableVisualStyles(); // 시각적 스타일 활성화
                Application.SetCompatibleTextRenderingDefault(false); // 텍스트 렌더링 호환성 설정
                LogManagerService.LogInfo("✅ Program.Main: Windows Forms 설정 완료");
                
                try
                {
                    // 데이터베이스 연결 테스트 (선택사항)
                    LogManagerService.LogInfo("🔍 Program.Main: 데이터베이스 연결 테스트 시작");
                    
                    // DatabaseTest 클래스가 제거되었으므로 주석 처리
                    // DatabaseTest.TestConnection(); // 데이터베이스 연결 상태 확인
                    
                    LogManagerService.LogInfo("✅ Program.Main: 데이터베이스 연결 테스트 완료!");
                }
                catch (Exception dbEx)
                {
                    // 데이터베이스 연결 실패 시 상세한 오류 정보 기록
                    LogManagerService.LogError($"❌ Program.Main: 데이터베이스 연결 테스트 실패: {dbEx.Message}");
                    LogManagerService.LogError($"🔍 Program.Main: DB 예외 타입: {dbEx.GetType().Name}");
                    LogManagerService.LogError($"🔍 Program.Main: DB 예외 상세: {dbEx}");
                    
                    // 내부 예외가 있는 경우 추가 정보 기록
                    if (dbEx.InnerException != null)
                    {
                        LogManagerService.LogError($"🔍 Program.Main: DB 내부 예외: {dbEx.InnerException.Message}");
                    }
                    
                    // 데이터베이스 연결 실패해도 애플리케이션은 계속 실행
                    // (데이터베이스 기능이 선택사항이므로)
                    LogManagerService.LogWarning("⚠️ Program.Main: 데이터베이스 연결 실패했지만 애플리케이션을 계속 실행합니다.");
                }

                // 매핑 정보 출력
                try
                {
                    LogManagerService.LogInfo("🔍 Program.Main: 매핑 정보 확인 시작");
                    
                    var mappingService = new MappingService();
                    mappingService.PrintMappingSummary();
                    mappingService.PrintDetailedMapping("order_table");
                    
                    LogManagerService.LogInfo("✅ Program.Main: 매핑 정보 확인 완료!");
                }
                catch (Exception mappingEx)
                {
                    LogManagerService.LogError($"❌ Program.Main: 매핑 정보 확인 실패: {mappingEx.Message}");
                }
                
                try
                {
                    // 메인 폼 실행
                    LogManagerService.LogInfo("🔍 Program.Main: MainForm 생성 시작");
                    var mainForm = new MainForm(); // 메인 폼 인스턴스 생성
                    LogManagerService.LogInfo("✅ Program.Main: MainForm 생성 완료");
                    
                    LogManagerService.LogInfo("🔍 Program.Main: MainForm 실행 시작");
                    Application.Run(mainForm); // 메인 폼을 메시지 루프로 실행
                    LogManagerService.LogInfo("✅ Program.Main: MainForm 실행 완료");
                }
                catch (Exception formEx)
                {
                    // 폼 실행 실패 시 상세한 오류 정보 기록
                    LogManagerService.LogError($"❌ Program.Main: MainForm 실행 실패: {formEx.Message}");
                    LogManagerService.LogError($"🔍 Program.Main: Form 예외 타입: {formEx.GetType().Name}");
                    LogManagerService.LogError($"🔍 Program.Main: Form 예외 상세: {formEx}");
                    
                    // 내부 예외가 있는 경우 추가 정보 기록
                    if (formEx.InnerException != null)
                    {
                        LogManagerService.LogError($"🔍 Program.Main: Form 내부 예외: {formEx.InnerException.Message}");
                    }
                    
                    throw; // 폼 실행 실패는 치명적 오류이므로 다시 던짐
                }
            }
            catch (Exception ex)
            {
                // 예상치 못한 오류 발생 시 메시지 표시
                LogManagerService.LogError($"❌ Program.Main: 치명적 오류 발생: {ex.Message}");
                LogManagerService.LogError($"🔍 Program.Main: 치명적 예외 타입: {ex.GetType().Name}");
                LogManagerService.LogError($"🔍 Program.Main: 치명적 예외 상세: {ex}");
                
                // 사용자에게 오류 메시지 표시
                MessageBox.Show($"애플리케이션 실행 중 오류가 발생했습니다.\n{ex.Message}", 
                    "치명적 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 