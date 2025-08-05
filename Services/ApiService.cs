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
        private readonly string _dropboxApiKey;
        private readonly string _dropboxFolderPath;
        private readonly string _kakaoWorkApiKey;
        private readonly string _kakaoWorkChatroomId;

        public ApiService()
        {
            _httpClient = new HttpClient();
            
            // App.config에서 API 키들을 읽어옴
            _dropboxApiKey = ConfigurationManager.AppSettings["DropboxApiKey"] 
                ?? throw new InvalidOperationException("Dropbox API 키를 찾을 수 없습니다.");
            _dropboxFolderPath = ConfigurationManager.AppSettings["DropboxFolderPath"] ?? "/LogisticManager/";
            _kakaoWorkApiKey = ConfigurationManager.AppSettings["KakaoWorkApiKey"] 
                ?? throw new InvalidOperationException("Kakao Work API 키를 찾을 수 없습니다.");
            _kakaoWorkChatroomId = ConfigurationManager.AppSettings["KakaoWorkChatroomId"] 
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
                // 파일이 존재하는지 확인
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"업로드할 파일을 찾을 수 없습니다: {filePath}");
                }

                // 파일을 바이트 배열로 읽기
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                var fullDropboxPath = $"{_dropboxFolderPath.TrimEnd('/')}/{dropboxPath}/{fileName}";

                // Dropbox API 요청 준비
                var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload");
                request.Headers.Add("Authorization", $"Bearer {_dropboxApiKey}");
                request.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(new
                {
                    path = fullDropboxPath,
                    mode = "overwrite",
                    autorename = true,
                    mute = false
                }));

                request.Content = new ByteArrayContent(fileBytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                // 파일 업로드
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // 공유 링크 생성
                var shareRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/sharing/create_shared_link_with_settings");
                shareRequest.Headers.Add("Authorization", $"Bearer {_dropboxApiKey}");
                shareRequest.Content = new StringContent(
                    JsonSerializer.Serialize(new { path = fullDropboxPath }),
                    Encoding.UTF8,
                    "application/json"
                );

                var shareResponse = await _httpClient.SendAsync(shareRequest);
                shareResponse.EnsureSuccessStatusCode();

                var shareResult = await shareResponse.Content.ReadAsStringAsync();
                var shareData = JsonSerializer.Deserialize<JsonElement>(shareResult);
                
                return shareData.GetProperty("url").GetString() ?? string.Empty;
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