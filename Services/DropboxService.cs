using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dropbox.Api;
using Newtonsoft.Json.Linq;

namespace LogisticManager.Services
{
    /// <summary>
    /// Dropbox API를 위한 Singleton 서비스 클래스
    /// 파일 업로드, 공유 링크 생성, 토큰 자동 갱신 기능 제공
    /// </summary>
    public class DropboxService
    {
        #region Singleton 패턴 구현
        private static readonly Lazy<DropboxService> _instance = 
            new Lazy<DropboxService>(() => new DropboxService());
        
        /// <summary>
        /// DropboxService의 단일 인스턴스
        /// </summary>
        public static DropboxService Instance => _instance.Value;
        #endregion

        #region Private 필드
        private readonly string _appKey = string.Empty;
        private readonly string _appSecret = string.Empty;
        private readonly string _refreshToken = string.Empty;
        private DropboxClient? _client;
        private DateTime _tokenExpiration;
        private readonly HttpClient _httpClient = new HttpClient();
        #endregion

        #region Private 생성자
        /// <summary>
        /// App.config에서 Dropbox 인증 정보를 읽어와 초기화
        /// </summary>
        private DropboxService()
        {
            try
            {
                _appKey = ConfigurationManager.AppSettings["Dropbox.AppKey"] ?? string.Empty;
                _appSecret = ConfigurationManager.AppSettings["Dropbox.AppSecret"] ?? string.Empty;
                _refreshToken = ConfigurationManager.AppSettings["Dropbox.RefreshToken"] ?? string.Empty;
                
                // 인증 정보가 없어도 초기화는 성공하도록 수정 (실제 사용 시에만 오류 발생)
                if (string.IsNullOrEmpty(_appKey) || string.IsNullOrEmpty(_appSecret) || string.IsNullOrEmpty(_refreshToken))
                {
                    Console.WriteLine("⚠️ Dropbox 인증 정보가 App.config에 설정되지 않았습니다. Dropbox 기능을 사용할 수 없습니다.");
                }

                _tokenExpiration = DateTime.MinValue; // 초기값으로 설정하여 첫 호출 시 토큰 갱신 강제
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DropboxService 초기화 오류: {ex.Message}");
                // 초기화 오류가 있어도 애플리케이션은 계속 실행되도록 예외를 던지지 않음
            }
        }
        #endregion

        #region Private 메서드
        /// <summary>
        /// 유효한 DropboxClient 인스턴스를 반환
        /// 토큰이 만료되었거나 만료가 임박한 경우 자동으로 갱신
        /// </summary>
        /// <returns>유효한 DropboxClient 인스턴스</returns>
        private async Task<DropboxClient> GetClientAsync()
        {
            try
            {
                // 토큰이 만료되었거나 만료가 임박한 경우 (5분 전) 갱신
                if (_client == null || DateTime.UtcNow.AddMinutes(5) >= _tokenExpiration)
                {
                    await RefreshAccessTokenAsync();
                }

                return _client!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DropboxClient 생성 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Refresh Token을 사용하여 새로운 Access Token을 갱신
        /// </summary>
        private async Task RefreshAccessTokenAsync()
        {
            try
            {
                Console.WriteLine("Dropbox Access Token 갱신 시작...");

                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", _refreshToken),
                    new KeyValuePair<string, string>("client_id", _appKey),
                    new KeyValuePair<string, string>("client_secret", _appSecret)
                });

                var response = await _httpClient.PostAsync("https://api.dropboxapi.com/oauth2/token", tokenRequest);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"토큰 갱신 실패: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenData = JObject.Parse(responseContent);

                var accessToken = tokenData["access_token"]?.ToString();
                var expiresIn = tokenData["expires_in"]?.Value<int>() ?? 14400; // 기본값 4시간

                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException("토큰 응답에서 access_token을 찾을 수 없습니다.");
                }

                // 새로운 DropboxClient 생성
                _client = new DropboxClient(accessToken);
                _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn);

                Console.WriteLine($"Dropbox Access Token 갱신 완료. 만료 시간: {_tokenExpiration:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Access Token 갱신 오류: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Public 메서드
        /// <summary>
        /// 로컬 파일을 Dropbox에 업로드하고 공유 링크를 생성
        /// </summary>
        /// <param name="localFilePath">업로드할 로컬 파일 경로</param>
        /// <param name="dropboxFolderPath">Dropbox 폴더 경로 (예: /LogisticManager/)</param>
        /// <returns>생성된 공유 링크 URL</returns>
        public async Task<string> UploadFileAsync(string localFilePath, string dropboxFolderPath)
        {
            try
            {
                Console.WriteLine($"파일 업로드 시작: {localFilePath} -> {dropboxFolderPath}");

                // Dropbox 인증 정보 확인
                if (string.IsNullOrEmpty(_appKey) || string.IsNullOrEmpty(_appSecret) || string.IsNullOrEmpty(_refreshToken))
                {
                    throw new InvalidOperationException("Dropbox 인증 정보가 설정되지 않았습니다. App.config에서 Dropbox.AppKey, Dropbox.AppSecret, Dropbox.RefreshToken을 확인해주세요.");
                }

                // 파일 존재 여부 확인
                if (!File.Exists(localFilePath))
                {
                    throw new FileNotFoundException($"업로드할 파일을 찾을 수 없습니다: {localFilePath}");
                }

                // 유효한 클라이언트 확보
                var client = await GetClientAsync();

                // 파일명 추출
                var fileName = Path.GetFileName(localFilePath);
                var dropboxPath = Path.Combine(dropboxFolderPath, fileName).Replace('\\', '/');

                Console.WriteLine($"Dropbox 업로드 경로: {dropboxPath}");

                // 파일 업로드
                using (var fileStream = File.OpenRead(localFilePath))
                {
                    var uploadResult = await client.Files.UploadAsync(
                        dropboxPath,
                        Dropbox.Api.Files.WriteMode.Overwrite.Instance,
                        body: fileStream
                    );

                    Console.WriteLine($"파일 업로드 완료: {uploadResult.Name}");
                }

                // 공유 링크 생성
                var sharedLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                var sharedUrl = sharedLink.Url;

                // Dropbox 공유 링크를 다운로드 링크로 변환 (dl=1 파라미터 추가)
                var downloadUrl = sharedUrl.Replace("www.dropbox.com", "dl.dropboxusercontent.com");

                Console.WriteLine($"공유 링크 생성 완료: {downloadUrl}");

                return downloadUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 업로드 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Dropbox 연결 상태를 테스트
        /// </summary>
        /// <returns>연결 성공 여부</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine("Dropbox 연결 테스트 시작...");

                // Dropbox 인증 정보 확인
                if (string.IsNullOrEmpty(_appKey) || string.IsNullOrEmpty(_appSecret) || string.IsNullOrEmpty(_refreshToken))
                {
                    Console.WriteLine("Dropbox 인증 정보가 설정되지 않았습니다.");
                    return false;
                }

                var client = await GetClientAsync();
                
                // 계정 정보 조회로 연결 테스트
                var account = await client.Users.GetCurrentAccountAsync();
                
                Console.WriteLine($"Dropbox 연결 성공: {account.Name.DisplayName} ({account.Email})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dropbox 연결 테스트 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region IDisposable 구현
        public void Dispose()
        {
            _httpClient?.Dispose();
            _client?.Dispose();
        }
        #endregion
    }
} 