DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GyeonggiProcessF$$

CREATE PROCEDURE sp_GyeonggiProcessF()
BEGIN
    /*--================================================================================
    -- (경기냉동) 처리
    --================================================================================*/
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
            @sqlstate = RETURNED_SQLSTATE,
            @errno    = MYSQL_ERRNO,
            @text     = MESSAGE_TEXT;

        INSERT INTO error_log (procedure_name, error_code, error_message)
        VALUES ('sp_GyeonggiProcessF', @errno, @text);

        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log, temp_sorted_data;
        SELECT '오류가 발생하여 모든 작업이 롤백되었습니다.' AS Message;

        SHOW ERRORS;
    END;

    CREATE TEMPORARY TABLE sp_execution_log ( StepID INT AUTO_INCREMENT PRIMARY KEY, OperationDescription VARCHAR(255), AffectedRows INT );
    START TRANSACTION;

    /*--TRUNCATE TABLE temp_invoices;
    --TRUNCATE TABLE temp_additional_invoices;*/
    TRUNCATE TABLE error_log;	
		
    /*-- =================================================================================
    -- 임시 작업 테이블 생성
    -- =================================================================================*/
    DROP TEMPORARY TABLE IF EXISTS temp_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_additional_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_sorted_data;

    CREATE TEMPORARY TABLE temp_sorted_data LIKE 송장출력_경기냉동_최종;
	CREATE TEMPORARY TABLE temp_invoices LIKE 송장출력_경기냉동;
	CREATE TEMPORARY TABLE temp_additional_invoices LIKE 송장출력_경기냉동;
	
    /*--================================================================================
	-- (경기냉동) 냉동낱개 분류
    --================================================================================*/
    INSERT INTO temp_invoices (
		msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 옵션명, 수량, 배송메세지, 
        주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 
        택배수량, 택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
        )
		SELECT 
			msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 옵션명,
            CASE WHEN 수량 REGEXP '^[0-9]+$' THEN CAST(수량 AS SIGNED) ELSE 0 END,
            배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드,
            CASE WHEN 택배비용 REGEXP '^[0-9]+$' THEN CAST(택배비용 AS SIGNED) ELSE 0 END,
            박스크기,
            CASE WHEN 출력개수 REGEXP '^[0-9]+$' THEN CAST(출력개수 AS SIGNED) ELSE 0 END,
            CASE WHEN 송장수량 REGEXP '^[0-9]+$' THEN CAST(송장수량 AS SIGNED) ELSE 0 END,
            별표1, 별표2,
            CASE WHEN 품목개수 REGEXP '^[0-9]+$' THEN CAST(품목개수 AS SIGNED) ELSE 0 END,
            CASE WHEN 택배수량 REGEXP '^[0-9]+(\\.[0-9]+)?$' THEN CAST(택배수량 AS DECIMAL(10,3)) ELSE 0 END,
            NULL, -- 택배수량1
            NULL, -- 택배수량합산
            송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
        FROM 송장출력_사방넷원본변환 WHERE 송장구분최종 = '냉동낱개';
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (경기냉동) 생성 및 냉동낱개 데이터 분류', ROW_COUNT());

    /*--=====================================================================================================
	-- (경기냉동) 택배수량 계산 및 송장구분자 업데이트
    --======================================================================================================*/
    UPDATE temp_invoices SET 송장구분자 = FLOOR((1 / IFNULL(NULLIF(택배수량, 0), 10)) * 1000) / 1000;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 송장구분자', ROW_COUNT());

    /*--================================================================================
	-- (경기냉동) 송장구분자와 수량 곱 업데이트
    --================================================================================*/	
    UPDATE temp_invoices SET 송장구분자 = 송장구분자 * 수량;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 송장구분자*수량', ROW_COUNT());

    /*--================================================================================
	-- (경기냉동) 주소 + 수취인명 기반 송장구분자 합산
    --================================================================================*/	
    UPDATE temp_invoices AS t JOIN (
        SELECT 주소, 수취인명, SUM(송장구분자) AS 구분자합
          FROM temp_invoices GROUP BY 주소, 수취인명
		) AS s ON t.주소 = s.주소 AND t.수취인명 = s.수취인명
		SET t.택배수량1 = s.구분자합;
		   
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 주소, 수취인명, 택배수량', ROW_COUNT());

    /*--================================================================================
	-- (경기냉동) 택배수량1 올림 처리  ('낱개' 로 남아있는 행이 있음)
    --================================================================================*/	
    UPDATE temp_invoices SET 택배수량1 = CEIL(IFNULL(택배수량1, 1));
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 택배수량1', ROW_COUNT());

    /*--================================================================================
	-- (경기냉동) 택배수량1에 따른 송장구분 업데이트
    --================================================================================*/	
    UPDATE temp_invoices SET 송장구분 = CASE WHEN 택배수량1 > 1 THEN '추가' ELSE '1장' END;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 송장구분', ROW_COUNT());

    /*--================================================================================
	-- (경기냉동) 주소 및 수취인명 유일성에 따른 송장구분 업데이트 시작
	--             주소 + 수취인명 조합이 유일한 경우 '단일'로 업데이트
    --================================================================================*/	
    UPDATE temp_invoices t1 JOIN (
        SELECT 주소, 수취인명 FROM temp_invoices WHERE 송장구분 = '1장' GROUP BY 주소, 수취인명 HAVING COUNT(*) = 1
		) AS unique_address ON t1.주소 = unique_address.주소 AND t1.수취인명 = unique_address.수취인명
		SET t1.송장구분 = '단일' WHERE t1.송장구분 = '1장';
	  
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 송장구분(단일, 1장)', ROW_COUNT());

    /*--================================================================================
	-- (경기냉동) 품목코드별 수량 합산 및 품목개수
    --================================================================================*/	
    UPDATE temp_invoices t1 JOIN (
        SELECT 품목코드, SUM(수량) AS total_quantity FROM temp_invoices WHERE 송장구분 = '단일' GROUP BY 품목코드
		) AS t2 ON t1.품목코드 = t2.품목코드
		SET t1.품목개수 = t2.total_quantity WHERE t1.송장구분 = '단일';
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 품목개수', ROW_COUNT());
	
    /*-- =================================================================================
    -- '추가' 송장 생성
	-- (경기냉동) 송장출력_서울공산추가송장 테이블로 유니크 주소 행 이동
    --================================================================================*/	
	INSERT INTO temp_additional_invoices (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 
	                                      우편번호, 주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 
										  송장명, 품목코드, 택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 
										  품목개수, 택배수량, 택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 
										  위치, 위치변환)
		SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소,
			   옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용,
			   박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 택배수량합산,
			   송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
		FROM (
			SELECT *, ROW_NUMBER() OVER (PARTITION BY 주소 ORDER BY id ASC) AS rn
			FROM temp_invoices WHERE 송장구분 = '추가'
		) AS subquery WHERE subquery.rn = 1;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (경기냉동) 추가송장 기본 데이터 생성', ROW_COUNT());

    /*--================================================================================
	-- (경기냉동) 경기냉동추가송장 업데이트
	--             주문번호 추가(20250821) 움직이지 않는 송장이 주문에 입혀지지 않기 위함
    --================================================================================*/	
    UPDATE temp_additional_invoices SET 수량 = 1, 송장명 = '+++', 옵션명 = '+++', 품목코드 = '0000', 주문번호 = '1234567890';
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 수량, 송장명, 옵션명, 품목코드, 주문번호', ROW_COUNT());	

    /*--================================================================================
	-- (경기냉동) 경기냉동 추가송장 늘리기
    --================================================================================*/	
    INSERT INTO temp_additional_invoices 
		(msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		 주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
		 택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 
		 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
		SELECT t.msg1, t.msg2, t.msg3, t.msg4, t.msg5, t.msg6, t.수취인명, t.전화번호1, t.전화번호2, t.우편번호, 
		       t.주소, t.옵션명, t.수량, t.배송메세지, t.주문번호, t.쇼핑몰, t.수집시간, t.송장명, t.품목코드, 
			   t.택배비용, t.박스크기, t.출력개수, t.송장수량, t.별표1, t.별표2, t.품목개수, t.택배수량, t.택배수량1, 
			   t.택배수량합산, t.송장구분자, t.송장구분, t.송장구분최종, t.위치, t.위치변환
        FROM temp_additional_invoices AS t JOIN Numbers AS n ON n.n <= (t.택배수량1 - 1) WHERE t.택배수량1 > 1;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (경기냉동) 추가송장 늘리기', ROW_COUNT());	

    /*--================================================================================
	-- (경기냉동) 경기냉동추가송장 순번 매기기
    --================================================================================*/	
    UPDATE temp_additional_invoices AS t JOIN (
        SELECT id, @seq := IF(@current_group = 주소, @seq + 1, 1) AS rn, @current_group := 주소
        FROM temp_additional_invoices, (SELECT @seq := 0, @current_group := '') AS vars ORDER BY 주소, id
    ) AS s ON t.id = s.id
    SET t.송장구분자 = CONCAT('[', s.rn, ']');
	   
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 추가송장 순번 매기기', ROW_COUNT());	

    /*--================================================================================
	-- (경기냉동) 경기냉동추가송장 주소업데이트
    --================================================================================*/	
    UPDATE temp_additional_invoices SET 주소 = CONCAT(주소, 송장구분자);
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 추가송장 주소에 순번 추가', ROW_COUNT());	

	
    /*--================================================================================
	-- 최종 데이터 통합
	--    별표 있는 데이터를 먼저, 그 안에서는 주소 순으로, 주소도 같으면 옵션명 순
    --================================================================================*/
	INSERT INTO temp_sorted_data
		SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2,
               우편번호, 주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰, 품목코드,
               택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수
		FROM (
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 
			       우편번호, 주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰, 품목코드,
                   택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수
			  FROM temp_invoices WHERE 송장구분 = '단일'
			UNION ALL
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 
			       우편번호, 주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰, 품목코드,
                   택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수
			  FROM 송장출력_공통박스 WHERE 송장구분최종 = '냉동박스'
			UNION ALL
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 
			       우편번호, 주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰, 품목코드,
                   택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수
			  FROM temp_invoices WHERE 송장구분 = '1장'
			UNION ALL
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 
			       우편번호, 주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰, 품목코드,
                   택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수
			  FROM temp_invoices WHERE 송장구분 = '추가'
			UNION ALL
			SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 
			       우편번호, 주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰, 품목코드,
                   택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수
			  FROM temp_additional_invoices
		) AS final_union
		ORDER BY
			CASE WHEN 별표1 <> '' OR 별표2 <> '' THEN 0 ELSE 1 END,
			/*주소 ASC, 옵션명 ASC;*/
			주소 ASC, 송장명 ASC;
    
    TRUNCATE TABLE 송장출력_경기냉동_최종;
	
    INSERT INTO 송장출력_경기냉동_최종 SELECT * FROM temp_sorted_data;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (경기냉동) 최종 테이블 데이터 통합', ROW_COUNT());

    /*-- =================================================================================
    -- 후처리
    -- =================================================================================*/
	UPDATE 송장출력_경기냉동_최종 AS d
		JOIN (
			SELECT IFNULL(전화번호1, '') AS 전화번호1, IFNULL(전화번호2, '') AS 전화번호2, IFNULL(우편번호, '') AS 우편번호, IFNULL(수취인명, '') AS 수취인명, MAX(주소) AS 주소
			FROM 송장출력_경기냉동_최종
			WHERE 송장명 <> '드라이아이스 추가' AND 주소 IS NOT NULL AND 주소 <> ''
			GROUP BY IFNULL(전화번호1, ''), IFNULL(전화번호2, ''), IFNULL(우편번호, ''), IFNULL(수취인명, '')
		) AS src ON
			IFNULL(d.전화번호1, '') = src.전화번호1 AND IFNULL(d.전화번호2, '') = src.전화번호2 AND
			IFNULL(d.우편번호, '') = src.우편번호 AND IFNULL(d.수취인명, '') = src.수취인명
		SET d.주소 = src.주소
		WHERE d.송장명 = '드라이아이스 추가' AND d.주소 IS NULL;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기냉동) 드라이아이스 추가건 주소 업데이트', ROW_COUNT());
	
    UPDATE 송장출력_경기냉동_최종, (
        SELECT
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_COST')  AS c_cost,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_SIZE')  AS c_size,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_COUNT') AS c_count
    ) AS p_values
    SET 택배비용 = p_values.c_cost, 박스크기 = p_values.c_size, 출력개수 = p_values.c_count;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 최종 테이블 기본값 설정', ROW_COUNT());
    
    COMMIT;

    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;
	
    DROP TEMPORARY TABLE sp_execution_log;
    DROP TEMPORARY TABLE temp_invoices;
    DROP TEMPORARY TABLE temp_additional_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_sorted_data;

END$$

DELIMITER ;