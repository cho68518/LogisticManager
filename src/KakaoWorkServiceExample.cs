using System;
using System.Threading.Tasks;
using LogisticManager.Services;
using LogisticManager.Models;
using System.Collections.Generic;

namespace LogisticManager.Examples
{
    /// <summary>
    /// KakaoWorkService 사용 예시 클래스
    /// 다양한 알림 종류별로 메시지를 전송하는 방법을 보여줍니다.
    /// </summary>
    public class KakaoWorkServiceExample
    {
        /// <summary>
        /// 서울냉동 파일 처리 후 알림 전송 예시
        /// </summary>
        /// <param name="seoulFrozenFilePath">서울냉동 파일 경로</param>
        /// <param name="batch">배치 정보</param>
        /// <param name="invoiceCount">송장 개수</param>
        public static async Task SendSeoulFrozenNotificationAsync(string seoulFrozenFilePath, string batch, int invoiceCount)
        {
            try
            {
                Console.WriteLine("📤 서울냉동 알림 전송 시작...");

                // 1. Dropbox에 파일 업로드
                var dropboxService = DropboxService.Instance;
                
                // App.config에서 Dropbox 경로 읽기
                var dropboxPath = System.Configuration.ConfigurationManager.AppSettings["KakaoWork.NotificationType.SeoulFrozen.DropboxPath"] ?? "/서울냉동";
                string seoulFrozenUrl = await dropboxService.UploadFileAsync(seoulFrozenFilePath, dropboxPath);

                // 2. KakaoWork로 알림 전송
                var kakaoWorkService = KakaoWorkService.Instance;
                await kakaoWorkService.SendInvoiceNotificationAsync(
                    NotificationType.SeoulFrozen, 
                    batch, 
                    invoiceCount, 
                    seoulFrozenUrl);

                Console.WriteLine("✅ 서울냉동 알림 전송 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 서울냉동 알림 전송 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 판매입력 자료 처리 후 알림 전송 예시
        /// </summary>
        /// <param name="salesDataFilePath">판매입력 파일 경로</param>
        /// <param name="batch">배치 정보</param>
        /// <param name="invoiceCount">송장 개수</param>
        public static async Task SendSalesDataNotificationAsync(string salesDataFilePath, string batch, int invoiceCount)
        {
            try
            {
                Console.WriteLine("📤 판매입력 알림 전송 시작...");

                // 1. Dropbox에 파일 업로드
                var dropboxService = DropboxService.Instance;
                
                // App.config에서 Dropbox 경로 읽기
                var dropboxPath = System.Configuration.ConfigurationManager.AppSettings["KakaoWork.NotificationType.SalesData.DropboxPath"] ?? "/판매입력";
                string salesDataUrl = await dropboxService.UploadFileAsync(salesDataFilePath, dropboxPath);

                // 2. KakaoWork로 알림 전송 (제목 접미사는 App.config에서 자동으로 읽어옴)
                var kakaoWorkService = KakaoWorkService.Instance;
                await kakaoWorkService.SendInvoiceNotificationAsync(
                    NotificationType.SalesData, 
                    batch, 
                    invoiceCount, 
                    salesDataUrl);

                Console.WriteLine("✅ 판매입력 알림 전송 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 판매입력 알림 전송 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 경기냉동 파일 처리 후 알림 전송 예시
        /// </summary>
        /// <param name="gyeonggiFrozenFilePath">경기냉동 파일 경로</param>
        /// <param name="batch">배치 정보</param>
        /// <param name="invoiceCount">송장 개수</param>
        public static async Task SendGyeonggiFrozenNotificationAsync(string gyeonggiFrozenFilePath, string batch, int invoiceCount)
        {
            try
            {
                Console.WriteLine("📤 경기냉동 알림 전송 시작...");

                // 1. Dropbox에 파일 업로드
                var dropboxService = DropboxService.Instance;
                
                // App.config에서 Dropbox 경로 읽기
                var dropboxPath = System.Configuration.ConfigurationManager.AppSettings["KakaoWork.NotificationType.GyeonggiFrozen.DropboxPath"] ?? "/경기냉동";
                string gyeonggiFrozenUrl = await dropboxService.UploadFileAsync(gyeonggiFrozenFilePath, dropboxPath);

                // 2. KakaoWork로 알림 전송
                var kakaoWorkService = KakaoWorkService.Instance;
                await kakaoWorkService.SendInvoiceNotificationAsync(
                    NotificationType.GyeonggiFrozen, 
                    batch, 
                    invoiceCount, 
                    gyeonggiFrozenUrl);

                Console.WriteLine("✅ 경기냉동 알림 전송 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 경기냉동 알림 전송 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 부산청과 파일 처리 후 알림 전송 예시
        /// </summary>
        /// <param name="busanCheonggwaFilePath">부산청과 파일 경로</param>
        /// <param name="batch">배치 정보</param>
        /// <param name="invoiceCount">송장 개수</param>
        public static async Task SendBusanCheonggwaNotificationAsync(string busanCheonggwaFilePath, string batch, int invoiceCount)
        {
            try
            {
                Console.WriteLine("📤 부산청과 알림 전송 시작...");

                // 1. Dropbox에 파일 업로드
                var dropboxService = DropboxService.Instance;
                
                // App.config에서 Dropbox 경로 읽기
                var dropboxPath = System.Configuration.ConfigurationManager.AppSettings["KakaoWork.NotificationType.BusanCheonggwa.DropboxPath"] ?? "/부산청과";
                string busanCheonggwaUrl = await dropboxService.UploadFileAsync(busanCheonggwaFilePath, dropboxPath);

                // 2. KakaoWork로 알림 전송
                var kakaoWorkService = KakaoWorkService.Instance;
                await kakaoWorkService.SendInvoiceNotificationAsync(
                    NotificationType.BusanCheonggwa, 
                    batch, 
                    invoiceCount, 
                    busanCheonggwaUrl);

                Console.WriteLine("✅ 부산청과 알림 전송 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 부산청과 알림 전송 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 통합송장 파일 처리 후 알림 전송 예시
        /// </summary>
        /// <param name="integratedFilePath">통합송장 파일 경로</param>
        /// <param name="batch">배치 정보</param>
        /// <param name="invoiceCount">송장 개수</param>
        public static async Task SendIntegratedNotificationAsync(string integratedFilePath, string batch, int invoiceCount)
        {
            try
            {
                Console.WriteLine("📤 통합송장 알림 전송 시작...");

                // 1. Dropbox에 파일 업로드
                var dropboxService = DropboxService.Instance;
                
                // App.config에서 Dropbox 경로 읽기
                var dropboxPath = System.Configuration.ConfigurationManager.AppSettings["KakaoWork.NotificationType.Integrated.DropboxPath"] ?? "/통합송장";
                string integratedUrl = await dropboxService.UploadFileAsync(integratedFilePath, dropboxPath);

                // 2. KakaoWork로 알림 전송
                var kakaoWorkService = KakaoWorkService.Instance;
                await kakaoWorkService.SendInvoiceNotificationAsync(
                    NotificationType.Integrated, 
                    batch, 
                    invoiceCount, 
                    integratedUrl);

                Console.WriteLine("✅ 통합송장 알림 전송 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 통합송장 알림 전송 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 모든 알림 종류에 대한 배치 처리 예시
        /// </summary>
        /// <param name="filePaths">각 알림 종류별 파일 경로</param>
        /// <param name="batch">배치 정보</param>
        /// <param name="invoiceCounts">각 알림 종류별 송장 개수</param>
        public static async Task SendAllNotificationsAsync(
            Dictionary<NotificationType, string> filePaths, 
            string batch, 
            Dictionary<NotificationType, int> invoiceCounts)
        {
            try
            {
                Console.WriteLine($"📤 배치 알림 전송 시작: {batch}");

                var dropboxService = DropboxService.Instance;
                var kakaoWorkService = KakaoWorkService.Instance;

                foreach (var kvp in filePaths)
                {
                    var notificationType = kvp.Key;
                    var filePath = kvp.Value;
                    var invoiceCount = invoiceCounts.GetValueOrDefault(notificationType, 0);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        Console.WriteLine($"📤 {notificationType} 알림 전송 중...");

                        // 1. Dropbox에 파일 업로드
                        // App.config에서 Dropbox 경로 읽기
                        var dropboxPathKey = $"KakaoWork.NotificationType.{notificationType}.DropboxPath";
                        var dropboxPath = System.Configuration.ConfigurationManager.AppSettings[dropboxPathKey] ?? $"/{notificationType}";
                        string fileUrl = await dropboxService.UploadFileAsync(filePath, dropboxPath);

                        // 2. KakaoWork로 알림 전송
                        await kakaoWorkService.SendInvoiceNotificationAsync(
                            notificationType, 
                            batch, 
                            invoiceCount, 
                            fileUrl);

                        Console.WriteLine($"✅ {notificationType} 알림 전송 완료");
                    }
                }

                Console.WriteLine("✅ 모든 알림 전송 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 배치 알림 전송 실패: {ex.Message}");
                throw;
            }
        }
    }
} 