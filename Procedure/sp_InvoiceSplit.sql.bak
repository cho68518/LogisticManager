DELIMITER $$

DROP PROCEDURE IF EXISTS sp_InvoiceSplit$$

CREATE PROCEDURE sp_InvoiceSplit()
BEGIN
    /*--============================================================================
    -- 엑셀에서 송장출력_특수출력_감천분리출고 테이블 Insert후 처리
    --============================================================================*/
    DECLARE error_info TEXT DEFAULT '';
    DECLARE error_code INT DEFAULT 0;
    DECLARE original_sql_safe_updates INT;
	
    DECLARE v_msg1_default, v_msg2_default, v_msg3_default, v_msg4_default, v_msg5_default, v_msg6_default VARCHAR(255);
    DECLARE v_msg1_busan, v_msg2_busan, v_msg3_busan, v_msg4_busan, v_msg5_busan, v_msg6_busan VARCHAR(255);
    DECLARE v_msg1_cheonggwa, v_msg2_cheonggwa, v_msg3_cheonggwa, v_msg4_cheonggwa, v_msg5_cheonggwa, v_msg6_cheonggwa VARCHAR(255);

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
            error_code = MYSQL_ERRNO,
            error_info = MESSAGE_TEXT;

        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
        DROP TEMPORARY TABLE IF EXISTS temp_gamcheon_codes;
        DROP TEMPORARY TABLE IF EXISTS temp_address_counts;
        SELECT '오류가 발생하여 모든 작업이 롤백되었습니다.' AS ErrorMessage,
               error_code AS MySQLErrorCode,
               error_info AS MySQLErrorMessage;
    END;

    CREATE TEMPORARY TABLE sp_execution_log (
        StepID INT AUTO_INCREMENT PRIMARY KEY,
        OperationDescription VARCHAR(255),
        AffectedRows INT
    );
	
	CREATE TEMPORARY TABLE temp_address_counts (
        주소 VARCHAR(500) PRIMARY KEY,
        address_count INT NOT NULL
    );

    -- 안전 업데이트 모드 임시 비활성화
    SET @original_sql_safe_updates = @@SESSION.sql_safe_updates;
    SET SESSION sql_safe_updates = 0;
	
    START TRANSACTION;
    TRUNCATE TABLE sp_execution_log;

    /*-- =================================================================
    -- 1. 메시지 복사 및 감천 대상 데이터 준비
    -- =================================================================*/
    -- 송장출력_메세지 --> 송장출력_메세지부산, 송장출력_메세지청과
    TRUNCATE TABLE 송장출력_메세지부산;
	
    INSERT INTO 송장출력_메세지부산 (쇼핑몰, msg1, msg2, msg3, msg4, msg5, msg6)
		SELECT 쇼핑몰, msg1, msg2, msg3, msg4, msg5, msg6 FROM 송장출력_메세지;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_메세지부산 복사', ROW_COUNT());

    TRUNCATE TABLE 송장출력_메세지청과;
	
    INSERT INTO 송장출력_메세지청과 (쇼핑몰, msg1, msg2, msg3, msg4, msg5, msg6)
		SELECT 쇼핑몰, msg1, msg2, msg3, msg4, msg5, msg6 FROM 송장출력_메세지;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_메세지청과 복사', ROW_COUNT());

    -- 기본 메시지 변수 설정
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_default, v_msg2_default, v_msg3_default, v_msg4_default, v_msg5_default, v_msg6_default
      FROM 송장출력_메세지 WHERE 쇼핑몰 = '나머지' LIMIT 1;
      
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_busan, v_msg2_busan, v_msg3_busan, v_msg4_busan, v_msg5_busan, v_msg6_busan
      FROM 송장출력_메세지부산 WHERE 쇼핑몰 = '나머지' LIMIT 1;
      
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_cheonggwa, v_msg2_cheonggwa, v_msg3_cheonggwa, v_msg4_cheonggwa, v_msg5_cheonggwa, v_msg6_cheonggwa
      FROM 송장출력_메세지청과 WHERE 쇼핑몰 = '나머지' LIMIT 1;
    
    
	UPDATE 송장출력_특수출력_감천분리출고 a
		LEFT JOIN 품목등록 b ON a.품목코드 = b.품목코드
		SET a.택배수량 = COALESCE(b.택배수량, 10);

	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (감천특별출고처리) 송장출력_특수출력_감천분리출고 (택배수량)', ROW_COUNT());
			
    /*-- =================================================================================
    -- 2. 감천 특별출고 처리 루틴
    -- 2-1. 송장구분 업데이트 ('합포장'/'단일')
    -- 2-2. '단일' 및 조건부 '합포장' 데이터에 대해 송장명 가공 (`GC_` 접두사 정리)
    -- =================================================================================*/
	-- 송장구분 업데이트
	INSERT INTO temp_address_counts (주소, address_count)
		SELECT 주소, COUNT(*) AS cnt
		FROM 송장출력_사방넷원본변환
		GROUP BY 주소;

    -- '합포장' 업데이트 (주소 개수가 1보다 큰 경우)
    UPDATE 송장출력_사방넷원본변환 s
		INNER JOIN temp_address_counts tc ON s.주소 = tc.주소
		SET
			s.송장구분 = '합포장'
		WHERE
			tc.address_count > 1
			AND s.품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_감천분리출고);

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (감천특별출고처리) 송장출력_사방넷원본변환, 송장구분(합포장)', ROW_COUNT());
	
    -- '단일' 업데이트 (주소 개수가 1인 경우)
    UPDATE 송장출력_사방넷원본변환 s
		INNER JOIN temp_address_counts tc ON s.주소 = tc.주소
		SET
			s.송장구분 = '단일'
		WHERE
			tc.address_count = 1
			AND s.품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_감천분리출고);

	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (감천특별출고처리) 송장출력_사방넷원본변환, 송장구분(단일)', ROW_COUNT());
				
			
	TRUNCATE 송장출력_사방넷원본변환_특수출력감천;
    /*--TRUNCATE 송장출력_사방넷원본변환_특수출력감천1;
    --TRUNCATE 송장출력_사방넷원본변환_특수출력감천2;
    --TRUNCATE 송장출력_사방넷원본변환_특수출력감천자료;*/
	
	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(감천 특별출고 처리) 송장출력_사방넷원본변환_특수출력감천 초기화', 0);
	
	/*--INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(감천 특별출고 처리) 송장출력_사방넷원본변환_특수출력감천1 초기화', 0);			
	--INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(감천 특별출고 처리) 송장출력_사방넷원본변환_특수출력감천2 초기화', 0);			
	--INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(감천 특별출고 처리) 송장출력_사방넷원본변환_특수출력감천자료 초기화', 0);*/

	/*--=============================================================================================
	-- (송장출력_사방넷원본변환_특수출력감천)	
    --     '송장출력_사방넷원본변환'에 있는 주문들 중에서, 특정 조건(?)에 맞는 
	--     '단일' 주문과 '합포장' 주문을 골라내서 '송장출력_특수출력_감천분리출고' (감천행)로 Move
	--=============================================================================================*/
	-- 1. '송장구분최종' 값 먼저 업데이트 (원본 코드 3번)
    -- 이동할 '합포장' 대상을 결정하는 선행 작업이므로 먼저 실행.
    UPDATE 송장출력_사방넷원본변환 A
		JOIN 송장출력_특수출력_감천분리출고 B ON A.품목코드 = B.품목코드
		SET A.송장구분최종 = IF(A.수량 >= B.수량, '1', NULL);

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (감천특별출고처리) 송장출력_사방넷원본변환, 송장구분최종', ROW_COUNT());
	
    -- 2. 이동할 모든 행의 PK를 저장할 임시 테이블 생성
    CREATE TEMPORARY TABLE temp_rows_to_move (
        Source_PK INT PRIMARY KEY
    );

    -- 이동할 모든 행('단일', 조건부 '합포장')의 PK를 한 번에 식별하여 임시 테이블에 저장
    INSERT INTO temp_rows_to_move (Source_PK)
		SELECT s.id
		FROM 송장출력_사방넷원본변환 s
		WHERE
			-- 조건 1: 이동할 '단일' 송장
			(
				s.송장구분 = '단일' AND
				s.품목코드 IN (SELECT 품목코드 FROM 송장출력_특수출력_감천분리출고)
			)
			OR
			-- 조건 2: 이동할 '합포장' 송장
			(
				s.송장구분 = '합포장' AND s.송장구분최종 = '1'
			);

    INSERT INTO 송장출력_사방넷원본변환_특수출력감천
		SELECT s.*
		  FROM 송장출력_사방넷원본변환 s
	 	  JOIN temp_rows_to_move m ON s.id = m.Source_PK;

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (감천특별출고처리) 송장출력_사방넷원본변환_특수출력감천', ROW_COUNT());
	
    DELETE s
      FROM 송장출력_사방넷원본변환 s
      JOIN temp_rows_to_move m ON s.id = m.Source_PK;

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] (감천특별출고처리) 송장출력_사방넷원본변환', ROW_COUNT());

    /*--====================================================================================================    		
	-- (송장출력_사방넷원본변환_특수출력감천) (추가: 20250821)
	--      드라이아이스를 추가로 구입하고, 제품이 감천으로 변경될 경우, 
    --      드라이아이스만 남아 송장번호 미입력이 되는 경우를 막기 위함
	--      송장명 = '드라이아이스 추가' 주문을 골라내서 '송장출력_특수출력_감천분리출고' (감천행)로 Move	
	--      ※ 송장출력_사방넷원본변환_특수출력감천 테이블에 단일, 합포장을 가져온후 추가
    --====================================================================================================*/
	TRUNCATE TABLE temp_rows_to_move;	
	
    INSERT INTO temp_rows_to_move (Source_PK)
		SELECT s.id
		FROM 송장출력_사방넷원본변환 AS s
		WHERE s.송장명 = '드라이아이스 추가'
		  -- 조건 1: 주소가 특수출력감천 테이블에 이미 존재해야 함 (단일주소)
		  AND EXISTS (
			  SELECT 1
			  FROM 송장출력_사방넷원본변환_특수출력감천 AS t
			  WHERE TRIM(t.주소) = TRIM(s.주소)
		  )
		  -- 조건 2: 동일한 행이 특수출력감천 테이블에 없어야 함 (중복 삽입 방지)
		  AND NOT EXISTS (
			  SELECT 1
			  FROM 송장출력_사방넷원본변환_특수출력감천 AS t2
			  WHERE t2.주소 = s.주소 AND t2.송장명 = s.송장명
		  );

    INSERT INTO 송장출력_사방넷원본변환_특수출력감천
		SELECT s.*
		FROM 송장출력_사방넷원본변환 AS s
		JOIN temp_rows_to_move m ON s.id = m.Source_PK;

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (감천특별출고처리) 송장출력_사방넷원본변환_특수출력감천 (드라이아이스)', ROW_COUNT());
	
    DELETE s
      FROM 송장출력_사방넷원본변환 AS s
      JOIN temp_rows_to_move m ON s.id = m.Source_PK;	

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] (감천특별출고처리) 송장출력_사방넷원본변환 (드라이아이스)', ROW_COUNT());
	
    /*--================================================================================    		
	-- (송장출력_사방넷원본변환_특수출력감천) GC_ 접두사 재정리	
	--      'GC_'로 시작하지 않는 송장명에만 'GC_'를 앞에 추가
    --================================================================================*/
    UPDATE 송장출력_사방넷원본변환_특수출력감천
		SET 송장명 = CONCAT('GC_', 송장명)
		WHERE 송장명 NOT LIKE 'GC_%';

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (감천특별출고처리) 송장출력_사방넷원본변환_특수출력감천', ROW_COUNT());				
				
    /*--================================================================================    		
	-- 사방넷 원본변환 테이블에 데이터 삽입
    --================================================================================*/
    -- '특수출력감천' 테이블의 데이터를 '사방넷원본변환' 테이블로 Insert/Update
    INSERT INTO 송장출력_사방넷원본변환
		SELECT * FROM 송장출력_사방넷원본변환_특수출력감천
		ON DUPLICATE KEY UPDATE
			ID       = VALUES(ID),
			품목코드 = VALUES(품목코드),
			수량     = VALUES(수량),
			택배비용 = VALUES(택배비용),
			박스크기 = VALUES(박스크기),
			송장구분 = VALUES(송장구분),
			송장명   = VALUES(송장명),
			주소     = VALUES(주소);

	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (감천특별출고처리) 송장출력_사방넷원본변환_특수출력감천 -> 송장출력_사방넷원본변환', ROW_COUNT());				
				
    /*--================================================================================    		
	-- 송장출력_사방넷원본변환_특수출력감천자료 (?)
    --================================================================================			
	--INSERT INTO 송장출력_사방넷원본변환_특수출력감천자료 (품목코드, 수량)
    --    SELECT 품목코드, 수량
    --      FROM 송장출력_사방넷원본변환_특수출력감천1;*/
			
			
	/*--=================================================================
    -- (송장출력_사방넷원본변환) 빈 품목코드, 한 글자 수취인명 처리
    -- =================================================================*/
    UPDATE 송장출력_사방넷원본변환
    SET
        품목코드 = CASE WHEN 품목코드 = '' OR 품목코드 IS NULL THEN '0000' ELSE 품목코드 END,
        송장명   = CASE WHEN 품목코드 = '' OR 품목코드 IS NULL THEN '--' ELSE 송장명 END,
        수취인명 = CASE WHEN CHAR_LENGTH(수취인명) = 1 THEN CONCAT(수취인명, 수취인명) ELSE 수취인명 END;
    
	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (감천특별출고처리) 송장출력_사방넷원본변환 (품목코드,송장명,수취인명)', ROW_COUNT());		
			
	/*--=================================================================
	-- (송장출력_사방넷원본변환) 송장구분자 및 출고지(송장구분) 설정
    -- =================================================================*/	
    UPDATE 송장출력_사방넷원본변환
    SET
        송장구분자 = LEFT(TRIM(송장명), 3),
        송장구분 = CASE
                       WHEN LEFT(TRIM(송장명), 3) IN ('GR_', 'AR_') THEN '위탁'
                       WHEN LEFT(TRIM(송장명), 3) = 'BS_' THEN '부산'
                       WHEN LEFT(TRIM(송장명), 3) = 'GS_' THEN '공산'
                       WHEN LEFT(TRIM(송장명), 3) IN ('YC_', 'HY_') THEN '청과'
                       WHEN LEFT(TRIM(송장명), 3) = 'GC_' THEN '감천'
                       ELSE '냉동'
                   END;
				   
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (감천특별출고처리) 송장출력_사방넷원본변환(송장구분자,송장구분(출고지))', ROW_COUNT());

	/*--=============================================================================================
    -- (송장출력_사방넷원본변환) 메시지 및 택배수량(이카운트 박스수량) 업데이트 (5개 테이블 JOIN)
    -- ============================================================================================*/	
    UPDATE 송장출력_사방넷원본변환 s
		LEFT JOIN 송장출력_메세지 m       ON s.쇼핑몰 = m.쇼핑몰
		LEFT JOIN 송장출력_메세지부산 mb  ON s.쇼핑몰 = mb.쇼핑몰
		LEFT JOIN 송장출력_메세지청과 mc  ON s.쇼핑몰 = mc.쇼핑몰
		LEFT JOIN 품목등록 p              ON s.품목코드 = p.품목코드
    SET
        s.msg1 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg1, v_msg1_busan) WHEN '청과' THEN IFNULL(mc.msg1, v_msg1_cheonggwa) ELSE IFNULL(m.msg1, v_msg1_default) END,
        s.msg2 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg2, v_msg2_busan) WHEN '청과' THEN IFNULL(mc.msg2, v_msg2_cheonggwa) ELSE IFNULL(m.msg2, v_msg2_default) END,
        s.msg3 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg3, v_msg3_busan) WHEN '청과' THEN IFNULL(mc.msg3, v_msg3_cheonggwa) ELSE IFNULL(m.msg3, v_msg3_default) END,
        s.msg4 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg4, v_msg4_busan) WHEN '청과' THEN IFNULL(mc.msg4, v_msg4_cheonggwa) ELSE IFNULL(m.msg4, v_msg4_default) END,
        s.msg5 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg5, v_msg5_busan) WHEN '청과' THEN IFNULL(mc.msg5, v_msg5_cheonggwa) ELSE IFNULL(m.msg5, v_msg5_default) END,
        s.msg6 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg6, v_msg6_busan) WHEN '청과' THEN IFNULL(mc.msg6, v_msg6_cheonggwa) ELSE IFNULL(m.msg6, v_msg6_default) END,
        s.택배수량 = IFNULL(p.택배수량, 10);
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (감천특별출고처리) 송장출력_사방넷원본변환(메시지, 택배수량)', ROW_COUNT());

    -- =================================================================
    -- (송장출력_주문정보) 최종 리포트 생성
    -- =================================================================
    TRUNCATE TABLE 송장출력_주문정보;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[TRUNCATE] (감천특별출고처리) 송장출력_주문정보 초기화', ROW_COUNT());

    INSERT INTO 송장출력_주문정보 (
        주문번호_쇼핑몰, 주문번호_사방넷, 수집일, 쇼핑몰, 받는사람, 상품명,
        물류용코드, 코드번호, 수량, 배송송, 결제금액, 주문금액,
        면과세_구분, 결제수단, 주소, 전화번호, 전화번호2, 주문상태
    )
    SELECT
        `주문번호(쇼핑몰)` AS 주문번호_쇼핑몰,
        주문번호 AS 주문번호_사방넷,
        수집시간 AS 수집일,
        쇼핑몰,
        수취인명 AS 받는사람,
        NULL     AS 상품명,     -- 기존 시스템에서 상품명이 Null로 들어감. 
        품목코드 AS 물류용코드,
        품목코드 AS 코드번호,
        수량,
        배송송,
        결제금액,
        주문금액,
        면과세구분 AS 면과세_구분,
        결제수단,
        주소,
        CASE 송장구분 WHEN '위탁' THEN '본사창고' WHEN '부산' THEN '부산창고' WHEN '공산' THEN '경기공산' WHEN '청과' THEN '부산청과' WHEN '냉동' THEN '본사창고' WHEN '감천' THEN '감천항 물류센터' ELSE '본사창고' END AS 전화번호,
        전화번호2,
        주문상태
    FROM 송장출력_사방넷원본변환;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (감천특별출고처리) 송장출력_주문정보(최종 리포트 테이블 생성)', ROW_COUNT());

    COMMIT;

    -- 최종 실행 결과 로그 반환
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- 프로시저 종료 후 임시 테이블 삭제
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
    DROP TEMPORARY TABLE IF EXISTS temp_gamcheon_codes;
    DROP TEMPORARY TABLE IF EXISTS temp_address_counts;
    DROP TEMPORARY TABLE IF EXISTS temp_rows_to_move;
	
	SET SESSION sql_safe_updates = @original_sql_safe_updates;
	
END$$

DELIMITER ;
