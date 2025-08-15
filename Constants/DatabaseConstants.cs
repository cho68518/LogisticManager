namespace LogisticManager.Constants
{
    /// <summary>
    /// 데이터베이스 관련 상수들을 정의하는 클래스
    /// 
    /// 주요 기능:
    /// - 데이터베이스 연결 기본값 정의
    /// - 연결 문자열 템플릿 정의
    /// - 데이터베이스 관련 메시지 정의
    /// - 설정 검증 규칙 정의
    /// </summary>
    public static class DatabaseConstants
    {
        #region 필수 설정 검증 메시지

        /// <summary>
        /// 필수 설정값 누락 시 표시할 메시지 (애플리케이션 중단)
        /// </summary>
        public const string ERROR_MISSING_REQUIRED_SETTINGS = 
            "❌ 필수 설정값이 누락되었습니다.\n\n" +
            "settings.json 파일에 다음 설정이 필요합니다:\n" +
            "• DB_SERVER: 데이터베이스 서버 주소\n" +
            "• DB_NAME: 데이터베이스 이름\n" +
            "• DB_USER: 데이터베이스 사용자명\n" +
            "• DB_PASSWORD: 데이터베이스 비밀번호\n" +
            "• DB_PORT: 데이터베이스 포트\n\n" +
            "애플리케이션을 시작할 수 없습니다.\n" +
            "올바른 설정 파일을 생성한 후 다시 시도해주세요.";
        
        /// <summary>
        /// 설정 파일이 완전히 누락된 경우 메시지
        /// </summary>
        public const string ERROR_SETTINGS_FILE_COMPLETELY_MISSING = 
            "❌ settings.json 파일이 존재하지 않습니다.\n\n" +
            "프로젝트 루트 디렉토리에 다음 내용의 settings.json 파일을 생성해주세요:\n\n" +
            "{\n" +
            "  \"DB_SERVER\": \"your-server-address\",\n" +
            "  \"DB_NAME\": \"your-database-name\",\n" +
            "  \"DB_USER\": \"your-username\",\n" +
            "  \"DB_PASSWORD\": \"your-password\",\n" +
            "  \"DB_PORT\": \"3306\"\n" +
            "}\n\n" +
            "파일 생성 후 애플리케이션을 다시 시작해주세요.";

        #endregion

        #region 연결 문자열 템플릿

        /// <summary>
        /// MySQL 연결 문자열 템플릿 (기본)
        /// </summary>
        public const string CONNECTION_STRING_TEMPLATE = 
            "Server={0};Database={1};User Id={2};Password={3};Port={4};CharSet=utf8;Convert Zero Datetime=True;Allow User Variables=True;";
        
        /// <summary>
        /// MySQL 연결 문자열 템플릿 (UTF8MB4)
        /// </summary>
        public const string CONNECTION_STRING_UTF8MB4_TEMPLATE = 
            "Server={0};Database={1};User ID={2};Password={3};Port={4};CharSet=utf8mb4;SslMode=none;AllowPublicKeyRetrieval=true;Convert Zero Datetime=True;";
        
        /// <summary>
        /// MySQL 연결 문자열 템플릿 (연결 테스트용)
        /// </summary>
        public const string CONNECTION_STRING_TEST_TEMPLATE = 
            "Server={0};Database={1};Uid={2};Pwd={3};CharSet=utf8mb4;Port={4};SslMode=none;AllowPublicKeyRetrieval=true;ConnectionTimeout=30;";

        #endregion

        #region 설정 파일 관련

        /// <summary>
        /// 설정 파일 이름
        /// </summary>
        public const string SETTINGS_FILE_NAME = "settings.json";
        
        /// <summary>
        /// 데이터베이스 서버 설정 키
        /// </summary>
        public const string CONFIG_KEY_DB_SERVER = "DB_SERVER";
        
        /// <summary>
        /// 데이터베이스 이름 설정 키
        /// </summary>
        public const string CONFIG_KEY_DB_NAME = "DB_NAME";
        
        /// <summary>
        /// 데이터베이스 사용자 설정 키
        /// </summary>
        public const string CONFIG_KEY_DB_USER = "DB_USER";
        
        /// <summary>
        /// 데이터베이스 비밀번호 설정 키
        /// </summary>
        public const string CONFIG_KEY_DB_PASSWORD = "DB_PASSWORD";
        
        /// <summary>
        /// 데이터베이스 포트 설정 키
        /// </summary>
        public const string CONFIG_KEY_DB_PORT = "DB_PORT";

        #endregion

        #region 사용자 친화적인 에러 메시지

        /// <summary>
        /// 설정 파일을 찾을 수 없을 때 표시할 메시지
        /// </summary>
        public const string ERROR_SETTINGS_FILE_NOT_FOUND = 
            "⚠️ 설정 파일을 찾을 수 없습니다.\n\n" +
            "프로젝트 루트 디렉토리에 'settings.json' 파일이 있는지 확인해주세요.\n\n" +
            "파일 내용 예시:\n" +
            "{\n" +
            "  \"DB_SERVER\": \"서버주소\",\n" +
            "  \"DB_NAME\": \"데이터베이스명\",\n" +
            "  \"DB_USER\": \"사용자명\",\n" +
            "  \"DB_PASSWORD\": \"비밀번호\",\n" +
            "  \"DB_PORT\": \"3306\"\n" +
            "}";

        /// <summary>
        /// 설정 파일 읽기 실패 시 표시할 메시지
        /// </summary>
        public const string ERROR_SETTINGS_FILE_READ_FAILED = 
            "❌ 설정 파일 읽기에 실패했습니다.\n\n" +
            "파일이 손상되었거나 접근 권한이 없을 수 있습니다.\n\n" +
            "해결 방법:\n" +
            "1. 파일이 올바른 JSON 형식인지 확인\n" +
            "2. 파일에 대한 읽기 권한 확인\n" +
            "3. 파일이 다른 프로그램에 의해 잠겨있지 않은지 확인";

        /// <summary>
        /// 설정 파일 파싱 실패 시 표시할 메시지
        /// </summary>
        public const string ERROR_SETTINGS_FILE_PARSE_FAILED = 
            "❌ 설정 파일 형식이 올바르지 않습니다.\n\n" +
            "JSON 형식에 오류가 있을 수 있습니다.\n\n" +
            "해결 방법:\n" +
            "1. JSON 문법 오류 확인 (괄호, 쉼표 등)\n" +
            "2. 온라인 JSON 검증 도구 사용\n" +
            "3. 파일을 다시 작성";

        /// <summary>
        /// 필수 설정값 누락 시 표시할 메시지
        /// </summary>
        public const string ERROR_REQUIRED_SETTINGS_MISSING = 
            "⚠️ 필수 설정값이 누락되었습니다.\n\n" +
            "다음 설정값들이 필요합니다:\n" +
            "• DB_SERVER: 데이터베이스 서버 주소\n" +
            "• DB_NAME: 데이터베이스 이름\n" +
            "• DB_USER: 데이터베이스 사용자명\n\n" +
            "기본값을 사용하여 계속 진행합니다.";

        /// <summary>
        /// 데이터베이스 연결 실패 시 표시할 메시지
        /// </summary>
        public const string ERROR_DATABASE_CONNECTION_FAILED = 
            "❌ 데이터베이스 연결에 실패했습니다.\n\n" +
            "가능한 원인:\n" +
            "1. 서버 주소가 올바르지 않음\n" +
            "2. 데이터베이스 이름이 올바르지 않음\n" +
            "3. 사용자명 또는 비밀번호가 올바르지 않음\n" +
            "4. 서버가 실행되지 않음\n" +
            "5. 방화벽 또는 네트워크 문제\n\n" +
            "설정을 확인하고 다시 시도해주세요.";

        #endregion

        #region 설정 검증 규칙

        /// <summary>
        /// 서버 주소 최소 길이
        /// </summary>
        public const int MIN_SERVER_LENGTH = 5;
        
        /// <summary>
        /// 서버 주소 최대 길이
        /// </summary>
        public const int MAX_SERVER_LENGTH = 255;
        
        /// <summary>
        /// 데이터베이스 이름 최소 길이
        /// </summary>
        public const int MIN_DATABASE_LENGTH = 1;
        
        /// <summary>
        /// 데이터베이스 이름 최대 길이
        /// </summary>
        public const int MAX_DATABASE_LENGTH = 64;
        
        /// <summary>
        /// 사용자명 최소 길이
        /// </summary>
        public const int MIN_USER_LENGTH = 1;
        
        /// <summary>
        /// 사용자명 최대 길이
        /// </summary>
        public const int MAX_USER_LENGTH = 32;
        
        /// <summary>
        /// 비밀번호 최소 길이
        /// </summary>
        public const int MIN_PASSWORD_LENGTH = 1;
        
        /// <summary>
        /// 비밀번호 최대 길이
        /// </summary>
        public const int MAX_PASSWORD_LENGTH = 255;
        
        /// <summary>
        /// 포트 번호 최소값
        /// </summary>
        public const int MIN_PORT = 1;
        
        /// <summary>
        /// 포트 번호 최대값
        /// </summary>
        public const int MAX_PORT = 65535;

        #endregion

        #region 성공 메시지

        /// <summary>
        /// 설정 파일 읽기 성공 시 표시할 메시지
        /// </summary>
        public const string SUCCESS_SETTINGS_LOADED = 
            "✅ 설정 파일을 성공적으로 읽었습니다.\n\n" +
            "데이터베이스 연결 정보가 올바르게 설정되었습니다.";

        /// <summary>
        /// 데이터베이스 연결 성공 시 표시할 메시지
        /// </summary>
        public const string SUCCESS_DATABASE_CONNECTED = 
            "✅ 데이터베이스 연결에 성공했습니다!\n\n" +
            "서버와 정상적으로 통신할 수 있습니다.";

        #endregion
    }
}
