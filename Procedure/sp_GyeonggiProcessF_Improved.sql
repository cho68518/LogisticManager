DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GyeonggiProcessF_Improved$$

CREATE PROCEDURE sp_GyeonggiProcessF_Improved()
BEGIN
    /*--================================================================================
	-- 경기냉동 처리 (개선된 오류 처리 버전)
    --================================================================================*/
    
    -- 오류 처리 변수 선언
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
        DROP TEMPORARY TABLE IF EXISTS temp_invoices;
        DROP TEMPORARY TABLE IF EXISTS temp_additional_invoices;
        
        -- 상세한 오류 정보 제공
        SELECT 
            '오류가 발생하여 모든 작업이 롤백되었습니다.' AS Message,
            'SQL_EXCEPTION' AS ErrorType,
            '프로시저 실행 중 예외가 발생했습니다.' AS ErrorDetail,
            '오류 코드: ' || SQLSTATE AS ErrorCode,
            '오류 메시지: ' || SQLERRM AS ErrorMessage;
            
        -- SHOW ERRORS 결과를 별도 결과셋으로 반환
        SHOW ERRORS;
    END;

    CREATE TEMPORARY TABLE sp_execution_log ( 
        StepID INT AUTO_INCREMENT PRIMARY KEY, 
        OperationDescription VARCHAR(255), 
        AffectedRows INT,
        Status VARCHAR(50) DEFAULT 'SUCCESS',
        ErrorMessage TEXT
    );
    
    START TRANSACTION;

    /*--================================================================================
	-- 1. 작업용 임시 테이블 생성 및 초기 데이터 설정
    --================================================================================*/
    CALL sp_CreateWorkTables();
    
    /*--================================================================================
	-- (경기냉동) 경기냉동 데이터 분석 - 상세 검증 포함
    --================================================================================*/
    
    -- 데이터 존재 여부 확인 및 상세 오류 정보 제공
    SET @gyeonggi_frozen_count = (SELECT COUNT(*) FROM 통합송장_경기냉동);
    
    IF @gyeonggi_frozen_count = 0 THEN
        -- 데이터가 없는 경우 상세한 오류 정보 제공
        INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status, ErrorMessage) 
        VALUES ('[ERROR] 경기냉동 데이터 검증', 0, 'FAILED', '통합송장_경기냉동 테이블에 데이터가 존재하지 않습니다.');
        
        SELECT 
            '경기냉동 데이터가 없습니다.' AS Message,
            'DATA_NOT_FOUND' AS ErrorType,
            '통합송장_경기냉동 테이블에 데이터가 존재하지 않습니다.' AS ErrorDetail,
            '테이블명: 통합송장_경기냉동' AS TableInfo,
            '필요 데이터: 경기냉동 관련 주문 정보' AS RequiredData,
            '해결방법: 경기냉동 데이터를 먼저 입력해주세요.' AS Solution;
            
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '경기냉동 데이터가 없습니다.';
    END IF;
    
    -- 데이터 로드 성공 시 로그 기록
    INSERT INTO temp_invoices (
        msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호, 주소, 상품명, 수량, 상품가격, 
        주문번호, 주문일시, 주문자, 상품코드, 배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계, 
        추가송장수, 추가송장수1, 추가송장수합계, 배송비율, 배송비, 배송비합계, 위치, 위치변환
    )
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호, 주소, 상품명, 수량, 상품가격, 
        주문번호, 주문자, 주문일시, 상품코드, 배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계, 
        추가송장수, 추가송장수1, 추가송장수합계, 배송비율, 배송비, 배송비합계, 위치, 위치변환
    FROM 통합송장_경기냉동;
        
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[INSERT] temp_invoices, 경기냉동데이터 분석', ROW_COUNT(), 'SUCCESS');

    /*--================================================================================
	-- 2. 작업용 임시 테이블 데이터 가공 및 변환 (기본 UPDATE 작업)
    --================================================================================*/
    
    -- 추가송장수 계산 및 배송비율 업데이트
    UPDATE temp_invoices SET 배송비율 = FLOOR((1 / IFNULL(NULLIF(추가송장수, 0), 10)) * 1000) / 1000;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_invoices, 추가송장수로 배송비율 계산', ROW_COUNT(), 'SUCCESS');

    -- 배송비율과 수량을 곱한 배송비 업데이트
    UPDATE temp_invoices SET 배송비 = 배송비율 * 수량;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_invoices, 배송비율과 수량으로 배송비 계산', ROW_COUNT(), 'SUCCESS');

    -- 주소 + 출고지구분별 배송비 합계
    UPDATE temp_invoices AS t JOIN (
        SELECT 주소, 출고지구분, SUM(배송비) AS 배송비합계
        FROM temp_invoices GROUP BY 주소, 출고지구분
    ) AS s ON t.주소 = s.주소 AND t.출고지구분 = s.출고지구분
    SET t.추가송장수1 = s.배송비합계;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_invoices, 주소/출고지구분별 배송비 합계', ROW_COUNT(), 'SUCCESS');

    -- 추가송장수1 올림 처리 ('별표'가 포함된 경우 제외)
    UPDATE temp_invoices SET 추가송장수1 = CEIL(IFNULL(추가송장수1, 1));
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_invoices, 추가송장수 올림 처리', ROW_COUNT(), 'SUCCESS');

    -- 추가송장수1에 따른 배송구분 업데이트
    UPDATE temp_invoices SET 배송구분 = CASE WHEN 추가송장수1 > 1 THEN '추가' ELSE '1개' END;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_invoices, 배송구분 설정 (추가/1개)', ROW_COUNT(), 'SUCCESS');

    -- 주소와 출고지구분 조합이 유일한 경우 배송구분 업데이트 처리
    --             주소 + 출고지구분 조합이 유일한 경우 '단일'로 업데이트
    UPDATE temp_invoices t1 JOIN (
        SELECT 주소, 출고지구분 FROM temp_invoices WHERE 배송구분 = '1개' GROUP BY 주소, 출고지구분 HAVING COUNT(*) = 1
    ) AS unique_address ON t1.주소 = unique_address.주소 AND t1.출고지구분 = unique_address.출고지구분
    SET t1.배송구분 = '단일' WHERE t1.배송구분 = '1개';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_invoices, 배송구분 설정 (단일)', ROW_COUNT(), 'SUCCESS');

    -- 상품코드별 수량 합계 및 상품수량합계
    UPDATE temp_invoices t1 JOIN (
        SELECT 상품코드, SUM(수량) AS total_quantity FROM temp_invoices WHERE 배송구분 = '단일' GROUP BY 상품코드
    ) AS t2 ON t1.상품코드 = t2.상품코드
    SET t1.상품수량합계 = t2.total_quantity WHERE t1.배송구분 = '단일';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_invoices, 단일 배송 상품수량 합계', ROW_COUNT(), 'SUCCESS');

    /*--================================================================================
	-- 3. '추가' 배송 처리 (기본 임시 테이블 복사)
    --================================================================================*/
    
    -- 경기냉동추가송장 테이블에서 기본 데이터 추출
    INSERT INTO temp_additional_invoices
    SELECT * FROM (
        SELECT *, ROW_NUMBER() OVER (PARTITION BY 주소 ORDER BY id ASC) AS rn
        FROM temp_invoices WHERE 배송구분 = '추가'
    ) AS subquery WHERE subquery.rn = 1;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[INSERT] temp_additional_invoices, 추가송장 기본 데이터 추출', ROW_COUNT(), 'SUCCESS');

    -- 경기냉동 추가송장 데이터 업데이트
    --             주문번호 추가(20250821) 상품명이 없는 경우 상품명을 초기 값
    UPDATE temp_additional_invoices
    SET 수량 = 1, 상품명 = '+++', 상품코드 = '0000', 주문번호 = '1234567890';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_additional_invoices, 추가송장 기본 값 설정 (+++)', ROW_COUNT(), 'SUCCESS');

    -- 경기냉동 추가송장 복제
    INSERT INTO temp_additional_invoices
    SELECT t.* FROM temp_additional_invoices AS t JOIN Numbers AS n ON n.n <= (t.추가송장수1 - 1) WHERE t.추가송장수1 > 1;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[INSERT] temp_additional_invoices, 추가송장 복제', ROW_COUNT(), 'SUCCESS');

    -- 경기냉동 추가송장 순번 부여
    UPDATE temp_additional_invoices AS t JOIN (
        SELECT id, @seq := IF(@current_group = 주소, @seq + 1, 1) AS rn, @current_group := 주소
        FROM temp_additional_invoices, (SELECT @seq := 0, @current_group := '') AS vars ORDER BY 주소, id
    ) AS s ON t.id = s.id
    SET t.배송비율 = CONCAT('[', s.rn, ']');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] temp_additional_invoices, 추가송장 순번 부여', ROW_COUNT(), 'SUCCESS');

    /*--================================================================================
	-- 4. 최종 결과 테이블에 모든 데이터 통합 및 정렬
    --================================================================================*/
    
    -- 최종 결과를 통합송장_경기냉동_최종 테이블에 삽입
    INSERT INTO 통합송장_경기냉동_최종 (
        msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호,
        주소, 상품명, 수량, 상품가격, 주문번호, 주문자, 상품코드,
        배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계
    )
    SELECT
        msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호,
        주소, 상품명, 수량, 상품가격, 주문번호, 주문자, 상품코드,
        배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계
    FROM (
        -- (경기냉동) 경기냉동 단일 배송
        SELECT msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호, 주소, 상품명, 수량, 
               상품가격, 주문번호, 주문자, 상품코드, 배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계 
        FROM temp_invoices WHERE 배송구분 = '단일'
        UNION ALL
        -- (경기냉동) 경기공산 데이터 (기본 테이블)
        SELECT msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호, 주소, 상품명, 수량, 
               상품가격, 주문번호, 주문자, 상품코드, 배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계 
        FROM 통합송장_경기공산 WHERE 출고지구분 = '경기공산'
        UNION ALL
        -- (경기냉동) 경기냉동 1개 배송
        SELECT msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호, 주소, 상품명, 수량, 
               상품가격, 주문번호, 주문자, 상품코드, 배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계 
        FROM temp_invoices WHERE 배송구분 = '1개'
        UNION ALL
        -- (경기냉동) 경기냉동 추가 (단일 + 추가 데이터)
        SELECT msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호, 주소, 상품명, 수량, 
               상품가격, 주문번호, 주문자, 상품코드, 배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계 
        FROM temp_invoices WHERE 배송구분 = '추가'
        UNION ALL
        SELECT msg1, msg2, msg3, msg4, msg5, msg6, 출고지구분, 전화번호1, 전화번호2, 주문자번호, 주소, 상품명, 수량, 
               상품가격, 주문번호, 주문자, 상품코드, 배송비, 상품크기, 상품수량, 상품설명, 별표1, 별표2, 상품수량합계 
        FROM temp_additional_invoices
    ) AS final_union
    /*--================================================================================
	-- (경기냉동) 별표 순 이동 순 정렬
    --================================================================================*/
    ORDER BY
        CASE WHEN 별표1 <> '' OR 별표2 <> '' THEN 0 ELSE 1 END,
        주소 ASC, 상품명 ASC;
        
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[INSERT] 최종 결과 테이블에 모든 데이터 통합 및 정렬', ROW_COUNT(), 'SUCCESS');

    -- =================================================================================
    -- 최종 결과 테이블 주문 중 하나는 '경기냉동 추가' 주문이고 
    --    다른 하나는 일반주문인 경우 주소가 중복되면, '경기냉동 추가' 주문 
    --    주소가 비어있는 주소 필드를 일반주문 주소로 채움.
    -- =================================================================================
    -- 주소 업데이트 처리
    UPDATE 통합송장_경기냉동_최종 AS d
    JOIN (
        -- (일반주문)주문 그룹별로 주소가 비어있지 않은 주소 중 하나를 찾기
        SELECT
            IFNULL(전화번호1, '') AS 전화번호1,
            IFNULL(전화번호2, '') AS 전화번호2,
            IFNULL(주문자번호,  '') AS 주문자번호,
            IFNULL(출고지구분,  '') AS 출고지구분,
            MAX(주소)             AS 주소 -- 주문별로 주소 중 하나를 선택
        FROM 통합송장_경기냉동_최종
        WHERE
            상품명 <> '경기냉동 추가' AND
            주소 IS NOT NULL AND
            주소 <> ''
        GROUP BY
            IFNULL(전화번호1, ''),
            IFNULL(전화번호2, ''),
            IFNULL(주문자번호,  ''),
            IFNULL(출고지구분,  '')
    ) AS src ON
        IFNULL(d.전화번호1, '') = src.전화번호1 AND
        IFNULL(d.전화번호2, '') = src.전화번호2 AND
        IFNULL(d.주문자번호,  '') = src.주문자번호  AND
        IFNULL(d.출고지구분,  '') = src.출고지구분
    SET d.주소 = src.주소  -- 경기냉동추가 주문의 주소를 업데이트
    WHERE d.상품명 = '경기냉동 추가' AND d.주소 IS NULL;
    
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] 경기냉동 추가 주문 주소 업데이트', ROW_COUNT(), 'SUCCESS');
    
    /*--================================================================================
	-- (경기냉동) 통합송장_경기냉동_최종 테이블 기본값 업데이트
    --================================================================================*/
    UPDATE 통합송장_경기냉동_최종, (
        SELECT
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_COST')  AS c_cost,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_SIZE')  AS c_size,
            (SELECT Attribute1 FROM CommonCode WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'GYEONGGI_FROZEN_COUNT') AS c_count
    ) AS p_values
    SET 배송비 = p_values.c_cost, 상품크기 = p_values.c_size, 상품수량 = p_values.c_count;
    
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows, Status) 
    VALUES ('[UPDATE] 최종 테이블 기본값 설정', ROW_COUNT(), 'SUCCESS');
        
    COMMIT;
    
    -- 성공적으로 완료된 경우 실행 로그 반환
    SELECT StepID, OperationDescription, AffectedRows, Status, ErrorMessage 
    FROM sp_execution_log 
    ORDER BY StepID;

    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
    DROP TEMPORARY TABLE IF EXISTS temp_invoices;
    DROP TEMPORARY TABLE IF EXISTS temp_additional_invoices;

END$$

DELIMITER ;
