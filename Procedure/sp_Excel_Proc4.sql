DELIMITER $$

DROP PROCEDURE IF EXISTS sp_Excel_Proc4$$

CREATE PROCEDURE sp_Excel_Proc4(IN tempTableName VARCHAR(255))
BEGIN
    /*--================================================================================
    -- ���� ���ε� ������ ó��
	--    ��õ Ư����� ó�� ���������� �о ���̺� Insert
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
        
        SELECT '������ �߻��Ͽ� �۾��� �ѹ�Ǿ����ϴ�. �Ʒ��� ���� �� �����Դϴ�.' AS Message;
        SHOW ERRORS;
    END;
    
    START TRANSACTION;

	TRUNCATE TABLE �������_Ư�����_��õ�и����;
	
    -- �ӽ� ���̺��� �� ������ �Ǽ� Ȯ��
    SET @countSql = CONCAT('SELECT COUNT(*) INTO @total_rows FROM ', tempTableName);
    PREPARE stmt FROM @countSql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
    SET v_total_rows = @total_rows;

    SET @processSql = CONCAT('
        INSERT INTO �������_Ư�����_��õ�и���� (
            ǰ���ڵ�, ��ǰ��, ����, �ù���� 
        )
        SELECT 
            ǰ���ڵ�, 
			��ǰ��, 
			����,
			NULL
        FROM ', tempTableName, '
    ');
	
    PREPARE stmt FROM @processSql;
    EXECUTE stmt;
    SET v_inserted_rows = ROW_COUNT();
    DEALLOCATE PREPARE stmt;

    COMMIT;
    
    SELECT 
        '����' AS ���,
        v_total_rows AS �ѵ����ͰǼ�,
        v_inserted_rows AS ó�������Ǽ�,
        (v_total_rows - v_inserted_rows) AS ó�����аǼ�,
        NOW() AS �Ϸ�ð�;
        
    SET @dropSql = CONCAT('DROP TEMPORARY TABLE IF EXISTS ', tempTableName);
    PREPARE stmt FROM @dropSql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
    
END$$

DELIMITER ;