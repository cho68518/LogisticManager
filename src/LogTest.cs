using System;
using LogisticManager.Services;

namespace LogisticManager.src
{
    /// <summary>
    /// 로그 통합 테스트 클래스
    /// 
    /// 📋 테스트 목적:
    /// - LogManagerService가 제대로 작동하는지 확인
    /// - 로그 파일이 올바른 위치에 생성되는지 확인
    /// - 로그 레벨별 분류가 제대로 되는지 확인
    /// </summary>
    public class LogTest
    {
        public static void TestLogIntegration()
        {
            Console.WriteLine("🚀 로그 통합 테스트 시작...");
            
            try
            {
                // 1. 기본 로그 테스트
                LogManagerService.LogInfo("📝 정보 로그 테스트 메시지");
                LogManagerService.LogWarning("⚠️ 경고 로그 테스트 메시지");
                LogManagerService.LogError("❌ 오류 로그 테스트 메시지");
                LogManagerService.LogDebug("🔍 디버그 로그 테스트 메시지");
                
                // 2. 기존 호환성 테스트
                LogManagerService.LogMessage("🔄 기존 호환성 테스트 메시지");
                
                // 3. 한글 메시지 테스트
                LogManagerService.LogInfo("🇰🇷 한글 로그 메시지 테스트");
                LogManagerService.LogWarning("🚨 한글 경고 메시지 테스트");
                
                // 4. 긴 메시지 테스트
                var longMessage = "이것은 매우 긴 로그 메시지입니다. " +
                                "여러 줄에 걸쳐 작성된 메시지로, " +
                                "로그 시스템이 긴 메시지를 제대로 처리할 수 있는지 테스트합니다.";
                LogManagerService.LogInfo(longMessage);
                
                Console.WriteLine("✅ 로그 통합 테스트 완료!");
                Console.WriteLine("📁 로그 파일 위치: logs/current/app.log");
                Console.WriteLine("🔍 로그 파일을 확인해보세요.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 로그 통합 테스트 실패: {ex.Message}");
            }
        }
        
        public static void Main(string[] args)
        {
            TestLogIntegration();
        }
    }
}
