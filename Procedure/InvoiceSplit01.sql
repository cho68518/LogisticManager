DELIMITER $$

DROP PROCEDURE IF EXISTS InvoiceSplit01$$

CREATE PROCEDURE InvoiceSplit01()
BEGIN
    -- =================================================================
	-- 감천 특별출고 처리 루틴
    -- =================================================================
    DECLARE v_msg1_default, v_msg2_default, v_msg3_default, v_msg4_default, v_msg5_default, v_msg6_default VARCHAR(255);
    DECLARE v_msg1_busan, v_msg2_busan, v_msg3_busan, v_msg4_busan, v_msg5_busan, v_msg6_busan VARCHAR(255);
    DECLARE v_msg1_cheonggwa, v_msg2_cheonggwa, v_msg3_cheonggwa, v_msg4_cheonggwa, v_msg5_cheonggwa, v_msg6_cheonggwa VARCHAR(255);
    
    -- 오류 발생 시 롤백하고 임시 테이블을 정리
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- 오류 저장 변수
        DECLARE v_sqlstate CHAR(5);
        DECLARE v_error_no INT;
        DECLARE v_error_message TEXT;

        -- 발생한 오류의 상세 정보.
        GET DIAGNOSTICS CONDITION 1
            v_sqlstate = RETURNED_SQLSTATE,
            v_error_no = MYSQL_ERRNO,
            v_error_message = MESSAGE_TEXT;

        -- 트랜잭션을 롤백.
        ROLLBACK;

        -- 상세 오류 메시지.
        SELECT CONCAT(
            'Error (SQLSTATE: ', v_sqlstate, ', Error No: ', v_error_no, '): ', v_error_message,
            ' | The transaction was rolled back.'
        ) AS ErrorMessage;
    END;

    -- =================================================================
    
    -- 송장출력_메세지 --> 송장출력_메세지부산, 송장출력_메세지청과
    TRUNCATE TABLE 송장출력_메세지부산;
    TRUNCATE TABLE 송장출력_메세지청과;
    
    INSERT INTO 송장출력_메세지부산 (쇼핑몰, msg1, msg2, msg3, msg4, msg5, msg6)
    SELECT 쇼핑몰, msg1, msg2, msg3, msg4, msg5, msg6
    FROM 송장출력_메세지;

    INSERT INTO 송장출력_메세지청과 (쇼핑몰, msg1, msg2, msg3, msg4, msg5, msg6)
    SELECT 쇼핑몰, msg1, msg2, msg3, msg4, msg5, msg6
    FROM 송장출력_메세지;

    -- 이 UPDATE 문은 '송장출력_특수출력_감천분리출고' 테이블에 '택배수량' 컬럼이 존재한다고 가정합니다.
    UPDATE 송장출력_특수출력_감천분리출고 a
    LEFT JOIN 품목등록 b ON a.품목코드 = b.품목코드
    SET a.택배수량 = COALESCE(b.택배수량, 10);
            
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_default, v_msg2_default, v_msg3_default, v_msg4_default, v_msg5_default, v_msg6_default
      FROM 송장출력_메세지
      WHERE 쇼핑몰 = '나머지' LIMIT 1;
      
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_busan, v_msg2_busan, v_msg3_busan, v_msg4_busan, v_msg5_busan, v_msg6_busan
      FROM 송장출력_메세지부산
      WHERE 쇼핑몰 = '나머지' LIMIT 1;
      
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_cheonggwa, v_msg2_cheonggwa, v_msg3_cheonggwa, v_msg4_cheonggwa, v_msg5_cheonggwa, v_msg6_cheonggwa
      FROM 송장출력_메세지청과
      WHERE 쇼핑몰 = '나머지' LIMIT 1;

    -- 대상 품목 정보를 임시 테이블에 한 번만 저장
    CREATE TEMPORARY TABLE temp_gamcheon_codes (
        품목코드 VARCHAR(255),
        수량     VARCHAR(255),
        택배수량 VARCHAR(255)
    );

    INSERT INTO temp_gamcheon_codes (품목코드, 수량, 택배수량)
    SELECT a.품목코드, a.수량, COALESCE(b.택배수량, 10)
    FROM 송장출력_특수출력_감천분리출고 a
    LEFT JOIN 품목등록 b ON a.품목코드 = b.품목코드;

    -- =================================================================
    START TRANSACTION;

    -- '송장출력_사방넷원본변환' 테이블을 대상으로 하도록 통일
    -- 2-1. 송장구분 업데이트 ('합포장'/'단일')
    UPDATE 송장출력_사방넷원본변환_Dev main_table
    JOIN (
        SELECT id, COUNT(*) OVER (PARTITION BY 주소) as address_count
        FROM 송장출력_사방넷원본변환_Dev
        WHERE 품목코드 IN (SELECT 품목코드 FROM temp_gamcheon_codes)
    ) AS sub_query ON main_table.id = sub_query.id
    SET main_table.송장구분 = IF(sub_query.address_count > 1, '합포장', '단일');

    -- 2-2. '단일' 및 조건부 '합포장' 데이터 분리 및 이동
    TRUNCATE TABLE 송장출력_사방넷원본변환_특수출력감천;

    -- '송장구분최종' 컬럼을 사용하는 대신, 조건을 직접 명시하여 한 번에 처리
    /*INSERT INTO 송장출력_사방넷원본변환_특수출력감천
		SELECT s.*
		FROM 송장출력_사방넷원본변환_Dev s
		JOIN temp_gamcheon_codes t ON s.품목코드 = t.품목코드
		WHERE s.송장구분 = '단일' OR (s.송장구분 = '합포장' AND s.수량 >= t.수량);
    */
	
	INSERT INTO 송장출력_사방넷원본변환_특수출력감천
		SELECT s.*
		FROM 송장출력_사방넷원본변환_Dev s
		JOIN (
			SELECT DISTINCT 품목코드, 수량 FROM temp_gamcheon_codes
		) AS t ON s.품목코드 = t.품목코드
		WHERE s.송장구분 = '단일' OR (s.송장구분 = '합포장' AND s.수량 >= t.수량);
  
    DELETE s
    FROM 송장출력_사방넷원본변환_Dev s
    JOIN temp_gamcheon_codes t ON s.품목코드 = t.품목코드
    WHERE s.송장구분 = '단일' OR (s.송장구분 = '합포장' AND s.수량 >= t.수량);

    -- 2-3. 분리된 데이터 가공 (`GC_` 접두사 정리)
    -- 모든 송장명이 'GC_'로 시작하도록 보장
    UPDATE 송장출력_사방넷원본변환_특수출력감천
    SET 송장명 = CONCAT('GC_', TRIM(LEADING 'GC_' FROM 송장명));

    -- 2-4. 가공된 데이터 원본 테이블로 다시 병합
    INSERT INTO 송장출력_사방넷원본변환_Dev SELECT * FROM 송장출력_사방넷원본변환_특수출력감천;

    -- 2-5. 전체 데이터 후처리 작업 (여러 UPDATE 문을 로직별로 통합)
    -- 빈 품목코드 및 한 글자 수취인명 처리
    UPDATE 송장출력_사방넷원본변환_Dev
    SET
        품목코드 = CASE WHEN 품목코드 = '' OR 품목코드 IS NULL THEN '0000' ELSE 품목코드 END,
        송장명   = CASE WHEN 품목코드 = '' OR 품목코드 IS NULL THEN '--' ELSE 송장명 END,
        수취인명 = CASE WHEN CHAR_LENGTH(수취인명) = 1 THEN CONCAT(수취인명, 수취인명) ELSE 수취인명 END;

    -- 송장구분자 및 송장구분(출고지) 업데이트 (하나의 UPDATE로 통합)
    UPDATE 송장출력_사방넷원본변환_Dev
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

    -- 메시지 및 택배수량 업데이트 (하나의 UPDATE로 통합)
    UPDATE 송장출력_사방넷원본변환_Dev s
    LEFT JOIN 송장출력_메세지 m       ON s.쇼핑몰   = m.쇼핑몰
    LEFT JOIN 송장출력_메세지부산 mb  ON s.쇼핑몰   = mb.쇼핑몰
    LEFT JOIN 송장출력_메세지청과 mc  ON s.쇼핑몰   = mc.쇼핑몰
    LEFT JOIN 품목등록 p              ON s.품목코드 = p.품목코드
    SET
        s.msg1 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg1, v_msg1_busan) WHEN '청과' THEN IFNULL(mc.msg1, v_msg1_cheonggwa) ELSE IFNULL(m.msg1, v_msg1_default) END,
        s.msg2 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg2, v_msg2_busan) WHEN '청과' THEN IFNULL(mc.msg2, v_msg2_cheonggwa) ELSE IFNULL(m.msg2, v_msg2_default) END,
        s.msg3 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg3, v_msg3_busan) WHEN '청과' THEN IFNULL(mc.msg3, v_msg3_cheonggwa) ELSE IFNULL(m.msg3, v_msg3_default) END,
        s.msg4 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg4, v_msg4_busan) WHEN '청과' THEN IFNULL(mc.msg4, v_msg4_cheonggwa) ELSE IFNULL(m.msg4, v_msg4_default) END,
        s.msg5 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg5, v_msg5_busan) WHEN '청과' THEN IFNULL(mc.msg5, v_msg5_cheonggwa) ELSE IFNULL(m.msg5, v_msg5_default) END,
        s.msg6 = CASE s.송장구분 WHEN '부산' THEN IFNULL(mb.msg6, v_msg6_busan) WHEN '청과' THEN IFNULL(mc.msg6, v_msg6_cheonggwa) ELSE IFNULL(m.msg6, v_msg6_default) END,
        s.택배수량 = IFNULL(p.택배수량, 10);

    -- 2-6. 최종 리포트 테이블 생성 (컬럼 순서 및 개수 수정)
    TRUNCATE TABLE 송장출력_주문정보;
	
    INSERT INTO 송장출력_주문정보 (주문번호_쇼핑몰, 
	                               주문번호_사방넷, 
								   수집일, 
								   쇼핑몰, 
								   받는사람, 
								   상품명, 
	                               물류용코드, 
								   코드번호, 
								   수량, 
								   배송송, 
								   결제금액, 
								   주문금액, 
								   면과세_구분, 
								   결제수단, 
								   주소, 
								   전화번호, 
								   전화번호2, 
								   주문상태)
    SELECT
        `주문번호(쇼핑몰)` AS 주문번호_쇼핑몰, 
		주문번호 AS 주문번호_사방넷, 
		수집시간 AS 수집일, 
		쇼핑몰, 
		수취인명 AS 받는사람, 
		옵션명   AS 상품명, 
		품목코드 AS 물류용코드, 
		품목코드 AS 코드번호, 
		수량, 
		배송송, 
		결제금액, 
		주문금액, 
		면과세구분 AS 면과세_구분, 
		결제수단, 
		주소, 
		/*송장구분 AS 전화번호, */
		CASE 송장구분 WHEN '위탁' THEN '본사창고' WHEN '부산' THEN '부산창고' WHEN '공산' THEN '경기공산' WHEN '청과' THEN '부산청과' WHEN '냉동' THEN '본사창고' WHEN '감천' THEN '감천항 물류센터' ELSE '본사창고' END AS 전화번호,
		전화번호2, 
		주문상태
    FROM 송장출력_사방넷원본변환_Dev;

    COMMIT;

    -- =================================================================
    -- 3. 마무리
    -- =================================================================
    DROP TEMPORARY TABLE IF EXISTS temp_gamcheon_codes;
    SELECT '작업완료.' AS ResultMessage;

END$$

DELIMITER ;
