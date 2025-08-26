DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GyeonggiGongsanProcessF$$

CREATE PROCEDURE sp_GyeonggiGongsanProcessF()
BEGIN
    -- =================================================================================
    -- (경기공산) 처리
    -- =================================================================================
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
            @sqlstate = RETURNED_SQLSTATE,
            @errno    = MYSQL_ERRNO,
            @text     = MESSAGE_TEXT;

        ROLLBACK;

        INSERT INTO error_log (procedure_name, error_code, error_message)
        VALUES ('sp_GyeonggiGongsanProcessF', @errno, @text);

        DROP TEMPORARY TABLE IF EXISTS sp_execution_log, temp_sorted_data;
        SELECT '오류가 발생하여 모든 작업이 롤백되었습니다.' AS Message;

        SHOW ERRORS;
    END;

    CREATE TEMPORARY TABLE sp_execution_log ( StepID INT AUTO_INCREMENT PRIMARY KEY, OperationDescription VARCHAR(255), AffectedRows INT );
    START TRANSACTION;

    /*-- =================================================================================
    -- 임시 작업 테이블 생성
    -- =================================================================================*/
    DROP TEMPORARY TABLE IF EXISTS temp_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_additional_invoices;
	DROP TEMPORARY TABLE IF EXISTS temp_sorted_data;

    CREATE TEMPORARY TABLE temp_sorted_data LIKE 송장출력_경기공산_최종;
	CREATE TEMPORARY TABLE temp_invoices LIKE 송장출력_경기공산;
	CREATE TEMPORARY TABLE temp_additional_invoices LIKE 송장출력_경기공산;
	
    /*--================================================================================
	-- (경기공산) 경기공산낱개 분류
    --================================================================================*/
    INSERT INTO temp_invoices (
		msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 옵션명, 수량, 배송메세지, 
        주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 
        택배수량, 택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
    )
		SELECT 
			msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 옵션명, 수량, 배송메세지, 
			주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 
			택배수량, 택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
        FROM 송장출력_사방넷원본변환 WHERE 송장구분최종 = '공산낱개';


    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[TEMP] (경기공산) 생성 및 공산낱개 데이터 분류', ROW_COUNT());

    /*--=====================================================================================================
	-- (경기공산) 택배수량 계산 및 송장구분자 업데이트
	--             택배수량을 이용하여 1을 나눈 결과를 소수점 세 자리에서 내림하여 송장구분자 컬럼에 업데이트
    --======================================================================================================*/
    UPDATE temp_invoices SET 송장구분자 = FLOOR((1 / IFNULL(NULLIF(CAST(택배수량 AS DECIMAL), 0), 10)) * 1000) / 1000;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 택배수량으로 송장구분자 계산', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 송장구분자와 수량 곱 업데이트
    --================================================================================*/
    UPDATE temp_invoices SET 송장구분자 = 송장구분자 * 수량;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 송장구분자에 수량 적용', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 주소 + 수취인명 기반 송장구분자 합산
    --================================================================================*/
    UPDATE temp_invoices AS t JOIN (
			SELECT 주소, 수취인명, SUM(송장구분자) AS 구분자합
			  FROM temp_invoices GROUP BY 주소, 수취인명
		 ) AS s ON t.주소 = s.주소 AND t.수취인명 = s.수취인명
		SET t.택배수량1 = s.구분자합;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 주소/수취인명별 송장구분자 합산', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 택배수량1 올림 처리  ('낱개' 로 남아있는 행이 있음)
    --================================================================================*/
    UPDATE temp_invoices SET 택배수량1 = CEIL(IFNULL(택배수량1, 1));
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 택배수량 올림 처리', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 택배수량1에 따른 송장구분 업데이트
    --================================================================================*/
    UPDATE temp_invoices SET 송장구분 = CASE WHEN 택배수량1 > 1 THEN '추가' ELSE '1장' END;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 송장구분 설정 (추가/1장)', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 주소 및 수취인명 유일성에 따른 송장구분 업데이트 시작
	--             주소 + 수취인명 조합이 유일한 경우 '단일'로 업데이트
    --================================================================================*/
    UPDATE temp_invoices t1 JOIN (
			SELECT 주소, 수취인명 FROM temp_invoices WHERE 송장구분 = '1장' GROUP BY 주소, 수취인명 HAVING COUNT(*) = 1
		) AS unique_address ON t1.주소 = unique_address.주소 AND t1.수취인명 = unique_address.수취인명
		SET t1.송장구분 = '단일' WHERE t1.송장구분 = '1장';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 송장구분 설정 (단일)', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 품목코드별 수량 합산 및 품목개수
    --================================================================================*/
    UPDATE temp_invoices t1 JOIN (
			SELECT 품목코드, SUM(수량) AS total_quantity FROM temp_invoices WHERE 송장구분 = '단일' GROUP BY 품목코드
		) AS t2 ON t1.품목코드 = t2.품목코드
		SET t1.품목개수 = t2.total_quantity WHERE t1.송장구분 = '단일';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 단일 데이터 품목개수 합산', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 송장출력_경기공산추가송장 테이블로 유니크 주소 행 이동
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
		
	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (경기공산) 추가송장 기본 데이터 생성', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 경기공산추가송장 업데이트
	--             주문번호 추가(20250821) 움직이지 않는 송장이 주문에 입혀지지 않기 위함
    --================================================================================*/
    UPDATE temp_additional_invoices
		SET 수량 = 1, 송장명 = '+++', 옵션명 = '+++', 품목코드 = '0000', 주문번호 = '1234567890';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 추가송장 내용 변경 (+++)', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 경기공산 추가송장 늘리기
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
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (경기공산) 추가송장 늘리기', ROW_COUNT());
    
    /*--================================================================================
	-- (경기공산) 경기공산 추가송장 순번 매기기
    --================================================================================*/
    UPDATE temp_additional_invoices AS t JOIN (
			SELECT id, @seq := IF(@current_group = 주소, @seq + 1, 1) AS rn, @current_group := 주소
			FROM temp_additional_invoices, (SELECT @seq := 0, @current_group := '') AS vars ORDER BY 주소, id
		) AS s ON t.id = s.id
		SET t.송장구분자 = CONCAT('[', s.rn, ']');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 추가송장 순번 매기기', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 경기공산추가송장 주소업데이트
    --================================================================================*/
    UPDATE temp_additional_invoices SET 주소 = CONCAT(주소, 송장구분자);
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (경기공산) 추가송장 주소에 순번 추가', ROW_COUNT());

    /*--================================================================================
	-- (경기공산) 경기공산 테이블 마지막정리
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
			  FROM 송장출력_공통박스 WHERE 송장구분최종 = '공산박스'
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
    
    TRUNCATE TABLE 송장출력_경기공산_최종;
	
    INSERT INTO 송장출력_경기공산_최종 SELECT * FROM temp_sorted_data;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (경기공산) 최종 테이블 데이터 통합', ROW_COUNT());
	
    /*--================================================================================
	-- (경기공산) 송장출력_경기공산_최종 테이블 업데이트
    --================================================================================*/
    UPDATE 송장출력_경기공산_최종, (
        SELECT
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_GONGSAN_COST')  AS c_cost,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_GONGSAN_SIZE')  AS c_size,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_GONGSAN_COUNT') AS c_count
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