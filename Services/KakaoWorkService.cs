using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using LogisticManager.Models;
using System.Linq; // Added for First()

namespace LogisticManager.Services
{
    /// <summary>
    /// KakaoWork API를 위한 Singleton 서비스 클래스
    /// 알림 종류별로 적절한 채팅방에 메시지를 전송하는 기능 제공
    /// </summary>
    public class KakaoWorkService
    {
        #region Singleton 패턴 구현
        private static readonly Lazy<KakaoWorkService> _instance = 
            new Lazy<KakaoWorkService>(() => 
            {
                try
                {
                    return new KakaoWorkService();
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ KakaoWorkService 인스턴스 생성 실패: {ex.Message}");
                    // 기본 인스턴스 반환
                    return new KakaoWorkService(true); // 안전 모드로 생성
                }
            });
        
        /// <summary>
        /// KakaoWorkService의 단일 인스턴스
        /// </summary>
        public static KakaoWorkService Instance => _instance.Value;
        #endregion

        #region Private 필드
        private readonly HttpClient _httpClient;
        private readonly Dictionary<NotificationType, string> _chatroomIds;
        private static readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kakaowork_debug.log");
        #endregion

        #region Private 생성자
        /// <summary>
        /// App.config에서 KakaoWork 인증 정보와 채팅방 ID들을 읽어와 초기화
        /// </summary>
        /// <param name="safeMode">안전 모드로 초기화 (기본값: false)</param>
        private KakaoWorkService(bool safeMode = false)
        {
            if (safeMode)
            {
                LogMessage("🛡️ KakaoWorkService 안전 모드로 초기화...");
                _httpClient = new HttpClient();
                _chatroomIds = new Dictionary<NotificationType, string>();
                LogMessage("⚠️ KakaoWorkService 안전 모드 초기화 완료");
                return;
            }

            try
            {
                LogMessage("🔄 KakaoWorkService 초기화 시작...");
                
                // HttpClient 초기화 (먼저 생성)
                _httpClient = new HttpClient();
                
                // API 키 읽기
                string appKey = ConfigurationManager.AppSettings["KakaoWork.AppKey"] ?? string.Empty;
                LogMessage($"🔑 KakaoWork API 키 확인: {(string.IsNullOrEmpty(appKey) ? "없음" : "설정됨")}");
                
                if (string.IsNullOrEmpty(appKey))
                {
                    LogMessage("⚠️ KakaoWork API 키가 App.config에 설정되지 않았습니다. KakaoWork 기능을 사용할 수 없습니다.");
                    // 초기화는 계속 진행하되, 실제 사용 시에만 오류 발생
                }
                else
                {
                    // API 키가 있으면 Authorization 헤더 설정
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appKey);
                    LogMessage("✅ KakaoWork Authorization 헤더 설정 완료");
                }

                // App.config에서 모든 채팅방 ID를 읽어와 Dictionary에 저장
                _chatroomIds = new Dictionary<NotificationType, string>();
                foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
                {
                    string key = $"KakaoWork.ChatroomId.{type}";
                    string? chatroomId = ConfigurationManager.AppSettings[key];
                    if (!string.IsNullOrEmpty(chatroomId))
                    {
                        _chatroomIds[type] = chatroomId!;
                        LogMessage($"✅ KakaoWorkService: {type} 채팅방 ID 로드 완료 - {chatroomId}");
                    }
                    else
                    {
                        LogMessage($"⚠️ KakaoWorkService: {type} 채팅방 ID가 설정되지 않음");
                    }
                }

                LogMessage($"✅ KakaoWorkService 초기화 완료 - {_chatroomIds.Count}개 채팅방 설정됨");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ KakaoWorkService 초기화 오류: {ex.Message}");
                LogMessage($"🔍 상세 오류: {ex}");
                // 초기화 실패 시에도 기본값으로 설정하여 애플리케이션 시작은 가능하도록 함
                _httpClient = new HttpClient();
                _chatroomIds = new Dictionary<NotificationType, string>();
                LogMessage("⚠️ KakaoWorkService 기본값으로 초기화됨");
            }
        }
        #endregion

        #region Public 메서드
        /// <summary>
        /// 송장 처리 완료 알림을 지정된 채팅방에 전송
        /// </summary>
        /// <param name="type">알림 종류 (채팅방 자동 선택)</param>
        /// <param name="batch">배치 정보 (예: "2차")</param>
        /// <param name="invoiceCount">처리된 송장 개수</param>
        /// <param name="fileUrl">업로드된 파일 URL</param>
        /// <param name="titleSuffix">제목 접미사 (기본값: "운송장")</param>
        /// <returns>전송 성공 여부</returns>
        public async Task SendInvoiceNotificationAsync(NotificationType type, string batch, int invoiceCount, string fileUrl, string titleSuffix = "")
        {
            try
            {
                // 성공한 패턴 적용: 배치 변수 수정
                batch = "테스트 모니터링";
                
                LogMessage($"📤 KakaoWork 알림 전송 시작: {type} -> {batch}");

                // KakaoWork API 키 확인
                if (string.IsNullOrEmpty(_httpClient.DefaultRequestHeaders.Authorization?.Parameter))
                {
                    throw new InvalidOperationException("KakaoWork API 키가 설정되지 않았습니다. App.config에서 KakaoWork.AppKey를 확인해주세요.");
                }

                // 채팅방 ID 확인
                if (!_chatroomIds.TryGetValue(type, out string? chatroomId) || string.IsNullOrEmpty(chatroomId))
                {
                    throw new ArgumentException($"알림 타입 '{type}'에 해당하는 채팅방 ID가 App.config에 설정되지 않았습니다.");
                }

                // App.config에서 제목 접미사 읽기 (기본값: "운송장")
                if (string.IsNullOrEmpty(titleSuffix))
                {
                    var configKey = $"KakaoWork.NotificationType.{type}.TitleSuffix";
                    titleSuffix = ConfigurationManager.AppSettings[configKey] ?? "운송장";
                }

                // 파이썬 코드와 정확히 동일한 제목 생성 (간단하게)
                string title = $"{batch} - {GetKoreanName(type)}";

                LogMessage($"📝 메시지 제목: {title}");
                LogMessage($"💬 채팅방 ID: {chatroomId}");

                // 성공한 패턴 적용: 간단한 블록 구조 (헤더 제거, 텍스트 메시지 변경)
                var payload = new KakaoWorkPayload
                {
                    ConversationId = chatroomId,
                    Text = title,
                    Blocks =
                    {
                        new TextBlock { Text = "파일을 다운로드 후 파일을 열어 확인해주세요", Markdown = true },
                        new ButtonBlock { Text = $"{GetKoreanName(type)} 파일 다운로드", Value = fileUrl, Style = "default", ActionType = "open_system_browser" }
                    }
                };

                // JSON 직렬화 (파이썬과 동일한 형식)
                var jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.None
                });

                LogMessage($"📦 JSON 페이로드 크기: {jsonPayload.Length} bytes");
                LogMessage($"📦 JSON 페이로드 내용: {jsonPayload}");

                // 파이썬 코드와 비교를 위한 예상 JSON
                var expectedJson = $"{{\"conversation_id\":\"{chatroomId}\",\"text\":\"{title}\",\"blocks\":[{{\"type\":\"text\",\"text\":\"파일을 다운로드 후 파일을 열어 확인해주세요\",\"markdown\":true}},{{\"type\":\"button\",\"text\":\"{GetKoreanName(type)} 파일 다운로드\",\"style\":\"default\",\"action_type\":\"open_system_browser\",\"value\":\"{fileUrl}\"}}]}}";
                LogMessage($"📦 예상 JSON (파이썬과 동일): {expectedJson}");

                // HTTP 요청 전송
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // 요청 헤더 로깅
                LogMessage($"🔗 요청 URL: https://api.kakaowork.com/v1/messages.send");
                LogMessage($"🔑 Authorization 헤더: Bearer {_httpClient.DefaultRequestHeaders.Authorization?.Parameter}");
                LogMessage($"📋 Content-Type: {content.Headers.ContentType}");
                
                var response = await _httpClient.PostAsync("https://api.kakaowork.com/v1/messages.send", content);
                
                LogMessage($"📡 HTTP 상태 코드: {response.StatusCode}");
                LogMessage($"📡 HTTP 상태 설명: {response.ReasonPhrase}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"✅ KakaoWork 알림 전송 성공: {type}");
                    LogMessage($"📨 응답 내용: {responseContent}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"❌ KakaoWork API 오류: {response.StatusCode}");
                    LogMessage($"❌ 오류 내용: {errorContent}");
                    
                    // 응답 헤더도 로깅
                    LogMessage($"📋 응답 헤더:");
                    foreach (var header in response.Headers)
                    {
                        LogMessage($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }
                    
                    throw new InvalidOperationException($"KakaoWork API 오류: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ KakaoWork 알림 전송 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// KakaoWork 연결 상태를 테스트
        /// </summary>
        /// <returns>연결 성공 여부</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                LogMessage("🔗 KakaoWork 연결 테스트 시작...");

                // KakaoWork API 키 확인
                if (string.IsNullOrEmpty(_httpClient.DefaultRequestHeaders.Authorization?.Parameter))
                {
                    LogMessage("❌ KakaoWork API 키가 설정되지 않았습니다.");
                    return false;
                }

                LogMessage($"🔑 API 키 확인: {_httpClient.DefaultRequestHeaders.Authorization.Parameter.Substring(0, 10)}...");
                LogMessage($"📋 설정된 채팅방 ID 개수: {_chatroomIds.Count}");
                
                foreach (var kvp in _chatroomIds)
                {
                    LogMessage($"  {kvp.Key}: {kvp.Value}");
                }

                // 성공한 패턴 적용: 간단한 테스트 메시지 (헤더 제거)
                var testPayload = new KakaoWorkPayload
                {
                    ConversationId = _chatroomIds.First().Value, // 첫 번째 채팅방 사용
                    Text = "2차 - 판매입력_이카운트자료",
                    Blocks =
                    {
                        new TextBlock { Text = "파일을 다운로드 후 파일을 열어 확인해주세요", Markdown = true },
                        new ButtonBlock { Text = "판매입력 파일 다운로드", Value = "https://example.com/test", Style = "default", ActionType = "open_system_browser" }
                    }
                };

                var jsonPayload = JsonConvert.SerializeObject(testPayload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                LogMessage($"📦 테스트 JSON 페이로드: {jsonPayload}");

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.kakaowork.com/v1/messages.send", content);
                
                LogMessage($"📡 HTTP 상태 코드: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"✅ KakaoWork 연결 성공: {responseContent}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"❌ KakaoWork 연결 실패: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ KakaoWork 연결 테스트 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Private 헬퍼 메서드
        /// <summary>
        /// 로그 메시지를 파일과 콘솔에 출력
        /// </summary>
        /// <param name="message">로그 메시지</param>
        private static void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}";
            
            // 콘솔에 출력 (UTF-8 인코딩 사용)
            try
            {
                // PowerShell에서 한글 출력을 위한 설정
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    Console.OutputEncoding = Encoding.UTF8;
                }
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                // 콘솔 출력 실패 시 파일에만 저장
                Console.WriteLine($"Console output failed: {ex.Message}");
            }
            
            // 파일에 출력 (UTF-8 인코딩 사용)
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그 파일 쓰기 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 알림 종류에 따른 한글 이름을 반환
        /// </summary>
        /// <param name="type">알림 종류</param>
        /// <returns>한글 이름</returns>
        private static string GetKoreanName(NotificationType type)
        {
            // App.config에서 한글 이름 읽기
            var configKey = $"KakaoWork.NotificationType.{type}.Name";
            var koreanName = ConfigurationManager.AppSettings[configKey];
            
            if (!string.IsNullOrEmpty(koreanName))
            {
                return koreanName;
            }
            
            // App.config에 설정이 없으면 기본값 반환
            return type switch
            {
                NotificationType.SeoulFrozen => "서울냉동",
                NotificationType.GyeonggiFrozen => "경기냉동",
                NotificationType.SeoulGongsan => "서울공산",
                NotificationType.GyeonggiGongsan => "경기공산",
                NotificationType.BusanCheonggwa => "부산청과",
                NotificationType.GamcheonFrozen => "감천냉동",
                NotificationType.SalesData => "판매입력",
                NotificationType.Integrated => "통합송장",
                NotificationType.Check => "모니터링체크용(봇방)",
                _ => type.ToString()
            };
        }
        #endregion

        #region IDisposable 구현
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
        #endregion
    }
} 