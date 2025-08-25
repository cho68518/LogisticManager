DELIMITER $$

DROP PROCEDURE IF EXISTS sp_BusanCheonggwaDocProcess$$

CREATE PROCEDURE sp_BusanCheonggwaDocProcess()
BEGIN
    /*--================================================================================
	-- �λ�û���ڷ� ó��
    --================================================================================*/
    DECLARE done INT DEFAULT FALSE;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
		DECLARE error_info TEXT DEFAULT '';
		DECLARE error_code INT DEFAULT 0;
		
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

    TRUNCATE TABLE sp_execution_log;

    /*--================================================================================
	-- (�λ�û���ڷ�) �������_�λ�û���ڷ�
    --================================================================================*/
	TRUNCATE TABLE �������_�λ�û���ڷ�;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(�λ�û���ڷ�) �������_�λ�û���ڷ� �ʱ�ȭ', 0);
	
	INSERT INTO �������_�λ�û���ڷ� (ǰ���ڵ�, �����, ����)
		SELECT 
			ǰ���ڵ�, 
			�����, 
			����
		FROM 
			�������_���ݿ�����ȯ
		WHERE 
			���屸���� LIKE 'YC_%'; -- LIKE ��� LEFT(���屸����, 3) = 'YC_'
			
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �������_�λ�û���ڷ�', ROW_COUNT());
	
    /*--================================================================================
	-- (�λ�û���ڷ�) �������_�λ�û���ڷ�_��ǰ
	--                ��ǰ(�ּҰ� ����ũ��) ������ ó��
    --================================================================================*/
	TRUNCATE TABLE �������_�λ�û���ڷ�_��ǰ;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(�λ�û���ڷ�) �������_�λ�û���ڷ�_��ǰ �ʱ�ȭ', 0);
	
	-- ���� ���̺��� '�ּ�'�� ����ũ��(��ü ���̺��� �� �� ���� �����ϴ�)
    INSERT INTO �������_�λ�û���ڷ�_��ǰ (ǰ���ڵ�, �����, ����)
		SELECT
			ǰ���ڵ�,
			�����,
			����
		FROM
			�������_�λ�û����ȯ
		WHERE
			�ּ� IN (
				SELECT �ּ�
				FROM �������_�λ�û����ȯ
				WHERE �ּ� IS NOT NULL
				GROUP BY �ּ�
				HAVING COUNT(*) = 1
			);	
			
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �������_�λ�û���ڷ�_��ǰ', ROW_COUNT());
	
    -- =================================================================
    -- (�λ�û���ڷ�) ����(�ּҰ� �ߺ��Ǵ�) ������ ó��
    -- =================================================================
    TRUNCATE TABLE �������_�λ�û���ڷ�_����;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(�λ�û���ڷ�) �������_�λ�û���ڷ�_���� �ʱ�ȭ', 0);
	
    -- ���� ���̺��� '�ּ�'�� �ߺ��Ǵ� �����͸� ��ȸ�Ͽ� �� ���� ����
    INSERT INTO �������_�λ�û���ڷ�_���� (ǰ���ڵ�, �����, ����)
		SELECT
			ǰ���ڵ�,
			�����,
			����
		FROM
			�������_�λ�û����ȯ
		WHERE
			�ּ� IN (
				SELECT �ּ�
				FROM �������_�λ�û����ȯ
				WHERE �ּ� IS NOT NULL
				GROUP BY �ּ�
				HAVING COUNT(*) > 1
			);
			
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �������_�λ�û���ڷ�_����', ROW_COUNT());
		
    -- =================================================================
    -- (�λ�û���ڷ�)  �� ǰ���ڵ庰�� ���� �հ� ���
    -- =================================================================
    UPDATE 
        �������_�λ�û���ڷ� AS main
    JOIN 
        (
            SELECT 
                ǰ���ڵ�, 
                SUM(����) AS c_sum
            FROM 
                �������_�λ�û���ڷ�
            GROUP BY 
                ǰ���ڵ�
        ) AS sub ON main.ǰ���ڵ� = sub.ǰ���ڵ�
    SET 
        main.�Ѽ��� = sub.c_sum;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �������_�λ�û���ڷ� (�Ѽ���)', ROW_COUNT());	
	
    -- =================================================================
    -- (�λ�û���ڷ�)  �� ǰ���ڵ庰�� ���� �հ� ���
    -- =================================================================	
    UPDATE 
        �������_�λ�û���ڷ�_��ǰ AS main
    JOIN 
        (
            SELECT 
                ǰ���ڵ�, 
                SUM(����) AS c_sum
            FROM 
                �������_�λ�û���ڷ�_��ǰ
            GROUP BY 
                ǰ���ڵ�
        ) AS sub ON main.ǰ���ڵ� = sub.ǰ���ڵ�
    SET 
        main.�Ѽ��� = sub.c_sum;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �������_�λ�û���ڷ�_��ǰ (�Ѽ���)', ROW_COUNT());	
	
    -- =================================================================
    -- (�λ�û���ڷ�)  �� ǰ���ڵ庰�� ���� �հ� ���
    -- =================================================================	
    UPDATE 
        �������_�λ�û���ڷ�_���� AS main
    JOIN 
        (
            SELECT 
                ǰ���ڵ�, 
                SUM(����) AS c_sum
            FROM 
                �������_�λ�û���ڷ�_����
            GROUP BY 
                ǰ���ڵ�
        ) AS sub ON main.ǰ���ڵ� = sub.ǰ���ڵ�
    SET 
        main.�Ѽ��� = sub.c_sum;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] �������_�λ�û���ڷ�_���� (�Ѽ���)', ROW_COUNT());	
	
    -- =================================================================
    -- (�λ�û���ڷ�) �������_�λ�û���ڷ�_����
    -- =================================================================
    TRUNCATE TABLE �������_�λ�û���ڷ�_����;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(�λ�û���ڷ�) �������_�λ�û���ڷ�_���� �ʱ�ȭ', 0);

    INSERT INTO �������_�λ�û���ڷ�_���� (
        ǰ���ڵ�, �����, �Ѽ���, 
        ��ǰǰ���ڵ�, ��ǰ�����, ��ǰ�Ѽ���, 
        ����ǰ���ڵ�, ���������, �����Ѽ���
    )
    SELECT
        -- '��ü'
        t1.ǰ���ڵ�,
        t1.�����,
        t1.�Ѽ���,
        -- '��ǰ'
        t2.ǰ���ڵ�,
        t2.�����,
        t2.�Ѽ���,
        -- '����'
        t3.ǰ���ڵ�,
        t3.�����,
        t3.�Ѽ���
    FROM
        -- ������ 'ǰ���ڵ�'
        (
            SELECT ǰ���ڵ� FROM �������_�λ�û���ڷ�      UNION
            SELECT ǰ���ڵ� FROM �������_�λ�û���ڷ�_��ǰ UNION
            SELECT ǰ���ڵ� FROM �������_�λ�û���ڷ�_����
        ) AS m_code
    -- ������ 'ǰ���ڵ�' ��Ͽ� �� ���̺��� ���� ����� ����.
    LEFT JOIN 
        (
            SELECT ǰ���ڵ�, REPLACE(MAX(�����), 'YC_', '') AS �����, SUM(����) AS �Ѽ���
            FROM �������_�λ�û���ڷ� GROUP BY ǰ���ڵ�
        ) AS t1 ON m_code.ǰ���ڵ� = t1.ǰ���ڵ�
    LEFT JOIN 
        (
            SELECT ǰ���ڵ�, REPLACE(MAX(�����), 'YC_', '') AS �����, SUM(����) AS �Ѽ���
            FROM �������_�λ�û���ڷ�_��ǰ GROUP BY ǰ���ڵ�
        ) AS t2 ON m_code.ǰ���ڵ� = t2.ǰ���ڵ�
    LEFT JOIN 
        (
            SELECT ǰ���ڵ�, REPLACE(MAX(�����), 'YC_', '') AS �����, SUM(����) AS �Ѽ���
            FROM �������_�λ�û���ڷ�_���� GROUP BY ǰ���ڵ�
        ) AS t3 ON m_code.ǰ���ڵ� = t3.ǰ���ڵ�
    ORDER BY 
        m_code.ǰ���ڵ� ASC;
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �������_�λ�û���ڷ�_����', ROW_COUNT());			
		
    COMMIT;
    
    -- ���� ����� SELECT ������ ��ȯ
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- ���ν��� ���� �� �ӽ� ���̺� ����
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;

END$$

DELIMITER ;
