DELIMITER $$

DROP PROCEDURE IF EXISTS sp_SeoulGongsanProcessF$$

CREATE PROCEDURE sp_SeoulGongsanProcessF()
BEGIN
    /*--================================================================================
	-- 서울공산 처리
    --================================================================================*/
    DECLARE done INT DEFAULT FALSE;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
		DECLARE error_info TEXT DEFAULT '';
		DECLARE error_code INT DEFAULT 0;
		
		-- MySQL 오류 정보 수집
		GET DIAGNOSTICS CONDITION 1
			error_code = MYSQL_ERRNO,
			error_info = MESSAGE_TEXT;
			
        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
        SELECT '오류가 발생하여 모든 작업이 롤백되었습니다.' AS ErrorMessage,
		        error_code AS MySQLErrorCode,
                error_info AS MySQLErrorMessage;
    END;

    CREATE TEMPORARY TABLE IF NOT EXISTS sp_execution_log (
		StepID INT AUTO_INCREMENT PRIMARY KEY,
		OperationDescription VARCHAR(255),
		AffectedRows INT
	);

    START TRANSACTION;

    TRUNCATE TABLE sp_execution_log;

    /*--================================================================================
	-- (서울공산) 공산낱개 중 서울 주소 → 서공낱개
    --================================================================================*/
	UPDATE 송장출력_사방넷원본변환
	   SET 송장구분최종 = CASE 
							WHEN 송장구분최종 = '공산낱개' THEN '서공낱개'
							WHEN 송장구분최종 = '공산박스' THEN '서공박스'
							ELSE 송장구분최종
						  END
	 WHERE 송장구분최종 IN ('공산낱개', '공산박스')
	   AND SUBSTRING(TRIM(SUBSTRING_INDEX(주소, ']', -1)), 1, 2) = '서울'
	   AND 쇼핑몰 IN ('Cafe24(신)', 'Gfresh');

    /*--================================================================================
	-- (서울공산) 공산박스 중 서울 주소 → 서공박스
    --================================================================================*/
    UPDATE 송장출력_공통박스
       SET 송장구분최종 = '서공박스'
     WHERE 송장구분최종 = '공산박스'
       AND SUBSTRING(
              TRIM(SUBSTRING_INDEX(주소, ']', -1)), 1, 2
           ) = '서울'
       AND 쇼핑몰 IN ('Cafe24(신)', 'Gfresh');

    /*--================================================================================
	-- (서울공산) 서울공산낱개 분류
    --================================================================================*/
	TRUNCATE TABLE 송장출력_서울공산변환;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산변환 초기화', 0);
	
	INSERT INTO 송장출력_서울공산변환 
	           (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
	            주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 
				택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 택배수량1, 
				택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
           FROM 송장출력_사방넷원본변환
          WHERE 송장구분최종 = '서공낱개';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산변환, 서울공산낱개 데이터 분류', ROW_COUNT());
	
    /*--=====================================================================================================
	-- (서울공산) 택배수량 계산 및 송장구분자 업데이트
	--            택배수량을 이용하여 1을 나눈 결과를 소수점 세 자리에서 내림하여 송장구분자 컬럼에 업데이트
    --======================================================================================================*/
	UPDATE 송장출력_서울공산변환
       SET 송장구분자 = FLOOR((1 / IFNULL(NULLIF(택배수량, 0), 10)) * 1000) / 1000;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산변환, 택배수량으로 송장구분자 계산', ROW_COUNT());
	   
    /*--================================================================================
	-- (서울공산) 송장구분자와 수량 곱 업데이트
    --================================================================================*/
	UPDATE 송장출력_서울공산변환
       SET 송장구분자 = 송장구분자 * 수량;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산변환, 송장구분자에 수량 적용', ROW_COUNT());
	   
    /*--================================================================================
	-- (서울공산) 주소 + 수취인명 기반 송장구분자 합산
    --================================================================================*/
	UPDATE 송장출력_서울공산변환 AS t
      JOIN (
            SELECT 주소, 수취인명, SUM(송장구분자) AS 구분자합
              FROM 송장출력_서울공산변환
          GROUP BY 주소, 수취인명
        ) AS s ON t.주소 = s.주소 AND t.수취인명 = s.수취인명
       SET t.택배수량1 = s.구분자합;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산변환, 주소/수취인명별 송장구분자 합산', ROW_COUNT());
	
    /*--================================================================================
	-- (서울공산) 택배수량1 올림 처리  ('낱개' 로 남아있는 행이 있음)
    --================================================================================*/
	UPDATE 송장출력_서울공산변환
       SET 택배수량1 = CEIL(IFNULL(택배수량1, 1));
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산변환, 택배수량 올림 처리', ROW_COUNT());

    /*--================================================================================
	-- (서울공산) 택배수량1에 따른 송장구분 업데이트
    --================================================================================*/
	UPDATE 송장출력_서울공산변환
       SET 송장구분 = CASE WHEN 택배수량1 > 1 THEN '추가' ELSE '1장' END;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산변환, 송장구분 설정 (추가/1장)', ROW_COUNT());
	
    /*--================================================================================
	-- (서울공산) 주소 및 수취인명 유일성에 따른 송장구분 업데이트 시작
	--            주소 + 수취인명 조합이 유일한 경우 '단일'로 업데이트
    --================================================================================*/
    UPDATE 송장출력_서울공산변환 t1
      JOIN (
            SELECT 주소, 수취인명
              FROM 송장출력_서울공산변환
             WHERE 송장구분 = '1장'
          GROUP BY 주소, 수취인명
            HAVING COUNT(*) = 1
        ) AS unique_address ON t1.주소 = unique_address.주소 AND t1.수취인명 = unique_address.수취인명
       SET t1.송장구분 = '단일'
     WHERE t1.송장구분 = '1장';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산변환, 송장구분 설정 (단일)', ROW_COUNT());
		
    /*--================================================================================
	-- (서울공산) 서울공산1장 분류
    --================================================================================*/
	TRUNCATE TABLE 송장출력_서울공산1장;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산1장 초기화', 0);
	
	INSERT INTO 송장출력_서울공산1장 
	           (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
	            주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
           FROM 송장출력_서울공산변환
          WHERE 송장구분 = '1장'
		  ORDER BY 위치변환 DESC;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 서울공산1장 데이터 분류', ROW_COUNT());
	   
    /*--================================================================================
	-- (서울공산) 서울공산 단일 분류
    --================================================================================*/
	TRUNCATE TABLE 송장출력_서울공산단일;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산단일 초기화', 0);
	
    INSERT INTO 송장출력_서울공산단일 
	           (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
	            주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
			    택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
           FROM 송장출력_서울공산변환
          WHERE 송장구분 = '단일'
		  ORDER BY 위치변환 DESC;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산단일 단일 데이터 분류', ROW_COUNT());
	
    /*--================================================================================
	-- (서울공산) 품목코드별 수량 합산 및 품목개수
	--            품목코드별로 수량을 합산하여 출력개수에 업데이트
    --================================================================================*/
    UPDATE 송장출력_서울공산단일 AS t1 
    INNER JOIN (
            SELECT 품목코드, SUM(수량) AS total_quantity
              FROM 송장출력_서울공산단일
          GROUP BY 품목코드
        ) AS t2 ON t1.품목코드 = t2.품목코드
       SET t1.품목개수 = t2.total_quantity;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산단일, 단일 데이터 품목개수 합산', ROW_COUNT());
		
    /*--================================================================================
	-- (서울공산) 서울공산 추가 분류
    --================================================================================*/
	TRUNCATE TABLE 송장출력_서울공산추가;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산추가 초기화', 0);
	
    INSERT INTO 송장출력_서울공산추가 
	           (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
	            주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
           FROM 송장출력_서울공산변환
          WHERE 송장구분 = '추가'
		  ORDER BY 주소 DESC;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산추가 데이터', ROW_COUNT());
	   
    /*--================================================================================
	-- (서울공산) 송장출력_서울공산추가송장 테이블로 유니크 주소 행 이동
    --================================================================================*/
	TRUNCATE TABLE 송장출력_서울공산추가송장;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산추가송장 초기화', 0);
	
	-- '주소'가 유니크한 첫 번째 행만 선택하여 대상 테이블로 Insert
	INSERT INTO 송장출력_서울공산추가송장 (
			msg1, msg2, msg3, msg4, msg5, msg6, 
			수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
			옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 
			수집시간, 송장명, 품목코드, 택배비용, 박스크기, 
			출력개수, 송장수량, 별표1, 별표2, 품목개수, 
			택배수량, 택배수량1, 택배수량합산, 송장구분자, 
			송장구분, 송장구분최종, 위치, 위치변환
		)
		SELECT 
			msg1, msg2, msg3, msg4, msg5, msg6, 
			수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 
			옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 
			수집시간, 송장명, 품목코드, 택배비용, 박스크기, 
			출력개수, 송장수량, 별표1, 별표2, 품목개수, 
			택배수량, 택배수량1, 택배수량합산, 송장구분자, 
			송장구분, 송장구분최종, 위치, 위치변환
		FROM (
			SELECT 
				*, 
				ROW_NUMBER() OVER (PARTITION BY 주소 ORDER BY id ASC) AS rn
			FROM 
				송장출력_서울공산추가
		) AS subquery
		WHERE 
			subquery.rn = 1;
			
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산추가송장', ROW_COUNT());

    /*--================================================================================
	-- (서울공산) 서울공산추가송장 업데이트
	--            주문번호 추가(20250821) 움직이지 않는 송장이 주문에 입혀지지 않기 위함
    --================================================================================*/
	UPDATE 송장출력_서울공산추가송장
       SET 수량 = 1, 송장명 = '+++', 옵션명 = '+++' , 품목코드 = '0000', 주문번호 = '1234567890';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산추가송장, 추가송장 내용 변경 (+++)', ROW_COUNT());

    /*--================================================================================
	-- (서울공산) 서울공산 추가송장 늘리기
    --================================================================================*/
	INSERT INTO 송장출력_서울공산추가송장 
	       (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
	        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
			택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
			택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
	SELECT t.msg1, t.msg2, t.msg3, t.msg4, t.msg5, t.msg6, t.수취인명, t.전화번호1, t.전화번호2, t.우편번호, 
	       t.주소, t.옵션명, t.수량, t.배송메세지, t.주문번호, t.쇼핑몰, t.수집시간, t.송장명, t.품목코드, 
		   t.택배비용, t.박스크기, t.출력개수, t.송장수량, t.별표1, t.별표2, t.품목개수, t.택배수량, 
		   t.택배수량1, t.택배수량합산, t.송장구분자, t.송장구분, t.송장구분최종, t.위치, t.위치변환
	  FROM 송장출력_서울공산추가송장 AS t
	  JOIN Numbers AS n ON n.n <= (t.택배수량1 - 2)
	 WHERE t.택배수량1 > 2;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산추가송장, 추가송장 늘리기', ROW_COUNT());
			
    /*--================================================================================
	-- (서울공산) 서울공산추가송장 순번 매기기
    --================================================================================*/
    UPDATE 송장출력_서울공산추가송장 AS t
      JOIN (
            SELECT id, @seq := IF(@current_group = 위치변환, @seq + 1, 1) AS rn, @current_group := 위치변환
              FROM 송장출력_서울공산추가송장, (SELECT @seq := 0, @current_group := '') AS vars
          ORDER BY 위치변환, id
        ) AS s ON t.id = s.id
       SET t.송장구분자 = CONCAT('[', s.rn, ']');
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산추가송장, 추가송장 순번 매기기', ROW_COUNT());
		
    /*--================================================================================
	-- (서울공산) 서울공산추가송장 주소업데이트
    --================================================================================*/
    UPDATE 송장출력_서울공산추가송장 SET 주소 = CONCAT(주소, 송장구분자);
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산추가송장, 추가송장 주소에 순번 추가', ROW_COUNT());
	
    /*--================================================================================
	-- (서울공산) 서울공산추가 합치기
    --================================================================================*/
    TRUNCATE TABLE 송장출력_서울공산추가합치기;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산추가합치기 초기화', 0);
	
	INSERT INTO 송장출력_서울공산추가합치기
	           (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
			    주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
           FROM (
              SELECT * FROM 송장출력_서울공산추가
              UNION ALL
              SELECT * FROM 송장출력_서울공산추가송장
           ) AS combined
		   ORDER BY 위치 DESC;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산추가합치기, 원본+추가송장 데이터 통합', ROW_COUNT());

    /*--================================================================================
	-- (서울공산) 서울공산 테이블 마지막정리
    --================================================================================*/
    TRUNCATE TABLE 송장출력_서울공산;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산 초기화', 0);

    INSERT INTO 송장출력_서울공산 
	           (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
	            주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환 
		   FROM 송장출력_서울공산단일
--		   ORDER BY 위치변환 ASC
		 UNION ALL
		 SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환 
		   FROM 송장출력_공통박스 
		  WHERE 송장구분최종 = '서공박스'
--		  ORDER BY 위치변환 ASC
		 UNION ALL
		 SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환 
		   FROM 송장출력_서울공산1장
		 UNION ALL
		 SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환 
		   FROM 송장출력_서울공산추가합치기
		   ORDER BY 주소 ASC;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산, 최종 데이터 통합 (UNION ALL)', ROW_COUNT());
	
    /*--================================================================================
	-- (서울공산) 별표 행 이동 및 삭제
    --================================================================================*/
    TRUNCATE TABLE 송장출력_서울공산별표;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산별표 초기화', 0);
	
    INSERT INTO 송장출력_서울공산별표
         SELECT * FROM 송장출력_서울공산
          WHERE 별표1 <> '' OR 별표2 <> '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산별표, 별표 데이터 임시 저장', ROW_COUNT());
	
    DELETE FROM 송장출력_서울공산
          WHERE 별표1 <> '' OR 별표2 <> '';
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] 송장출력_서울공산, 원본 테이블에서 별표 데이터 삭제', ROW_COUNT());
	
    /*--=======================================================================================
	-- (서울공산) 별표1 기준으로 정렬하여 행 이동
	--            송장출력_서울공산별표에서 별표1로 정렬하여 송장출력_서울공산에 데이터 삽입
    --=======================================================================================*/
    INSERT INTO 송장출력_서울공산
	           (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
	            주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 옵션명, 수량, 배송메세지, 주문번호, 쇼핑몰, 수집시간, 송장명, 품목코드, 
				택배비용, 박스크기, 출력개수, 송장수량, 별표1, 별표2, 품목개수, 택배수량, 
				택배수량1, 택배수량합산, 송장구분자, 송장구분, 송장구분최종, 위치, 위치변환
           FROM 송장출력_서울공산별표
       ORDER BY 주소 ASC, 옵션명 ASC;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산, 별표 데이터를 다시 맨 위로 이동', ROW_COUNT());
		
    /*--================================================================================
	-- (서울공산) 송장출력_서울공산에서 송장출력_서울공산_최종으로 데이터 이동
    --================================================================================*/
    TRUNCATE TABLE 송장출력_서울공산_최종;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(서울공산) 송장출력_서울공산_최종 초기화', 0);
	
	INSERT INTO 송장출력_서울공산_최종
	           (msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
	            주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰, 품목코드, 
				택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수)
         SELECT msg1, msg2, msg3, msg4, msg5, msg6, 수취인명, 전화번호1, 전화번호2, 우편번호, 
		        주소, 송장명, 수량, 배송메세지, 주문번호, 쇼핑몰, 품목코드, 
				택배비용, 박스크기, 출력개수, 별표1, 별표2, 품목개수
           FROM 송장출력_서울공산;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] 송장출력_서울공산_최종, 최종 테이블로 데이터 이동', ROW_COUNT());
		
    /*--================================================================================
	-- (서울공산) 송장출력_서울공산_최종 테이블 업데이트
    --================================================================================
    UPDATE 송장출력_서울공산_최종
       SET 택배비용 = 2150, 박스크기 = '극소', 출력개수 = 1;*/
	   
	UPDATE 
		송장출력_서울공산_최종,
		(
			SELECT
				(SELECT Attribute1 FROM CommonCodes WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'SEOUL_GONGSAN_COST')  AS c_cost,
				(SELECT Attribute1 FROM CommonCodes WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'SEOUL_GONGSAN_SIZE')  AS c_size,
				(SELECT Attribute1 FROM CommonCodes WHERE GroupCode = 'DELIVERY_POLICY' AND Code = 'SEOUL_GONGSAN_COUNT') AS c_count
		) AS p_values
	SET
		택배비용 = p_values.c_cost,
		박스크기 = p_values.c_size,
		출력개수 = p_values.c_count;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] 송장출력_서울공산_최종, 최종 테이블 기본값 설정', ROW_COUNT());
		
    COMMIT;
    
    -- 최종 결과를 SELECT 문으로 반환
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- 프로시저 종료 전 임시 테이블 삭제
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
