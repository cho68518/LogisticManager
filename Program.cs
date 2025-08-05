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
            // 로그 파일에 시작 기록
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            File.AppendAllText(logPath, $"=== 애플리케이션 시작: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
            
            try
            {
                Console.WriteLine("🔍 Program.Main: 애플리케이션 시작");
                File.AppendAllText(logPath, "🔍 Program.Main: 애플리케이션 시작\n");
                
                // Windows Forms 애플리케이션 설정
                Console.WriteLine("🔍 Program.Main: Windows Forms 설정 시작");
                File.AppendAllText(logPath, "🔍 Program.Main: Windows Forms 설정 시작\n");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Console.WriteLine("✅ Program.Main: Windows Forms 설정 완료");
                File.AppendAllText(logPath, "✅ Program.Main: Windows Forms 설정 완료\n");
                
                try
                {
                    // 데이터베이스 연결 테스트
                    Console.WriteLine("🔍 Program.Main: 데이터베이스 연결 테스트 시작");
                    File.AppendAllText(logPath, "🔍 Program.Main: 데이터베이스 연결 테스트 시작\n");
                    DatabaseTest.TestConnection();
                    Console.WriteLine("✅ Program.Main: 데이터베이스 연결 테스트 완료!");
                    File.AppendAllText(logPath, "✅ Program.Main: 데이터베이스 연결 테스트 완료!\n");
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"❌ Program.Main: 데이터베이스 연결 테스트 실패: {dbEx.Message}");
                    File.AppendAllText(logPath, $"❌ Program.Main: 데이터베이스 연결 테스트 실패: {dbEx.Message}\n");
                    Console.WriteLine($"🔍 Program.Main: DB 예외 타입: {dbEx.GetType().Name}");
                    File.AppendAllText(logPath, $"🔍 Program.Main: DB 예외 타입: {dbEx.GetType().Name}\n");
                    Console.WriteLine($"🔍 Program.Main: DB 예외 상세: {dbEx}");
                    File.AppendAllText(logPath, $"🔍 Program.Main: DB 예외 상세: {dbEx}\n");
                    
                    if (dbEx.InnerException != null)
                    {
                        Console.WriteLine($"🔍 Program.Main: DB 내부 예외: {dbEx.InnerException.Message}");
                        File.AppendAllText(logPath, $"🔍 Program.Main: DB 내부 예외: {dbEx.InnerException.Message}\n");
                    }
                    
                    // 데이터베이스 연결 실패해도 애플리케이션은 계속 실행
                    Console.WriteLine("⚠️ Program.Main: 데이터베이스 연결 실패했지만 애플리케이션을 계속 실행합니다.");
                    File.AppendAllText(logPath, "⚠️ Program.Main: 데이터베이스 연결 실패했지만 애플리케이션을 계속 실행합니다.\n");
                }
                
                try
                {
                    // 메인 폼 실행
                    Console.WriteLine("🔍 Program.Main: MainForm 생성 시작");
                    File.AppendAllText(logPath, "🔍 Program.Main: MainForm 생성 시작\n");
                    var mainForm = new MainForm();
                    Console.WriteLine("✅ Program.Main: MainForm 생성 완료");
                    File.AppendAllText(logPath, "✅ Program.Main: MainForm 생성 완료\n");
                    
                    Console.WriteLine("🔍 Program.Main: MainForm 실행 시작");
                    File.AppendAllText(logPath, "🔍 Program.Main: MainForm 실행 시작\n");
                    Application.Run(mainForm);
                    Console.WriteLine("✅ Program.Main: MainForm 실행 완료");
                    File.AppendAllText(logPath, "✅ Program.Main: MainForm 실행 완료\n");
                }
                catch (Exception formEx)
                {
                    Console.WriteLine($"❌ Program.Main: MainForm 실행 실패: {formEx.Message}");
                    File.AppendAllText(logPath, $"❌ Program.Main: MainForm 실행 실패: {formEx.Message}\n");
                    Console.WriteLine($"🔍 Program.Main: Form 예외 타입: {formEx.GetType().Name}");
                    File.AppendAllText(logPath, $"🔍 Program.Main: Form 예외 타입: {formEx.GetType().Name}\n");
                    Console.WriteLine($"🔍 Program.Main: Form 예외 상세: {formEx}");
                    File.AppendAllText(logPath, $"🔍 Program.Main: Form 예외 상세: {formEx}\n");
                    
                    if (formEx.InnerException != null)
                    {
                        Console.WriteLine($"🔍 Program.Main: Form 내부 예외: {formEx.InnerException.Message}");
                        File.AppendAllText(logPath, $"🔍 Program.Main: Form 내부 예외: {formEx.InnerException.Message}\n");
                    }
                    
                    throw; // 폼 실행 실패는 치명적 오류이므로 다시 던짐
                }
            }
            catch (Exception ex)
            {
                // 예상치 못한 오류 발생 시 메시지 표시
                Console.WriteLine($"❌ Program.Main: 치명적 오류 발생: {ex.Message}");
                File.AppendAllText(logPath, $"❌ Program.Main: 치명적 오류 발생: {ex.Message}\n");
                Console.WriteLine($"🔍 Program.Main: 치명적 예외 타입: {ex.GetType().Name}");
                File.AppendAllText(logPath, $"🔍 Program.Main: 치명적 예외 타입: {ex.GetType().Name}\n");
                Console.WriteLine($"🔍 Program.Main: 치명적 예외 상세: {ex}");
                File.AppendAllText(logPath, $"🔍 Program.Main: 치명적 예외 상세: {ex}\n");
                
                MessageBox.Show($"애플리케이션 실행 중 오류가 발생했습니다.\n{ex.Message}", 
                    "치명적 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 