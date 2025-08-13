DELIMITER $$

DROP PROCEDURE IF EXISTS TalkDealUnavailable$$

CREATE PROCEDURE TalkDealUnavailable()
BEGIN
    -- =================================================================
	-- ����Ұ� ó�� ��ƾ
	/*���� Index�߰� �Ұ�
	CREATE INDEX ix_����_�ּ�    ON `�������_���ݿ�����ȯ` (`�ּ�`);
	CREATE INDEX ix_����_ǰ���ڵ� ON `�������_���ݿ�����ȯ` (`ǰ���ڵ�`);
	CREATE INDEX ix_����_���θ�   ON `�������_���ݿ�����ȯ` (`���θ�`);

	-- ����Ұ�
	CREATE INDEX ix_���_ǰ��_���� ON `�������_����Ұ�` (`ǰ���ڵ�`,`���θ�`);
	CREATE INDEX ix_���_�ʼ�1     ON `�������_����Ұ�` (`�ʼ��ڵ�1`);
	CREATE INDEX ix_���_�ʼ�2     ON `�������_����Ұ�` (`�ʼ��ڵ�2`);
	CREATE INDEX ix_���_�ʼ�3     ON `�������_����Ұ�` (`�ʼ��ڵ�3`);	*/
    -- =================================================================
    START TRANSACTION;
	
	-- Target �ּҸ� ���� �ӽ����̺�
	CREATE TEMPORARY TABLE IF NOT EXISTS _target_addr (
		�ּ� VARCHAR(255)
	  ) ENGINE=Memory;

	TRUNCATE _target_addr;	
	
	-- ��ǥ1 ó��
	-- '�������_���ݿ�����ȯ' ���̺�� '�������_����Ұ�'���� ���� ��ǰ(ǰ���ڵ�)�̰�, ���� ���θ��� ��
	
	INSERT IGNORE INTO _target_addr(�ּ�)
		SELECT DISTINCT A.�ּ�
		  FROM �������_���ݿ�����ȯ_Dev AS A
		  JOIN �������_����Ұ�       AS B
			ON A.ǰ���ڵ� = B.ǰ���ڵ�
		AND A.���θ�   = B.���θ�
	  JOIN (
		SELECT �ּ�, COUNT(*) AS cnt
		FROM �������_���ݿ�����ȯ_Dev
		WHERE �ּ� IS NOT NULL
		GROUP BY �ּ�
	  ) AS C
		ON C.�ּ� = A.�ּ�
	  LEFT JOIN �������_���ݿ�����ȯ_Dev AS A3
		ON A3.�ּ� = A.�ּ�
	   AND A3.ǰ���ڵ� IN (B.�ʼ��ڵ�1, B.�ʼ��ڵ�2, B.�ʼ��ڵ�3)
	  WHERE A.�ּ� IS NOT NULL
		AND (
			  C.cnt = 1
			  OR (C.cnt > 1 AND A3.ǰ���ڵ� IS NULL)
			);

	  -- ��� �ּҿ� �ش��ϴ� A-�ุ �ϰ� ������Ʈ
	  UPDATE �������_���ݿ�����ȯ_Dev AS S
	  JOIN _target_addr AS T ON T.�ּ� = S.�ּ�
	  SET S.��ǥ1 = '***';

	  DROP TEMPORARY TABLE IF EXISTS _target_addr;
	  

    COMMIT;

    -- =================================================================
    -- 3. ������
    -- =================================================================
    SELECT '�۾��Ϸ�.' AS ResultMessage;

END$$

DELIMITER ;
