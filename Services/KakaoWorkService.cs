using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LogisticManager.Models;

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
                    Console.WriteLine($"❌ KakaoWorkService 인스턴스 생성 실패: {ex.Message}");
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
                Console.WriteLine("🛡️ KakaoWorkService 안전 모드로 초기화...");
                _httpClient = new HttpClient();
                _chatroomIds = new Dictionary<NotificationType, string>();
                Console.WriteLine("⚠️ KakaoWorkService 안전 모드 초기화 완료");
                return;
            }

            try
            {
                Console.WriteLine("🔄 KakaoWorkService 초기화 시작...");
                
                // HttpClient 초기화 (먼저 생성)
                _httpClient = new HttpClient();
                
                // API 키 읽기
                string appKey = ConfigurationManager.AppSettings["KakaoWork.AppKey"] ?? string.Empty;
                Console.WriteLine($"🔑 KakaoWork API 키 확인: {(string.IsNullOrEmpty(appKey) ? "없음" : "설정됨")}");
                
                if (string.IsNullOrEmpty(appKey))
                {
                    Console.WriteLine("⚠️ KakaoWork API 키가 App.config에 설정되지 않았습니다. KakaoWork 기능을 사용할 수 없습니다.");
                    // 초기화는 계속 진행하되, 실제 사용 시에만 오류 발생
                }
                else
                {
                    // API 키가 있으면 Authorization 헤더 설정
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appKey);
                    Console.WriteLine("✅ KakaoWork Authorization 헤더 설정 완료");
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
                        Console.WriteLine($"✅ KakaoWorkService: {type} 채팅방 ID 로드 완료 - {chatroomId}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ KakaoWorkService: {type} 채팅방 ID가 설정되지 않음");
                    }
                }

                Console.WriteLine($"✅ KakaoWorkService 초기화 완료 - {_chatroomIds.Count}개 채팅방 설정됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ KakaoWorkService 초기화 오류: {ex.Message}");
                Console.WriteLine($"🔍 상세 오류: {ex}");
                // 초기화 실패 시에도 기본값으로 설정하여 애플리케이션 시작은 가능하도록 함
                _httpClient = new HttpClient();
                _chatroomIds = new Dictionary<NotificationType, string>();
                Console.WriteLine("⚠️ KakaoWorkService 기본값으로 초기화됨");
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
        public async Task SendInvoiceNotificationAsync(NotificationType type, string batch, int invoiceCount, string fileUrl, string titleSuffix = "운송장")
        {
            try
            {
                Console.WriteLine($"📤 KakaoWork 알림 전송 시작: {type} -> {batch}");

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

                // 제목 생성 (한글 변환 포함)
                string title = $"{batch} - {GetKoreanName(type)} {titleSuffix} 수집 완료";

                Console.WriteLine($"📝 메시지 제목: {title}");
                Console.WriteLine($"💬 채팅방 ID: {chatroomId}");

                // Block Kit 페이로드 생성
                var payload = new KakaoWorkPayload
                {
                    ConversationId = chatroomId,
                    Text = title,
                    Blocks =
                    {
                        new HeaderBlock { Text = title, Style = "blue" },
                        new TextBlock { Text = "아래 링크에서 파일을 다운로드하세요!", Markdown = true },
                        new ButtonBlock { Text = "파일 다운로드", Value = fileUrl, Style = "primary" },
                        new DividerBlock(),
                        new DescriptionBlock
                        {
                            Term = "송장 개수",
                            Content = new TextBlock { Text = $"{invoiceCount}건", Markdown = false }
                        },
                        new DividerBlock(),
                        new TextBlock
                        {
                            Text = "*송장넘기기*\n아이디: `gram`\n비번: `3535`\n[👉 송장 관리 페이지 바로가기](https://gramwon.me/orders/transfer)",
                            Markdown = true
                        }
                    }
                };

                // JSON 직렬화
                var jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                Console.WriteLine($"📦 JSON 페이로드 크기: {jsonPayload.Length} bytes");

                // HTTP 요청 전송
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.kakaowork.com/v1/messages.send", content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ KakaoWork 알림 전송 성공: {type}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"KakaoWork API 오류: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ KakaoWork 알림 전송 실패: {ex.Message}");
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
                Console.WriteLine("🔗 KakaoWork 연결 테스트 시작...");

                // KakaoWork API 키 확인
                if (string.IsNullOrEmpty(_httpClient.DefaultRequestHeaders.Authorization?.Parameter))
                {
                    Console.WriteLine("KakaoWork API 키가 설정되지 않았습니다.");
                    return false;
                }

                // 간단한 API 호출로 연결 테스트
                var response = await _httpClient.GetAsync("https://api.kakaowork.com/v1/users.me");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"✅ KakaoWork 연결 성공: {content}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ KakaoWork 연결 실패: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ KakaoWork 연결 테스트 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Private 헬퍼 메서드
        /// <summary>
        /// NotificationType을 한글 이름으로 변환
        /// </summary>
        /// <param name="type">알림 종류</param>
        /// <returns>한글 이름</returns>
        private static string GetKoreanName(NotificationType type)
        {
            return type switch
            {
                NotificationType.SalesData => "판매입력",
                NotificationType.Integrated => "통합송장",
                NotificationType.SeoulFrozen => "서울냉동",
                NotificationType.GyeonggiFrozen => "경기냉동",
                NotificationType.SeoulGongsan => "서울공산",
                NotificationType.GyeonggiGongsan => "경기공산",
                NotificationType.BusanCheonggwa => "부산청과",
                NotificationType.GamcheonFrozen => "감천냉동",
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