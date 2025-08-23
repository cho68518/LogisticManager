DELIMITER $$

DROP PROCEDURE IF EXISTS sp_Excel_Proc4$$

CREATE PROCEDURE sp_Excel_Proc4(IN tempTableName VARCHAR(255))
BEGIN
    /*--================================================================================
    -- 엑셀 업로드 데이터 처리
	--    감천 특별출고 처리 엑셀파일을 읽어서 테이블 Insert
    --================================================================================*/
    DECLARE v_total_rows INT DEFAULT 0;
    DECLARE v_inserted_rows INT DEFAULT 0;
    
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        
        SET @dropSql = CONCAT('DROP TEMPORARY TABLE IF EXISTS ', tempTableName);
        PREPARE stmt FROM @dropSql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
        
        SELECT '오류가 발생하여 작업이 롤백되었습니다. 아래는 오류 상세 정보입니다.' AS Message;
        SHOW ERRORS;
    END;
    
    START TRANSACTION;

	TRUNCATE TABLE 송장출력_특수출력_감천분리출고;
	
    -- 임시 테이블의 총 데이터 건수 확인
    SET @countSql = CONCAT('SELECT COUNT(*) INTO @total_rows FROM ', tempTableName);
    PREPARE stmt FROM @countSql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
    SET v_total_rows = @total_rows;

    SET @processSql = CONCAT('
        INSERT INTO 송장출력_특수출력_감천분리출고 (
            품목코드, 상품명, 수량, 택배수량 
        )
        SELECT 
            품목코드, 
			상품명, 
			수량,
			NULL
        FROM ', tempTableName, '
    ');
	
    PREPARE stmt FROM @processSql;
    EXECUTE stmt;
    SET v_inserted_rows = ROW_COUNT();
    DEALLOCATE PREPARE stmt;

    COMMIT;
    
    SELECT 
        '성공' AS 결과,
        v_total_rows AS 총데이터건수,
        v_inserted_rows AS 처리성공건수,
        (v_total_rows - v_inserted_rows) AS 처리실패건수,
        NOW() AS 완료시간;
        
    SET @dropSql = CONCAT('DROP TEMPORARY TABLE IF EXISTS ', tempTableName);
    PREPARE stmt FROM @dropSql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
    
END$$

DELIMITER ;