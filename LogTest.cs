using System;

namespace LogisticManager
{
    /// <summary>
    /// 로그 확인 기능을 테스트하는 클래스
    /// </summary>
    public class LogTest
    {
        /// <summary>
        /// 로그 확인 기능을 테스트하는 메인 메서드
        /// </summary>
        public static void TestLogViewer()
        {
            Console.WriteLine("🔍 로그 확인 도구 테스트");
            Console.WriteLine("=" * 50);

            // 1. 로그 파일 정보 확인
            Console.WriteLine("\n📊 1. 로그 파일 정보:");
            Console.WriteLine(LogViewer.GetLogFileInfo());

            // 2. 최근 로그 확인 (10줄)
            Console.WriteLine("\n📄 2. 최근 로그 (10줄):");
            Console.WriteLine(LogViewer.GetRecentLogs(10));

            // 3. 오류 로그 확인
            Console.WriteLine("\n⚠️ 3. 오류 로그:");
            Console.WriteLine(LogViewer.GetErrorLogs(5));

            // 4. 특정 키워드 검색 (예: "송장")
            Console.WriteLine("\n🔍 4. '송장' 키워드 검색:");
            Console.WriteLine(LogViewer.SearchLogs("송장", 5));

            Console.WriteLine("\n✅ 로그 확인 테스트 완료");
        }

        /// <summary>
        /// 특정 키워드로 로그를 검색하는 메서드
        /// </summary>
        /// <param name="keyword">검색할 키워드</param>
        public static void SearchLogsByKeyword(string keyword)
        {
            Console.WriteLine($"🔍 키워드 '{keyword}' 검색 결과:");
            Console.WriteLine(LogViewer.SearchLogs(keyword, 10));
        }

        /// <summary>
        /// 로그 파일을 클리어하는 메서드 (주의: 백업 후 클리어)
        /// </summary>
        public static void ClearLogFile()
        {
            Console.WriteLine("⚠️ 로그 파일을 클리어하시겠습니까? (y/n)");
            var response = Console.ReadLine()?.ToLower();
            
            if (response == "y" || response == "yes")
            {
                Console.WriteLine(LogViewer.ClearLogFile());
            }
            else
            {
                Console.WriteLine("❌ 로그 파일 클리어가 취소되었습니다.");
            }
        }
    }
}
