using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private static readonly string _logFilePath = LogPathManager.KakaoWorkDebugLogPath;
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
                // 초기화 실패 시에도 기본값으로 설정하여 프로그램 시작은 가능하도록 함
                _httpClient = new HttpClient();
                _chatroomIds = new Dictionary<NotificationType, string>();
                LogMessage("⚠️ KakaoWorkService 기본값으로 초기화됨");
            }
        }
        #endregion

        #region Public 메서드
        /// <summary>
        /// 송장 처리 완료 알림을 지정된 채팅방에 전송
        /// 새로운 메시지 빌더를 사용하여 메시지 타입별로 적절한 구조 생성
        /// </summary>
        /// <param name="type">알림 종류 (채팅방 자동 선택)</param>
        /// <param name="batch">배치 정보 (예: "2차")</param>
        /// <param name="invoiceCount">처리된 송장 개수</param>
        /// <param name="fileUrl">업로드된 파일 URL</param>
        /// <param name="titleSuffix">제목 접미사 (기본값: "운송장")</param>
        /// <returns>전송 성공 여부</returns>
        public async Task SendInvoiceNotificationAsync(NotificationType type, string batch, int invoiceCount, string fileUrl, string? chatroomId = null, string titleSuffix = "")
        {
            try
            {
                LogMessage($"📤 KakaoWork 알림 전송 시작: {type} -> {batch}");

                // KakaoCheck 설정 확인 - 'Y'인 경우에만 카카오워크 메시지 전송
                string kakaoCheck = ConfigurationManager.AppSettings["KakaoCheck"] ?? "N";
                if (kakaoCheck.ToUpper() != "Y")
                {
                    LogMessage($"⚠️ KakaoCheck 설정이 'Y'가 아닙니다 (현재: {kakaoCheck}). 카카오워크 메시지 전송을 건너뜁니다.");
                    return; // 메시지 전송 없이 정상 종료
                }

                // KakaoWork API 키 확인
                if (string.IsNullOrEmpty(_httpClient.DefaultRequestHeaders.Authorization?.Parameter))
                {
                    throw new InvalidOperationException("KakaoWork API 키가 설정되지 않았습니다. App.config에서 KakaoWork.AppKey를 확인해주세요.");
                }

                // 채팅방 ID 결정: 명시적으로 전달된 값 우선, 없으면 타입 기반 기본값 사용
                string targetChatroomId = !string.IsNullOrWhiteSpace(chatroomId)
                    ? chatroomId
                    : GetChatroomId(type);

                if (string.IsNullOrWhiteSpace(targetChatroomId))
                {
                    throw new ArgumentException($"알림 타입 '{type}'에 대한 채팅방 ID를 결정할 수 없습니다. App.config 설정을 확인하거나 chatroomId를 직접 전달하세요.");
                }

                // 메시지 타입 결정 (단계별로 다른 메시지 구조 사용)
                var messageType = GetMessageTypeByNotificationType(type);
                
                // 출고지 이름 가져오기
                var centerName = GetKoreanName(type);
                
                // 새로운 메시지 빌더를 사용하여 메시지 생성
                var messagePayload = KakaoWorkMessageBuilder.Build(
                    messageType, 
                    batch, 
                    fileUrl, 
                    centerName, 
                    invoiceCount);
                
                // 채팅방 ID 설정
                messagePayload.ConversationId = targetChatroomId;

                LogMessage($"📝 메시지 제목: {messagePayload.Text}");
                LogManagerService.LogInfo($"💬 채팅방 ID: {targetChatroomId}");
                LogManagerService.LogInfo($"🔧 메시지 타입: {messageType}");

                // JSON 직렬화 (System.Text.Json 사용)
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    IncludeFields = false
                };
                var jsonPayload = JsonSerializer.Serialize(messagePayload, jsonOptions);

                LogMessage($"📦 JSON 페이로드 크기: {jsonPayload.Length} bytes");
                LogMessage($"📦 JSON 페이로드 내용: {jsonPayload}");

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
        /// KakaoWork 연결 상태를 테스트 (실제 메시지 전송 없이)
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

                // 실제 메시지 전송 없이 연결만 테스트
                // KakaoWork API의 사용자 정보 조회 엔드포인트 사용 (메시지 전송 없음)
                var response = await _httpClient.GetAsync("https://api.kakaowork.com/v1/users.me");
                
                LogMessage($"📡 HTTP 상태 코드: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"✅ KakaoWork 연결 성공 (사용자 정보 조회): {responseContent}");
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
                NotificationType.SeoulFrozen2 => "서울냉장",
                NotificationType.GyeonggiFrozen => "경기냉동",
                NotificationType.FrapwonFrozen => "프랩원냉동",
                NotificationType.SeoulGongsan => "서울공산",
                NotificationType.GyeonggiGongsan => "경기공산",
                NotificationType.BusanCheonggwa => "부산청과",
                NotificationType.BusanCheonggwaPrint => "부산청과",
                NotificationType.GamcheonFrozen => "감천냉동",
                NotificationType.SalesData => "판매입력",
                NotificationType.Integrated => "통합 송장",
                NotificationType.Check => "모니터링체크용(봇방)",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// 알림 종류에 따른 메시지 타입을 결정
        /// </summary>
        /// <param name="type">알림 종류</param>
        /// <returns>메시지 타입</returns>
        private static KakaoWorkMessageType GetMessageTypeByNotificationType(NotificationType type)
        {
            return type switch
            {
                // [4-4] 단계: 판매입력 타입
                NotificationType.SalesData => KakaoWorkMessageType.SalesInput,
                
                // [4-8], [4-10], [4-12], [4-14], [4-16], [4-18], [4-20]: 운송장 타입
                NotificationType.SeoulFrozen => KakaoWorkMessageType.Shipment,
                NotificationType.SeoulFrozen2 => KakaoWorkMessageType.Shipment,
                NotificationType.GyeonggiFrozen => KakaoWorkMessageType.Shipment,
                NotificationType.FrapwonFrozen => KakaoWorkMessageType.Shipment,
                NotificationType.SeoulGongsan => KakaoWorkMessageType.Shipment,
                NotificationType.GyeonggiGongsan => KakaoWorkMessageType.Shipment,
                                        NotificationType.BusanCheonggwa => KakaoWorkMessageType.Shipment,
                        NotificationType.BusanCheonggwaPrint => KakaoWorkMessageType.PrintMaterial,
                        NotificationType.GamcheonFrozen => KakaoWorkMessageType.Shipment,
                
                // [4-22] 단계: 감천냉동 운송장 타입
                // [4-23] 단계: 송장출력 최종 처리 타입
                // [4-24] 단계: 통합 송장도 Shipment 패턴과 동일하게 사용
                NotificationType.Integrated => KakaoWorkMessageType.Shipment,
                
                // 기본값: 운송장 타입
                _ => KakaoWorkMessageType.Shipment
            };
        }
        #endregion

        #region 판매입력 데이터 전송 (Sales Data Notification)

        /// <summary>
        /// 판매입력 이카운트 자료를 특정 채팅방에 전송하는 메서드 (채팅방 ID 직접 지정 가능)
        /// 
        /// 처리 과정:
        /// 1. 현재 시간 기반 배치 구분 (1차~막차, 추가)
        /// 2. 파일 다운로드 링크와 함께 메시지 전송
        /// 3. 헤더, 텍스트, 버튼 블록으로 구성된 메시지
        /// 
        /// 배치 구분 규칙:
        /// - 1차: 01:00~07:00
        /// - 2차: 08:00~10:00
        /// - 3차: 11:00~11:00
        /// - 4차: 12:00~13:00
        /// - 5차: 14:00~15:00
        /// - 막차: 16:00~18:00
        /// - 추가: 19:00~23:00
        /// - 기타: 00:00
        /// 
        /// 메시지 구성:
        /// - 헤더: 배치 - 판매입력_이카운트자료
        /// - 텍스트: 파일 다운로드 후 DB로 한번 더 돌려주세요
        /// - 버튼: 판매입력 파일 다운로드 (파일 링크)
        /// </summary>
        /// <param name="fileUrl">Dropbox 공유 링크</param>
        /// <param name="chatroomId">카카오워크 채팅방 ID (null 또는 빈 값이면 기본값 사용)</param>
        /// <returns>전송 성공 여부</returns>
        public async Task<bool> SendSalesDataNotificationAsync(string fileUrl, string? chatroomId = null)
        {
            try
            {
                // 한글 주석: 판매입력 데이터 알림 전송 시작 로그
                LogMessage("🔄 판매입력 데이터 알림 전송 시작...");
                LogMessage($"📋 전달받은 채팅방 ID: {chatroomId ?? "null"}");
                LogMessage($"🔗 파일 URL: {fileUrl}");

                // 한글 주석: 현재 시간 기반 배치 구분
                var now = DateTime.Now;
                var timeString = now.ToString("MM월 dd일 HH시 mm분");
                var batch = BatchTimeService.Instance.GetCurrentBatchType();
                LogMessage($"⏰ 현재 시간: {timeString}, 배치: {batch}");

                // 한글 주석: 채팅방 ID가 명시적으로 전달되지 않으면 기본값 사용
                string targetChatroomId = !string.IsNullOrWhiteSpace(chatroomId)
                    ? chatroomId
                    : GetChatroomId(NotificationType.Check);
                
                LogMessage($"🎯 최종 사용할 채팅방 ID: {targetChatroomId}");

                if (string.IsNullOrWhiteSpace(targetChatroomId))
                {
                    LogMessage("❌ 카카오워크 채팅방 ID가 지정되지 않았습니다.");
                    return false;
                }

                // 새로운 메시지 빌더를 사용하여 메시지 생성
                var messagePayload = KakaoWorkMessageBuilder.Build(
                    KakaoWorkMessageType.SalesInput,
                    batch,
                    fileUrl);
                
                // 채팅방 ID 설정
                messagePayload.ConversationId = targetChatroomId;

                LogMessage($"📝 메시지 구성 완료: conversation_id={targetChatroomId}, text={messagePayload.Text}");

                // JSON 직렬화 (System.Text.Json 사용)
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    IncludeFields = false
                };
                var jsonContent = JsonSerializer.Serialize(messagePayload, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                LogMessage($"📦 JSON 페이로드 크기: {jsonContent.Length} bytes");
                LogMessage($"📦 JSON 페이로드 내용: {jsonContent}");

                // 한글 주석: 카카오워크 API 호출
                LogMessage($"🔗 요청 URL: https://api.kakaowork.com/v1/messages.send");
                LogMessage($"🔑 Authorization 헤더: Bearer {_httpClient.DefaultRequestHeaders.Authorization?.Parameter ?? "설정되지 않음"}");
                LogMessage($"📋 Content-Type: {content.Headers.ContentType}");
                
                var response = await _httpClient.PostAsync("https://api.kakaowork.com/v1/messages.send", content);
                
                LogMessage($"📡 HTTP 상태 코드: {response.StatusCode}");
                LogMessage($"📡 HTTP 상태 설명: {response.ReasonPhrase}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"✅ 판매입력 데이터 알림 전송 성공: {responseContent}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"❌ 판매입력 데이터 알림 전송 실패: {response.StatusCode} - {errorContent}");
                    
                    // 응답 헤더도 로깅
                    LogMessage($"📋 응답 헤더:");
                    foreach (var header in response.Headers)
                    {
                        LogMessage($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 판매입력 데이터 알림 전송 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 알림 종류에 따른 채팅방 ID를 반환하는 메서드
        /// </summary>
        /// <param name="type">알림 종류</param>
        /// <returns>채팅방 ID</returns>
        private string GetChatroomId(NotificationType type)
        {
            // 한글 주석: 알림 종류에 따른 채팅방 ID 조회
            if (_chatroomIds.TryGetValue(type, out var chatroomId))
            {
                return chatroomId;
            }

            // 한글 주석: 기본값으로 Check 채팅방 사용
            if (_chatroomIds.TryGetValue(NotificationType.Check, out var defaultChatroomId))
            {
                return defaultChatroomId;
            }

            // 한글 주석: 설정된 채팅방이 없으면 빈 문자열 반환
            return string.Empty;
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