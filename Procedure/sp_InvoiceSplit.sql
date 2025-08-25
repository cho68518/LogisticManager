DELIMITER $$

DROP PROCEDURE IF EXISTS sp_InvoiceSplit$$

CREATE PROCEDURE sp_InvoiceSplit()
BEGIN
    /*--============================================================================
    -- �������� �������_Ư�����_��õ�и���� ���̺� Insert�� ó��
    --============================================================================*/
    DECLARE error_info TEXT DEFAULT '';
    DECLARE error_code INT DEFAULT 0;
    DECLARE original_sql_safe_updates INT;
	
    DECLARE v_msg1_default, v_msg2_default, v_msg3_default, v_msg4_default, v_msg5_default, v_msg6_default VARCHAR(255);
    DECLARE v_msg1_busan, v_msg2_busan, v_msg3_busan, v_msg4_busan, v_msg5_busan, v_msg6_busan VARCHAR(255);
    DECLARE v_msg1_cheonggwa, v_msg2_cheonggwa, v_msg3_cheonggwa, v_msg4_cheonggwa, v_msg5_cheonggwa, v_msg6_cheonggwa VARCHAR(255);

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
            error_code = MYSQL_ERRNO,
            error_info = MESSAGE_TEXT;

        ROLLBACK;
        DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
        DROP TEMPORARY TABLE IF EXISTS temp_gamcheon_codes;
        DROP TEMPORARY TABLE IF EXISTS temp_address_counts;
        SELECT '������ �߻��Ͽ� ��� �۾��� �ѹ�Ǿ����ϴ�.' AS ErrorMessage,
               error_code AS MySQLErrorCode,
               error_info AS MySQLErrorMessage;
    END;

    CREATE TEMPORARY TABLE sp_execution_log (
        StepID INT AUTO_INCREMENT PRIMARY KEY,
        OperationDescription VARCHAR(255),
        AffectedRows INT
    );
	
	CREATE TEMPORARY TABLE temp_address_counts (
        �ּ� VARCHAR(500) PRIMARY KEY,
        address_count INT NOT NULL
    );

    -- ���� ������Ʈ ��� �ӽ� ��Ȱ��ȭ
    SET @original_sql_safe_updates = @@SESSION.sql_safe_updates;
    SET SESSION sql_safe_updates = 0;
	
    START TRANSACTION;
    TRUNCATE TABLE sp_execution_log;

    /*-- =================================================================
    -- 1. �޽��� ���� �� ��õ ��� ������ �غ�
    -- =================================================================*/
    -- �������_�޼��� --> �������_�޼����λ�, �������_�޼���û��
    TRUNCATE TABLE �������_�޼����λ�;
	
    INSERT INTO �������_�޼����λ� (���θ�, msg1, msg2, msg3, msg4, msg5, msg6)
		SELECT ���θ�, msg1, msg2, msg3, msg4, msg5, msg6 FROM �������_�޼���;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �������_�޼����λ� ����', ROW_COUNT());

    TRUNCATE TABLE �������_�޼���û��;
	
    INSERT INTO �������_�޼���û�� (���θ�, msg1, msg2, msg3, msg4, msg5, msg6)
		SELECT ���θ�, msg1, msg2, msg3, msg4, msg5, msg6 FROM �������_�޼���;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] �������_�޼���û�� ����', ROW_COUNT());

    -- �⺻ �޽��� ���� ����
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_default, v_msg2_default, v_msg3_default, v_msg4_default, v_msg5_default, v_msg6_default
      FROM �������_�޼��� WHERE ���θ� = '������' LIMIT 1;
      
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_busan, v_msg2_busan, v_msg3_busan, v_msg4_busan, v_msg5_busan, v_msg6_busan
      FROM �������_�޼����λ� WHERE ���θ� = '������' LIMIT 1;
      
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_cheonggwa, v_msg2_cheonggwa, v_msg3_cheonggwa, v_msg4_cheonggwa, v_msg5_cheonggwa, v_msg6_cheonggwa
      FROM �������_�޼���û�� WHERE ���θ� = '������' LIMIT 1;
    
    
	UPDATE �������_Ư�����_��õ�и���� a
		LEFT JOIN ǰ���� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
		SET a.�ù���� = COALESCE(b.�ù����, 10);

	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (��õƯ�����ó��) �������_Ư�����_��õ�и���� (�ù����)', ROW_COUNT());
			
    /*-- =================================================================================
    -- 2. ��õ Ư����� ó�� ��ƾ
    -- 2-1. ���屸�� ������Ʈ ('������'/'����')
    -- 2-2. '����' �� ���Ǻ� '������' �����Ϳ� ���� ����� ���� (`GC_` ���λ� ����)
    -- =================================================================================*/
	-- ���屸�� ������Ʈ
	INSERT INTO temp_address_counts (�ּ�, address_count)
		SELECT �ּ�, COUNT(*) AS cnt
		FROM �������_���ݿ�����ȯ
		GROUP BY �ּ�;

    -- '������' ������Ʈ (�ּ� ������ 1���� ū ���)
    UPDATE �������_���ݿ�����ȯ s
		INNER JOIN temp_address_counts tc ON s.�ּ� = tc.�ּ�
		SET
			s.���屸�� = '������'
		WHERE
			tc.address_count > 1
			AND s.ǰ���ڵ� IN (SELECT ǰ���ڵ� FROM �������_Ư�����_��õ�и����);

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (��õƯ�����ó��) �������_���ݿ�����ȯ, ���屸��(������)', ROW_COUNT());
	
    -- '����' ������Ʈ (�ּ� ������ 1�� ���)
    UPDATE �������_���ݿ�����ȯ s
		INNER JOIN temp_address_counts tc ON s.�ּ� = tc.�ּ�
		SET
			s.���屸�� = '����'
		WHERE
			tc.address_count = 1
			AND s.ǰ���ڵ� IN (SELECT ǰ���ڵ� FROM �������_Ư�����_��õ�и����);

	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (��õƯ�����ó��) �������_���ݿ�����ȯ, ���屸��(����)', ROW_COUNT());
				
			
	TRUNCATE �������_���ݿ�����ȯ_Ư����°�õ;
    /*--TRUNCATE �������_���ݿ�����ȯ_Ư����°�õ1;
    --TRUNCATE �������_���ݿ�����ȯ_Ư����°�õ2;
    --TRUNCATE �������_���ݿ�����ȯ_Ư����°�õ�ڷ�;*/
	
	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(��õ Ư����� ó��) �������_���ݿ�����ȯ_Ư����°�õ �ʱ�ȭ', 0);
	
	/*--INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(��õ Ư����� ó��) �������_���ݿ�����ȯ_Ư����°�õ1 �ʱ�ȭ', 0);			
	--INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(��õ Ư����� ó��) �������_���ݿ�����ȯ_Ư����°�õ2 �ʱ�ȭ', 0);			
	--INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('(��õ Ư����� ó��) �������_���ݿ�����ȯ_Ư����°�õ�ڷ� �ʱ�ȭ', 0);*/

	/*--=============================================================================================
	-- (�������_���ݿ�����ȯ_Ư����°�õ)	
    --     '�������_���ݿ�����ȯ'�� �ִ� �ֹ��� �߿���, Ư�� ����(?)�� �´� 
	--     '����' �ֹ��� '������' �ֹ��� ��󳻼� '�������_Ư�����_��õ�и����' (��õ��)�� Move
	--=============================================================================================*/
	-- 1. '���屸������' �� ���� ������Ʈ (���� �ڵ� 3��)
    -- �̵��� '������' ����� �����ϴ� ���� �۾��̹Ƿ� ���� ����.
    UPDATE �������_���ݿ�����ȯ A
		JOIN �������_Ư�����_��õ�и���� B ON A.ǰ���ڵ� = B.ǰ���ڵ�
		SET A.���屸������ = IF(A.���� >= B.����, '1', NULL);

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (��õƯ�����ó��) �������_���ݿ�����ȯ, ���屸������', ROW_COUNT());
	
    -- 2. �̵��� ��� ���� PK�� ������ �ӽ� ���̺� ����
    CREATE TEMPORARY TABLE temp_rows_to_move (
        Source_PK INT PRIMARY KEY
    );

    -- �̵��� ��� ��('����', ���Ǻ� '������')�� PK�� �� ���� �ĺ��Ͽ� �ӽ� ���̺� ����
    INSERT INTO temp_rows_to_move (Source_PK)
		SELECT s.id
		FROM �������_���ݿ�����ȯ s
		WHERE
			-- ���� 1: �̵��� '����' ����
			(
				s.���屸�� = '����' AND
				s.ǰ���ڵ� IN (SELECT ǰ���ڵ� FROM �������_Ư�����_��õ�и����)
			)
			OR
			-- ���� 2: �̵��� '������' ����
			(
				s.���屸�� = '������' AND s.���屸������ = '1'
			);

    INSERT INTO �������_���ݿ�����ȯ_Ư����°�õ
		SELECT s.*
		  FROM �������_���ݿ�����ȯ s
	 	  JOIN temp_rows_to_move m ON s.id = m.Source_PK;

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (��õƯ�����ó��) �������_���ݿ�����ȯ_Ư����°�õ', ROW_COUNT());
	
    DELETE s
      FROM �������_���ݿ�����ȯ s
      JOIN temp_rows_to_move m ON s.id = m.Source_PK;

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] (��õƯ�����ó��) �������_���ݿ�����ȯ', ROW_COUNT());

    /*--====================================================================================================    		
	-- (�������_���ݿ�����ȯ_Ư����°�õ) (�߰�: 20250821)
	--      ����̾��̽��� �߰��� �����ϰ�, ��ǰ�� ��õ���� ����� ���, 
    --      ����̾��̽��� ���� �����ȣ ���Է��� �Ǵ� ��츦 ���� ����
	--      ����� = '����̾��̽� �߰�' �ֹ��� ��󳻼� '�������_Ư�����_��õ�и����' (��õ��)�� Move	
	--      �� �������_���ݿ�����ȯ_Ư����°�õ ���̺� ����, �������� �������� �߰�
    --====================================================================================================*/
	TRUNCATE TABLE temp_rows_to_move;	
	
    INSERT INTO temp_rows_to_move (Source_PK)
		SELECT s.id
		FROM �������_���ݿ�����ȯ AS s
		WHERE s.����� = '����̾��̽� �߰�'
		  -- ���� 1: �ּҰ� Ư����°�õ ���̺� �̹� �����ؾ� �� (�����ּ�)
		  AND EXISTS (
			  SELECT 1
			  FROM �������_���ݿ�����ȯ_Ư����°�õ AS t
			  WHERE TRIM(t.�ּ�) = TRIM(s.�ּ�)
		  )
		  -- ���� 2: ������ ���� Ư����°�õ ���̺� ����� �� (�ߺ� ���� ����)
		  AND NOT EXISTS (
			  SELECT 1
			  FROM �������_���ݿ�����ȯ_Ư����°�õ AS t2
			  WHERE t2.�ּ� = s.�ּ� AND t2.����� = s.�����
		  );

    INSERT INTO �������_���ݿ�����ȯ_Ư����°�õ
		SELECT s.*
		FROM �������_���ݿ�����ȯ AS s
		JOIN temp_rows_to_move m ON s.id = m.Source_PK;

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (��õƯ�����ó��) �������_���ݿ�����ȯ_Ư����°�õ (����̾��̽�)', ROW_COUNT());
	
    DELETE s
      FROM �������_���ݿ�����ȯ AS s
      JOIN temp_rows_to_move m ON s.id = m.Source_PK;	

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[DELETE] (��õƯ�����ó��) �������_���ݿ�����ȯ (����̾��̽�)', ROW_COUNT());
	
    /*--================================================================================    		
	-- (�������_���ݿ�����ȯ_Ư����°�õ) GC_ ���λ� ������	
	--      'GC_'�� �������� �ʴ� ������� 'GC_'�� �տ� �߰�
    --================================================================================*/
    UPDATE �������_���ݿ�����ȯ_Ư����°�õ
		SET ����� = CONCAT('GC_', �����)
		WHERE ����� NOT LIKE 'GC_%';

    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (��õƯ�����ó��) �������_���ݿ�����ȯ_Ư����°�õ', ROW_COUNT());				
				
    /*--================================================================================    		
	-- ���� ������ȯ ���̺� ������ ����
    --================================================================================*/
    -- 'Ư����°�õ' ���̺��� �����͸� '���ݿ�����ȯ' ���̺�� Insert/Update
    INSERT INTO �������_���ݿ�����ȯ
		SELECT * FROM �������_���ݿ�����ȯ_Ư����°�õ
		ON DUPLICATE KEY UPDATE
			ID       = VALUES(ID),
			ǰ���ڵ� = VALUES(ǰ���ڵ�),
			����     = VALUES(����),
			�ù��� = VALUES(�ù���),
			�ڽ�ũ�� = VALUES(�ڽ�ũ��),
			���屸�� = VALUES(���屸��),
			�����   = VALUES(�����),
			�ּ�     = VALUES(�ּ�);

	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (��õƯ�����ó��) �������_���ݿ�����ȯ_Ư����°�õ -> �������_���ݿ�����ȯ', ROW_COUNT());				
				
    /*--================================================================================    		
	-- �������_���ݿ�����ȯ_Ư����°�õ�ڷ� (?)
    --================================================================================			
	--INSERT INTO �������_���ݿ�����ȯ_Ư����°�õ�ڷ� (ǰ���ڵ�, ����)
    --    SELECT ǰ���ڵ�, ����
    --      FROM �������_���ݿ�����ȯ_Ư����°�õ1;*/
			
			
	/*--=================================================================
    -- (�������_���ݿ�����ȯ) �� ǰ���ڵ�, �� ���� �����θ� ó��
    -- =================================================================*/
    UPDATE �������_���ݿ�����ȯ
    SET
        ǰ���ڵ� = CASE WHEN ǰ���ڵ� = '' OR ǰ���ڵ� IS NULL THEN '0000' ELSE ǰ���ڵ� END,
        �����   = CASE WHEN ǰ���ڵ� = '' OR ǰ���ڵ� IS NULL THEN '--' ELSE ����� END,
        �����θ� = CASE WHEN CHAR_LENGTH(�����θ�) = 1 THEN CONCAT(�����θ�, �����θ�) ELSE �����θ� END;
    
	INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (��õƯ�����ó��) �������_���ݿ�����ȯ (ǰ���ڵ�,�����,�����θ�)', ROW_COUNT());		
			
	/*--=================================================================
	-- (�������_���ݿ�����ȯ) ���屸���� �� �����(���屸��) ����
    -- =================================================================*/	
    UPDATE �������_���ݿ�����ȯ
    SET
        ���屸���� = LEFT(TRIM(�����), 3),
        ���屸�� = CASE
                       WHEN LEFT(TRIM(�����), 3) IN ('GR_', 'AR_') THEN '��Ź'
                       WHEN LEFT(TRIM(�����), 3) = 'BS_' THEN '�λ�'
                       WHEN LEFT(TRIM(�����), 3) = 'GS_' THEN '����'
                       WHEN LEFT(TRIM(�����), 3) IN ('YC_', 'HY_') THEN 'û��'
                       WHEN LEFT(TRIM(�����), 3) = 'GC_' THEN '��õ'
                       ELSE '�õ�'
                   END;
				   
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (��õƯ�����ó��) �������_���ݿ�����ȯ(���屸����,���屸��(�����))', ROW_COUNT());

	/*--=============================================================================================
    -- (�������_���ݿ�����ȯ) �޽��� �� �ù����(��ī��Ʈ �ڽ�����) ������Ʈ (5�� ���̺� JOIN)
    -- ============================================================================================*/	
    UPDATE �������_���ݿ�����ȯ s
		LEFT JOIN �������_�޼��� m       ON s.���θ� = m.���θ�
		LEFT JOIN �������_�޼����λ� mb  ON s.���θ� = mb.���θ�
		LEFT JOIN �������_�޼���û�� mc  ON s.���θ� = mc.���θ�
		LEFT JOIN ǰ���� p              ON s.ǰ���ڵ� = p.ǰ���ڵ�
    SET
        s.msg1 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg1, v_msg1_busan) WHEN 'û��' THEN IFNULL(mc.msg1, v_msg1_cheonggwa) ELSE IFNULL(m.msg1, v_msg1_default) END,
        s.msg2 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg2, v_msg2_busan) WHEN 'û��' THEN IFNULL(mc.msg2, v_msg2_cheonggwa) ELSE IFNULL(m.msg2, v_msg2_default) END,
        s.msg3 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg3, v_msg3_busan) WHEN 'û��' THEN IFNULL(mc.msg3, v_msg3_cheonggwa) ELSE IFNULL(m.msg3, v_msg3_default) END,
        s.msg4 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg4, v_msg4_busan) WHEN 'û��' THEN IFNULL(mc.msg4, v_msg4_cheonggwa) ELSE IFNULL(m.msg4, v_msg4_default) END,
        s.msg5 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg5, v_msg5_busan) WHEN 'û��' THEN IFNULL(mc.msg5, v_msg5_cheonggwa) ELSE IFNULL(m.msg5, v_msg5_default) END,
        s.msg6 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg6, v_msg6_busan) WHEN 'û��' THEN IFNULL(mc.msg6, v_msg6_cheonggwa) ELSE IFNULL(m.msg6, v_msg6_default) END,
        s.�ù���� = IFNULL(p.�ù����, 10);
		
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[UPDATE] (��õƯ�����ó��) �������_���ݿ�����ȯ(�޽���, �ù����)', ROW_COUNT());

    -- =================================================================
    -- (�������_�ֹ�����) ���� ����Ʈ ����
    -- =================================================================
    TRUNCATE TABLE �������_�ֹ�����;
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[TRUNCATE] (��õƯ�����ó��) �������_�ֹ����� �ʱ�ȭ', ROW_COUNT());

    INSERT INTO �������_�ֹ����� (
        �ֹ���ȣ_���θ�, �ֹ���ȣ_����, ������, ���θ�, �޴»��, ��ǰ��,
        �������ڵ�, �ڵ��ȣ, ����, ��ۼ�, �����ݾ�, �ֹ��ݾ�,
        �����_����, ��������, �ּ�, ��ȭ��ȣ, ��ȭ��ȣ2, �ֹ�����
    )
    SELECT
        `�ֹ���ȣ(���θ�)` AS �ֹ���ȣ_���θ�,
        �ֹ���ȣ AS �ֹ���ȣ_����,
        �����ð� AS ������,
        ���θ�,
        �����θ� AS �޴»��,
        NULL     AS ��ǰ��,     -- ���� �ý��ۿ��� ��ǰ���� Null�� ��. 
        ǰ���ڵ� AS �������ڵ�,
        ǰ���ڵ� AS �ڵ��ȣ,
        ����,
        ��ۼ�,
        �����ݾ�,
        �ֹ��ݾ�,
        ��������� AS �����_����,
        ��������,
        �ּ�,
        CASE ���屸�� WHEN '��Ź' THEN '����â��' WHEN '�λ�' THEN '�λ�â��' WHEN '����' THEN '������' WHEN 'û��' THEN '�λ�û��' WHEN '�õ�' THEN '����â��' WHEN '��õ' THEN '��õ�� ��������' ELSE '����â��' END AS ��ȭ��ȣ,
        ��ȭ��ȣ2,
        �ֹ�����
    FROM �������_���ݿ�����ȯ;
	
    INSERT INTO sp_execution_log (OperationDescription, AffectedRows) VALUES ('[INSERT] (��õƯ�����ó��) �������_�ֹ�����(���� ����Ʈ ���̺� ����)', ROW_COUNT());

    COMMIT;

    -- ���� ���� ��� �α� ��ȯ
    SELECT StepID, OperationDescription, AffectedRows FROM sp_execution_log ORDER BY StepID;

    -- ���ν��� ���� �� �ӽ� ���̺� ����
    DROP TEMPORARY TABLE IF EXISTS sp_execution_log;
    DROP TEMPORARY TABLE IF EXISTS temp_gamcheon_codes;
    DROP TEMPORARY TABLE IF EXISTS temp_address_counts;
    DROP TEMPORARY TABLE IF EXISTS temp_rows_to_move;
	
	SET SESSION sql_safe_updates = @original_sql_safe_updates;
	
END$$

DELIMITER ;
