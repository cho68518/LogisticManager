using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Newtonsoft.Json.Linq;
using System.Linq; // Added for FirstOrDefault
using System.Collections.Generic; // Added for List

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
                // 초기화 오류가 있어도 프로그램은 계속 실행되도록 예외를 던지지 않음
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

                // 파이썬 코드와 동일한 토큰 URL 사용
                var response = await _httpClient.PostAsync("https://api.dropbox.com/oauth2/token", tokenRequest);
                
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
                    try
                    {
                        // 먼저 파일이 존재하는지 확인
                        try
                        {
                            var existingFile = await client.Files.GetMetadataAsync(dropboxPath);
                            Console.WriteLine($"파일이 이미 존재합니다: {existingFile.Name}. 덮어쓰기 모드로 업로드합니다.");
                        }
                        catch (Dropbox.Api.ApiException<Dropbox.Api.Files.GetMetadataError>)
                        {
                            Console.WriteLine($"새로운 파일을 업로드합니다: {fileName}");
                        }

                        var uploadResult = await client.Files.UploadAsync(
                            dropboxPath,
                            Dropbox.Api.Files.WriteMode.Overwrite.Instance,
                            body: fileStream
                        );

                        Console.WriteLine($"파일 업로드 완료: {uploadResult.Name}");
                    }
                    catch (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError> ex)
                    {
                        if (ex.ErrorResponse.IsPath)
                        {
                            // 경로 관련 오류인 경우에도 덮어쓰기 시도
                            Console.WriteLine($"경로 오류 발생, 덮어쓰기로 재시도합니다: {ex.Message}");
                            
                            // 파일 스트림을 다시 열어서 재시도
                            using (var retryStream = File.OpenRead(localFilePath))
                            {
                                var retryResult = await client.Files.UploadAsync(
                                    dropboxPath,
                                    Dropbox.Api.Files.WriteMode.Overwrite.Instance,
                                    body: retryStream
                                );
                                Console.WriteLine($"재시도 성공: {retryResult.Name}");
                            }
                        }
                        else
                        {
                            throw; // 다른 오류는 그대로 던지기
                        }
                    }
                }

                // 공유 링크 생성
                string sharedUrl;
                try
                {
                    var sharedLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                    sharedUrl = sharedLink.Url;
                    Console.WriteLine($"새로운 공유 링크 생성 완료");
                }
                catch (Dropbox.Api.ApiException<Dropbox.Api.Sharing.CreateSharedLinkWithSettingsError> ex)
                {
                    // 이미 공유 링크가 존재하는 경우
                    if (ex.ErrorResponse.IsSharedLinkAlreadyExists)
                    {
                        Console.WriteLine($"이미 공유 링크가 존재합니다. 기존 링크를 가져옵니다.");
                        
                        try
                        {
                            // 기존 공유 링크 목록에서 해당 파일의 링크 찾기
                            var sharedLinks = await client.Sharing.ListSharedLinksAsync(dropboxPath, directOnly: true);
                            var existingLink = sharedLinks.Links.FirstOrDefault(link => link.PathLower == dropboxPath.ToLower());
                            
                            if (existingLink != null)
                            {
                                sharedUrl = existingLink.Url;
                                Console.WriteLine($"기존 공유 링크 사용: {sharedUrl}");
                            }
                            else
                            {
                                // 공유 링크 목록에서 찾지 못한 경우, 다시 생성 시도
                                Console.WriteLine($"기존 링크를 찾을 수 없어 다시 생성 시도합니다.");
                                var retryLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                                sharedUrl = retryLink.Url;
                                Console.WriteLine($"재시도로 공유 링크 생성 완료");
                            }
                        }
                        catch (Exception listEx)
                        {
                            Console.WriteLine($"공유 링크 목록 조회 실패: {listEx.Message}. 다시 생성 시도합니다.");
                            var retryLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                            sharedUrl = retryLink.Url;
                            Console.WriteLine($"재시도로 공유 링크 생성 완료");
                        }
                    }
                    else
                    {
                        throw; // 다른 오류는 그대로 던지기
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"공유 링크 생성 중 오류 발생: {ex.Message}. 재시도합니다.");
                    
                    // 일반적인 오류의 경우에도 재시도
                    try
                    {
                        var retryLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                        sharedUrl = retryLink.Url;
                        Console.WriteLine($"재시도로 공유 링크 생성 완료");
                    }
                    catch (Exception retryEx)
                    {
                        throw new InvalidOperationException($"공유 링크 생성 실패: {ex.Message}. 재시도도 실패: {retryEx.Message}");
                    }
                }

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
        /// 로컬 파일을 Dropbox에 업로드하고 공유 링크를 생성 (파일명 지정 가능)
        /// </summary>
        /// <param name="localFilePath">업로드할 로컬 파일 경로</param>
        /// <param name="dropboxFolderPath">Dropbox 폴더 경로 (예: /LogisticManager/)</param>
        /// <param name="fileName">Dropbox에 저장할 파일명 (확장자 포함)</param>
        /// <returns>생성된 공유 링크 URL</returns>
        public async Task<string> UploadFileAsync(string localFilePath, string dropboxFolderPath, string fileName)
        {
            try
            {
                Console.WriteLine($"파일 업로드 시작: {localFilePath} -> {dropboxFolderPath}/{fileName}");

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

                // 지정된 파일명으로 Dropbox 경로 생성
                var dropboxPath = Path.Combine(dropboxFolderPath, fileName).Replace('\\', '/');

                Console.WriteLine($"Dropbox 업로드 경로: {dropboxPath}");

                // 파일 업로드
                using (var fileStream = File.OpenRead(localFilePath))
                {
                    try
                    {
                        // 먼저 파일이 존재하는지 확인
                        try
                        {
                            var existingFile = await client.Files.GetMetadataAsync(dropboxPath);
                            Console.WriteLine($"파일이 이미 존재합니다: {existingFile.Name}. 덮어쓰기 모드로 업로드합니다.");
                        }
                        catch (Dropbox.Api.ApiException<Dropbox.Api.Files.GetMetadataError>)
                        {
                            Console.WriteLine($"새로운 파일을 업로드합니다: {fileName}");
                        }

                        var uploadResult = await client.Files.UploadAsync(
                            dropboxPath,
                            Dropbox.Api.Files.WriteMode.Overwrite.Instance,
                            body: fileStream
                        );

                        Console.WriteLine($"파일 업로드 완료: {uploadResult.Name}");
                    }
                    catch (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError> ex)
                    {
                        if (ex.ErrorResponse.IsPath)
                        {
                            // 경로 관련 오류인 경우에도 덮어쓰기 시도
                            Console.WriteLine($"경로 오류 발생, 덮어쓰기로 재시도합니다: {ex.Message}");
                            
                            // 파일 스트림을 다시 열어서 재시도
                            using (var retryStream = File.OpenRead(localFilePath))
                            {
                                var retryResult = await client.Files.UploadAsync(
                                    dropboxPath,
                                    Dropbox.Api.Files.WriteMode.Overwrite.Instance,
                                    body: retryStream
                                );
                                Console.WriteLine($"재시도 성공: {retryResult.Name}");
                            }
                        }
                        else
                        {
                            throw; // 다른 오류는 그대로 던지기
                        }
                    }
                }

                // 공유 링크 생성
                string sharedUrl;
                try
                {
                    var sharedLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                    sharedUrl = sharedLink.Url;
                    Console.WriteLine($"새로운 공유 링크 생성 완료");
                }
                catch (Dropbox.Api.ApiException<Dropbox.Api.Sharing.CreateSharedLinkWithSettingsError> ex)
                {
                    // 이미 공유 링크가 존재하는 경우
                    if (ex.ErrorResponse.IsSharedLinkAlreadyExists)
                    {
                        Console.WriteLine($"이미 공유 링크가 존재합니다. 기존 링크를 가져옵니다.");
                        
                        try
                        {
                            // 기존 공유 링크 목록에서 해당 파일의 링크 찾기
                            var sharedLinks = await client.Sharing.ListSharedLinksAsync(dropboxPath, directOnly: true);
                            var existingLink = sharedLinks.Links.FirstOrDefault(link => link.PathLower == dropboxPath.ToLower());
                            
                            if (existingLink != null)
                            {
                                sharedUrl = existingLink.Url;
                                Console.WriteLine($"기존 공유 링크를 가져왔습니다.");
                            }
                            else
                            {
                                // 공유 링크 목록에서 찾지 못한 경우, 다시 생성 시도
                                Console.WriteLine($"기존 링크를 찾을 수 없어 다시 생성 시도합니다.");
                                var retryLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                                sharedUrl = retryLink.Url;
                                Console.WriteLine($"재시도로 공유 링크 생성 완료");
                            }
                        }
                        catch (Exception listEx)
                        {
                            Console.WriteLine($"공유 링크 목록 조회 실패: {listEx.Message}. 다시 생성 시도합니다.");
                            var retryLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                            sharedUrl = retryLink.Url;
                            Console.WriteLine($"재시도로 공유 링크 생성 완료");
                        }
                    }
                    else
                    {
                        throw; // 다른 오류는 그대로 던지기
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"공유 링크 생성 중 오류 발생: {ex.Message}. 재시도합니다.");
                    
                    // 일반적인 오류의 경우에도 재시도
                    try
                    {
                        var retryLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                        sharedUrl = retryLink.Url;
                        Console.WriteLine($"재시도로 공유 링크 생성 완료");
                    }
                    catch (Exception retryEx)
                    {
                        throw new InvalidOperationException($"공유 링크 생성 실패: {ex.Message}. 재시도도 실패: {retryEx.Message}");
                    }
                }

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
        /// Dropbox에 파일을 업로드하는 메서드 (공유 링크 생성 없음)
        /// </summary>
        /// <param name="localFilePath">로컬 파일 경로</param>
        /// <param name="dropboxFolderPath">Dropbox 폴더 경로</param>
        /// <returns>업로드 성공 여부</returns>
        public async Task<bool> UploadFileOnlyAsync(string localFilePath, string dropboxFolderPath)
        {
            try
            {
                Console.WriteLine($"📤 [UploadFileOnlyAsync] 파일 업로드 시작: {localFilePath} -> {dropboxFolderPath}");

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

                Console.WriteLine($"📤 [UploadFileOnlyAsync] Dropbox 업로드 경로: {dropboxPath}");

                // 파일 업로드
                using (var fileStream = File.OpenRead(localFilePath))
                {
                    try
                    {
                        // 먼저 파일이 존재하는지 확인
                        try
                        {
                            var existingFile = await client.Files.GetMetadataAsync(dropboxPath);
                            Console.WriteLine($"📤 [UploadFileOnlyAsync] 파일이 이미 존재합니다: {existingFile.Name}. 덮어쓰기 모드로 업로드합니다.");
                        }
                        catch (Dropbox.Api.ApiException<Dropbox.Api.Files.GetMetadataError>)
                        {
                            Console.WriteLine($"📤 [UploadFileOnlyAsync] 새로운 파일을 업로드합니다: {fileName}");
                        }

                        var uploadResult = await client.Files.UploadAsync(
                            dropboxPath,
                            Dropbox.Api.Files.WriteMode.Overwrite.Instance,
                            body: fileStream
                        );

                        Console.WriteLine($"✅ [UploadFileOnlyAsync] 파일 업로드 완료: {uploadResult.Name}");
                        return true;
                    }
                    catch (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError> ex)
                    {
                        if (ex.ErrorResponse.IsPath)
                        {
                            // 경로 관련 오류인 경우에도 덮어쓰기 시도
                            Console.WriteLine($"📤 [UploadFileOnlyAsync] 경로 오류 발생, 덮어쓰기로 재시도합니다: {ex.Message}");
                            
                            // 파일 스트림을 다시 열어서 재시도
                            using (var retryStream = File.OpenRead(localFilePath))
                            {
                                var retryResult = await client.Files.UploadAsync(
                                    dropboxPath,
                                    Dropbox.Api.Files.WriteMode.Overwrite.Instance,
                                    body: retryStream
                                );
                                Console.WriteLine($"✅ [UploadFileOnlyAsync] 재시도 성공: {retryResult.Name}");
                                return true;
                            }
                        }
                        else
                        {
                            throw; // 다른 오류는 그대로 던지기
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [UploadFileOnlyAsync] 파일 업로드 오류: {ex.Message}");
                return false;
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

        /// <summary>
        /// Dropbox에서 파일을 다운로드하는 메서드
        /// </summary>
        /// <param name="dropboxPath">Dropbox 파일 경로 (예: /folder/file.xlsx)</param>
        /// <param name="localFilePath">로컬 저장 경로</param>
        /// <returns>다운로드 성공 여부</returns>
        public async Task<bool> DownloadFileAsync(string dropboxPath, string localFilePath)
        {
            try
            {
                Console.WriteLine($"파일 다운로드 시작: {dropboxPath} -> {localFilePath}");

                // Dropbox 인증 정보 확인
                if (string.IsNullOrEmpty(_appKey) || string.IsNullOrEmpty(_appSecret) || string.IsNullOrEmpty(_refreshToken))
                {
                    throw new InvalidOperationException("Dropbox 인증 정보가 설정되지 않았습니다. App.config에서 Dropbox.AppKey, Dropbox.AppSecret, Dropbox.RefreshToken을 확인해주세요.");
                }

                // 유효한 클라이언트 확보
                var client = await GetClientAsync();

                // 파일 존재 여부 확인
                try
                {
                    var fileMetadata = await client.Files.GetMetadataAsync(dropboxPath);
                    if (fileMetadata.IsFolder)
                    {
                        throw new InvalidOperationException($"지정된 경로는 폴더입니다: {dropboxPath}");
                    }
                    
                    // 파일 크기 정보 출력 (FileMetadata 타입인 경우에만 Size 속성 접근)
                    long fileSize = 0;
                    if (fileMetadata is FileMetadata fileMeta)
                    {
                        fileSize = (long)fileMeta.Size;
                    }
                    Console.WriteLine($"파일 발견: {fileMetadata.Name} (크기: {fileSize} bytes)");
                }
                catch (Dropbox.Api.ApiException<Dropbox.Api.Files.GetMetadataError> ex)
                {
                    throw new FileNotFoundException($"Dropbox에서 파일을 찾을 수 없습니다: {dropboxPath}", ex);
                }

                // 로컬 디렉토리 생성
                var localDirectory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(localDirectory) && !Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                    Console.WriteLine($"로컬 디렉토리 생성: {localDirectory}");
                }

                // 파일 다운로드
                using (var response = await client.Files.DownloadAsync(dropboxPath))
                using (var fileStream = File.Create(localFilePath))
                {
                    var contentStream = await response.GetContentAsStreamAsync();
                    await contentStream.CopyToAsync(fileStream);
                }

                Console.WriteLine($"파일 다운로드 완료: {localFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"파일 다운로드 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Dropbox에서 파일 목록을 조회하는 메서드
        /// </summary>
        /// <param name="folderPath">조회할 폴더 경로 (예: /folder/)</param>
        /// <returns>파일 목록</returns>
        public async Task<List<string>> ListFilesAsync(string folderPath)
        {
            try
            {
                Console.WriteLine($"폴더 내용 조회 시작: {folderPath}");

                // Dropbox 인증 정보 확인
                if (string.IsNullOrEmpty(_appKey) || string.IsNullOrEmpty(_appSecret) || string.IsNullOrEmpty(_refreshToken))
                {
                    throw new InvalidOperationException("Dropbox 인증 정보가 설정되지 않았습니다.");
                }

                // 유효한 클라이언트 확보
                var client = await GetClientAsync();

                var fileList = new List<string>();

                // 폴더 내용 조회
                var listFolderResult = await client.Files.ListFolderAsync(folderPath);
                
                foreach (var item in listFolderResult.Entries)
                {
                    if (item.IsFile)
                    {
                        fileList.Add(item.PathDisplay);
                        Console.WriteLine($"파일 발견: {item.Name}");
                    }
                }

                Console.WriteLine($"폴더 내용 조회 완료: {fileList.Count}개 파일");
                return fileList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"폴더 내용 조회 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Dropbox 파일의 공유 링크를 생성하는 메서드
        /// </summary>
        /// <param name="dropboxPath">Dropbox 파일 경로</param>
        /// <returns>공유 링크 URL</returns>
        public async Task<string?> CreateSharedLinkAsync(string dropboxPath)
        {
            try
            {
                Console.WriteLine($"🔗 [DropboxService] 공유 링크 생성 시작: {dropboxPath}");

                // Dropbox 인증 정보 확인
                if (string.IsNullOrEmpty(_appKey) || string.IsNullOrEmpty(_appSecret) || string.IsNullOrEmpty(_refreshToken))
                {
                    var errorMsg = "Dropbox 인증 정보가 설정되지 않았습니다.";
                    Console.WriteLine($"❌ [DropboxService] {errorMsg}");
                    throw new InvalidOperationException(errorMsg);
                }

                Console.WriteLine($"✅ [DropboxService] Dropbox 인증 정보 확인 완료");

                // 유효한 클라이언트 확보
                Console.WriteLine($"🔗 [DropboxService] Dropbox 클라이언트 획득 시작...");
                var client = await GetClientAsync();
                Console.WriteLine($"✅ [DropboxService] Dropbox 클라이언트 획득 완료");

                // 공유 링크 생성 (이미 존재하는 경우 기존 링크 반환)
                Console.WriteLine($"🔗 [DropboxService] 공유 링크 API 호출 시작...");
                var sharedLink = await client.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);
                Console.WriteLine($"✅ [DropboxService] 공유 링크 API 호출 완료");
                
                if (sharedLink == null)
                {
                    Console.WriteLine($"❌ [DropboxService] 공유 링크 API 응답이 null입니다.");
                    return null;
                }

                Console.WriteLine($"📋 [DropboxService] 원본 공유 링크: {sharedLink.Url}");
                
                // 공유 링크 URL 반환 (dl=1 파라미터 추가로 직접 다운로드 링크 생성)
                var downloadUrl = sharedLink.Url.Replace("www.dropbox.com", "dl.dropboxusercontent.com");
                
                Console.WriteLine($"✅ [DropboxService] 공유 링크 생성 완료: {downloadUrl}");
                return downloadUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [DropboxService] 공유 링크 생성 오류: {ex.Message}");
                Console.WriteLine($"📋 [DropboxService] 예외 타입: {ex.GetType().Name}");
                Console.WriteLine($"📋 [DropboxService] 스택 트레이스: {ex.StackTrace}");
                
                // 내부 예외가 있는 경우 추가 로그
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"📋 [DropboxService] 내부 예외: {ex.InnerException.Message}");
                }
                
                return null;
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