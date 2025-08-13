DELIMITER $$

DROP PROCEDURE IF EXISTS TalkDealUnavailable$$

CREATE PROCEDURE TalkDealUnavailable()
BEGIN
    -- =================================================================
	-- 톡딜불가 처리 루틴
	/*추후 Index추가 할것
	CREATE INDEX ix_사방넷_주소    ON `송장출력_사방넷원본변환` (`주소`);
	CREATE INDEX ix_사방넷_품목코드 ON `송장출력_사방넷원본변환` (`품목코드`);
	CREATE INDEX ix_사방넷_쇼핑몰   ON `송장출력_사방넷원본변환` (`쇼핑몰`);

	-- 톡딜불가
	CREATE INDEX ix_톡딜_품목_쇼핑 ON `송장출력_톡딜불가` (`품목코드`,`쇼핑몰`);
	CREATE INDEX ix_톡딜_필수1     ON `송장출력_톡딜불가` (`필수코드1`);
	CREATE INDEX ix_톡딜_필수2     ON `송장출력_톡딜불가` (`필수코드2`);
	CREATE INDEX ix_톡딜_필수3     ON `송장출력_톡딜불가` (`필수코드3`);	*/
    -- =================================================================
    START TRANSACTION;
	
	-- Target 주소를 담을 임시테이블
	CREATE TEMPORARY TABLE IF NOT EXISTS _target_addr (
		주소 VARCHAR(255)
	  ) ENGINE=Memory;

	TRUNCATE _target_addr;	
	
	-- 별표1 처리
	-- '송장출력_사방넷원본변환' 테이블과 '송장출력_톡딜불가'에서 같은 상품(품목코드)이고, 같은 쇼핑몰인 행
	
	INSERT IGNORE INTO _target_addr(주소)
		SELECT DISTINCT A.주소
		  FROM 송장출력_사방넷원본변환_Dev AS A
		  JOIN 송장출력_톡딜불가       AS B
			ON A.품목코드 = B.품목코드
		AND A.쇼핑몰   = B.쇼핑몰
	  JOIN (
		SELECT 주소, COUNT(*) AS cnt
		FROM 송장출력_사방넷원본변환_Dev
		WHERE 주소 IS NOT NULL
		GROUP BY 주소
	  ) AS C
		ON C.주소 = A.주소
	  LEFT JOIN 송장출력_사방넷원본변환_Dev AS A3
		ON A3.주소 = A.주소
	   AND A3.품목코드 IN (B.필수코드1, B.필수코드2, B.필수코드3)
	  WHERE A.주소 IS NOT NULL
		AND (
			  C.cnt = 1
			  OR (C.cnt > 1 AND A3.품목코드 IS NULL)
			);

	  -- 대상 주소에 해당하는 A-행만 일괄 업데이트
	  UPDATE 송장출력_사방넷원본변환_Dev AS S
	  JOIN _target_addr AS T ON T.주소 = S.주소
	  SET S.별표1 = '***';

	  DROP TEMPORARY TABLE IF EXISTS _target_addr;
	  

    COMMIT;

    -- =================================================================
    -- 3. 마무리
    -- =================================================================
    SELECT '작업완료.' AS ResultMessage;

END$$

DELIMITER ;
