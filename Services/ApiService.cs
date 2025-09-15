using System.Text;
using System.Text.Json;
using System.Configuration;

namespace LogisticManager.Services
{
    /// <summary>
    /// 외부 API 연동을 담당하는 서비스 클래스
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _dropboxFolderPath;
        private readonly string _kakaoWorkApiKey;
        private readonly string _kakaoWorkChatroomId;

        public ApiService()
        {
            _httpClient = new HttpClient();
            
            // App.config에서 API 키들을 읽어옴 (Dropbox는 DropboxService에서 처리)
            _dropboxFolderPath = ConfigurationManager.AppSettings["DropboxFolderPath"] ?? "/LogisticManager/";
            _kakaoWorkApiKey = ConfigurationManager.AppSettings["KakaoWork.AppKey"] 
                ?? throw new InvalidOperationException("Kakao Work API 키를 찾을 수 없습니다.");
            _kakaoWorkChatroomId = ConfigurationManager.AppSettings["KakaoWork.ChatroomId.Integrated"] 
                ?? throw new InvalidOperationException("Kakao Work 채팅방 ID를 찾을 수 없습니다.");
        }

        /// <summary>
        /// 파일을 Dropbox에 업로드하는 비동기 메서드
        /// </summary>
        /// <param name="filePath">업로드할 파일 경로</param>
        /// <param name="dropboxPath">Dropbox 내 저장 경로</param>
        /// <returns>업로드된 파일의 공유 링크</returns>
        public async Task<string> UploadFileToDropboxAsync(string filePath, string dropboxPath)
        {
            try
            {
                // DropboxService Singleton 인스턴스 사용
                var dropboxService = DropboxService.Instance;
                return await dropboxService.UploadFileAsync(filePath, dropboxPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Dropbox 업로드 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dropbox에 파일을 업로드하는 비동기 메서드 (파일명 지정 가능)
        /// </summary>
        /// <param name="filePath">업로드할 로컬 파일 경로</param>
        /// <param name="dropboxPath">Dropbox 폴더 경로</param>
        /// <param name="fileName">Dropbox에 저장할 파일명 (확장자 포함)</param>
        /// <returns>생성된 공유 링크 URL</returns>
        public async Task<string> UploadFileToDropboxAsync(string filePath, string dropboxPath, string fileName)
        {
            try
            {
                // DropboxService Singleton 인스턴스 사용
                var dropboxService = DropboxService.Instance;
                return await dropboxService.UploadFileAsync(filePath, dropboxPath, fileName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Dropbox 업로드 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Kakao Work로 메시지를 전송하는 비동기 메서드
        /// </summary>
        /// <param name="chatroomId">채팅방 ID</param>
        /// <param name="message">전송할 메시지</param>
        /// <param name="fileUrl">첨부 파일 URL (선택사항)</param>
        /// <returns>전송 성공 여부</returns>
        public async Task<bool> SendKakaoWorkMessageAsync(string chatroomId, string message, string? fileUrl = null)
        {
            try
            {
                // KakaoCheck 설정 확인 - 'Y'인 경우에만 카카오워크 메시지 전송
                string kakaoCheck = ConfigurationManager.AppSettings["KakaoCheck"] ?? "N";
                if (kakaoCheck.ToUpper() != "Y")
                {
                    LogManagerService.LogInfo($"⚠️ KakaoCheck 설정이 'Y'가 아닙니다 (현재: {kakaoCheck}). 카카오워크 메시지 전송을 건너뜁니다.");
                    return true; // 메시지 전송 없이 정상 종료
                }

                var requestData = new
                {
                    bot_key = _kakaoWorkApiKey,
                    room_id = chatroomId,
                    message = message,
                    type = "text"
                };

                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.kakao.com/v1/api/talk/friends/message/default/send", content);
                response.EnsureSuccessStatusCode();

                // 파일 URL이 제공된 경우 파일 메시지도 전송
                if (!string.IsNullOrEmpty(fileUrl))
                {
                    var fileMessageData = new
                    {
                        bot_key = _kakaoWorkApiKey,
                        room_id = chatroomId,
                        message = $"파일이 업로드되었습니다: {fileUrl}",
                        type = "text"
                    };

                    var fileJsonContent = JsonSerializer.Serialize(fileMessageData);
                    var fileContent = new StringContent(fileJsonContent, Encoding.UTF8, "application/json");

                    var fileResponse = await _httpClient.PostAsync("https://api.kakao.com/v1/api/talk/friends/message/default/send", fileContent);
                    fileResponse.EnsureSuccessStatusCode();
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Kakao Work 메시지 전송 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 