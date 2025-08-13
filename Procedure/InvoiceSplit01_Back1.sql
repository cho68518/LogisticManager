DELIMITER $$

DROP PROCEDURE IF EXISTS InvoiceSplit01$$

CREATE PROCEDURE InvoiceSplit01()
BEGIN
    -- =================================================================
	-- ��õ Ư����� ó�� ��ƾ
    -- =================================================================
    DECLARE v_msg1_default, v_msg2_default, v_msg3_default, v_msg4_default, v_msg5_default, v_msg6_default VARCHAR(255);
    DECLARE v_msg1_busan, v_msg2_busan, v_msg3_busan, v_msg4_busan, v_msg5_busan, v_msg6_busan VARCHAR(255);
    DECLARE v_msg1_cheonggwa, v_msg2_cheonggwa, v_msg3_cheonggwa, v_msg4_cheonggwa, v_msg5_cheonggwa, v_msg6_cheonggwa VARCHAR(255);
    
    -- ���� �߻� �� �ѹ��ϰ� �ӽ� ���̺��� ����
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- ���� ���� ����
        DECLARE v_sqlstate CHAR(5);
        DECLARE v_error_no INT;
        DECLARE v_error_message TEXT;

        -- �߻��� ������ �� ����.
        GET DIAGNOSTICS CONDITION 1
            v_sqlstate = RETURNED_SQLSTATE,
            v_error_no = MYSQL_ERRNO,
            v_error_message = MESSAGE_TEXT;

        -- Ʈ������� �ѹ�.
        ROLLBACK;

        -- �� ���� �޽���.
        SELECT CONCAT(
            'Error (SQLSTATE: ', v_sqlstate, ', Error No: ', v_error_no, '): ', v_error_message,
            ' | The transaction was rolled back.'
        ) AS ErrorMessage;
    END;

    -- =================================================================
    
    -- �������_�޼��� --> �������_�޼����λ�, �������_�޼���û��
    TRUNCATE TABLE �������_�޼����λ�;
    TRUNCATE TABLE �������_�޼���û��;
    
    INSERT INTO �������_�޼����λ� (���θ�, msg1, msg2, msg3, msg4, msg5, msg6)
    SELECT ���θ�, msg1, msg2, msg3, msg4, msg5, msg6
    FROM �������_�޼���;

    INSERT INTO �������_�޼���û�� (���θ�, msg1, msg2, msg3, msg4, msg5, msg6)
    SELECT ���θ�, msg1, msg2, msg3, msg4, msg5, msg6
    FROM �������_�޼���;

    -- �� UPDATE ���� '�������_Ư�����_��õ�и����' ���̺� '�ù����' �÷��� �����Ѵٰ� �����մϴ�.
    UPDATE �������_Ư�����_��õ�и���� a
    LEFT JOIN ǰ���� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
    SET a.�ù���� = COALESCE(b.�ù����, 10);
            
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_default, v_msg2_default, v_msg3_default, v_msg4_default, v_msg5_default, v_msg6_default
      FROM �������_�޼���
      WHERE ���θ� = '������' LIMIT 1;
      
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_busan, v_msg2_busan, v_msg3_busan, v_msg4_busan, v_msg5_busan, v_msg6_busan
      FROM �������_�޼����λ�
      WHERE ���θ� = '������' LIMIT 1;
      
    SELECT msg1, msg2, msg3, msg4, msg5, msg6
      INTO v_msg1_cheonggwa, v_msg2_cheonggwa, v_msg3_cheonggwa, v_msg4_cheonggwa, v_msg5_cheonggwa, v_msg6_cheonggwa
      FROM �������_�޼���û��
      WHERE ���θ� = '������' LIMIT 1;

    -- ��� ǰ�� ������ �ӽ� ���̺� �� ���� ����
    CREATE TEMPORARY TABLE temp_gamcheon_codes (
        ǰ���ڵ� VARCHAR(255),
        ����     VARCHAR(255),
        �ù���� VARCHAR(255)
    );

    INSERT INTO temp_gamcheon_codes (ǰ���ڵ�, ����, �ù����)
    SELECT a.ǰ���ڵ�, a.����, COALESCE(b.�ù����, 10)
    FROM �������_Ư�����_��õ�и���� a
    LEFT JOIN ǰ���� b ON a.ǰ���ڵ� = b.ǰ���ڵ�;

    -- =================================================================
    START TRANSACTION;

    -- '�������_���ݿ�����ȯ' ���̺��� ������� �ϵ��� ����
    -- 2-1. ���屸�� ������Ʈ ('������'/'����')
    UPDATE �������_���ݿ�����ȯ_Dev main_table
    JOIN (
        SELECT id, COUNT(*) OVER (PARTITION BY �ּ�) as address_count
        FROM �������_���ݿ�����ȯ_Dev
        WHERE ǰ���ڵ� IN (SELECT ǰ���ڵ� FROM temp_gamcheon_codes)
    ) AS sub_query ON main_table.id = sub_query.id
    SET main_table.���屸�� = IF(sub_query.address_count > 1, '������', '����');

    -- 2-2. '����' �� ���Ǻ� '������' ������ �и� �� �̵�
    TRUNCATE TABLE �������_���ݿ�����ȯ_Ư����°�õ;

    -- '���屸������' �÷��� ����ϴ� ���, ������ ���� ����Ͽ� �� ���� ó��
    /*INSERT INTO �������_���ݿ�����ȯ_Ư����°�õ
		SELECT s.*
		FROM �������_���ݿ�����ȯ_Dev s
		JOIN temp_gamcheon_codes t ON s.ǰ���ڵ� = t.ǰ���ڵ�
		WHERE s.���屸�� = '����' OR (s.���屸�� = '������' AND s.���� >= t.����);
    */
	
	INSERT INTO �������_���ݿ�����ȯ_Ư����°�õ
		SELECT s.*
		FROM �������_���ݿ�����ȯ_Dev s
		JOIN (
			SELECT DISTINCT ǰ���ڵ�, ���� FROM temp_gamcheon_codes
		) AS t ON s.ǰ���ڵ� = t.ǰ���ڵ�
		WHERE s.���屸�� = '����' OR (s.���屸�� = '������' AND s.���� >= t.����);
  
    DELETE s
    FROM �������_���ݿ�����ȯ_Dev s
    JOIN temp_gamcheon_codes t ON s.ǰ���ڵ� = t.ǰ���ڵ�
    WHERE s.���屸�� = '����' OR (s.���屸�� = '������' AND s.���� >= t.����);

    -- 2-3. �и��� ������ ���� (`GC_` ���λ� ����)
    -- ��� ������� 'GC_'�� �����ϵ��� ����
    UPDATE �������_���ݿ�����ȯ_Ư����°�õ
    SET ����� = CONCAT('GC_', TRIM(LEADING 'GC_' FROM �����));

    -- 2-4. ������ ������ ���� ���̺�� �ٽ� ����
    INSERT INTO �������_���ݿ�����ȯ_Dev SELECT * FROM �������_���ݿ�����ȯ_Ư����°�õ;

    -- 2-5. ��ü ������ ��ó�� �۾� (���� UPDATE ���� �������� ����)
    -- �� ǰ���ڵ� �� �� ���� �����θ� ó��
    UPDATE �������_���ݿ�����ȯ_Dev
    SET
        ǰ���ڵ� = CASE WHEN ǰ���ڵ� = '' OR ǰ���ڵ� IS NULL THEN '0000' ELSE ǰ���ڵ� END,
        �����   = CASE WHEN ǰ���ڵ� = '' OR ǰ���ڵ� IS NULL THEN '--' ELSE ����� END,
        �����θ� = CASE WHEN CHAR_LENGTH(�����θ�) = 1 THEN CONCAT(�����θ�, �����θ�) ELSE �����θ� END;

    -- ���屸���� �� ���屸��(�����) ������Ʈ (�ϳ��� UPDATE�� ����)
    UPDATE �������_���ݿ�����ȯ_Dev
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

    -- �޽��� �� �ù���� ������Ʈ (�ϳ��� UPDATE�� ����)
    UPDATE �������_���ݿ�����ȯ_Dev s
    LEFT JOIN �������_�޼��� m       ON s.���θ�   = m.���θ�
    LEFT JOIN �������_�޼����λ� mb  ON s.���θ�   = mb.���θ�
    LEFT JOIN �������_�޼���û�� mc  ON s.���θ�   = mc.���θ�
    LEFT JOIN ǰ���� p              ON s.ǰ���ڵ� = p.ǰ���ڵ�
    SET
        s.msg1 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg1, v_msg1_busan) WHEN 'û��' THEN IFNULL(mc.msg1, v_msg1_cheonggwa) ELSE IFNULL(m.msg1, v_msg1_default) END,
        s.msg2 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg2, v_msg2_busan) WHEN 'û��' THEN IFNULL(mc.msg2, v_msg2_cheonggwa) ELSE IFNULL(m.msg2, v_msg2_default) END,
        s.msg3 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg3, v_msg3_busan) WHEN 'û��' THEN IFNULL(mc.msg3, v_msg3_cheonggwa) ELSE IFNULL(m.msg3, v_msg3_default) END,
        s.msg4 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg4, v_msg4_busan) WHEN 'û��' THEN IFNULL(mc.msg4, v_msg4_cheonggwa) ELSE IFNULL(m.msg4, v_msg4_default) END,
        s.msg5 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg5, v_msg5_busan) WHEN 'û��' THEN IFNULL(mc.msg5, v_msg5_cheonggwa) ELSE IFNULL(m.msg5, v_msg5_default) END,
        s.msg6 = CASE s.���屸�� WHEN '�λ�' THEN IFNULL(mb.msg6, v_msg6_busan) WHEN 'û��' THEN IFNULL(mc.msg6, v_msg6_cheonggwa) ELSE IFNULL(m.msg6, v_msg6_default) END,
        s.�ù���� = IFNULL(p.�ù����, 10);

    -- 2-6. ���� ����Ʈ ���̺� ���� (�÷� ���� �� ���� ����)
    TRUNCATE TABLE �������_�ֹ�����;
    INSERT INTO �������_�ֹ����� (�ֹ���ȣ_���θ�, 
	                               �ֹ���ȣ_����, 
								   ������, 
								   ���θ�, 
								   �޴»��, 
								   ��ǰ��, 
	                               �������ڵ�, 
								   �ڵ��ȣ, 
								   ����, 
								   ��ۼ�, 
								   �����ݾ�, 
								   �ֹ��ݾ�, 
								   �����_����, 
								   ��������, 
								   �ּ�, 
								   ��ȭ��ȣ, 
								   ��ȭ��ȣ2, 
								   �ֹ�����, 
								   �����)
    SELECT
        `�ֹ���ȣ(���θ�)` AS �ֹ���ȣ_���θ�, 
		�ֹ���ȣ, 
		�����ð� AS ������, 
		���θ�, 
		�����θ� AS �޴»��, 
		�ɼǸ� AS ��ǰ��, 
		ǰ���ڵ� AS �������ڵ�, 
		ǰ���ڵ� AS �ڵ��ȣ, 
		����, 
		��ۼ�, 
		�����ݾ�, 
		�ֹ��ݾ�, 
		��������� AS �����_����, 
		��������, 
		�ּ�, 
		���屸�� AS ��ȭ��ȣ, 
		��ȭ��ȣ2, 
		�ֹ�����,
        CASE ���屸�� WHEN '��Ź' THEN '����â��' WHEN '�λ�' THEN '�λ�â��' WHEN '����' THEN '������' WHEN 'û��' THEN '�λ�û��' WHEN '�õ�' THEN '����â��' WHEN '��õ' THEN '��õ�� ��������' ELSE '����â��' END AS �����
    FROM �������_���ݿ�����ȯ_Dev;

    COMMIT;

    -- =================================================================
    -- 3. ������
    -- =================================================================
    DROP TEMPORARY TABLE IF EXISTS temp_gamcheon_codes;
    SELECT '�۾��Ϸ�.' AS ResultMessage;

END$$

DELIMITER ;
