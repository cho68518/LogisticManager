DELIMITER $$

DROP PROCEDURE IF EXISTS sp_TalkDealUnavailable$$

CREATE PROCEDURE sp_TalkDealUnavailable()
BEGIN
    -- ���� ó���� ������ (EXIT HANDLER���� ���)
    DECLARE error_info TEXT DEFAULT '';
    DECLARE error_code INT  DEFAULT 0;
	
    /*-- =============================================================================
	-- ����Ұ� ó�� ��ƾ
	-- ���� Index�߰� �Ұ�
	-- CREATE INDEX ix_����_�ּ�    ON `�������_���ݿ�����ȯ` (`�ּ�`);
	-- CREATE INDEX ix_����_ǰ���ڵ� ON `�������_���ݿ�����ȯ` (`ǰ���ڵ�`);
	-- CREATE INDEX ix_����_���θ�   ON `�������_���ݿ�����ȯ` (`���θ�`);

	-- ����Ұ�
	-- CREATE INDEX ix_���_ǰ��_���� ON `�������_����Ұ�` (`ǰ���ڵ�`,`���θ�`);
	-- CREATE INDEX ix_���_�ʼ�1     ON `�������_����Ұ�` (`�ʼ��ڵ�1`);
	-- CREATE INDEX ix_���_�ʼ�2     ON `�������_����Ұ�` (`�ʼ��ڵ�2`);
	-- CREATE INDEX ix_���_�ʼ�3     ON `�������_����Ұ�` (`�ʼ��ڵ�3`);	
    -- =============================================================================*/
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
		-- MySQL ���� ���� ����
		GET DIAGNOSTICS CONDITION 1
			error_code = MYSQL_ERRNO,
			error_info = MESSAGE_TEXT;

        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
        SELECT '������ �߻��Ͽ� ��� �۾��� �ѹ�Ǿ����ϴ�.' AS ErrorMessage,
		        error_code AS MySQLErrorCode,
                error_info AS MySQLErrorMessage;
    END;

    CREATE TEMPORARY TABLE IF NOT EXISTS sp_execution_log (
		StepID INT AUTO_INCREMENT PRIMARY KEY,
		OperationDescription VARCHAR(255),
		AffectedRows INT
	);
	
    START TRANSACTION;

	-- �ӽ� �α� ���̺� �ʱ�ȭ
    TRUNCATE TABLE sp_execution_log;
	
	-- Target �ּҸ� ���� �ӽ����̺�
	CREATE TEMPORARY TABLE IF NOT EXISTS _target_addr (
		�ּ� VARCHAR(255)
	  ) ENGINE=Memory;

	TRUNCATE _target_addr;	

	/*--========================================================================================================
	-- ��ǥ1 ó��
	-- '�������_���ݿ�����ȯ' ���̺�� '�������_����Ұ�'���� ���� ��ǰ(ǰ���ڵ�)�̰�, ���� ���θ��� ��
	--========================================================================================================*/
	INSERT IGNORE INTO _target_addr(�ּ�)
		SELECT DISTINCT A.�ּ�
		  FROM �������_���ݿ�����ȯ AS A
		  JOIN �������_����Ұ�       AS B
			ON A.ǰ���ڵ� = B.ǰ���ڵ�
		AND A.���θ�   = B.���θ�
	  JOIN (
		SELECT �ּ�, COUNT(*) AS cnt
		FROM �������_���ݿ�����ȯ
		WHERE �ּ� IS NOT NULL
		GROUP BY �ּ�
	  ) AS C
		ON C.�ּ� = A.�ּ�
	  LEFT JOIN �������_���ݿ�����ȯ AS A3
		ON A3.�ּ� = A.�ּ�
	   AND A3.ǰ���ڵ� IN (B.�ʼ��ڵ�1, B.�ʼ��ڵ�2, B.�ʼ��ڵ�3)
	  WHERE A.�ּ� IS NOT NULL
		AND (
			  C.cnt = 1
			  OR (C.cnt > 1 AND A3.ǰ���ڵ� IS NULL)
			);
	
	  -- ��� �ּҿ� �ش��ϴ� A-�ุ �ϰ� ������Ʈ
	  UPDATE �������_���ݿ�����ȯ AS S
	  JOIN _target_addr AS T ON T.�ּ� = S.�ּ�
	  SET S.��ǥ1 = '***';
      INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �ּҿ� �ش��ϴ� A-�ุ �ϰ� ������Ʈ', ROW_COUNT());

	  DROP TEMPORARY TABLE IF EXISTS _target_addr;
	  

    COMMIT;

    -- ���� ����� SELECT ������ ��ȯ
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- ���ν��� ���� �� �ӽ� ���̺� ����
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
