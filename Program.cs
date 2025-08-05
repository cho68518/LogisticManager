using LogisticManager.Forms;
using System.Text;

namespace LogisticManager
{
    /// <summary>
    /// 송장 처리 자동화 애플리케이션의 진입점
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 애플리케이션의 메인 진입점
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Windows Forms 애플리케이션 설정
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                // 데이터베이스 연결 테스트
                Console.WriteLine("🔍 데이터베이스 연결을 테스트합니다...");
                DatabaseTest.TestConnection();
                Console.WriteLine("✅ 데이터베이스 연결 테스트 완료!");
                
                // 메인 폼 실행
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                // 예상치 못한 오류 발생 시 메시지 표시
                MessageBox.Show($"애플리케이션 실행 중 오류가 발생했습니다.\n{ex.Message}", 
                    "치명적 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 